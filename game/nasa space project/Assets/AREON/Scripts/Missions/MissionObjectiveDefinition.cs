using System;
using UnityEngine;
using UnityEngine.Serialization;
using AREON.ResourceSystem;

namespace AREON.Missions
{
    /// <summary>
    /// Static configuration of ONE objective. Lives inside MissionScenarioData lists.
    /// Whether it is required or optional is decided by WHICH LIST it is in
    /// (requiredObjectives / optionalObjectives), so the two can never disagree.
    /// </summary>
    [Serializable]
    public class MissionObjectiveDefinition
    {
        [Header("Identity")]
        [FormerlySerializedAs("objectiveId")]
        public string id;
        public string title;
        [TextArea(1, 3)] public string description;

        [Header("Stage 9 - Trigger")]
        public ObjectiveTriggerType trigger = ObjectiveTriggerType.FacilityInteracted;

        [Tooltip("Facility id / acknowledgement id. Empty = matches any key for that trigger type.")]
        public string triggerKey;

        [Tooltip("Counted triggers: how many events. PowerRecharged: Power units. " +
                 "ResourceHeld / MissionTimeElapsed: seconds (0 = whole mission duration).")]
        public float targetAmount = 1f;

        [Header("Stage 9 - ResourceHeld only")]
        public ResourceType resourceType = ResourceType.Power;
        [Tooltip("Resource must stay at or above this value (0-100) for the hold time.")]
        public float resourceThreshold = 20f;

        // Stage 8 compatibility accessors
        public string ObjectiveId => id;
        public string Title => title;
        public string Description => description;
    }
}
