// AREON - Stage 7 / Task 34
// Path: Assets/AREON/Scripts/Environment/SolarChargingPermissionSource.cs
//
// TEMPORARY permission provider used until MissionScenarioData exists (Stage 8).
// It is only an Inspector toggle + event. It owns no Power values and no scenario data.
// Stage 8 replaces it by assigning a different ISolarChargingPermission component to
// SolarGenerator; this script can then be deleted or kept for tests.

using System;
using UnityEngine;

public class SolarChargingPermissionSource : MonoBehaviour, ISolarChargingPermission
{
    [Header("Temporary scenario permission (Stage 8 replaces this with MissionScenarioData)")]
    [SerializeField] private bool solarChargingAllowed = true;

    [SerializeField] private string blockedReason = "Scenario blocks solar charging.";

    private bool lastAnnouncedValue;
    private bool announcedValueInitialised;

    public bool IsSolarChargingAllowed => solarChargingAllowed;
    public string SolarChargingBlockedReason => blockedReason;

    public event Action SolarChargingPermissionChanged;

    private void Awake()
    {
        lastAnnouncedValue = solarChargingAllowed;
        announcedValueInitialised = true;
    }

    /// <summary>Changes the permission and notifies listeners only if the value really changed.</summary>
    public void SetSolarChargingAllowed(bool allowed)
    {
        solarChargingAllowed = allowed;
        AnnounceIfChanged();
    }

    // ---- Stage 8: scenario configuration -------------------------------------------------
    public void SetChargingAllowed(bool allowed, string deniedReason)
    {
        solarChargingAllowed = allowed;
        blockedReason = allowed ? string.Empty : deniedReason;
        AnnounceIfChanged();
    }

    // ---- Stage 9: mission scenario configuration -----------------------------------------
    public bool IsChargingAllowed => IsSolarChargingAllowed;

    public void ApplyScenario(MissionScenarioData data)
    {
        if (data == null) return;
        SetChargingAllowed(data.SolarChargingAllowed, data.SolarChargingAllowed ? string.Empty : "Scenario blocks solar charging.");
    }

    // Lets you tick the checkbox in the Inspector during Play Mode and see charging react.
    private void OnValidate()
    {
        if (Application.isPlaying && announcedValueInitialised)
        {
            AnnounceIfChanged();
        }
    }

    private void AnnounceIfChanged()
    {
        if (!announcedValueInitialised || lastAnnouncedValue == solarChargingAllowed)
        {
            return;
        }

        lastAnnouncedValue = solarChargingAllowed;
        SolarChargingPermissionChanged?.Invoke();
    }
}
