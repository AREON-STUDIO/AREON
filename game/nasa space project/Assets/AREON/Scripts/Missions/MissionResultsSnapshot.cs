// Assets/AREON/Scripts/Missions/MissionResultsSnapshot.cs
// Stage 10 - Task 46 / 48
using System;
using System.Collections.Generic;

namespace AREON.Missions
{
    /// <summary>Terminal outcome shown on the results panel.</summary>
    public enum MissionResultOutcome
    {
        None = 0,
        Success = 1,
        Failure = 2
    }

    /// <summary>One objective line on the results panel (copied by value, never a live reference).</summary>
    [Serializable]
    public struct ObjectiveResultEntry
    {
        public string Title;
        public bool Required;
        public bool Completed;
    }

    /// <summary>
    /// Immutable-by-convention record captured ONCE, at the instant the mission becomes terminal.
    /// The results UI renders only from this object. It never reads or stores live resource values,
    /// so the panel stays correct after completion/failure.
    /// </summary>
    [Serializable]
    public sealed class MissionResultsSnapshot
    {
        public MissionResultOutcome Outcome;
        public int SolNumber;
        public string MissionName;
        public string EndReason;
        public float ElapsedSeconds;

        // Copied from ResourceManager at the moment the mission ended.
        public float Oxygen;
        public float Power;
        public float Shield;

        public List<ObjectiveResultEntry> Objectives = new List<ObjectiveResultEntry>();

        public int TotalCount { get { return Objectives.Count; } }
        public int CompletedCount { get { return Count(false, true); } }
        public int RequiredTotal { get { return Count(true, false); } }
        public int RequiredCompleted { get { return Count(true, true); } }
        public int OptionalTotal { get { return TotalCount - RequiredTotal; } }
        public int OptionalCompleted { get { return CompletedCount - RequiredCompleted; } }

        private int Count(bool requiredOnly, bool completedOnly)
        {
            int n = 0;
            for (int i = 0; i < Objectives.Count; i++)
            {
                ObjectiveResultEntry e = Objectives[i];
                if (requiredOnly && !e.Required) continue;
                if (completedOnly && !e.Completed) continue;
                n++;
            }
            return n;
        }
    }

    /// <summary>
    /// Read-only picture of runtime state, used ONLY by validation (Stage10TestHarness).
    /// Filled by MissionFlowBridge.CaptureProbe() in the adapter file.
    /// </summary>
    public sealed class MissionRuntimeProbe
    {
        public bool MissionRunning;
        public bool MissionTerminal;
        public float MissionElapsed;

        public float Oxygen, Power, Shield;
        public float OxygenMultiplier, ShieldMultiplier;

        public bool SolarChargingAllowed;
        public bool SolarChargingActive;
        public bool DaylightActive;
        public float DaylightRemaining;

        public int ObjectivesCompleted;
        public int ObjectivesTotal;

        public bool ScienceDownloaded;
        public int ShieldReinforcements;

        public int Sol;
        public float PressurePa;
        public string StormStatus;   // "Clear" / "Regional" / "SevereGlobal"
        public string FlareClass;    // "None" / "C" / "X"
    }
}
