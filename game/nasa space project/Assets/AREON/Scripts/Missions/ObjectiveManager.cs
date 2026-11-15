using AREON.ResourceSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

// ADAPT: add the using for ResourceType's namespace if it has one.

namespace AREON.Missions
{
    /// <summary>
    /// Owns objective progress and completion state for the active mission.
    /// It loads MissionObjectiveDefinition entries from MissionScenarioData (read-only),
    /// listens to neutral gameplay events from MissionGameplayAdapter, and is ticked by MissionManager.
    /// It knows nothing about mission success/failure - MissionManager decides that.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        [SerializeField] private MissionGameplayAdapter gameplayEvents;

        private readonly List<ObjectiveRuntime> all = new List<ObjectiveRuntime>();
        private readonly List<ObjectiveRuntime> required = new List<ObjectiveRuntime>();
        private readonly List<ObjectiveRuntime> optional = new List<ObjectiveRuntime>();
        private Func<ResourceType, float> resourceReader;
        private bool tracking;
        private bool requiredCompleteRaised;

        /// <summary>The objective list was rebuilt or cleared (UI should rebuild).</summary>
        public event Action ObjectivesChanged;
        /// <summary>Progress of one objective changed (whole-second granularity for timed objectives).</summary>
        public event Action<ObjectiveRuntime> ObjectiveProgressed;
        public event Action<ObjectiveRuntime> ObjectiveCompleted;
        /// <summary>Raised once, when the last required objective completes.</summary>
        public event Action RequiredObjectivesCompleted;

        // ------------------------------------------------------------------ public state

        /// <summary>Required objectives first, then optional, each in configured order.</summary>
        public IReadOnlyList<ObjectiveRuntime> All => all;
        public IReadOnlyList<ObjectiveRuntime> Required => required;
        public IReadOnlyList<ObjectiveRuntime> Optional => optional;
        public bool IsTracking => tracking;

        public int RequiredTotal => required.Count;
        public int OptionalTotal => optional.Count;
        public int RequiredCompleted => CountCompleted(required);
        public int OptionalCompleted => CountCompleted(optional);
        public bool AllRequiredCompleted => RequiredCompleted == RequiredTotal;
        public float RequiredProgress01 => RequiredTotal == 0 ? 1f : (float)RequiredCompleted / RequiredTotal;

        /// <summary>First incomplete required objective in configured order (null if none).</summary>
        public ObjectiveRuntime CurrentObjective
        {
            get
            {
                foreach (ObjectiveRuntime r in required) if (!r.IsCompleted) return r;
                return null;
            }
        }

        public ObjectiveRuntime GetById(string id)
        {
            foreach (ObjectiveRuntime r in all) if (r.Id == id) return r;
            return null;
        }

        public bool HasIncompleteObjective(ObjectiveTriggerType trigger, string key)
        {
            foreach (ObjectiveRuntime r in all)
                if (!r.IsCompleted && r.Trigger == trigger && KeyMatches(r.Definition.triggerKey, key)) return true;
            return false;
        }

        // ------------------------------------------------------------------ lifecycle

        private void OnEnable()
        {
            if (gameplayEvents == null)
            {
                Debug.LogError("[ObjectiveManager] MissionGameplayAdapter reference missing.", this);
                return;
            }
            gameplayEvents.FacilityInteracted += ReportFacilityInteracted;
            gameplayEvents.PowerRecharged += ReportPowerRecharged;
            gameplayEvents.ScienceDownloaded += ReportScienceDownloaded;
            gameplayEvents.ShieldReinforced += ReportShieldReinforced;
        }

        private void OnDisable()
        {
            if (gameplayEvents == null) return;
            gameplayEvents.FacilityInteracted -= ReportFacilityInteracted;
            gameplayEvents.PowerRecharged -= ReportPowerRecharged;
            gameplayEvents.ScienceDownloaded -= ReportScienceDownloaded;
            gameplayEvents.ShieldReinforced -= ReportShieldReinforced;
        }

        /// <summary>
        /// Builds runtime objectives from scenario data (init step 6). Tracking stays OFF until
        /// MissionManager calls SetTracking(true), so events before mission start are ignored.
        /// </summary>
        public void Initialize(MissionScenarioData data, float missionDurationSeconds,
                               Func<ResourceType, float> resourceValueReader)
        {
            ClearInternal();
            resourceReader = resourceValueReader;
            if (data == null) { Debug.LogError("[ObjectiveManager] Initialize called with null scenario.", this); return; }

            var usedIds = new HashSet<string>();
            AddObjectives(data.requiredObjectives, true, missionDurationSeconds, usedIds);
            AddObjectives(data.optionalObjectives, false, missionDurationSeconds, usedIds);
            ObjectivesChanged?.Invoke();
        }

        public void SetTracking(bool value) => tracking = value;

        public void Clear()
        {
            ClearInternal();
            ObjectivesChanged?.Invoke();
        }

        // ------------------------------------------------------------------ event entry points (public so tests can call them)

        public void ReportFacilityInteracted(string facilityId) =>
            ReportEvent(ObjectiveTriggerType.FacilityInteracted, facilityId, 1f);

        public void ReportAcknowledgement(string key) =>
            ReportEvent(ObjectiveTriggerType.Acknowledgement, key, 1f);

        public void ReportPowerRecharged(float amount)
        {
            if (amount > 0f) ReportEvent(ObjectiveTriggerType.PowerRecharged, null, amount);
        }

        public void ReportScienceDownloaded() => ReportEvent(ObjectiveTriggerType.ScienceDownloaded, null, 1f);
        public void ReportShieldReinforced() => ReportEvent(ObjectiveTriggerType.ShieldReinforced, null, 1f);

        /// <summary>Marks an objective complete by id. Returns false if unknown, already complete or not tracking.</summary>
        public bool TryMarkCompleted(string id)
        {
            if (!tracking) return false;
            ObjectiveRuntime r = GetById(id);
            return r != null && Complete(r);
        }

        /// <summary>Called by MissionManager every frame while the mission is Active.</summary>
        public void Tick(float deltaTime, float missionElapsedSeconds)
        {
            if (!tracking) return;
            for (int i = 0; i < all.Count; i++)
            {
                ObjectiveRuntime r = all[i];
                if (r.IsCompleted) continue;

                if (r.Trigger == ObjectiveTriggerType.ResourceHeld)
                {
                    float value = resourceReader != null ? resourceReader(r.Definition.resourceType) : 0f;
                    if (value >= r.Definition.resourceThreshold) r.HoldTimer += deltaTime;
                    else r.HoldTimer = 0f;
                    SetProgress(r, r.HoldTimer);
                }
                else if (r.Trigger == ObjectiveTriggerType.MissionTimeElapsed)
                {
                    SetProgress(r, missionElapsedSeconds);
                }
            }
        }

        // ------------------------------------------------------------------ internals

        private void ReportEvent(ObjectiveTriggerType type, string key, float amount)
        {
            if (!tracking) return;
            for (int i = 0; i < all.Count; i++)
            {
                ObjectiveRuntime r = all[i];
                if (r.IsCompleted || r.Trigger != type) continue;
                if (!KeyMatches(r.Definition.triggerKey, key)) continue;
                SetProgress(r, r.Progress + amount);
            }
        }

        private void SetProgress(ObjectiveRuntime r, float value)
        {
            r.Progress = value;
            if (r.Progress >= r.Target - 0.0001f)
            {
                Complete(r);
                return;
            }
            int second = Mathf.FloorToInt(r.Progress);
            bool timed = r.Trigger == ObjectiveTriggerType.ResourceHeld || r.Trigger == ObjectiveTriggerType.MissionTimeElapsed;
            if (!timed || second != r.LastNotifiedSecond)
            {
                r.LastNotifiedSecond = second;
                ObjectiveProgressed?.Invoke(r);
            }
        }

        private bool Complete(ObjectiveRuntime r)
        {
            if (r.IsCompleted) return false;      // duplicate completion guard
            r.IsCompleted = true;
            r.Progress = r.Target;
            ObjectiveCompleted?.Invoke(r);
            ObjectiveProgressed?.Invoke(r);

            if (!requiredCompleteRaised && required.Count > 0 && AllRequiredCompleted)
            {
                requiredCompleteRaised = true;
                RequiredObjectivesCompleted?.Invoke();
            }
            return true;
        }

        private void AddObjectives(IEnumerable<MissionObjectiveDefinition> source, bool isRequired,
                                   float missionDuration, HashSet<string> usedIds)
        {
            if (source == null) return;
            foreach (MissionObjectiveDefinition def in source)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                {
                    Debug.LogError("[ObjectiveManager] Objective with empty id skipped.", this);
                    continue;
                }
                if (!usedIds.Add(def.id))
                {
                    Debug.LogError($"[ObjectiveManager] Duplicate objective id '{def.id}' skipped.", this);
                    continue;
                }

                bool timed = def.trigger == ObjectiveTriggerType.ResourceHeld ||
                             def.trigger == ObjectiveTriggerType.MissionTimeElapsed;
                float target = timed
                    ? (def.targetAmount > 0f ? def.targetAmount : missionDuration)   // 0 = whole mission
                    : Mathf.Max(def.targetAmount, 1f);

                var runtime = new ObjectiveRuntime(def, isRequired, target);
                all.Add(runtime);
                (isRequired ? required : optional).Add(runtime);
            }
        }

        private void ClearInternal()
        {
            tracking = false;
            requiredCompleteRaised = false;
            all.Clear(); required.Clear(); optional.Clear();
            resourceReader = null;
        }

        private static int CountCompleted(List<ObjectiveRuntime> list)
        {
            int n = 0;
            foreach (ObjectiveRuntime r in list) if (r.IsCompleted) n++;
            return n;
        }

        private static bool KeyMatches(string configured, string reported) =>
            string.IsNullOrEmpty(configured) ||
            string.Equals(configured, reported, StringComparison.OrdinalIgnoreCase);
    }
}
