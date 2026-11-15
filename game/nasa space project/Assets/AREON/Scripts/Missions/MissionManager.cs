using AREON.ResourceSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// Central coordinator and owner of the mission lifecycle.
    ///
    /// States: NotSelected -> Ready -> Active -> Completed | Failed.
    /// Initialization (inside TryStartMission) runs in ONE fixed order, once, through the adapter:
    ///   1 Validate  2 Weather  3 Resources  4 Daylight  5 Solar permission  6 Objectives  7 Telemetry  8 Active.
    ///
    /// PROTOTYPE RULES (blueprint 2.6, tunable in MissionScenarioData - not NASA facts):
    ///  - Oxygen at 0 fails the mission when failAtZeroOxygen is on.
    ///  - Shield at 0 fails the mission when failAtZeroShield is on.
    ///  - Power at 0 NEVER fails the mission by itself.
    ///  - Optional objectives never block completion and never cause failure.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public const float ZeroEpsilon = 0.0001f;

        [Header("Dependencies")]
        [SerializeField] private MissionGameplayAdapter gameplay;
        [SerializeField] private ObjectiveManager objectives;
        [SerializeField] private TelemetryManager telemetry;

        [Header("Scenarios")]
        [SerializeField] private MissionScenarioData[] availableScenarios;
        [Tooltip("Optional. Leave empty - the temporary test harness starts missions.")]
        [SerializeField] private MissionScenarioData autoStartScenario;

        [Header("Behaviour")]
        [Tooltip("ON: Oxygen/Shield depletion and daylight only run while a mission is Active. " +
                 "Turn OFF only when running the Stage 4-8 regression harnesses.")]
        [SerializeField] private bool gateSimulationByMissionState = true;
        [SerializeField] private bool logRejections = true;

        private readonly List<MissionInitStep> lastInitSteps = new List<MissionInitStep>();
        private float durationOverride;
        private bool configValid;

        public MissionState State { get; private set; } = MissionState.NotSelected;
        public MissionScenarioData SelectedScenario { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float DurationSeconds { get; private set; }
        public MissionFailureReason FailureReason { get; private set; }
        public string LastRejection { get; private set; }
        public IReadOnlyList<MissionInitStep> LastInitSteps => lastInitSteps;
        public float RemainingSeconds => Mathf.Max(0f, DurationSeconds - ElapsedSeconds);
        public bool LogRejections { get => logRejections; set => logRejections = value; }
        public bool GatesSimulation => gateSimulationByMissionState;

        public event Action<MissionState, MissionState> StateChanged;
        public event Action<MissionScenarioData> ScenarioSelected;
        public event Action MissionStarted;
        public event Action MissionCompleted;
        public event Action<MissionFailureReason> MissionFailed;
        public event Action MissionCleared;

        // ------------------------------------------------------------------ Unity

        private void Awake()
        {
            configValid = true;
            if (gameplay == null) { Debug.LogError("[MissionManager] gameplay adapter missing.", this); configValid = false; }
            if (objectives == null) { Debug.LogError("[MissionManager] ObjectiveManager missing.", this); configValid = false; }
            if (telemetry == null) { Debug.LogError("[MissionManager] TelemetryManager missing.", this); configValid = false; }
            if (configValid && !gameplay.Validate(out string err))
            {
                Debug.LogError($"[MissionManager] Adapter invalid: {err}", this);
                configValid = false;
            }
        }

        private void Start()
        {
            if (!configValid) return;
            if (gateSimulationByMissionState) gameplay.SetSimulationActive(false);
            if (autoStartScenario != null && TrySelectScenario(autoStartScenario)) TryStartMission();
        }

        private void OnEnable()
        {
            if (objectives != null) objectives.RequiredObjectivesCompleted += HandleRequiredCompleted;
        }

        private void OnDisable()
        {
            if (objectives != null) objectives.RequiredObjectivesCompleted -= HandleRequiredCompleted;
        }

        private void Update()
        {
            if (State != MissionState.Active) return;

            float dt = Time.deltaTime;
            ElapsedSeconds += dt;
            objectives.Tick(dt, ElapsedSeconds);

            if (EvaluateFailure()) return;
            EvaluateCompletion();
        }

        private void HandleRequiredCompleted()
        {
            // Completion itself is evaluated in Update (same rule path every frame).
        }

        // ------------------------------------------------------------------ public commands

        public static bool IsValidTransition(MissionState from, MissionState to)
        {
            switch (from)
            {
                case MissionState.NotSelected: return to == MissionState.Ready;
                case MissionState.Ready: return to == MissionState.Active || to == MissionState.NotSelected;
                case MissionState.Active:
                    return to == MissionState.Completed || to == MissionState.Failed || to == MissionState.NotSelected;
                case MissionState.Completed:
                case MissionState.Failed: return to == MissionState.NotSelected;
                default: return false;
            }
        }

        /// <summary>Select (or re-select) a scenario. Allowed from NotSelected and Ready only.</summary>
        public bool TrySelectScenario(MissionScenarioData scenario)
        {
            if (!configValid) return Reject("MissionManager is not configured correctly.");
            if (State != MissionState.NotSelected && State != MissionState.Ready)
                return Reject($"Cannot select a scenario while mission is {State}.");
            if (!ValidateScenario(scenario, 0f, out string error))
                return Reject($"Scenario rejected: {error}");
            if (availableScenarios != null && availableScenarios.Length > 0 &&
                Array.IndexOf(availableScenarios, scenario) < 0)
                return Reject("Scenario is not in this MissionManager's available list.");

            SelectedScenario = scenario;
            durationOverride = 0f;
            if (State == MissionState.NotSelected) TransitionTo(MissionState.Ready);
            ScenarioSelected?.Invoke(scenario);
            return true;
        }

        /// <summary>TEST ONLY. Runtime-only duration override; never touches the asset. Ready state only.</summary>
        public bool SetRuntimeDurationOverride(float seconds)
        {
            if (State != MissionState.Ready) return Reject("Duration override is only allowed in the Ready state.");
            if (seconds <= 0f) return Reject("Duration override must be > 0.");
            durationOverride = seconds;
            return true;
        }

        /// <summary>Runs the deterministic initialization sequence and enters Active.</summary>
        public bool TryStartMission()
        {
            if (State != MissionState.Ready) return Reject($"Cannot start a mission while it is {State}.");
            if (!RunInitialization(out string error))
            {
                Debug.LogError($"[MissionManager] Initialization failed: {error}", this);
                objectives.Clear();
                telemetry.Clear();
                if (gateSimulationByMissionState) gameplay.SetSimulationActive(false);
                return false;
            }
            return true;
        }

        public bool TryCompleteMission()
        {
            if (State != MissionState.Active) return Reject($"Cannot complete a mission while it is {State}.");
            if (!IsCompletionConditionMet()) return Reject("Completion conditions are not met.");
            return Finish(MissionState.Completed, MissionFailureReason.None);
        }

        public bool TryFailMission(MissionFailureReason reason)
        {
            if (State != MissionState.Active) return Reject($"Cannot fail a mission while it is {State}.");
            return Finish(MissionState.Failed, reason == MissionFailureReason.None ? MissionFailureReason.ForcedByTest : reason);
        }

        /// <summary>
        /// Returns to NotSelected from any state except NotSelected. Minimal primitive so scenarios can be
        /// re-run; the Stage 10 restart / return-to-selection UI flow will be built on top of it.
        /// </summary>
        public bool TryClearMission()
        {
            if (State == MissionState.NotSelected) return Reject("No mission to clear.");
            objectives.SetTracking(false);
            objectives.Clear();
            telemetry.Clear();
            if (gateSimulationByMissionState) gameplay.SetSimulationActive(false);
            SelectedScenario = null;
            durationOverride = 0f;
            ElapsedSeconds = 0f;
            DurationSeconds = 0f;
            FailureReason = MissionFailureReason.None;
            TransitionTo(MissionState.NotSelected);
            MissionCleared?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ initialization

        private bool RunInitialization(out string error)
        {
            error = null;
            lastInitSteps.Clear();
            MissionScenarioData s = SelectedScenario;

            lastInitSteps.Add(MissionInitStep.ValidateScenario);
            if (!ValidateScenario(s, durationOverride, out error)) return false;
            DurationSeconds = ResolveDuration(s, durationOverride);

            lastInitSteps.Add(MissionInitStep.ApplyWeather);
            if (!gameplay.ApplyWeather(s)) { error = "Weather step failed."; return false; }

            lastInitSteps.Add(MissionInitStep.InitializeResources);
            if (!gameplay.InitializeResources(s)) { error = "Resource step failed."; return false; }

            lastInitSteps.Add(MissionInitStep.ConfigureDaylight);
            if (!gameplay.ConfigureDaylight(s)) { error = "Daylight step failed."; return false; }

            lastInitSteps.Add(MissionInitStep.ConfigureSolarPermission);
            if (!gameplay.ConfigureSolarPermission(s)) { error = "Solar permission step failed."; return false; }

            lastInitSteps.Add(MissionInitStep.InitializeObjectives);
            objectives.Initialize(s, DurationSeconds, gameplay.GetResource);

            lastInitSteps.Add(MissionInitStep.InitializeTelemetry);
            telemetry.Initialize(s);

            lastInitSteps.Add(MissionInitStep.EnterActive);
            ElapsedSeconds = 0f;
            FailureReason = MissionFailureReason.None;
            if (!TransitionTo(MissionState.Active)) { error = "State transition failed."; return false; }
            objectives.SetTracking(true);
            if (gateSimulationByMissionState) gameplay.SetSimulationActive(true);
            MissionStarted?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ rules

        private bool EvaluateFailure()
        {
            MissionScenarioData s = SelectedScenario;

            if (s.failAtZeroOxygen && gameplay.GetResource(ResourceType.Oxygen) <= ZeroEpsilon)
                return Finish(MissionState.Failed, MissionFailureReason.OxygenDepleted);

            if (s.failAtZeroShield && gameplay.GetResource(ResourceType.Shield) <= ZeroEpsilon)
                return Finish(MissionState.Failed, MissionFailureReason.ShieldDepleted);

            // Power at zero is deliberately NOT a failure condition.

            bool timeDone = ElapsedSeconds >= DurationSeconds;
            if (timeDone && s.failIfObjectivesIncompleteAtTimeout &&
                s.completionPolicy != MissionCompletionPolicy.SurvivalOnly &&
                !objectives.AllRequiredCompleted)
                return Finish(MissionState.Failed, MissionFailureReason.TimeExpiredObjectivesIncomplete);

            return false;
        }

        private void EvaluateCompletion()
        {
            if (IsCompletionConditionMet()) Finish(MissionState.Completed, MissionFailureReason.None);
        }

        public bool IsCompletionConditionMet()
        {
            if (SelectedScenario == null) return false;
            bool objectivesDone = objectives.AllRequiredCompleted;
            bool timeDone = ElapsedSeconds >= DurationSeconds;
            switch (SelectedScenario.completionPolicy)
            {
                case MissionCompletionPolicy.ObjectivesOnly: return objectivesDone;
                case MissionCompletionPolicy.SurvivalOnly: return timeDone;
                default: return objectivesDone && timeDone;
            }
        }

        private bool Finish(MissionState end, MissionFailureReason reason)
        {
            if (!TransitionTo(end)) return false;
            FailureReason = reason;
            objectives.SetTracking(false);
            if (gateSimulationByMissionState) gameplay.SetSimulationActive(false);
            if (end == MissionState.Completed) MissionCompleted?.Invoke();
            else MissionFailed?.Invoke(reason);
            return true;
        }

        // ------------------------------------------------------------------ validation helpers

        private static float ResolveDuration(MissionScenarioData s, float overrideSeconds) =>
            overrideSeconds > 0f ? overrideSeconds : s.missionDurationSeconds;

        public static bool ValidateScenario(MissionScenarioData s, float overrideSeconds, out string error)
        {
            if (s == null) { error = "scenario is null."; return false; }

            bool needsDuration = s.completionPolicy != MissionCompletionPolicy.ObjectivesOnly ||
                                 s.failIfObjectivesIncompleteAtTimeout;
            if (needsDuration && ResolveDuration(s, overrideSeconds) <= 0f)
            {
                error = $"'{s.name}' needs missionDurationSeconds > 0 for its completion/failure policy.";
                return false;
            }

            var ids = new HashSet<string>();
            int requiredCount = 0;
            if (!CheckList(s.requiredObjectives, ids, ref requiredCount, out error)) return false;
            int ignored = 0;
            if (!CheckList(s.optionalObjectives, ids, ref ignored, out error)) return false;

            if (s.completionPolicy != MissionCompletionPolicy.SurvivalOnly && requiredCount == 0)
            {
                error = $"'{s.name}' policy needs at least one required objective.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool CheckList(IEnumerable<MissionObjectiveDefinition> list, HashSet<string> ids,
                                      ref int count, out string error)
        {
            error = null;
            if (list == null) return true;
            foreach (MissionObjectiveDefinition d in list)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.id) || string.IsNullOrWhiteSpace(d.title))
                { error = "an objective has an empty id or title."; return false; }
                if (!ids.Add(d.id)) { error = $"duplicate objective id '{d.id}'."; return false; }
                count++;
            }
            return true;
        }

        // ------------------------------------------------------------------ state helpers

        private bool TransitionTo(MissionState next)
        {
            if (!IsValidTransition(State, next)) return Reject($"Invalid transition {State} -> {next}.");
            MissionState old = State;
            State = next;
            StateChanged?.Invoke(old, next);
            return true;
        }

        private bool Reject(string message)
        {
            LastRejection = message;
            if (logRejections) Debug.LogWarning($"[MissionManager] {message}", this);
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugSetElapsed(float seconds) => ElapsedSeconds = Mathf.Max(0f, seconds);
#endif
    }
}
