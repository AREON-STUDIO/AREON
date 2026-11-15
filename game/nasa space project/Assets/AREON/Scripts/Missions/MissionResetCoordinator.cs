// Assets/AREON/Scripts/Missions/MissionResetCoordinator.cs
// Stage 10 - Task 47
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// Runs ResetRuntimeState() on every registered IMissionResettable.
    /// It only clears leftover per-mission flags. It does NOT start a mission and does NOT touch
    /// resources: MissionFlowBridge calls the EXISTING MissionManager start sequence afterwards,
    /// which re-applies the selected scenario to ResourceManager / ObjectiveManager / Daylight / Weather.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionResetCoordinator : MonoBehaviour
    {
        [Tooltip("Components that implement IMissionResettable. Order does not matter (resets are independent).")]
        [SerializeField] private MonoBehaviour[] resettables = new MonoBehaviour[0];

        private readonly List<IMissionResettable> cache = new List<IMissionResettable>();
        private bool cacheBuilt;

        public int RegisteredCount
        {
            get { BuildCache(); return cache.Count; }
        }

        private void Awake()
        {
            BuildCache();
        }

        private void OnValidate()
        {
            if (resettables == null) return;
            for (int i = 0; i < resettables.Length; i++)
            {
                if (resettables[i] != null && !(resettables[i] is IMissionResettable))
                {
                    Debug.LogWarning("[MissionResetCoordinator] '" + resettables[i].name + "' (" +
                                     resettables[i].GetType().Name + ") does not implement IMissionResettable and will be ignored.", this);
                }
            }
            cacheBuilt = false;
        }

        private void BuildCache()
        {
            if (cacheBuilt) return;
            cache.Clear();
            if (resettables != null)
            {
                for (int i = 0; i < resettables.Length; i++)
                {
                    IMissionResettable r = resettables[i] as IMissionResettable;
                    if (r != null && !cache.Contains(r)) cache.Add(r);   // de-duplicate
                }
            }
            cacheBuilt = true;
        }

        /// <summary>Resets every registered component. One failing component never blocks the others.</summary>
        public void ResetAll()
        {
            BuildCache();
            for (int i = 0; i < cache.Count; i++)
            {
                try
                {
                    cache[i].ResetRuntimeState();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[MissionResetCoordinator] ResetRuntimeState failed on " + cache[i].GetType().Name, this);
                    Debug.LogException(ex, this);
                }
            }
        }
    }
}
