using AREON.Stations;
using AREON.ResourceSystem;
using System;
using UnityEngine;

// ADAPT: if Stage 1-8 types live in namespaces (e.g. AREON.Resources) add the usings here.
// This is the ONLY Stage 9 file that talks to existing Stage 1-8 classes.
// Every line marked "ADAPT" calls an API that is ASSUMED from the Stage 8 handoff.
// If one does not compile, fix it HERE (copy the exact call from ScenarioSwitchTestHarness.cs,
// which already applies a scenario in Stage 8). Do not change the rest of Stage 9.

namespace AREON.Missions
{
    /// <summary>
    /// Single boundary between the Stage 9 mission layer and the existing gameplay systems.
    ///  - OUTBOUND: apply scenario data to Weather / Resources / Daylight / Solar permission.
    ///  - INBOUND : relays existing gameplay events (facility, solar, science, shield) as neutral events.
    /// It owns NO gameplay state.
    /// </summary>
    public class MissionGameplayAdapter : MonoBehaviour
    {
        [Serializable]
        public class FacilityBinding
        {
            [Tooltip("Use the ObjectiveKeys constants: BaseDome, ResearchCenter, PowerGenerator")]
            public string facilityId;
            public FacilityStation station;
        }

        [Header("Systems that receive scenario data")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private DaylightManager daylightManager;
        [SerializeField] private SolarChargingPermissionSource solarPermission;

        [Header("Gameplay event sources")]
        [SerializeField] private FacilityBinding[] facilities;
        [SerializeField] private SolarGenerator solarGenerator;
        [SerializeField] private ScienceDownloadAction scienceDownload;
        [SerializeField] private ShieldReinforcementAction shieldReinforcement;

        /// <summary>Raised with the facility id when the player interacts with a bound facility.</summary>
        public event Action<string> FacilityInteracted;
        /// <summary>Raised with the Power amount gained from solar charging.</summary>
        public event Action<float> PowerRecharged;
        public event Action ScienceDownloaded;
        public event Action ShieldReinforced;

        private Action<FacilityStation>[] facilityHandlers;

        // ------------------------------------------------------------------ validation

        public bool Validate(out string error)
        {
            if (resourceManager == null) { error = "ResourceManager reference missing."; return false; }
            if (weatherManager == null) { error = "WeatherManager reference missing."; return false; }
            if (daylightManager == null) { error = "DaylightManager reference missing."; return false; }
            if (solarPermission == null) { error = "SolarChargingPermissionSource reference missing."; return false; }
            error = null;
            return true;
        }

        // ------------------------------------------------------------------ outbound (init steps 2-5)

        public bool ApplyWeather(MissionScenarioData data) =>
            Run("WeatherManager", () => weatherManager.ApplyScenario(data));                 // ADAPT

        public bool InitializeResources(MissionScenarioData data) =>
            Run("ResourceManager", () => resourceManager.InitializeFromScenario(data));      // ADAPT

        public bool ConfigureDaylight(MissionScenarioData data) =>
            Run("DaylightManager", () => daylightManager.ConfigureFromScenario(data));       // ADAPT

        public bool ConfigureSolarPermission(MissionScenarioData data) =>
            Run("SolarChargingPermissionSource", () => solarPermission.ApplyScenario(data)); // ADAPT

        // ------------------------------------------------------------------ simulation gate

        /// <summary>
        /// Starts/stops continuous Oxygen/Shield depletion and the daylight countdown.
        /// If ResourceManager/DaylightManager already have a pause flag, call it here.
        /// Otherwise add a minimal "SimulationActive" bool guard to their Update (report it as a modification).
        /// </summary>
        public void SetSimulationActive(bool active)
        {
            if (resourceManager != null) resourceManager.SetSimulationActive(active);        // ADAPT
            if (daylightManager != null) daylightManager.SetRunning(active);                 // ADAPT
        }

        // ------------------------------------------------------------------ read-only queries

        public float GetResource(ResourceType type) => resourceManager.GetValue(type);        // ADAPT
        public float OxygenDrainMultiplier => weatherManager.OxygenDrainMultiplier;          // ADAPT
        public float ShieldDrainMultiplier => weatherManager.ShieldDrainMultiplier;          // ADAPT
        public bool SolarChargingPermitted => solarPermission.IsChargingAllowed;             // ADAPT

        /// <summary>TEST ONLY. Sets a resource directly so failure rules can be tested quickly.</summary>
        public void Debug_SetResource(ResourceType type, float value) =>
            resourceManager.Debug_SetValue(type, value);                                     // ADAPT

        // ------------------------------------------------------------------ inbound events

        private void OnEnable()
        {
            if (facilities != null)
            {
                facilityHandlers = new Action<FacilityStation>[facilities.Length];
                for (int i = 0; i < facilities.Length; i++)
                {
                    FacilityBinding binding = facilities[i];
                    if (binding == null || binding.station == null) continue;
                    string id = binding.facilityId;
                    Action<FacilityStation> handler = _ => FacilityInteracted?.Invoke(id);
                    facilityHandlers[i] = handler;
                    binding.station.Interacted += handler;                                   // ADAPT
                }
            }
            if (solarGenerator != null) solarGenerator.PowerCharged += HandlePowerCharged;   // ADAPT Action<float>
            if (scienceDownload != null) scienceDownload.Succeeded += HandleScience;         // ADAPT Action
            if (shieldReinforcement != null) shieldReinforcement.Succeeded += HandleShield;  // ADAPT Action
        }

        private void OnDisable()
        {
            if (facilities != null && facilityHandlers != null)
            {
                for (int i = 0; i < facilities.Length; i++)
                {
                    if (facilities[i] == null || facilities[i].station == null || facilityHandlers[i] == null) continue;
                    facilities[i].station.Interacted -= facilityHandlers[i];                 // ADAPT
                }
            }
            facilityHandlers = null;
            if (solarGenerator != null) solarGenerator.PowerCharged -= HandlePowerCharged;
            if (scienceDownload != null) scienceDownload.Succeeded -= HandleScience;
            if (shieldReinforcement != null) shieldReinforcement.Succeeded -= HandleShield;
        }

        private void HandlePowerCharged(float amount) => PowerRecharged?.Invoke(amount);
        private void HandleScience() => ScienceDownloaded?.Invoke();
        private void HandleShield() => ShieldReinforced?.Invoke();

        // ------------------------------------------------------------------ helpers

        private bool Run(string label, Action action)
        {
            try { action(); return true; }
            catch (Exception e)
            {
                Debug.LogError($"[MissionGameplayAdapter] {label} step failed: {e.Message}", this);
                return false;
            }
        }
    }
}
