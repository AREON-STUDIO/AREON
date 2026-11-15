// AREON - Stage 8 / Task 37
// Path: Assets/AREON/Scripts/Stations/SolarGenerator.Scenario.cs
//
// Partial class extending SolarGenerator with scenario runtime configuration.

using UnityEngine;

namespace AREON.Stations
{
    public partial class SolarGenerator
    {
        public float RechargePerSecond => rechargeRatePerSecond;

        public void SetRechargePerSecond(float value)
        {
            rechargeRatePerSecond = Mathf.Max(0f, value);
        }
    }
}
