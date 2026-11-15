// AREON - Stage 7 / Task 35
// Path: Assets/AREON/Scripts/Core/ActionEconomyTestHarness.cs
//
// Automated Play Mode checks for the Stage 7 power economy.

using System;
using System.Collections;
using UnityEngine;
using AREON.ResourceSystem;
using AREON.Stations;

public class ActionEconomyTestHarness : MonoBehaviour
{
    [Header("Systems under test (assign in Inspector)")]
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private ScienceDownloadAction scienceDownload;
    [SerializeField] private ShieldReinforcementAction shieldReinforcement;
    [SerializeField] private DaylightManager daylightManager;
    [SerializeField] private SolarGenerator solarGenerator;
    [SerializeField] private SolarChargingPermissionSource permissionSource;

    [Header("Options")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField, Min(0.00001f)] private float tolerance = 0.001f;

    private int passed;
    private int failed;
    private bool running;

    private void Start()
    {
        if (runOnStart)
        {
            RunAll();
        }
    }

    [ContextMenu("Run Action Economy Tests")]
    public void RunAll()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[Stage7Tests] Enter Play Mode first.");
            return;
        }

        if (running)
        {
            return;
        }

        StartCoroutine(RunAllRoutine());
    }

    // =====================================================================================

    private IEnumerator RunAllRoutine()
    {
        running = true;
        passed = 0;
        failed = 0;

        if (!ReferencesValid())
        {
            running = false;
            yield break;
        }

        // ---- Snapshot -----
        float power0 = Power;
        float shield0 = Shield;
        bool repeat0 = scienceDownload.AllowRepeatDownloads;
        SolarChargingMode mode0 = solarGenerator.ChargingMode;
        bool permission0 = permissionSource.IsSolarChargingAllowed;

        Debug.Log("[Stage7Tests] Starting Stage 7 action economy tests...");

        // ---- Same-frame tests -----
        RunScienceTests();
        RunShieldTests();
        RunSolarImmediateTests();

        // ---- Real-time tests -----
        yield return RunDaylightTimerTests();
        yield return RunSolarRealTimeTests();

        // ---- Restore -----
        scienceDownload.ResetDownloads();
        scienceDownload.SetAllowRepeatDownloads(repeat0);
        solarGenerator.SetChargingMode(mode0);
        permissionSource.SetSolarChargingAllowed(permission0);
        daylightManager.StartDaylight();
        SetPower(power0);
        SetShield(shield0);

        string summary = "[Stage7Tests] " + passed + "/" + (passed + failed) + " assertions passed.";
        if (failed == 0)
        {
            Debug.Log(summary + " ALL TESTS PASSED.");
        }
        else
        {
            Debug.LogError(summary + " " + failed + " FAILED - see messages above.");
        }

        running = false;
    }

    // ---- Task 31: science download --------------------------------------------------------

    private void RunScienceTests()
    {
        scienceDownload.ResetDownloads();
        scienceDownload.SetAllowRepeatDownloads(false);

        // S1: exact cost
        SetPower(60f);
        float before = Power;
        ActionResult r = scienceDownload.TryDownload();
        Check(r.Succeeded, "S1 science download succeeds with enough Power");
        Check(Near(before - Power, 15f), "S1 science download costs exactly 15 Power (spent " + (before - Power) + ")");
        Check(Near(r.PowerSpent, 15f), "S1 result reports 15 Power spent");
        Check(scienceDownload.HasDownloadedCurrentPacket, "S1 packet is marked downloaded");

        // S2: duplicate rejected, no spend
        before = Power;
        r = scienceDownload.TryDownload();
        Check(!r.Succeeded && r.Outcome == ActionOutcome.AlreadyDownloaded, "S2 duplicate download is rejected");
        Check(Near(Power, before), "S2 duplicate download spends no Power");

        // S3: insufficient Power rejected, no partial deduction, packet not consumed
        scienceDownload.ResetDownloads();
        SetPower(14.9f);
        before = Power;
        r = scienceDownload.TryDownload();
        Check(!r.Succeeded && r.Outcome == ActionOutcome.InsufficientPower, "S3 download rejected at 14.9 Power");
        Check(Near(Power, before), "S3 no partial Power deduction (Power still " + Power + ")");
        Check(!scienceDownload.HasDownloadedCurrentPacket, "S3 rejected download does not consume the packet");

        // S4: boundary - exactly 15 Power is enough and leaves exactly 0
        SetPower(15f);
        r = scienceDownload.TryDownload();
        Check(r.Succeeded, "S4 download succeeds with exactly 15 Power");
        Check(Near(Power, 0f), "S4 Power is exactly 0 afterwards");

        // S5: repeat downloads only when explicitly enabled
        scienceDownload.ResetDownloads();
        scienceDownload.SetAllowRepeatDownloads(true);
        SetPower(60f);
        bool first = scienceDownload.TryDownload().Succeeded;
        bool second = scienceDownload.TryDownload().Succeeded;
        Check(first && second, "S5 repeat download allowed when explicitly enabled");
        Check(Near(Power, 30f), "S5 each repeat costs 15 Power (Power " + Power + ")");
        scienceDownload.SetAllowRepeatDownloads(false);
        scienceDownload.ResetDownloads();
    }

    // ---- Task 32: shield reinforcement ----------------------------------------------------

    private void RunShieldTests()
    {
        float amount = shieldReinforcement.ShieldRestoreAmount;

        // H1: exact cost + restoration
        SetPower(50f);
        SetShield(40f);
        float powerBefore = Power;
        float shieldBefore = Shield;
        ActionResult r = shieldReinforcement.TryReinforce();
        Check(r.Succeeded, "H1 reinforcement succeeds with enough Power");
        Check(Near(powerBefore - Power, 10f), "H1 reinforcement costs exactly 10 Power (spent " + (powerBefore - Power) + ")");
        Check(Near(Shield - shieldBefore, Mathf.Min(100f - shieldBefore, amount)),
              "H1 Shield restored by the configured amount (" + amount + ")");
        Check(Near(r.AmountApplied, Shield - shieldBefore), "H1 result reports the real Shield change");

        // H2: insufficient Power
        SetPower(9.9f);
        SetShield(40f);
        powerBefore = Power;
        shieldBefore = Shield;
        r = shieldReinforcement.TryReinforce();
        Check(!r.Succeeded && r.Outcome == ActionOutcome.InsufficientPower, "H2 rejected at 9.9 Power");
        Check(Near(Power, powerBefore), "H2 no Power spent");
        Check(Near(Shield, shieldBefore), "H2 Shield unchanged");

        // H3: boundary - exactly 10 Power works
        SetPower(10f);
        SetShield(40f);
        r = shieldReinforcement.TryReinforce();
        Check(r.Succeeded && Near(Power, 0f), "H3 succeeds with exactly 10 Power and leaves 0");

        // H4: full Shield rejected, no Power spent
        SetPower(50f);
        SetShield(100f);
        r = shieldReinforcement.TryReinforce();
        Check(!r.Succeeded && r.Outcome == ActionOutcome.ShieldFull, "H4 full Shield rejects reinforcement");
        Check(Near(Power, 50f), "H4 full Shield spends no Power");
        Check(Near(Shield, 100f), "H4 Shield stays at 100");

        // H5: restoration respects the maximum
        SetPower(50f);
        SetShield(100f - amount * 0.5f);
        r = shieldReinforcement.TryReinforce();
        Check(r.Succeeded, "H5 reinforcement near the top succeeds");
        Check(Near(Shield, 100f), "H5 Shield is clamped to exactly 100 (is " + Shield + ")");
        Check(Near(r.AmountApplied, amount * 0.5f), "H5 reports only the Shield actually restored");
        Check(Near(Power, 40f), "H5 still costs exactly 10 Power");

        // H6: repeated reinforcement never exceeds 100 and the last attempt is free
        SetPower(100f);
        SetShield(10f);
        bool neverAbove100 = true;
        int guard = 0;
        while (guard++ < 20 && shieldReinforcement.TryReinforce().Succeeded)
        {
            if (Shield > 100f + tolerance)
            {
                neverAbove100 = false;
            }
        }

        Check(neverAbove100, "H6 Shield never exceeded 100 across repeated reinforcement");
        Check(Near(Shield, 100f), "H6 repeated reinforcement ends at exactly 100");
        float powerAtFull = Power;
        r = shieldReinforcement.TryReinforce();
        Check(r.Outcome == ActionOutcome.ShieldFull && Near(Power, powerAtFull), "H6 attempt at full Shield costs nothing");
    }

    // ---- Task 34: solar charging, same-frame checks ---------------------------------------

    private void RunSolarImmediateTests()
    {
        permissionSource.SetSolarChargingAllowed(true);
        solarGenerator.SetChargingMode(SolarChargingMode.Automatic);
        float rate = solarGenerator.RechargeRatePerSecond;
        Check(rate > 0f, "L0 recharge rate is configured and positive");

        // L1: charges during daylight
        daylightManager.StartDaylight(600f);
        SetPower(50f);
        float added = solarGenerator.ApplyCharge(1f);
        Check(solarGenerator.IsCharging, "L1 generator reports charging during daylight");
        Check(Near(added, rate) && Near(Power, 50f + rate), "L1 one second adds exactly the recharge rate (" + rate + ")");

        // L2: stops immediately when daylight ends
        daylightManager.ForceEndDaylight();
        Check(!daylightManager.IsDaylightActive, "L2 daylight is no longer active");
        Check(!solarGenerator.IsCharging, "L2 charging stops immediately when daylight expires");
        Check(solarGenerator.ChargeBlockReason == SolarChargeBlockReason.NoDaylight, "L2 block reason is NoDaylight");
        float before = Power;
        added = solarGenerator.ApplyCharge(1f);
        Check(Near(added, 0f) && Near(Power, before), "L2 no Power added at night");

        // L3: scenario permission
        daylightManager.StartDaylight(600f);
        Check(solarGenerator.IsCharging, "L3 charging resumes when daylight restarts");
        permissionSource.SetSolarChargingAllowed(false);
        Check(!solarGenerator.IsCharging, "L3 charging stops immediately when permission is disabled");
        Check(solarGenerator.ChargeBlockReason == SolarChargeBlockReason.ScenarioDisabled, "L3 block reason is ScenarioDisabled");
        before = Power;
        added = solarGenerator.ApplyCharge(1f);
        Check(Near(added, 0f) && Near(Power, before), "L3 no Power added while permission is disabled");
        permissionSource.SetSolarChargingAllowed(true);
        Check(solarGenerator.IsCharging, "L3 charging resumes when permission is restored");

        // L4: Power cap
        SetPower(100f - rate * 0.5f);
        added = solarGenerator.ApplyCharge(1f);
        Check(Near(Power, 100f), "L4 Power reaches exactly 100 and no more (is " + Power + ")");
        Check(Near(added, rate * 0.5f), "L4 only the missing Power is added");
        Check(!solarGenerator.IsCharging && solarGenerator.ChargeBlockReason == SolarChargeBlockReason.PowerFull,
              "L4 charging stops when Power is full");
        added = solarGenerator.ApplyCharge(1f);
        Check(Near(added, 0f) && Near(Power, 100f), "L4 further ticks add nothing");

        // L5: many ticks never exceed 100
        SetPower(90f);
        bool neverAbove100 = true;
        for (int i = 0; i < 50; i++)
        {
            solarGenerator.ApplyCharge(1f);
            if (Power > 100f + tolerance)
            {
                neverAbove100 = false;
            }
        }

        Check(neverAbove100 && Near(Power, 100f), "L5 Power never exceeds 100 over many charge ticks");

        // L6: ManualToggle mode
        SetPower(50f);
        solarGenerator.SetChargingMode(SolarChargingMode.ManualToggle);
        Check(!solarGenerator.IsCharging, "L6 ManualToggle: not charging until the player switches it on");
        before = Power;
        solarGenerator.ApplyCharge(1f);
        Check(Near(Power, before), "L6 ManualToggle: no Power added while switched off");
        solarGenerator.ToggleCharging();
        added = solarGenerator.ApplyCharge(1f);
        Check(solarGenerator.IsCharging && Near(added, rate), "L6 ManualToggle: charges after the player switches it on");
        solarGenerator.ToggleCharging();
        Check(!solarGenerator.IsCharging, "L6 ManualToggle: stops when switched off");
        solarGenerator.SetChargingMode(SolarChargingMode.Automatic);
    }

    // ---- Task 33: daylight timer ---------------------------------------------------------

    private IEnumerator RunDaylightTimerTests()
    {
        int started = 0;
        int ended = 0;
        Action onStarted = () => started++;
        Action onEnded = () => ended++;

        daylightManager.DaylightStarted += onStarted;
        daylightManager.DaylightEnded += onEnded;

        // D1: starts with configured duration
        daylightManager.StartDaylight(300f);
        Check(daylightManager.IsDaylightActive, "D1 daylight is active after start");
        Check(Near(daylightManager.RemainingSeconds, 300f), "D1 remaining seconds matches duration");
        Check(started == 1, "D1 DaylightStarted raised");

        // D2: countdown format helper
        Check(DaylightManager.FormatTime(299f) == "04:59", "D2 299 s -> 04:59");
        Check(DaylightManager.FormatTime(300f) == "05:00", "D2 300 s -> 05:00");
        Check(DaylightManager.FormatTime(0f) == "00:00", "D2 0 s -> 00:00");
        Check(DaylightManager.FormatTime(-5f) == "00:00", "D2 negative seconds clamp to 00:00");
        Check(daylightManager.GetDisplayText().StartsWith(DaylightManager.CountdownPrefix), "D2 display text has prefix");

        // D3: short 2-second run to real expiry
        daylightManager.StartDaylight(2f);
        float t0 = Time.realtimeSinceStartup;
        while (daylightManager.IsDaylightActive && Time.realtimeSinceStartup - t0 < 4f)
        {
            yield return null;
        }

        Check(!daylightManager.IsDaylightActive, "D3 daylight ended naturally");
        Check(Near(daylightManager.RemainingSeconds, 0f), "D3 remaining is exactly 0");
        Check(ended >= 1, "D3 DaylightEnded raised");

        // D4: stays at 0 and does not tick further
        yield return null;
        Check(Near(daylightManager.RemainingSeconds, 0f), "D4 remaining stays 0 on next frame");
        Check(daylightManager.GetDisplayText() == DaylightManager.NightMessage, "D4 display text is NightMessage");

        daylightManager.DaylightStarted -= onStarted;
        daylightManager.DaylightEnded -= onEnded;
    }

    // ---- Task 34: real-time solar charging ------------------------------------------------

    private IEnumerator RunSolarRealTimeTests()
    {
        permissionSource.SetSolarChargingAllowed(true);
        solarGenerator.SetChargingMode(SolarChargingMode.Automatic);
        float rate = solarGenerator.RechargeRatePerSecond;

        // R1: 1 second of real charging adds ~1 second of rate
        SetPower(40f);
        daylightManager.StartDaylight(60f);
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 1.05f)
        {
            yield return null;
        }

        Check(Power > 40f, "R1 real-time solar delivered Power during daylight (now " + Power + ")");

        // R2: real-time expiry stops delivery
        daylightManager.StartDaylight(1f);
        while (daylightManager.IsDaylightActive)
        {
            yield return null;
        }

        float powerAtNight = Power;
        yield return new WaitForSeconds(0.3f);
        // Note: Stage 4 continuous depletion may slowly drain Power, so we only assert that
        // it did NOT increase.
        Check(Power <= powerAtNight + tolerance, "R2 no solar delivery after daylight expired");

        // R3: permission toggle while running
        daylightManager.StartDaylight(60f);
        permissionSource.SetSolarChargingAllowed(false);
        yield return null;
        Check(!solarGenerator.IsCharging, "R3 generator stopped immediately on permission toggle");
        permissionSource.SetSolarChargingAllowed(true);
        yield return null;
        Check(solarGenerator.IsCharging, "R3 generator resumed on permission re-enable");
    }

    // ---- Helpers --------------------------------------------------------------------------

    private float Power => ResourceManagerBridge.GetPower(resourceManager);
    private float Shield => ResourceManagerBridge.GetShield(resourceManager);

    private void SetPower(float value)
    {
        ResourceManagerBridge.SetValueForTests(resourceManager, ResourceType.Power, value);
    }

    private void SetShield(float value)
    {
        ResourceManagerBridge.SetValueForTests(resourceManager, ResourceType.Shield, value);
    }

    private bool Near(float a, float b)
    {
        return Mathf.Abs(a - b) <= tolerance;
    }

    private void Check(bool condition, string message)
    {
        if (condition)
        {
            passed++;
        }
        else
        {
            failed++;
            Debug.LogError("[Stage7Tests] FAIL: " + message, this);
        }
    }

    private bool ReferencesValid()
    {
        bool ok = true;
        if (resourceManager == null) ok = LogMissing(nameof(resourceManager));
        if (scienceDownload == null) ok = LogMissing(nameof(scienceDownload));
        if (shieldReinforcement == null) ok = LogMissing(nameof(shieldReinforcement));
        if (daylightManager == null) ok = LogMissing(nameof(daylightManager));
        if (solarGenerator == null) ok = LogMissing(nameof(solarGenerator));
        if (permissionSource == null) ok = LogMissing(nameof(permissionSource));
        return ok;
    }

    private bool LogMissing(string field)
    {
        Debug.LogError("[Stage7Tests] Missing required reference: " + field, this);
        return false;
    }
}
