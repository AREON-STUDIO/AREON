using System;
using UnityEngine;

namespace AREON.ResourceSystem
{
    /// <summary>
    /// The single authoritative owner of AREON's runtime Oxygen, Power and Shield values.
    ///
    /// Rules enforced here (and nowhere else):
    ///  * Every value is always clamped to [MinValue, MaxValue] = [0, 100].
    ///  * Power is spent atomically: an unaffordable request changes nothing.
    ///  * Oxygen and Shield deplete continuously using Time.deltaTime.
    ///  * Drain multipliers (scenario/weather) are applied in ONE place:
    ///    EffectiveXDrainPerSecond = base rate * multiplier.
    ///
    /// Other systems (stations, HUD, mission logic) must call the methods below and
    /// subscribe to the events. They must never keep their own copy of these values.
    ///
    /// All rates are in PERCENTAGE POINTS PER SECOND.
    /// The default rates are PROTOTYPE TUNING VALUES that need playtesting; they are
    /// not specified by the source description or the blueprint.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // Initialise before other scripts read values in Start/OnEnable.
    public class ResourceManager : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        /// <summary>Lowest legal value of any resource.</summary>
        public const float MinValue = 0f;

        /// <summary>Highest legal value (capacity) of any resource.</summary>
        public const float MaxValue = 100f;

        // ------------------------------------------------------------------
        // Event types
        // ------------------------------------------------------------------

        /// <summary>Raised after a resource value actually changed.</summary>
        public delegate void ResourceChangedHandler(ResourceType type, float oldValue, float newValue);

        /// <summary>Raised when a Power spend was refused because Power was insufficient.</summary>
        public delegate void PowerSpendRejectedHandler(float requestedAmount, float availablePower);

        // ------------------------------------------------------------------
        // Inspector configuration
        // ------------------------------------------------------------------

        [Header("Initial Values (0-100). Prototype defaults from the website HUD example.")]
        [SerializeField, Range(MinValue, MaxValue)] private float initialOxygen = 88f;
        [SerializeField, Range(MinValue, MaxValue)] private float initialPower = 64f;
        [SerializeField, Range(MinValue, MaxValue)] private float initialShield = 72f;

        [Header("Continuous Depletion (percentage points per second) - PROTOTYPE TUNING, needs playtesting")]
        [Tooltip("When false, Oxygen and Shield do not deplete over time.")]
        [SerializeField] private bool depletionEnabled = true;

        [Tooltip("Baseline Oxygen drain in percentage points per second (prototype value).")]
        [SerializeField, Min(0f)] private float oxygenDrainPerSecond = 0.5f;

        [Tooltip("Multiplier applied to the baseline Oxygen drain (later set by mission scenario, e.g. 1.5 for Sol 2085).")]
        [SerializeField, Min(0f)] private float oxygenDrainMultiplier = 1f;

        [Tooltip("Baseline Shield drain in percentage points per second (prototype value).")]
        [SerializeField, Min(0f)] private float shieldDrainPerSecond = 0.4f;

        [Tooltip("Multiplier applied to the baseline Shield drain (later set by mission scenario, e.g. 2.0 for Sol 852 and Sol 2085).")]
        [SerializeField, Min(0f)] private float shieldDrainMultiplier = 1f;

        [Header("Diagnostics")]
        [Tooltip("Log a warning when a caller passes an invalid amount (negative, NaN, infinity).")]
        [SerializeField] private bool logInvalidRequests = true;

        [Tooltip("Log every resource change. Very noisy while depletion is running; leave off normally.")]
        [SerializeField] private bool verboseLogging = false;

        // ------------------------------------------------------------------
        // Runtime state (the ONLY copy of these values in the project)
        // ------------------------------------------------------------------

        private float oxygen;
        private float power;
        private float shield;

        // ------------------------------------------------------------------
        // Events (observation API)
        // ------------------------------------------------------------------

        /// <summary>
        /// Raised after any resource value changes (including depletion every frame).
        /// Parameters: which resource, its old value, its new value.
        /// Subscribe in OnEnable and unsubscribe in OnDisable. Read the current value
        /// directly from this manager when you subscribe; no event is replayed.
        /// </summary>
        public event ResourceChangedHandler ResourceChanged;

        /// <summary>
        /// Raised once when a resource transitions from above zero to exactly zero.
        /// This is only a notification. It does not cause mission failure by itself.
        /// </summary>
        public event Action<ResourceType> ResourceDepleted;

        /// <summary>Raised when TrySpendPower refuses an unaffordable request. Nothing was spent.</summary>
        public event PowerSpendRejectedHandler PowerSpendRejected;

        // ------------------------------------------------------------------
        // Read-only public state
        // ------------------------------------------------------------------

        public float Oxygen => oxygen;
        public float Power => power;
        public float Shield => shield;

        /// <summary>Configured starting values (Inspector). Used by ResetToInitialValues.</summary>
        public float InitialOxygen => initialOxygen;
        public float InitialPower => initialPower;
        public float InitialShield => initialShield;

        public bool DepletionEnabled => depletionEnabled;
        public float OxygenDrainPerSecond => oxygenDrainPerSecond;
        public float OxygenDrainMultiplier => oxygenDrainMultiplier;
        public float ShieldDrainPerSecond => shieldDrainPerSecond;
        public float ShieldDrainMultiplier => shieldDrainMultiplier;

        // ---- Stage 8: scenario configuration -------------------------------------------------
        public float OxygenBaseDrainPerSecond => oxygenDrainPerSecond;
        public float ShieldBaseDrainPerSecond => shieldDrainPerSecond;

        /// <summary>Current value of the given resource (alias for GetValue).</summary>
        public float GetCurrent(ResourceType type) => GetValue(type);

        /// <summary>Baseline rate * multiplier. This is the rate actually applied each second.</summary>
        public float EffectiveOxygenDrainPerSecond => oxygenDrainPerSecond * oxygenDrainMultiplier;

        /// <summary>Baseline rate * multiplier. This is the rate actually applied each second.</summary>
        public float EffectiveShieldDrainPerSecond => shieldDrainPerSecond * shieldDrainMultiplier;

        public bool LogInvalidRequests => logInvalidRequests;

        /// <summary>Current value of the given resource (0-100).</summary>
        public float GetValue(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Oxygen: return oxygen;
                case ResourceType.Power: return power;
                case ResourceType.Shield: return shield;
                default: return MinValue;
            }
        }

        /// <summary>Current value of the given resource as 0..1 (handy for UI fill bars).</summary>
        public float GetNormalized(ResourceType type)
        {
            return Mathf.InverseLerp(MinValue, MaxValue, GetValue(type));
        }

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            SanitizeConfiguration();
            ResetToInitialValues();
        }

        private void Update()
        {
            if (!depletionEnabled)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return; // Paused (timeScale 0) or first-frame edge case.
            }

            ApplyDepletion(ResourceType.Oxygen, EffectiveOxygenDrainPerSecond * deltaTime);
            ApplyDepletion(ResourceType.Shield, EffectiveShieldDrainPerSecond * deltaTime);
        }

        private void OnDestroy()
        {
            // Drop all subscribers so nothing can be called on a destroyed manager's behalf.
            ResourceChanged = null;
            ResourceDepleted = null;
            PowerSpendRejected = null;
        }

        private void OnValidate()
        {
            SanitizeConfiguration();
        }

        // ------------------------------------------------------------------
        // Initialisation
        // ------------------------------------------------------------------

        /// <summary>
        /// Sets all three current values at once (each is clamped to 0-100).
        /// Intended for mission start/restart. Raises ResourceChanged for each value that changed.
        /// Returns false (and changes nothing) if any argument is NaN or infinite.
        /// </summary>
        public bool InitializeResources(float newOxygen, float newPower, float newShield)
        {
            if (!IsFinite(newOxygen) || !IsFinite(newPower) || !IsFinite(newShield))
            {
                Debug.LogError("[ResourceManager] InitializeResources rejected: values must be finite numbers.", this);
                return false;
            }

            SetValue(ResourceType.Oxygen, newOxygen);
            SetValue(ResourceType.Power, newPower);
            SetValue(ResourceType.Shield, newShield);
            return true;
        }

        /// <summary>Resets all current values to the Inspector's initial values.</summary>
        public void ResetToInitialValues()
        {
            InitializeResources(initialOxygen, initialPower, initialShield);
        }

        // ------------------------------------------------------------------
        // Power API
        // ------------------------------------------------------------------

        /// <summary>True if the amount is valid and the current Power can pay for it. Never changes state.</summary>
        public bool CanAffordPower(float amount)
        {
            return IsFinite(amount) && amount >= 0f && amount <= power;
        }

        /// <summary>
        /// Spends Power atomically.
        /// Returns true if the full amount was deducted (an amount of 0 succeeds and changes nothing).
        /// Returns false, deducts NOTHING and raises PowerSpendRejected if Power is insufficient.
        /// Returns false and deducts nothing for an invalid amount (negative, NaN, infinity).
        /// </summary>
        public bool TrySpendPower(float amount)
        {
            if (!IsValidAmount(amount, nameof(TrySpendPower)))
            {
                return false;
            }

            if (amount == 0f)
            {
                return true;
            }

            if (amount > power)
            {
                PowerSpendRejected?.Invoke(amount, power);
                return false;
            }

            SetValue(ResourceType.Power, power - amount);
            return true;
        }

        /// <summary>
        /// Adds Power, clamped to MaxValue.
        /// Returns true only if some Power was actually added.
        /// Returns false for an invalid/zero amount or when Power is already full.
        /// </summary>
        public bool TryRechargePower(float amount)
        {
            return TryIncrease(ResourceType.Power, amount, out _, nameof(TryRechargePower));
        }

        /// <summary>Same as TryRechargePower but also reports how much was really added (after clamping).</summary>
        public bool TryRechargePower(float amount, out float appliedAmount)
        {
            return TryIncrease(ResourceType.Power, amount, out appliedAmount, nameof(TryRechargePower));
        }

        // ------------------------------------------------------------------
        // Shield API
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds Shield, clamped to MaxValue. Does NOT spend Power; the caller is responsible
        /// for calling TrySpendPower first (Stage 6/7 will do this).
        /// Returns true only if some Shield was actually added.
        /// </summary>
        public bool TryRestoreShield(float amount)
        {
            return TryIncrease(ResourceType.Shield, amount, out _, nameof(TryRestoreShield));
        }

        /// <summary>Same as TryRestoreShield but also reports how much was really added (after clamping).</summary>
        public bool TryRestoreShield(float amount, out float appliedAmount)
        {
            return TryIncrease(ResourceType.Shield, amount, out appliedAmount, nameof(TryRestoreShield));
        }

        // ------------------------------------------------------------------
        // Depletion configuration API (for scenario/weather systems and tests)
        // ------------------------------------------------------------------

        /// <summary>Enables or disables continuous Oxygen and Shield depletion.</summary>
        public void SetDepletionEnabled(bool enabledValue)
        {
            depletionEnabled = enabledValue;
        }

        /// <summary>Sets the baseline drain rates (percentage points per second). Both must be finite and >= 0.</summary>
        public bool SetBaseDrainRates(float oxygenPerSecond, float shieldPerSecond)
        {
            if (!IsNonNegativeFinite(oxygenPerSecond) || !IsNonNegativeFinite(shieldPerSecond))
            {
                Debug.LogError("[ResourceManager] SetBaseDrainRates rejected: rates must be finite and >= 0.", this);
                return false;
            }

            oxygenDrainPerSecond = oxygenPerSecond;
            shieldDrainPerSecond = shieldPerSecond;
            return true;
        }

        /// <summary>Sets the BASELINE (un-multiplied) depletion rates in percentage points per second.</summary>
        public void SetBaselineDrainRates(float oxygenPerSecond, float shieldPerSecond)
        {
            SetBaseDrainRates(oxygenPerSecond, shieldPerSecond);
        }

        // ---- Stage 9: mission simulation gate and scenario initialisation --------------------
        /// <summary>Enables or disables continuous depletion. Alias for SetDepletionEnabled.</summary>
        public void SetSimulationActive(bool active) => SetDepletionEnabled(active);

        /// <summary>Copies scenario starting values and drain configuration into runtime state.</summary>
        public void InitializeFromScenario(MissionScenarioData data)
        {
            if (data == null) return;
            SetBaselineDrainRates(data.OxygenDrainPerSecond, data.ShieldDrainPerSecond);
            SetDrainMultipliers(data.OxygenDrainMultiplier, data.ShieldDrainMultiplier);
            InitializeResources(data.InitialOxygen, data.InitialPower, data.InitialShield);
        }

        /// <summary>TEST ONLY. Directly sets a resource value, clamped to [0, 100].</summary>
        public void Debug_SetValue(ResourceType type, float value) => SetValue(type, value);

        /// <summary>Sets the drain multipliers (the one place scenario multipliers are applied). Both must be finite and >= 0.</summary>
        public bool SetDrainMultipliers(float oxygenMultiplier, float shieldMultiplier)
        {
            if (!IsNonNegativeFinite(oxygenMultiplier) || !IsNonNegativeFinite(shieldMultiplier))
            {
                Debug.LogError("[ResourceManager] SetDrainMultipliers rejected: multipliers must be finite and >= 0.", this);
                return false;
            }

            oxygenDrainMultiplier = oxygenMultiplier;
            shieldDrainMultiplier = shieldMultiplier;
            return true;
        }

        /// <summary>Turns the invalid-request warning on or off (used by the temporary test harness).</summary>
        public void SetInvalidRequestLogging(bool enabledValue)
        {
            logInvalidRequests = enabledValue;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void ApplyDepletion(ResourceType type, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            float current = GetValue(type);
            if (current <= MinValue)
            {
                return;
            }

            SetValue(type, current - amount);
        }

        private bool TryIncrease(ResourceType type, float amount, out float appliedAmount, string callerName)
        {
            appliedAmount = 0f;

            if (!IsValidAmount(amount, callerName))
            {
                return false;
            }

            float current = GetValue(type);
            if (amount == 0f || current >= MaxValue)
            {
                return false;
            }

            float target = Mathf.Min(MaxValue, current + amount);
            appliedAmount = target - current;
            SetValue(type, target);
            return true;
        }

        /// <summary>
        /// The ONLY place a resource value is written. Clamps, stores, then notifies.
        /// Returns true if the stored value changed.
        /// </summary>
        private bool SetValue(ResourceType type, float requestedValue)
        {
            float newValue = Mathf.Clamp(requestedValue, MinValue, MaxValue);
            float oldValue = GetValue(type);

            if (newValue == oldValue)
            {
                return false;
            }

            switch (type)
            {
                case ResourceType.Oxygen: oxygen = newValue; break;
                case ResourceType.Power: power = newValue; break;
                case ResourceType.Shield: shield = newValue; break;
                default: return false;
            }

            if (verboseLogging)
            {
                Debug.Log($"[ResourceManager] {type}: {oldValue:F2} -> {newValue:F2}", this);
            }

            // State is already updated, so subscribers may safely read or call back into the manager.
            ResourceChanged?.Invoke(type, oldValue, newValue);

            if (newValue <= MinValue && oldValue > MinValue)
            {
                ResourceDepleted?.Invoke(type);
            }

            return true;
        }

        private bool IsValidAmount(float amount, string callerName)
        {
            if (IsFinite(amount) && amount >= 0f)
            {
                return true;
            }

            if (logInvalidRequests)
            {
                Debug.LogWarning($"[ResourceManager] {callerName} rejected invalid amount ({amount}). Amount must be a finite number >= 0.", this);
            }

            return false;
        }

        private void SanitizeConfiguration()
        {
            initialOxygen = SanitizePercent(initialOxygen);
            initialPower = SanitizePercent(initialPower);
            initialShield = SanitizePercent(initialShield);

            oxygenDrainPerSecond = SanitizeRate(oxygenDrainPerSecond);
            oxygenDrainMultiplier = SanitizeRate(oxygenDrainMultiplier);
            shieldDrainPerSecond = SanitizeRate(shieldDrainPerSecond);
            shieldDrainMultiplier = SanitizeRate(shieldDrainMultiplier);
        }

        private static float SanitizePercent(float value)
        {
            return IsFinite(value) ? Mathf.Clamp(value, MinValue, MaxValue) : MinValue;
        }

        private static float SanitizeRate(float value)
        {
            return IsNonNegativeFinite(value) ? value : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return IsFinite(value) && value >= 0f;
        }
    }
}
