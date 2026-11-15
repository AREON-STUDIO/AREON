// Assets/AREON/Scripts/Missions/IMissionResettable.cs
// Stage 10 - Task 47
using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// Implemented by any Stage 7-9 runtime component that keeps MUTABLE per-mission state
    /// which the existing mission initialization sequence does NOT already clear
    /// (for example: "science already downloaded", "facilities visited", "daylight expired").
    ///
    /// RULES for implementers:
    ///   1. Clear runtime fields only.
    ///   2. NEVER write to a ScriptableObject asset (MissionScenarioData, objective definitions...).
    ///   3. NEVER subscribe or unsubscribe events here (that belongs in OnEnable/OnDisable).
    ///   4. NEVER write Oxygen/Power/Shield. ResourceManager stays the single authority and is
    ///      re-initialized by the existing MissionManager start sequence.
    ///   5. Must be safe to call repeatedly and when the mission is already idle.
    ///   6. Must not depend on the order in which other resettables run.
    /// </summary>
    public interface IMissionResettable
    {
        /// <summary>Return every per-mission runtime flag, timer and cache to its "no mission yet" value.</summary>
        void ResetRuntimeState();
    }
}
