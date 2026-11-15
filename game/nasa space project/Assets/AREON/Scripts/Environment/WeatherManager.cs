// PATH: Assets/AREON/Scripts/Environment/WeatherManager.cs

using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using AREON.ResourceSystem;
using AREON.Stations;

/// <summary>
/// Applies one MissionScenarioData to the EXISTING systems and exposes the active conditions.
///
/// Rules this class follows:
///  - It never multiplies a drain rate itself. It hands baseline rates and multipliers to
///    ResourceManager, which is the single place where they are combined.
///  - It never creates or finds other systems. Every dependency is a serialized reference.
///  - The explicit StormStatus of the scenario is authoritative. Pressure is displayed as
///    telemetry and may only add an extra "dust-front risk" line; it never changes the storm state.
///  - It reads the ScriptableObject but never writes to it.
///
/// Curated historical training data only. No NASA API call, no live telemetry.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)] // Start() runs after other systems finished their own Awake/Start.
public class WeatherManager : MonoBehaviour
{
    public const string TrainingDisclosure = "CURATED HISTORICAL TRAINING SCENARIO";

    // If TMP shows a missing-glyph box for the em dash, change this to " - ".
    private const string HeadlineSeparator = " \u2014 ";

    [Header("Scenario")]
    [Tooltip("Scenario applied in Start(). Stage 9 MissionManager will replace this later.")]
    [SerializeField] private MissionScenarioData defaultScenario;
    [SerializeField] private bool applyDefaultScenarioOnStart = true;

    [Header("Existing systems (assign - do not create duplicates)")]
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private DaylightManager daylightManager;
    [SerializeField] private SolarChargingPermissionSource solarPermissionSource;
    [SerializeField] private SolarGenerator solarGenerator;
    [SerializeField] private ScienceDownloadAction scienceDownloadAction;
    [SerializeField] private ShieldReinforcementAction shieldReinforcementAction;

    [Header("Weather rules")]
    [Tooltip("Pressure below this value adds a 'dust-front risk' line. It never changes the storm state.")]
    [SerializeField, Min(0f)] private float dustFrontPressureThresholdPa = 800f;

    private MissionScenarioData currentScenario;
    private string statusText = string.Empty;

    /// <summary>Raised after a scenario was fully applied to every system.</summary>
    public event Action<MissionScenarioData> ScenarioApplied;

    /// <summary>Raised with the new HUD text whenever the weather display changes.</summary>
    public event Action<string> WeatherStatusChanged;

    // ------------------------------------------------------------------ Exposed state
    public bool HasScenario => currentScenario != null;
    public MissionScenarioData CurrentScenario => currentScenario;

    public int CurrentSol => HasScenario ? currentScenario.SolNumber : 0;
    public string ScenarioName => HasScenario ? currentScenario.DisplayName : string.Empty;
    public float PressurePa => HasScenario ? currentScenario.PressurePa : 0f;
    public SolarFlareClass SolarFlare => HasScenario ? currentScenario.SolarFlareClass : SolarFlareClass.None;
    public StormStatus Storm => HasScenario ? currentScenario.StormStatus : StormStatus.Clear;
    public bool SolarChargingAllowed => HasScenario && currentScenario.SolarChargingAllowed;

    /// <summary>Informational copies of the scenario values (no calculation happens here).</summary>
    public float OxygenDrainMultiplier => HasScenario ? currentScenario.OxygenDrainMultiplier : 1f;
    public float ShieldDrainMultiplier => HasScenario ? currentScenario.ShieldDrainMultiplier : 1f;

    /// <summary>True when pressure is below the threshold. Informational only.</summary>
    public bool DustFrontRiskActive => HasScenario && PressurePa < dustFrontPressureThresholdPa;

    /// <summary>Full multi-line text for the HUD weather label.</summary>
    public string StatusText => statusText;

    public string WeatherLine => "WEATHER: " + StormLabel(Storm);
    public string SolarEventLine => "SOLAR EVENT: " + FlareLabel(SolarFlare);

    // ------------------------------------------------------------------ Lifecycle
    private void Start()
    {
        if (!applyDefaultScenarioOnStart) return;

        if (defaultScenario == null)
        {
            Debug.LogError("[WeatherManager] 'Default Scenario' is not assigned.", this);
            return;
        }
        ApplyScenario(defaultScenario);
    }

    // ------------------------------------------------------------------ Public API
    /// <summary>
    /// Applies a scenario to ResourceManager, DaylightManager, the solar systems and the action costs.
    /// Validates everything first, so a failure changes nothing. Returns true on success.
    /// </summary>
    public bool ApplyScenario(MissionScenarioData scenario)
    {
        if (scenario == null)
        {
            Debug.LogError("[WeatherManager] ApplyScenario was called with a null scenario.", this);
            return false;
        }
        if (!scenario.TryValidate(out string scenarioError))
        {
            Debug.LogError("[WeatherManager] " + scenarioError, this);
            return false;
        }
        if (!ValidateReferences()) return false;

        // 1) ResourceManager: the ONE central resource path.
        //    Baseline rate and multiplier are stored separately; ResourceManager multiplies them once.
        resourceManager.SetBaselineDrainRates(scenario.OxygenDrainPerSecond, scenario.ShieldDrainPerSecond);
        resourceManager.SetDrainMultipliers(scenario.OxygenDrainMultiplier, scenario.ShieldDrainMultiplier);
        resourceManager.InitializeResources(scenario.InitialOxygen, scenario.InitialPower, scenario.InitialShield);

        // 2) Daylight window.
        daylightManager.ConfigureDuration(scenario.DaylightDurationSeconds);

        // 3) Solar charging permission + recharge rate.
        solarPermissionSource.SetChargingAllowed(scenario.SolarChargingAllowed, BuildSolarDeniedReason(scenario));
        solarGenerator.SetRechargePerSecond(scenario.SolarRechargePerSecond);

        // 4) Stage 7 action values now come from the scenario.
        scienceDownloadAction.SetPowerCost(scenario.ScienceDownloadPowerCost);
        shieldReinforcementAction.SetPowerCost(scenario.ShieldReinforcePowerCost);
        shieldReinforcementAction.SetRestoreAmount(scenario.ShieldRestoreAmount);

        // 5) Publish.
        currentScenario = scenario;
        statusText = BuildStatusText();

        Debug.Log($"[WeatherManager] Applied scenario Sol {scenario.SolNumber} ({scenario.DisplayName}).", this);
        ScenarioApplied?.Invoke(currentScenario);
        WeatherStatusChanged?.Invoke(statusText);
        return true;
    }

    // ------------------------------------------------------------------ Text
    private string BuildStatusText()
    {
        var sb = new StringBuilder(160);
        sb.Append("SOL ").Append(CurrentSol).Append(HeadlineSeparator).Append(ScenarioName.ToUpperInvariant()).Append('\n');
        sb.Append("PRESSURE: ").Append(PressurePa.ToString("0.#", CultureInfo.InvariantCulture)).Append(" Pa").Append('\n');
        sb.Append(WeatherLine).Append('\n');
        sb.Append(SolarEventLine);

        // Optional secondary rule - never contradicts the explicit storm state.
        if (DustFrontRiskActive)
        {
            sb.Append('\n').Append("DUST-FRONT RISK: PRESSURE BELOW ")
              .Append(dustFrontPressureThresholdPa.ToString("0.#", CultureInfo.InvariantCulture)).Append(" Pa");
        }

        sb.Append('\n').Append(TrainingDisclosure);
        return sb.ToString();
    }

    private static string StormLabel(StormStatus storm)
    {
        switch (storm)
        {
            case StormStatus.Clear: return "CLEAR";
            case StormStatus.Regional: return "REGIONAL DUST STORM";
            case StormStatus.SevereGlobal: return "SEVERE GLOBAL STORM";
            default: return "UNKNOWN";
        }
    }

    private static string FlareLabel(SolarFlareClass flare)
    {
        switch (flare)
        {
            case SolarFlareClass.None: return "NONE";
            case SolarFlareClass.C: return "C-CLASS FLARE";
            case SolarFlareClass.X: return "X-CLASS FLARE";
            default: return "UNKNOWN";
        }
    }

    private static string BuildSolarDeniedReason(MissionScenarioData scenario)
    {
        if (scenario.SolarChargingAllowed) return string.Empty;
        return scenario.StormStatus == StormStatus.SevereGlobal
            ? "Severe global storm is blocking sunlight."
            : "Solar charging is disabled in this scenario.";
    }

    // ------------------------------------------------------------------ Validation
    private bool ValidateReferences()
    {
        var missing = new StringBuilder();
        if (resourceManager == null) missing.Append(" ResourceManager");
        if (daylightManager == null) missing.Append(" DaylightManager");
        if (solarPermissionSource == null) missing.Append(" SolarChargingPermissionSource");
        if (solarGenerator == null) missing.Append(" SolarGenerator");
        if (scienceDownloadAction == null) missing.Append(" ScienceDownloadAction");
        if (shieldReinforcementAction == null) missing.Append(" ShieldReinforcementAction");

        if (missing.Length == 0) return true;

        Debug.LogError("[WeatherManager] Missing serialized references:" + missing + ". Scenario was NOT applied.", this);
        return false;
    }
}
