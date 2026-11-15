namespace AREON.Missions
{
    /// <summary>
    /// Shared string keys used to connect objective assets with gameplay events.
    /// Keeping them in one place prevents typos between the asset creator and the scene builder.
    /// </summary>
    public static class ObjectiveKeys
    {
        // Facility ids - matched against FacilityStation objects in MissionGameplayAdapter
        public const string BaseDome = "BaseDome";
        public const string ResearchCenter = "ResearchCenter";
        public const string PowerGenerator = "PowerGenerator";

        // Acknowledgement ids - matched against ObjectiveAcknowledgeInput
        public const string ResourceMonitoring = "ResourceMonitoring";
    }
}
