// Assets/AREON/Scripts/Missions/MissionFlowBridge.cs
// Stage 10 - Tasks 46 and 47 (CORE LOGIC - contains NO calls into existing Stage 1-9 APIs)
//
// All reads/writes of MissionManager, ObjectiveManager, ResourceManager etc. live in
// MissionFlowBridge.Adapter.cs (same partial class). If an existing API name differs from what the
// adapter assumes, edit ONLY that adapter file.
using System;
using System.Collections.Generic;
using UnityEngine;
// ADAPT[0]: add the namespace(s) that contain MissionScenarioData (and the other existing types) if they are not in AREON.Missions.
// using AREON.Core;

namespace AREON.Missions
{
    /// <summary>
    /// The single door for the Stage 10 flow:
    ///   * captures a MissionResultsSnapshot exactly once when the mission becomes terminal,
    ///   * restarts the current mission,
    ///   * returns to mission selection,
    ///   * starts any scenario with fully reset runtime state.
    /// It never owns Oxygen/Power/Shield. It copies them once from ResourceManager into the snapshot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class MissionFlowBridge : MonoBehaviour
    {
        [Header("Stage 10 helpers")]
        [SerializeField] private MissionResetCoordinator resetCoordinator;

        [Tooltip("If the existing lifecycle pauses time on a terminal state, restore Time.timeScale = 1 when a new mission begins.")]
        [SerializeField] private bool restoreTimeScaleOnReset = true;

        // ---------------------------------------------------------------- events
        /// <summary>Raised once per mission when it reaches Success or Failure.</summary>
        public event Action<MissionResultsSnapshot> MissionEnded;
        /// <summary>Raised BEFORE runtime reset so results/selection UI can hide immediately.</summary>
        public event Action MissionFlowReset;
        /// <summary>Raised after the player chose "Mission Selection".</summary>
        public event Action MissionSelectionRequested;
        /// <summary>Raised after the existing start sequence ran for a scenario.</summary>
        public event Action<MissionScenarioData> MissionStarted;

        // ---------------------------------------------------------------- state
        public MissionResultsSnapshot LastSnapshot { get; private set; }
        public MissionScenarioData LastScenario { get; private set; }
        public int ResetCount { get; private set; }
        public int TerminalNotificationCount { get; private set; }
        public bool IsAwaitingSelection { get; private set; }

        private bool subscribed;
        private bool transitioning;

        // ---------------------------------------------------------------- lifecycle
        private void OnEnable()
        {
            if (!ValidateReferences())
            {
                enabled = false;           // fail loudly once, never run half-wired
                return;
            }
            if (subscribed) return;        // guard against duplicate subscriptions
            SubscribeToMissionEvents();
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            UnsubscribeFromMissionEvents();
            subscribed = false;
        }

        // ---------------------------------------------------------------- called by the adapter
        /// <summary>The adapter calls this when MissionManager reports a terminal state.</summary>
        private void NotifyMissionEnded(MissionResultOutcome outcome)
        {
            if (LastSnapshot != null) return;   // one result per mission, ignore repeated terminal notifications

            MissionScenarioData active = ReadActiveScenario();
            if (active != null) LastScenario = active;

            LastSnapshot = BuildSnapshot(outcome, LastScenario);
            TerminalNotificationCount++;

            Action<MissionResultsSnapshot> handler = MissionEnded;
            if (handler != null) handler(LastSnapshot);
        }

        private MissionResultsSnapshot BuildSnapshot(MissionResultOutcome outcome, MissionScenarioData scenario)
        {
            MissionResultsSnapshot s = new MissionResultsSnapshot();
            s.Outcome = outcome;
            s.SolNumber = scenario != null ? ReadSolNumber(scenario) : 0;
            s.MissionName = scenario != null ? ReadMissionName(scenario) : "Unknown mission";
            s.ElapsedSeconds = ReadElapsedSeconds();

            float o, p, sh;
            ReadResources(out o, out p, out sh);          // copied ONCE from ResourceManager
            s.Oxygen = o;
            s.Power = p;
            s.Shield = sh;

            ReadObjectives(s.Objectives);
            s.EndReason = ReadEndReason(outcome, o, sh);
            return s;
        }

        // ---------------------------------------------------------------- public flow API
        /// <summary>Restart the scenario that was last played, with fully reset runtime state.</summary>
        public bool RestartCurrentMission()
        {
            MissionScenarioData scenario = LastScenario != null ? LastScenario : ReadActiveScenario();
            if (scenario == null)
            {
                Debug.LogError("[MissionFlowBridge] RestartCurrentMission: no scenario has been started yet.", this);
                return false;
            }
            return BeginScenario(scenario);
        }

        /// <summary>Start (or switch to) any scenario with fully reset runtime state.</summary>
        public bool StartScenario(MissionScenarioData scenario)
        {
            if (scenario == null)
            {
                Debug.LogError("[MissionFlowBridge] StartScenario called with a null scenario.", this);
                return false;
            }
            return BeginScenario(scenario);
        }

        /// <summary>Stop the current mission, clear all runtime state and show mission selection.</summary>
        public void ReturnToMissionSelection()
        {
            if (!enabled || transitioning) return;
            transitioning = true;
            try
            {
                PrepareForNewMission();
                ReturnMissionManagerToIdle();      // existing lifecycle, ADAPT[11]
                IsAwaitingSelection = true;
                Action selection = MissionSelectionRequested;
                if (selection != null) selection();
            }
            finally
            {
                transitioning = false;
            }
        }

        private bool BeginScenario(MissionScenarioData scenario)
        {
            if (!enabled)
            {
                Debug.LogError("[MissionFlowBridge] Disabled (missing references). Cannot start a mission.", this);
                return false;
            }
            if (transitioning) return false;       // ignore double clicks / double key presses

            transitioning = true;
            try
            {
                PrepareForNewMission();                       // 1-4: clear snapshot, time scale, UI hide, leftover flags
                LastScenario = scenario;
                StartMissionThroughExistingSequence(scenario); // 5: existing select -> initialize -> start order
                IsAwaitingSelection = false;

                Action<MissionScenarioData> started = MissionStarted;
                if (started != null) started(scenario);
                return true;
            }
            finally
            {
                transitioning = false;
            }
        }

        private void PrepareForNewMission()
        {
            LastSnapshot = null;
            ResetCount++;

            if (restoreTimeScaleOnReset) Time.timeScale = 1f;

            Action reset = MissionFlowReset;           // results panel + selection panel hide
            if (reset != null) reset();

            if (resetCoordinator != null) resetCoordinator.ResetAll();   // clear leftover Stage 7-9 flags
            else Debug.LogWarning("[MissionFlowBridge] No MissionResetCoordinator assigned; leftover per-mission flags will not be cleared.", this);
        }
    }
}
