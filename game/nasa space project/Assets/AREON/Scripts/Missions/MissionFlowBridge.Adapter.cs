// Assets/AREON/Scripts/Missions/MissionFlowBridge.Adapter.cs
// Stage 10 - ADAPTER ZONE.
//
// This is the ONLY file in Stage 10 that touches existing Stage 1-9 APIs.
// Every member below connects to the verified Stage 1-9 APIs.
using System.Collections.Generic;
using UnityEngine;
using AREON.ResourceSystem;
using AREON.Stations;

namespace AREON.Missions
{
    public sealed partial class MissionFlowBridge
    {
        // ------------------------------------------------------------------
        // ADAPT[1]: serialized references to the existing single instances.
        // ------------------------------------------------------------------
        [Header("Existing Stage 1-9 systems (assign in Inspector, no scene searching)")]
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private ObjectiveManager objectiveManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private DaylightManager daylightManager;
        [SerializeField] private SolarGenerator solarGenerator;
        [SerializeField] private ScienceTerminal scienceTerminal;
        [SerializeField] private ShieldConsole shieldConsole;
        [SerializeField] private WeatherManager weatherManager;

        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= Require(missionManager, "missionManager");
            ok &= Require(objectiveManager, "objectiveManager");
            ok &= Require(resourceManager, "resourceManager");
            ok &= Require(daylightManager, "daylightManager");
            ok &= Require(solarGenerator, "solarGenerator");
            ok &= Require(scienceTerminal, "scienceTerminal");
            ok &= Require(shieldConsole, "shieldConsole");
            ok &= Require(weatherManager, "weatherManager");
            return ok;
        }

        private bool Require(Object reference, string fieldName)
        {
            if (reference != null) return true;
            Debug.LogError("[MissionFlowBridge] Serialized reference '" + fieldName + "' is not assigned.", this);
            return false;
        }

        // ------------------------------------------------------------------
        // ADAPT[2]: subscribe/unsubscribe to the mission-state event.
        // ------------------------------------------------------------------
        private void SubscribeToMissionEvents()
        {
            if (missionManager != null) missionManager.StateChanged += HandleMissionStateChanged;
        }

        private void UnsubscribeFromMissionEvents()
        {
            if (missionManager != null) missionManager.StateChanged -= HandleMissionStateChanged;
        }

        // ADAPT[3]: map the real terminal states onto Success/Failure.
        private void HandleMissionStateChanged(MissionState from, MissionState to)
        {
            if (to == MissionState.Completed) NotifyMissionEnded(MissionResultOutcome.Success);
            else if (to == MissionState.Failed) NotifyMissionEnded(MissionResultOutcome.Failure);
        }

        // ADAPT[4]: the scenario MissionManager is currently running / last ran.
        private MissionScenarioData ReadActiveScenario()
        {
            return missionManager != null ? missionManager.SelectedScenario : null;
        }

        // ADAPT[5]: the ONLY place Oxygen/Power/Shield are read for the results panel.
        private void ReadResources(out float oxygen, out float power, out float shield)
        {
            if (resourceManager != null)
            {
                oxygen = resourceManager.Oxygen;
                power = resourceManager.Power;
                shield = resourceManager.Shield;
            }
            else
            {
                oxygen = 0f;
                power = 0f;
                shield = 0f;
            }
        }

        // ADAPT[6]: elapsed mission time in seconds.
        private float ReadElapsedSeconds()
        {
            return missionManager != null ? missionManager.ElapsedSeconds : 0f;
        }

        // ADAPT[7]: scenario identity.
        private int ReadSolNumber(MissionScenarioData scenario)
        {
            return scenario != null ? scenario.SolNumber : 0;
        }

        private string ReadMissionName(MissionScenarioData scenario)
        {
            return scenario != null ? scenario.DisplayName : string.Empty;
        }

        /// <summary>Text used on the results panel and on the selection buttons.</summary>
        public string DescribeScenario(MissionScenarioData scenario)
        {
            if (scenario == null) return "(no scenario)";
            return "Sol " + ReadSolNumber(scenario) + " - " + ReadMissionName(scenario);
        }

        // ADAPT[8]: copy the objective list BY VALUE (title, required flag, completed flag).
        private void ReadObjectives(List<ObjectiveResultEntry> target)
        {
            target.Clear();
            if (objectiveManager == null || objectiveManager.All == null) return;
            foreach (ObjectiveRuntime runtime in objectiveManager.All)
            {
                ObjectiveResultEntry entry = new ObjectiveResultEntry();
                entry.Title = runtime.Title;
                entry.Required = runtime.IsRequired;
                entry.Completed = runtime.IsCompleted;
                target.Add(entry);
            }
        }

        // ADAPT[9]: prefer the real reason from MissionManager.
        private string ReadEndReason(MissionResultOutcome outcome, float oxygen, float shield)
        {
            if (outcome == MissionResultOutcome.Success) return "All required objectives completed.";
            if (missionManager != null && missionManager.FailureReason != MissionFailureReason.None)
            {
                switch (missionManager.FailureReason)
                {
                    case MissionFailureReason.OxygenDepleted: return "Oxygen was depleted.";
                    case MissionFailureReason.ShieldDepleted: return "The defense shield was depleted.";
                    case MissionFailureReason.TimeExpiredObjectivesIncomplete: return "Mission time expired before the required objectives were completed.";
                    case MissionFailureReason.ForcedByTest: return "Mission failed by test trigger.";
                }
            }
            if (oxygen <= 0f) return "Oxygen was depleted.";
            if (shield <= 0f) return "The defense shield was depleted.";
            return "Mission time expired before the required objectives were completed.";
        }

        // ADAPT[10]: the EXISTING select -> initialize -> start sequence.
        private void StartMissionThroughExistingSequence(MissionScenarioData scenario)
        {
            if (missionManager == null || scenario == null) return;
            if (missionManager.State != MissionState.NotSelected)
            {
                missionManager.TryClearMission();
            }
            if (missionManager.TrySelectScenario(scenario))
            {
                missionManager.TryStartMission();
            }
        }

        // ADAPT[11]: return MissionManager to its Idle / not-started state without starting a mission.
        private void ReturnMissionManagerToIdle()
        {
            if (missionManager != null && missionManager.State != MissionState.NotSelected)
            {
                missionManager.TryClearMission();
            }
        }

        // ADAPT[12]: values used only by Stage10TestHarness.
        public MissionRuntimeProbe CaptureProbe()
        {
            MissionRuntimeProbe p = new MissionRuntimeProbe();

            p.MissionRunning = missionManager != null && missionManager.State == MissionState.Active;
            p.MissionTerminal = missionManager != null && (missionManager.State == MissionState.Completed || missionManager.State == MissionState.Failed);
            p.MissionElapsed = missionManager != null ? missionManager.ElapsedSeconds : 0f;

            ReadResources(out p.Oxygen, out p.Power, out p.Shield);

            p.OxygenMultiplier = weatherManager != null ? weatherManager.OxygenDrainMultiplier : 1f;
            p.ShieldMultiplier = weatherManager != null ? weatherManager.ShieldDrainMultiplier : 1f;
            p.StormStatus = weatherManager != null ? weatherManager.Storm.ToString() : string.Empty;
            p.FlareClass = weatherManager != null ? weatherManager.SolarFlare.ToString() : string.Empty;
            p.PressurePa = weatherManager != null ? weatherManager.PressurePa : 0f;

            p.SolarChargingAllowed = weatherManager != null && weatherManager.SolarChargingAllowed;
            p.SolarChargingActive = solarGenerator != null && solarGenerator.IsCharging;
            p.DaylightActive = daylightManager != null && daylightManager.IsDaylightActive;
            p.DaylightRemaining = daylightManager != null ? daylightManager.RemainingSeconds : 0f;

            int done = 0, total = 0;
            if (objectiveManager != null && objectiveManager.All != null)
            {
                foreach (ObjectiveRuntime r in objectiveManager.All)
                {
                    total++;
                    if (r.IsCompleted) done++;
                }
            }
            p.ObjectivesCompleted = done;
            p.ObjectivesTotal = total;

            p.ScienceDownloaded = scienceTerminal != null && scienceTerminal.DownloadAction != null && scienceTerminal.DownloadAction.HasDownloadedCurrentPacket;
            p.ShieldReinforcements = shieldConsole != null && shieldConsole.ReinforcementAction != null ? shieldConsole.ReinforcementAction.ReinforceCount : 0;

            MissionScenarioData s = ReadActiveScenario();
            p.Sol = s != null ? ReadSolNumber(s) : 0;
            return p;
        }

        // ADAPT[13]: Scenario configuration properties
        public void ReadScenarioStartingResources(MissionScenarioData scenario, out float oxygen, out float power, out float shield)
        {
            oxygen = scenario != null ? scenario.InitialOxygen : 0f;
            power = scenario != null ? scenario.InitialPower : 0f;
            shield = scenario != null ? scenario.InitialShield : 0f;
        }

        public void ReadScenarioFailureFlags(MissionScenarioData scenario, out bool failOxygen, out bool failShield, out bool failTimeout)
        {
            failOxygen = scenario != null && scenario.FailAtZeroOxygen;
            failShield = scenario != null && scenario.FailAtZeroShield;
            failTimeout = scenario != null && scenario.FailIfObjectivesIncompleteAtTimeout;
        }

        public float ReadScenarioDuration(MissionScenarioData scenario)
        {
            return scenario != null ? scenario.MissionDurationSeconds : 0f;
        }

        // ------------------------------------------------------------------
        // TEST HOOKS (ADAPT[14]). Used only by Stage10TestHarness.
        // ------------------------------------------------------------------

        public bool TestCompleteRequiredObjectives()
        {
            if (objectiveManager == null || objectiveManager.All == null) return false;
            foreach (ObjectiveRuntime r in objectiveManager.All)
            {
                if (r.IsRequired && !r.IsCompleted)
                {
                    objectiveManager.TryMarkCompleted(r.Id);
                }
            }
            return true;
        }

        public bool TestDepleteOxygen()
        {
            if (resourceManager == null) return false;
            resourceManager.Debug_SetValue(ResourceType.Oxygen, 0f);
            return true;
        }

        public bool TestDepleteShield()
        {
            if (resourceManager == null) return false;
            resourceManager.Debug_SetValue(ResourceType.Shield, 0f);
            return true;
        }

        public bool TestJumpElapsed(float seconds)
        {
            if (missionManager == null) return false;
            missionManager.DebugSetElapsed(seconds);
            return true;
        }

        public bool TestDirtyRuntimeState()
        {
            bool science = scienceTerminal != null && scienceTerminal.DownloadAction != null && scienceTerminal.DownloadAction.TryDownload().Succeeded;
            bool shield = shieldConsole != null && shieldConsole.ReinforcementAction != null && shieldConsole.ReinforcementAction.TryReinforce().Succeeded;
            if (daylightManager != null) daylightManager.ForceEndDaylight();
            return science || shield;
        }
    }
}
