// PATH: Assets/AREON/Scripts/Missions/MissionScenarioData.cs

using System.Collections.Generic;
using UnityEngine;
using AREON.Missions;

/// <summary>
/// Static configuration of ONE curated historical training scenario.
/// Contains NO mutable runtime state: runtime Oxygen/Power/Shield live only in ResourceManager.
/// Never write to this asset during Play Mode.
///
/// Historical values (pressure, flare class, storm status) come from the curated records in
/// blueprint.md section 1.5 / 1.7. Everything marked "PROTOTYPE TUNING" is an editable
/// gameplay value that needs playtesting; it is NOT a NASA measurement.
/// </summary>
[CreateAssetMenu(fileName = "SolXXX_Name", menuName = "AREON/Missions/Mission Scenario Data", order = 0)]
public class MissionScenarioData : ScriptableObject
{
    // ------------------------------------------------------------------ Identity
    [Header("Identity (curated historical training scenario)")]
    [Tooltip("Scenario identifier, e.g. 800, 852, 2085.")]
    [Min(0)] public int solNumber = 800;

    [Tooltip("Human-readable name, e.g. 'Orientation Sol'.")]
    public string displayName = "Orientation Sol";

    // ------------------------------------------------------------------ Environment (historical)
    [Header("Environment (curated historical values)")]
    [Tooltip("Atmospheric pressure in Pascals. Telemetry only; it never decides the storm state.")]
    [Min(0f)] public float pressurePa = 887f;

    [Tooltip("Explicit solar flare classification of the scenario.")]
    public SolarFlareClass solarFlareClass = SolarFlareClass.None;

    [Tooltip("Explicit storm state. AUTHORITATIVE - never derived from pressure.")]
    public StormStatus stormStatus = StormStatus.Clear;

    // ------------------------------------------------------------------ Starting resources
    [Header("Starting resources (PROTOTYPE TUNING, 0-100)")]
    [SerializeField, Range(0f, 100f)] private float initialOxygen = 88f;
    [SerializeField, Range(0f, 100f)] private float initialPower = 64f;
    [SerializeField, Range(0f, 100f)] private float initialShield = 72f;

    // ------------------------------------------------------------------ Drain
    [Header("Drain (percentage points per second; baseline = PROTOTYPE TUNING)")]
    [Tooltip("Baseline Oxygen depletion before the multiplier.")]
    [SerializeField, Min(0f)] private float oxygenDrainPerSecond = 0.5f;

    [Tooltip("Scenario multiplier applied ONCE, inside ResourceManager. Sol 800/852 = 1.0, Sol 2085 = 1.5.")]
    [SerializeField, Min(0f)] private float oxygenDrainMultiplier = 1f;

    [Tooltip("Baseline Shield depletion before the multiplier.")]
    [SerializeField, Min(0f)] private float shieldDrainPerSecond = 0.4f;

    [Tooltip("Scenario multiplier applied ONCE, inside ResourceManager. Sol 800 = 1.0, Sol 852/2085 = 2.0.")]
    [SerializeField, Min(0f)] private float shieldDrainMultiplier = 1f;

    // ------------------------------------------------------------------ Actions
    [Header("Action economy")]
    [Tooltip("Blueprint 1.4: exactly 15 Power.")]
    [SerializeField, Min(0f)] private float scienceDownloadPowerCost = 15f;

    [Tooltip("Blueprint 1.4: exactly 10 Power.")]
    [SerializeField, Min(0f)] private float shieldReinforcePowerCost = 10f;

    [Tooltip("Shield restored by one reinforcement (PROTOTYPE TUNING).")]
    [SerializeField, Min(0f)] private float shieldRestoreAmount = 25f;

    // ------------------------------------------------------------------ Daylight / solar
    [Header("Daylight and solar")]
    [Tooltip("Compressed gameplay daylight window in seconds (blueprint 1.6 default: 300).")]
    [SerializeField, Min(1f)] private float daylightDurationSeconds = 300f;

    [Tooltip("Power gained per second while charging (PROTOTYPE TUNING).")]
    [SerializeField, Min(0f)] private float solarRechargePerSecond = 2f;

    [Tooltip("False in Sol 2085 (global storm blocks sunlight).")]
    [SerializeField] private bool solarChargingAllowed = true;

    // ------------------------------------------------------------------ Stage 9 - Mission flow
    [Header("Stage 9 - Mission flow (PROTOTYPE rules, tunable - not NASA facts)")]
    [Tooltip("Mission duration in seconds. PROTOTYPE TUNING.")]
    [Min(1f)] public float missionDurationSeconds = 180f;

    [Tooltip("Success policy: ObjectivesAndSurvival, ObjectivesOnly, SurvivalOnly.")]
    public MissionCompletionPolicy completionPolicy = MissionCompletionPolicy.ObjectivesAndSurvival;

    [Tooltip("If the mission timer ends while required objectives are incomplete, fail the mission.")]
    public bool failIfObjectivesIncompleteAtTimeout = false;

    [Tooltip("If Oxygen reaches zero, fail the mission.")]
    public bool failAtZeroOxygen = true;

    [Tooltip("If Shield reaches zero, fail the mission.")]
    public bool failAtZeroShield = true;

    // ------------------------------------------------------------------ Objectives
    [Header("Objectives (data only - consumed by the Stage 9 ObjectiveManager)")]
    public List<MissionObjectiveDefinition> requiredObjectives = new List<MissionObjectiveDefinition>();
    public List<MissionObjectiveDefinition> optionalObjectives = new List<MissionObjectiveDefinition>();

    // ------------------------------------------------------------------ Read-only API
    public int SolNumber => solNumber;
    public string DisplayName => displayName;
    public float PressurePa => pressurePa;
    public SolarFlareClass SolarFlareClass => solarFlareClass;
    public StormStatus StormStatus => stormStatus;

    public float InitialOxygen => initialOxygen;
    public float InitialPower => initialPower;
    public float InitialShield => initialShield;

    public float OxygenDrainPerSecond => oxygenDrainPerSecond;
    public float OxygenDrainMultiplier => oxygenDrainMultiplier;
    public float ShieldDrainPerSecond => shieldDrainPerSecond;
    public float ShieldDrainMultiplier => shieldDrainMultiplier;

    public float ScienceDownloadPowerCost => scienceDownloadPowerCost;
    public float ShieldReinforcePowerCost => shieldReinforcePowerCost;
    public float ShieldRestoreAmount => shieldRestoreAmount;

    public float DaylightDurationSeconds => daylightDurationSeconds;
    public float SolarRechargePerSecond => solarRechargePerSecond;
    public bool SolarChargingAllowed => solarChargingAllowed;

    public float MissionDurationSeconds => missionDurationSeconds;
    public MissionCompletionPolicy CompletionPolicy => completionPolicy;
    public bool FailIfObjectivesIncompleteAtTimeout => failIfObjectivesIncompleteAtTimeout;
    public bool FailAtZeroOxygen => failAtZeroOxygen;
    public bool FailAtZeroShield => failAtZeroShield;

    public IReadOnlyList<MissionObjectiveDefinition> RequiredObjectives => requiredObjectives;
    public IReadOnlyList<MissionObjectiveDefinition> OptionalObjectives => optionalObjectives;

    /// <summary>Returns false with a readable message when the asset holds unusable values.</summary>
    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            error = $"Scenario '{name}' has an empty Display Name.";
            return false;
        }
        if (daylightDurationSeconds <= 0f)
        {
            error = $"Scenario '{name}' has a non-positive Daylight Duration.";
            return false;
        }
        if (oxygenDrainMultiplier < 0f || shieldDrainMultiplier < 0f ||
            oxygenDrainPerSecond < 0f || shieldDrainPerSecond < 0f)
        {
            error = $"Scenario '{name}' has a negative drain rate or multiplier.";
            return false;
        }
        error = null;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Attribute [Min]/[Range] already clamp most values; keep the lists non-null.
        requiredObjectives ??= new List<MissionObjectiveDefinition>();
        optionalObjectives ??= new List<MissionObjectiveDefinition>();
    }
#endif
}
