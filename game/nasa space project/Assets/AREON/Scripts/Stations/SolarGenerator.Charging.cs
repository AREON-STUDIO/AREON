// AREON - Stage 7 / Task 34
// Path: Assets/AREON/Scripts/Stations/SolarGenerator.Charging.cs
//
// Extends the EXISTING Stage 6 SolarGenerator (a `partial class`) with solar charging.

using System;
using UnityEngine;
using AREON.ResourceSystem;

namespace AREON.Stations
{
    public enum SolarChargingMode
    {
        Automatic,     // Charges whenever every condition is met. (Default)
        ManualToggle   // Also requires the player to switch charging on via ToggleCharging().
    }

    public enum SolarChargeBlockReason
    {
        None,
        MissingReferences,
        NotActivatedByPlayer,
        NoDaylight,
        ScenarioDisabled,
        PowerFull
    }

    public partial class SolarGenerator
    {
        [Header("Stage 7 - Solar charging")]
        [SerializeField] private ResourceManager chargingResourceManager;
        [SerializeField] private DaylightManager daylightManager;

        [Tooltip("Any component implementing ISolarChargingPermission. Stage 7: SolarChargingPermissionSource. " +
                 "Stage 8: the MissionScenarioData-backed permission component.")]
        [SerializeField] private MonoBehaviour permissionSource;

        [Tooltip("Power units per second. PROTOTYPE TUNING VALUE - requires playtesting.")]
        [SerializeField, Min(0f)] private float rechargeRatePerSecond = 2f;

        [SerializeField] private SolarChargingMode chargingMode = SolarChargingMode.Automatic;

        private ISolarChargingPermission scenarioPermission;
        private bool playerActivatedCharging;
        private bool chargingSubscribed;
        private float totalPowerDelivered;

        // ---- Public read API --------------------------------------------------------------

        /// <summary>True while every charging condition is currently met.</summary>
        public bool IsCharging { get; private set; }

        public SolarChargeBlockReason ChargeBlockReason { get; private set; } = SolarChargeBlockReason.MissingReferences;
        public float RechargeRatePerSecond => rechargeRatePerSecond;
        public SolarChargingMode ChargingMode => chargingMode;
        public bool PlayerActivatedCharging => playerActivatedCharging;

        /// <summary>Sum of Power this generator has actually delivered (measured, never invented).</summary>
        public float TotalPowerDelivered => totalPowerDelivered;

        /// <summary>Raised when IsCharging flips. Argument = new IsCharging value.</summary>
        public event Action<bool> ChargingStateChanged;

        /// <summary>Raised with the Power amount added each tick (Stage 9 objective tracking).</summary>
        public event Action<float> PowerCharged;

        /// <summary>One-line status for the facility panel / world label.</summary>
        public string ChargingStatusText
        {
            get
            {
                switch (ChargeBlockReason)
                {
                    case SolarChargeBlockReason.None:
                        return "Solar charging: +" + rechargeRatePerSecond.ToString("0.#") + " Power/s";
                    case SolarChargeBlockReason.NoDaylight:
                        return "Night - solar charging offline";
                    case SolarChargeBlockReason.ScenarioDisabled:
                        return scenarioPermission != null
                            ? scenarioPermission.SolarChargingBlockedReason
                            : "Scenario blocks solar charging.";
                    case SolarChargeBlockReason.PowerFull:
                        return "Battery full - charging stopped";
                    case SolarChargeBlockReason.NotActivatedByPlayer:
                        return "Solar charging is switched off";
                    default:
                        return "Solar generator is not configured";
                }
            }
        }

        // ---- Hooks called from SolarGenerator.cs ------------------------------------------

        /// <summary>Call from OnEnable: validates references and subscribes to events.</summary>
        public void InitializeCharging()
        {
            ResolvePermissionFromInspector();
            ValidateChargingReferences();
            SubscribeChargingEvents();
            RefreshChargingState();
        }

        /// <summary>Call from OnDisable: unsubscribes from every event.</summary>
        public void ShutdownCharging()
        {
            UnsubscribeChargingEvents();
        }

        /// <summary>Call from Update with Time.deltaTime.</summary>
        public void TickCharging(float deltaSeconds)
        {
            ApplyCharge(deltaSeconds);
        }

        // ---- Charging ---------------------------------------------------------------------

        /// <summary>
        /// Tries to charge for deltaSeconds and returns the Power actually added (0 if blocked).
        /// Public so tests can drive it deterministically.
        /// </summary>
        public float ApplyCharge(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                RefreshChargingState();
                return 0f;
            }

            if (EvaluateBlockReason() != SolarChargeBlockReason.None)
            {
                RefreshChargingState();
                return 0f;
            }

            float requested = rechargeRatePerSecond * deltaSeconds;
            float added = ResourceManagerBridge.RechargePower(chargingResourceManager, requested);
            totalPowerDelivered += added;
            if (added > 0f) PowerCharged?.Invoke(added);

            // Re-evaluate right away so reaching 100 Power stops charging in the same call.
            RefreshChargingState();
            return added;
        }

        /// <summary>ManualToggle mode only: the player switches charging on/off.</summary>
        public void ToggleCharging()
        {
            playerActivatedCharging = !playerActivatedCharging;
            RefreshChargingState();
        }

        public void SetChargingMode(SolarChargingMode mode)
        {
            chargingMode = mode;
            playerActivatedCharging = false;
            RefreshChargingState();
        }

        public void SetRechargeRate(float unitsPerSecond)
        {
            rechargeRatePerSecond = Mathf.Max(0f, unitsPerSecond);
        }

        /// <summary>Stage 8 can inject a MissionScenarioData-backed permission at runtime.</summary>
        public void SetPermissionSource(ISolarChargingPermission permission)
        {
            UnsubscribeChargingEvents();
            scenarioPermission = permission;
            if (isActiveAndEnabled)
            {
                SubscribeChargingEvents();
            }

            RefreshChargingState();
        }

        // ---- Internals --------------------------------------------------------------------

        private SolarChargeBlockReason EvaluateBlockReason()
        {
            if (chargingResourceManager == null || daylightManager == null || scenarioPermission == null)
            {
                return SolarChargeBlockReason.MissingReferences;
            }

            if (chargingMode == SolarChargingMode.ManualToggle && !playerActivatedCharging)
            {
                return SolarChargeBlockReason.NotActivatedByPlayer;
            }

            if (!daylightManager.IsDaylightActive)
            {
                return SolarChargeBlockReason.NoDaylight;
            }

            if (!scenarioPermission.IsSolarChargingAllowed)
            {
                return SolarChargeBlockReason.ScenarioDisabled;
            }

            if (ResourceManagerBridge.IsPowerFull(chargingResourceManager))
            {
                return SolarChargeBlockReason.PowerFull;
            }

            return SolarChargeBlockReason.None;
        }

        private void RefreshChargingState()
        {
            ChargeBlockReason = EvaluateBlockReason();
            bool nowCharging = ChargeBlockReason == SolarChargeBlockReason.None;

            if (nowCharging == IsCharging)
            {
                return;
            }

            IsCharging = nowCharging;
            ChargingStateChanged?.Invoke(IsCharging);
        }

        private void ResolvePermissionFromInspector()
        {
            if (scenarioPermission != null || permissionSource == null)
            {
                return;
            }

            scenarioPermission = permissionSource as ISolarChargingPermission;
            if (scenarioPermission == null)
            {
                Debug.LogError("[SolarGenerator] 'Permission Source' must be a component that implements " +
                               "ISolarChargingPermission.", this);
            }
        }

        private void ValidateChargingReferences()
        {
            if (chargingResourceManager == null)
            {
                Debug.LogError("[SolarGenerator] Charging Resource Manager is not assigned.", this);
            }

            if (daylightManager == null)
            {
                Debug.LogError("[SolarGenerator] Daylight Manager is not assigned.", this);
            }

            if (scenarioPermission == null && permissionSource == null)
            {
                Debug.LogError("[SolarGenerator] Permission Source is not assigned. Solar charging stays " +
                               "disabled until it is.", this);
            }
        }

        private void SubscribeChargingEvents()
        {
            if (chargingSubscribed)
            {
                return;
            }

            if (daylightManager != null)
            {
                daylightManager.DaylightStarted += OnChargingConditionChanged;
                daylightManager.DaylightEnded += OnChargingConditionChanged;
            }

            if (scenarioPermission != null)
            {
                scenarioPermission.SolarChargingPermissionChanged += OnChargingConditionChanged;
            }

            chargingSubscribed = true;
        }

        private void UnsubscribeChargingEvents()
        {
            if (!chargingSubscribed)
            {
                return;
            }

            if (daylightManager != null)
            {
                daylightManager.DaylightStarted -= OnChargingConditionChanged;
                daylightManager.DaylightEnded -= OnChargingConditionChanged;
            }

            if (scenarioPermission != null)
            {
                scenarioPermission.SolarChargingPermissionChanged -= OnChargingConditionChanged;
            }

            chargingSubscribed = false;
        }

        private void OnChargingConditionChanged()
        {
            RefreshChargingState();
        }
    }
}
