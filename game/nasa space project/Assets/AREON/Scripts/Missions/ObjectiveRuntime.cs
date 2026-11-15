using UnityEngine;

namespace AREON.Missions
{
    /// <summary>
    /// Runtime progress of one objective. Created by ObjectiveManager from a
    /// MissionObjectiveDefinition. The definition (a ScriptableObject sub-object) is never modified.
    /// </summary>
    public sealed class ObjectiveRuntime
    {
        public MissionObjectiveDefinition Definition { get; }
        public bool IsRequired { get; }
        public bool IsCompleted { get; internal set; }
        public float Progress { get; internal set; }
        public float Target { get; }

        internal float HoldTimer;
        internal int LastNotifiedSecond = -1;

        // ADAPT: only these four lines depend on the Stage 8 field names.
        public string Id => Definition.id;
        public string Title => Definition.title;
        public string Description => Definition.description;
        public ObjectiveTriggerType Trigger => Definition.trigger;

        public float Progress01 =>
            IsCompleted ? 1f : (Target <= 0f ? 0f : Mathf.Clamp01(Progress / Target));

        public ObjectiveRuntime(MissionObjectiveDefinition definition, bool isRequired, float target)
        {
            Definition = definition;
            IsRequired = isRequired;
            Target = target;
        }

        /// <summary>Short progress text, empty when not useful (single-step objectives).</summary>
        public string ProgressLabel
        {
            get
            {
                if (IsCompleted) return string.Empty;
                switch (Trigger)
                {
                    case ObjectiveTriggerType.ResourceHeld:
                    case ObjectiveTriggerType.MissionTimeElapsed:
                        return $"{Progress:0}s / {Target:0}s";
                    case ObjectiveTriggerType.PowerRecharged:
                        return $"{Progress:0} / {Target:0} Power";
                    default:
                        return Target > 1f ? $"{Progress:0} / {Target:0}" : string.Empty;
                }
            }
        }
    }
}
