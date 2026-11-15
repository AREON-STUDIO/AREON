using AREON.ResourceSystem;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// TEMPORARY Stage 9 test tool. Delete (or disable) in Stage 10 cleanup.
    /// - On-screen buttons start each scenario independently.
    /// - RunAutomatedTests() runs the lifecycle, scenario, objective and telemetry assertions.
    /// Test objective events are injected through ObjectiveManager's public Report* methods
    /// (the same entry points the adapter uses). Real facility/solar/science wiring is verified
    /// by the manual play-through procedure.
    /// </summary>
    public class MissionTestHarness : MonoBehaviour
    {
        [SerializeField] private MissionManager mission;
        [SerializeField] private ObjectiveManager objectives;
        [SerializeField] private TelemetryManager telemetry;
        [SerializeField] private MissionGameplayAdapter gameplay;
        [SerializeField] private MissionScenarioData sol800;
        [SerializeField] private MissionScenarioData sol852;
        [SerializeField] private MissionScenarioData sol2085;
        [SerializeField] private bool showOnGui = true;

        public int LastPassed { get; private set; }
        public int LastFailed { get; private set; }
        public bool IsRunning { get; private set; }

        private int pass, fail, completedEvents;
        private string lastResult = "Tests not run.";

        private static readonly MissionInitStep[] ExpectedOrder =
        {
            MissionInitStep.ValidateScenario, MissionInitStep.ApplyWeather, MissionInitStep.InitializeResources,
            MissionInitStep.ConfigureDaylight, MissionInitStep.ConfigureSolarPermission,
            MissionInitStep.InitializeObjectives, MissionInitStep.InitializeTelemetry, MissionInitStep.EnterActive
        };

        private void OnEnable()
        {
            if (mission == null || objectives == null || telemetry == null || gameplay == null ||
                sol800 == null || sol852 == null || sol2085 == null)
            {
                Debug.LogError("[MissionTestHarness] Missing serialized references.", this);
                enabled = false;
                return;
            }
            objectives.ObjectiveCompleted += CountCompleted;
        }

        private void OnDisable()
        {
            if (objectives != null) objectives.ObjectiveCompleted -= CountCompleted;
        }

        private void CountCompleted(ObjectiveRuntime _) => completedEvents++;

        // ------------------------------------------------------------------ interactive starts (callable from MCP too)

        [ContextMenu("Start Sol 800")] public void StartSol800() => StartInteractive(sol800);
        [ContextMenu("Start Sol 852")] public void StartSol852() => StartInteractive(sol852);
        [ContextMenu("Start Sol 2085")] public void StartSol2085() => StartInteractive(sol2085);
        [ContextMenu("Clear Mission")] public void ClearMission() { if (mission.State != MissionState.NotSelected) mission.TryClearMission(); }
        [ContextMenu("Run Automated Tests")] public void RunAutomatedTests() { if (!IsRunning) StartCoroutine(RunTests()); }

        private void StartInteractive(MissionScenarioData s)
        {
            ClearMission();
            if (mission.TrySelectScenario(s)) mission.TryStartMission();
        }

        private void OnGUI()
        {
            if (!showOnGui) return;
            GUILayout.BeginArea(new Rect(10, 10, 300, 330), GUI.skin.box);
            GUILayout.Label("STAGE 9 TEST HARNESS (temporary)");
            GUILayout.Label($"State: {mission.State}   Elapsed: {mission.ElapsedSeconds:0.0}/{mission.DurationSeconds:0.0}s");
            GUILayout.Label($"Required {objectives.RequiredCompleted}/{objectives.RequiredTotal}  Optional {objectives.OptionalCompleted}/{objectives.OptionalTotal}");
            if (GUILayout.Button("Start Sol 800")) StartSol800();
            if (GUILayout.Button("Start Sol 852")) StartSol852();
            if (GUILayout.Button("Start Sol 2085")) StartSol2085();
            if (GUILayout.Button("Clear Mission")) ClearMission();
            GUI.enabled = !IsRunning;
            if (GUILayout.Button("Run Automated Tests")) RunAutomatedTests();
            GUI.enabled = true;
            GUILayout.Label(lastResult);
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ assertions

        private void Check(string name, bool condition)
        {
            if (condition) { pass++; Debug.Log($"[Stage9] PASS  {name}"); }
            else { fail++; Debug.LogError($"[Stage9] FAIL  {name}"); }
        }

        private IEnumerator StartScenario(MissionScenarioData s, float duration)
        {
            if (mission.State != MissionState.NotSelected) mission.TryClearMission();
            mission.TrySelectScenario(s);
            if (duration > 0f) mission.SetRuntimeDurationOverride(duration);
            mission.TryStartMission();
            completedEvents = 0;
            yield return null;
        }

        // ------------------------------------------------------------------ test suite

        private IEnumerator RunTests()
        {
            IsRunning = true; pass = 0; fail = 0;
            float assetDurationBefore = sol800.missionDurationSeconds;
            bool previousLogging = mission.LogRejections;
            mission.LogRejections = false;
            if (mission.State != MissionState.NotSelected) mission.TryClearMission();
            yield return null;

            // ---------- Lifecycle and invalid transitions
            Check("Transition table: NotSelected->Active invalid", !MissionManager.IsValidTransition(MissionState.NotSelected, MissionState.Active));
            Check("Transition table: Completed->Active invalid", !MissionManager.IsValidTransition(MissionState.Completed, MissionState.Active));
            Check("Start rejected from NotSelected", !mission.TryStartMission());
            Check("Complete rejected from NotSelected", !mission.TryCompleteMission());
            Check("Fail rejected from NotSelected", !mission.TryFailMission(MissionFailureReason.ForcedByTest));
            Check("Clear rejected from NotSelected", !mission.TryClearMission());
            Check("Null scenario rejected", !mission.TrySelectScenario(null));
            Check("State still NotSelected", mission.State == MissionState.NotSelected);
            Check("Select Sol 800", mission.TrySelectScenario(sol800));
            Check("State is Ready", mission.State == MissionState.Ready && mission.SelectedScenario == sol800);
            Check("Complete rejected from Ready", !mission.TryCompleteMission());
            Check("Fail rejected from Ready", !mission.TryFailMission(MissionFailureReason.ForcedByTest));
            Check("Duration override rejects <= 0", !mission.SetRuntimeDurationOverride(0f));
            Check("Duration override accepted in Ready", mission.SetRuntimeDurationOverride(2f));
            Check("Start Sol 800", mission.TryStartMission());
            Check("State is Active", mission.State == MissionState.Active);
            Check("Initialization order is exactly the 8 specified steps", mission.LastInitSteps.SequenceEqual(ExpectedOrder));
            Check("Start rejected while Active", !mission.TryStartMission());
            Check("Select rejected while Active", !mission.TrySelectScenario(sol852));
            Check("Timer uses configured (override) duration", Mathf.Approximately(mission.DurationSeconds, 2f));
            yield return new WaitForSeconds(0.5f);
            Check("Timer advances", mission.ElapsedSeconds >= 0.4f);
            Check("Scenario asset duration is untouched by the runtime override", Mathf.Approximately(sol800.missionDurationSeconds, assetDurationBefore));

            // ---------- Sol 800 scenario info and objectives
            Check("Sol 800 number", sol800.solNumber == 800);
            Check("Sol 800 pressure 887", Mathf.Approximately(sol800.pressurePa, 887f));
            Check("Sol 800 flare None", sol800.solarFlareClass == SolarFlareClass.None);
            Check("Sol 800 storm Clear", sol800.stormStatus == StormStatus.Clear);
            Check("Sol 800 oxygen multiplier 1", Mathf.Approximately(gameplay.OxygenDrainMultiplier, 1f));
            Check("Sol 800 shield multiplier 1", Mathf.Approximately(gameplay.ShieldDrainMultiplier, 1f));
            Check("Sol 800 solar charging permitted", gameplay.SolarChargingPermitted);
            Check("Sol 800 telemetry loaded", telemetry.HasData && telemetry.Current.Sol == 800);
            Check("Sol 800 has 6 required objectives", objectives.RequiredTotal == 6);
            Check("Sol 800 has optional objectives", objectives.OptionalTotal >= 1);
            Check("Sol 800 first objective is Base Dome visit", objectives.Required[0].Trigger == ObjectiveTriggerType.FacilityInteracted);
            Check("Acknowledge objective exists and is incomplete", objectives.HasIncompleteObjective(ObjectiveTriggerType.Acknowledgement, ObjectiveKeys.ResourceMonitoring));

            objectives.ReportFacilityInteracted(ObjectiveKeys.BaseDome);
            Check("Base Dome visit counted", objectives.RequiredCompleted == 1);
            objectives.ReportFacilityInteracted(ObjectiveKeys.BaseDome);
            Check("Repeated Base Dome visit does not double count", objectives.RequiredCompleted == 1 && completedEvents == 1);
            objectives.ReportFacilityInteracted("Nowhere");
            Check("Unknown facility ignored", objectives.RequiredCompleted == 1);
            objectives.ReportAcknowledgement("WrongKey");
            Check("Wrong acknowledgement key ignored", objectives.RequiredCompleted == 1);
            objectives.ReportFacilityInteracted(ObjectiveKeys.ResearchCenter);
            objectives.ReportFacilityInteracted(ObjectiveKeys.PowerGenerator);
            Check("All three facility objectives complete", objectives.RequiredCompleted == 3);
            objectives.ReportAcknowledgement(ObjectiveKeys.ResourceMonitoring);
            Check("Resource monitoring acknowledgement completes", objectives.RequiredCompleted == 4);
            Check("Acknowledge no longer offered", !objectives.HasIncompleteObjective(ObjectiveTriggerType.Acknowledgement, ObjectiveKeys.ResourceMonitoring));
            objectives.ReportPowerRecharged(4f);
            Check("Partial recharge does not complete", objectives.RequiredCompleted == 4);
            Check("Recharge shows partial progress", objectives.CurrentObjective != null && objectives.CurrentObjective.Progress > 3.9f);
            objectives.ReportPowerRecharged(6f);
            Check("Recharge objective completes at target", objectives.RequiredCompleted == 5);
            yield return new WaitForSeconds(2.0f);
            Check("Timer elapsed but incomplete tutorial objective keeps mission Active", mission.State == MissionState.Active && mission.ElapsedSeconds >= 2f);
            objectives.ReportScienceDownloaded();
            Check("All required objectives complete", objectives.AllRequiredCompleted);
            yield return null; yield return null;
            Check("Mission completes with optional objectives incomplete", mission.State == MissionState.Completed && objectives.OptionalCompleted == 0);
            Check("Exactly 6 completion events", completedEvents == 6);
            objectives.ReportScienceDownloaded();
            objectives.ReportFacilityInteracted(ObjectiveKeys.BaseDome);
            Check("Events after end are ignored", completedEvents == 6);
            Check("Complete rejected from Completed", !mission.TryCompleteMission());
            Check("Fail rejected from Completed", !mission.TryFailMission(MissionFailureReason.ForcedByTest));
            Check("Start rejected from Completed", !mission.TryStartMission());
            Check("Select rejected from Completed", !mission.TrySelectScenario(sol852));
            Check("Clear Completed mission", mission.TryClearMission() && mission.State == MissionState.NotSelected);
            Check("Telemetry cleared with mission", !telemetry.HasData);

            // ---------- Configured duration (no override)
            yield return StartScenario(sol800, 0f);
            Check("Duration comes from scenario asset", Mathf.Approximately(mission.DurationSeconds, sol800.missionDurationSeconds));

            // ---------- Configurable failure: shield, oxygen, power on Sol 800
            gameplay.Debug_SetResource(ResourceType.Power, 0f);
            yield return new WaitForSeconds(0.3f);
            Check("Power at zero does NOT fail the mission", mission.State == MissionState.Active);
            gameplay.Debug_SetResource(ResourceType.Shield, 0f);
            yield return new WaitForSeconds(0.3f);
            bool shieldRule = sol800.failAtZeroShield;
            Check($"Sol 800 shield zero follows failAtZeroShield={shieldRule}",
                  shieldRule ? mission.State == MissionState.Failed : mission.State == MissionState.Active);
            if (mission.State == MissionState.Active)
            {
                gameplay.Debug_SetResource(ResourceType.Oxygen, 0f);
                yield return new WaitForSeconds(0.3f);
            }
            Check("Oxygen zero fails mission", mission.State == MissionState.Failed && (mission.FailureReason == MissionFailureReason.OxygenDepleted || mission.FailureReason == MissionFailureReason.ShieldDepleted));
            Check("Complete rejected from Failed", !mission.TryCompleteMission());
            Check("Fail rejected from Failed", !mission.TryFailMission(MissionFailureReason.ForcedByTest));

            // ---------- Sol 852
            yield return StartScenario(sol852, 2f);
            Check("Sol 852 number", sol852.solNumber == 852);
            Check("Sol 852 pressure 924", Mathf.Approximately(sol852.pressurePa, 924f));
            Check("Sol 852 flare C", sol852.solarFlareClass == SolarFlareClass.C);
            Check("Sol 852 storm Regional despite 924 Pa", sol852.stormStatus == StormStatus.Regional);
            Check("Sol 852 shield multiplier 2x", Mathf.Approximately(gameplay.ShieldDrainMultiplier, 2f));
            Check("Sol 852 oxygen multiplier 1x", Mathf.Approximately(gameplay.OxygenDrainMultiplier, 1f));
            Check("Sol 852 telemetry weather text", telemetry.WeatherText(telemetry.Current) == "REGIONAL DUST STORM");
            Check("Sol 852 telemetry flare text", telemetry.SolarEventText(telemetry.Current) == "C-CLASS FLARE");
            Check("Sol 852 objectives loaded (4 required)", objectives.RequiredTotal == 4);
            Check("Sol 852 optional objectives loaded (2)", objectives.OptionalTotal == 2);
            Check("Sol 852 timer uses override", Mathf.Approximately(mission.DurationSeconds, 2f));
            objectives.ReportShieldReinforced();
            objectives.ReportPowerRecharged(20f);
            Check("Sol 852 triage objectives progress", objectives.RequiredCompleted == 2);
            yield return new WaitForSeconds(1.0f);
            Check("Objectives done but survival time not satisfied: still Active", mission.State == MissionState.Active && !mission.IsCompletionConditionMet());
            yield return new WaitForSeconds(1.4f);
            Check("Sol 852 completes after survival time", mission.State == MissionState.Completed);
            Check("Optional objectives did not block completion", objectives.OptionalCompleted < objectives.OptionalTotal);

            yield return StartScenario(sol852, 1f);
            yield return new WaitForSeconds(1.4f);
            Check("Sol 852 timeout with incomplete objectives fails (policy)",
                  sol852.failIfObjectivesIncompleteAtTimeout
                      ? mission.State == MissionState.Failed && mission.FailureReason == MissionFailureReason.TimeExpiredObjectivesIncomplete
                      : mission.State == MissionState.Active);

            yield return StartScenario(sol852, 5f);
            gameplay.Debug_SetResource(ResourceType.Power, 0f);
            yield return new WaitForSeconds(0.3f);
            Check("Sol 852: Power zero does not fail", mission.State == MissionState.Active);

            // ---------- Sol 2085
            yield return StartScenario(sol2085, 2f);
            Check("Sol 2085 number", sol2085.solNumber == 2085);
            Check("Sol 2085 pressure 772", Mathf.Approximately(sol2085.pressurePa, 772f));
            Check("Sol 2085 flare X", sol2085.solarFlareClass == SolarFlareClass.X);
            Check("Sol 2085 storm SevereGlobal", sol2085.stormStatus == StormStatus.SevereGlobal);
            Check("Sol 2085 oxygen multiplier 1.5x", Mathf.Approximately(gameplay.OxygenDrainMultiplier, 1.5f));
            Check("Sol 2085 shield multiplier 2x", Mathf.Approximately(gameplay.ShieldDrainMultiplier, 2f));
            Check("Sol 2085 solar charging disabled", !gameplay.SolarChargingPermitted);
            Check("Sol 2085 telemetry text", telemetry.WeatherText(telemetry.Current) == "SEVERE GLOBAL DUST STORM" && telemetry.SolarEventText(telemetry.Current) == "X-CLASS FLARE");
            Check("Sol 2085 objectives loaded (3 required)", objectives.RequiredTotal == 3);
            Check("Sol 2085 optional objectives loaded (2)", objectives.OptionalTotal == 2);
            Check("Sol 2085 has no recharge objective", !objectives.All.Any(o => o.Trigger == ObjectiveTriggerType.PowerRecharged));
            objectives.ReportPowerRecharged(50f);
            Check("Recharge event does nothing in Sol 2085", objectives.RequiredCompleted == 0);
            objectives.ReportShieldReinforced();
            Check("Sol 2085 shield reinforcement objective", objectives.RequiredCompleted == 1);
            yield return new WaitForSeconds(2.4f);
            Check("Sol 2085 completes when reserve held and survival met", mission.State == MissionState.Completed);

            yield return StartScenario(sol2085, 2f);
            gameplay.Debug_SetResource(ResourceType.Shield, 0f);
            yield return new WaitForSeconds(0.3f);
            Check("Sol 2085 shield zero fails (failAtZeroShield)", sol2085.failAtZeroShield
                ? mission.State == MissionState.Failed && mission.FailureReason == MissionFailureReason.ShieldDepleted
                : mission.State == MissionState.Active);

            yield return StartScenario(sol2085, 2f);
            gameplay.Debug_SetResource(ResourceType.Oxygen, 0f);
            yield return new WaitForSeconds(0.3f);
            Check("Sol 2085 oxygen zero fails", mission.State == MissionState.Failed && mission.FailureReason == MissionFailureReason.OxygenDepleted);

            yield return StartScenario(sol2085, 1.5f);
            objectives.ReportShieldReinforced();
            yield return new WaitForSeconds(0.3f);
            gameplay.Debug_SetResource(ResourceType.Power, 0f);
            yield return new WaitForSeconds(0.3f);
            Check("Sol 2085 Power zero alone does not fail", mission.State == MissionState.Active);
            yield return new WaitForSeconds(1.2f);
            Check("Power reserve objective cannot complete after dropping below threshold", !objectives.GetById("s2085_power_reserve").IsCompleted);
            Check("Sol 2085 timeout with incomplete objectives (policy)",
                  sol2085.failIfObjectivesIncompleteAtTimeout
                      ? mission.State == MissionState.Failed && mission.FailureReason == MissionFailureReason.TimeExpiredObjectivesIncomplete
                      : mission.State == MissionState.Active);

            // ---------- Telemetry
            TelemetryRecord r;
            Check("Telemetry record Sol 800", telemetry.TryGetRecord(800, out r) && Mathf.Approximately(r.PressurePa, 887f) && r.SolarFlare == SolarFlareClass.None && r.Storm == StormStatus.Clear && r.AlertLevel == "SAFE");
            Check("Telemetry record Sol 852", telemetry.TryGetRecord(852, out r) && Mathf.Approximately(r.PressurePa, 924f) && r.SolarFlare == SolarFlareClass.C && r.Storm == StormStatus.Regional && r.AlertLevel == "WARNING");
            Check("Telemetry record Sol 2085", telemetry.TryGetRecord(2085, out r) && Mathf.Approximately(r.PressurePa, 772f) && r.SolarFlare == SolarFlareClass.X && r.Storm == StormStatus.SevereGlobal && r.AlertLevel == "WARNING");
            Check("Unknown Sol has no record", !telemetry.TryGetRecord(1, out r));
            telemetry.TryGetRecord(852, out r);
            string panel = telemetry.BuildPanelText(r);
            Check("Panel text shows Sol, pressure, flare, weather",
                  panel.Contains("SOL: 852") && panel.Contains("PRESSURE: 924 Pa") && panel.Contains("C-CLASS FLARE") && panel.Contains("REGIONAL DUST STORM"));
            Check("Panel shows curated-data disclosure", panel.Contains("CURATED HISTORICAL TRAINING DATA") && panel.Contains("NOT LIVE TELEMETRY"));
            string report = telemetry.BuildScienceReportText(r);
            Check("Science report includes record and disclosure", report.Contains("SOL 852") && report.Contains("924 Pa") && report.Contains("NOT LIVE TELEMETRY"));
            Check("No external API used", !telemetry.UsesExternalApi);

            yield return StartScenario(sol800, 0f);
            Check("Telemetry follows mission: Sol 800", telemetry.HasData && telemetry.Current.Sol == 800);
            yield return StartScenario(sol852, 0f);
            Check("Telemetry follows mission: Sol 852", telemetry.Current.Sol == 852);
            yield return StartScenario(sol2085, 0f);
            Check("Telemetry follows mission: Sol 2085", telemetry.Current.Sol == 2085);

            // ---------- Finish
            if (mission.State != MissionState.NotSelected) mission.TryClearMission();
            mission.LogRejections = previousLogging;
            LastPassed = pass; LastFailed = fail;
            lastResult = $"RESULT: {pass}/{pass + fail} passed" + (fail == 0 ? " - ALL PASS" : $" - {fail} FAILED");
            Debug.Log($"[Stage9] {lastResult}");
            IsRunning = false;
        }
    }
}
