namespace AREON.Missions
{
    /// <summary>Lifecycle states of a mission. Owned by MissionManager.</summary>
    public enum MissionState
    {
        NotSelected,
        Ready,
        Active,
        Completed,
        Failed
    }

    public enum MissionFailureReason
    {
        None,
        OxygenDepleted,
        ShieldDepleted,
        TimeExpiredObjectivesIncomplete,
        ForcedByTest
    }

    /// <summary>
    /// PROTOTYPE GAMEPLAY RULE (blueprint 2.6) - not NASA information.
    /// ObjectivesAndSurvival : all required objectives done AND mission timer elapsed (recommended default).
    /// ObjectivesOnly        : all required objectives done, timer is ignored for success.
    /// SurvivalOnly          : mission timer elapsed, objectives are ignored for success.
    /// </summary>
    public enum MissionCompletionPolicy
    {
        ObjectivesAndSurvival,
        ObjectivesOnly,
        SurvivalOnly
    }

    /// <summary>The existing gameplay event an objective listens to.</summary>
    public enum ObjectiveTriggerType
    {
        FacilityInteracted,   // key = facility id (see ObjectiveKeys); amount 1 per interaction
        Acknowledgement,      // key = acknowledgement id; explicit player action
        PowerRecharged,       // cumulative Power gained from the solar generator
        ScienceDownloaded,    // successful science download (count)
        ShieldReinforced,     // successful shield reinforcement (count)
        ResourceHeld,         // resource >= threshold continuously for N seconds (0 = whole mission)
        MissionTimeElapsed    // mission clock reached N seconds (0 = whole mission)
    }

    /// <summary>Recorded by MissionManager so the initialization order can be verified by tests.</summary>
    public enum MissionInitStep
    {
        ValidateScenario,
        ApplyWeather,
        InitializeResources,
        ConfigureDaylight,
        ConfigureSolarPermission,
        InitializeObjectives,
        InitializeTelemetry,
        EnterActive
    }
}
