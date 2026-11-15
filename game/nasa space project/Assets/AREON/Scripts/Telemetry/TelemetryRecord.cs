namespace AREON.Missions
{
    /// <summary>
    /// Immutable snapshot of one curated scenario record (blueprint 1.7 CSV):
    /// sol, pressure_pa, solar_flare, storm_status. Built from MissionScenarioData; never live data.
    /// </summary>
    public readonly struct TelemetryRecord
    {
        public readonly int Sol;
        public readonly string DisplayName;
        public readonly float PressurePa;
        public readonly SolarFlareClass SolarFlare;
        public readonly StormStatus Storm;
        /// <summary>"SAFE" or "WARNING" - matches the curated CSV storm_status column.</summary>
        public readonly string AlertLevel;

        public TelemetryRecord(int sol, string displayName, float pressurePa,
                               SolarFlareClass flare, StormStatus storm)
        {
            Sol = sol;
            DisplayName = displayName;
            PressurePa = pressurePa;
            SolarFlare = flare;
            Storm = storm;
            AlertLevel = (storm != StormStatus.Clear || flare != SolarFlareClass.None) ? "WARNING" : "SAFE";
        }
    }
}
