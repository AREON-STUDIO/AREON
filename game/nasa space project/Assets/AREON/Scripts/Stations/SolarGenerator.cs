using System;
using UnityEngine;
using AREON.ResourceSystem;

namespace AREON.Stations
{
    /// <summary>Display state of the generator. Stage 7 drives this from daylight rules.</summary>
    public enum GeneratorStatus
    {
        Offline,
        Standby,
        Generating
    }

    /// <summary>
    /// Power Generator - Solar Generator.
    /// Stage 7: charges Power through ResourceManager when daylight and scenario rules allow it.
    /// </summary>
    public partial class SolarGenerator : FacilityStation
    {
        [Header("Generator Status (display only in Stage 6)")]
        [SerializeField] private GeneratorStatus status = GeneratorStatus.Standby;

        /// <summary>Raised when SetStatus changes the status.</summary>
        public event Action<GeneratorStatus> StatusChanged;

        public GeneratorStatus Status => status;

        protected override string DefaultInteractionName => "Solar Generator";

        protected override FacilityInfo CreateDefaultInfo()
        {
            return new FacilityInfo
            {
                facilityName = "Power Generator",
                purpose = "Solar collection and battery storage. Recharges Power when daylight and mission rules allow it.",
                actionName = "Solar Charging",
                powerCost = 0f,
                statusText = "",
                accentColor = new Color(1.00f, 0.80f, 0.25f, 1f)
            };
        }

        private void OnEnable()
        {
            InitializeCharging();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ShutdownCharging();
        }

        private void Update()
        {
            TickCharging(Time.deltaTime);
        }

        /// <summary>Changes the displayed status.</summary>
        public void SetStatus(GeneratorStatus newStatus)
        {
            if (status == newStatus) return;
            status = newStatus;
            StatusChanged?.Invoke(status);
        }

        public override string GetStatusText()
        {
            if (!string.IsNullOrEmpty(ChargingStatusText))
            {
                return ChargingStatusText;
            }

            switch (status)
            {
                case GeneratorStatus.Generating: return "Power generation: Generating";
                case GeneratorStatus.Offline:    return "Power generation: Offline";
                default:                         return "Power generation: Standby (not charging yet)";
            }
        }
    }
}
