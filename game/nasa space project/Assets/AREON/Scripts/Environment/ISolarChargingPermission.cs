// AREON - Stage 7 / Task 34
// Path: Assets/AREON/Scripts/Environment/ISolarChargingPermission.cs
//
// The integration hook between SolarGenerator and "whatever decides if the active scenario
// allows solar charging".
//
// Stage 7:  SolarChargingPermissionSource (a plain Inspector toggle) implements this.
// Stage 8:  a small component that reads MissionScenarioData.SolarChargingAllowed will
//           implement this same interface. SolarGenerator is NOT edited again - the new
//           component is simply dragged into its "Permission Source" slot.

using System;

public interface ISolarChargingPermission
{
    /// <summary>True when the active scenario permits solar charging (e.g. Sol 800: yes, Sol 2085: no).</summary>
    bool IsSolarChargingAllowed { get; }

    /// <summary>Short player-facing reason shown when charging is not allowed.</summary>
    string SolarChargingBlockedReason { get; }

    /// <summary>Raised whenever IsSolarChargingAllowed changes, so charging can stop immediately.</summary>
    event Action SolarChargingPermissionChanged;
}
