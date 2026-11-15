// PATH: Assets/AREON/Scripts/Environment/ScenarioSwitchTestHarness.cs
// TEMPORARY Stage 8 test tool. Not a mission-selection system (that is Stage 9).

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using AREON.ResourceSystem;
using AREON.Stations;
using AREON.UI;

/// <summary>
/// Switches between the three scenarios and verifies Stage 8 behaviour with hard-coded expectations
/// (independent from the asset values, so a wrong asset is detected).
///
/// Keys (when enabled):  F6 = Sol 800   F7 = Sol 852   F8 = Sol 2085   F9 = run automated tests.
/// Context menu: "Apply Sol 800/852/2085", "Run Scenario Tests".
/// </summary>
public class ScenarioSwitchTestHarness : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private DaylightManager daylightManager;
    [SerializeField] private SolarChargingPermissionSource solarPermissionSource;
    [SerializeField] private SolarGenerator solarGenerator;
    [SerializeField] private HUDController hud;

    [Header("Scenario assets")]
    [SerializeField] private MissionScenarioData sol800;
    [SerializeField] private MissionScenarioData sol852;
    [SerializeField] private MissionScenarioData sol2085;

    [Header("Test settings")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField, Min(1f)] private float drainSampleSeconds = 4f;
    [SerializeField, Range(0.05f, 0.5f)] private float drainTolerance = 0.2f;

    private int passed;
    private int failed;
    private bool running;

    private void Update()
    {
        if (!enableKeyboardShortcuts || Keyboard.current == null || running) return;
        if (Keyboard.current.f6Key.wasPressedThisFrame) Apply(sol800);
        else if (Keyboard.current.f7Key.wasPressedThisFrame) Apply(sol852);
        else if (Keyboard.current.f8Key.wasPressedThisFrame) Apply(sol2085);
        else if (Keyboard.current.f9Key.wasPressedThisFrame) RunScenarioTests();
    }

    [ContextMenu("Apply Sol 800")] public void ApplySol800() => Apply(sol800);
    [ContextMenu("Apply Sol 852")] public void ApplySol852() => Apply(sol852);
    [ContextMenu("Apply Sol 2085")] public void ApplySol2085() => Apply(sol2085);

    private void Apply(MissionScenarioData data)
    {
        if (weatherManager == null) { Debug.LogError("[ScenarioTest] WeatherManager reference missing.", this); return; }
        weatherManager.ApplyScenario(data);
    }

    [ContextMenu("Run Scenario Tests")]
    public void RunScenarioTests()
    {
        if (running) return;
        StartCoroutine(RunRoutine());
    }

    // ------------------------------------------------------------------ Test run
    private IEnumerator RunRoutine()
    {
        running = true;
        passed = 0;
        failed = 0;

        if (!HasAllReferences()) { running = false; yield break; }

        Verify(sol800, "Sol 800", 800, 887f, SolarFlareClass.None, StormStatus.Clear, 1f, 1f, true,
               "WEATHER: CLEAR", "SOLAR EVENT: NONE");
        yield return DrainTest("Sol 800", 1f, 1f);

        Verify(sol852, "Sol 852", 852, 924f, SolarFlareClass.C, StormStatus.Regional, 1f, 2f, true,
               "WEATHER: REGIONAL DUST STORM", "SOLAR EVENT: C-CLASS FLARE");
        Check("Sol 852 stays Regional although pressure is 924 Pa",
              weatherManager.Storm == StormStatus.Regional && !weatherManager.DustFrontRiskActive);
        yield return DrainTest("Sol 852", 1f, 2f);

        Verify(sol2085, "Sol 2085", 2085, 772f, SolarFlareClass.X, StormStatus.SevereGlobal, 1.5f, 2f, false,
               "WEATHER: SEVERE GLOBAL STORM", "SOLAR EVENT: X-CLASS FLARE");
        Check("Sol 2085 pressure < 800 adds dust-front line without changing storm",
              weatherManager.DustFrontRiskActive && weatherManager.Storm == StormStatus.SevereGlobal &&
              weatherManager.StatusText.Contains("DUST-FRONT RISK"));
        yield return DrainTest("Sol 2085", 1.5f, 2f);

        // Switch back: proves multipliers and permission are restored, not left over.
        Verify(sol800, "Sol 800 (switch back)", 800, 887f, SolarFlareClass.None, StormStatus.Clear, 1f, 1f, true,
               "WEATHER: CLEAR", "SOLAR EVENT: NONE");

        string summary = $"[ScenarioTest] RESULT: {passed} passed, {failed} failed.";
        if (failed == 0) Debug.Log(summary, this); else Debug.LogError(summary, this);
        running = false;
    }

    private void Verify(MissionScenarioData asset, string label, int sol, float pressure,
                        SolarFlareClass flare, StormStatus storm, float oxMult, float shMult, bool charging,
                        string weatherLine, string flareLine)
    {
        bool applied = weatherManager.ApplyScenario(asset);
        Check($"{label}: ApplyScenario succeeded", applied);
        if (!applied) return;

        // WeatherManager exposure
        Check($"{label}: Sol = {sol}", weatherManager.CurrentSol == sol);
        Check($"{label}: pressure = {pressure} Pa", Mathf.Approximately(weatherManager.PressurePa, pressure));
        Check($"{label}: flare = {flare}", weatherManager.SolarFlare == flare);
        Check($"{label}: storm = {storm}", weatherManager.Storm == storm);
        Check($"{label}: charging allowed = {charging} (WeatherManager)", weatherManager.SolarChargingAllowed == charging);

        // ResourceManager (central path)
        Check($"{label}: ResourceManager Oxygen multiplier = {oxMult}",
              Mathf.Approximately(resourceManager.OxygenDrainMultiplier, oxMult));
        Check($"{label}: ResourceManager Shield multiplier = {shMult}",
              Mathf.Approximately(resourceManager.ShieldDrainMultiplier, shMult));
        Check($"{label}: baseline Oxygen drain = 0.5 (not pre-multiplied)",
              Mathf.Approximately(resourceManager.OxygenBaseDrainPerSecond, 0.5f));
        Check($"{label}: baseline Shield drain = 0.4 (not pre-multiplied)",
              Mathf.Approximately(resourceManager.ShieldBaseDrainPerSecond, 0.4f));
        Check($"{label}: starting Oxygen/Power/Shield = 88/64/72",
              Mathf.Approximately(resourceManager.GetCurrent(ResourceType.Oxygen), 88f) &&
              Mathf.Approximately(resourceManager.GetCurrent(ResourceType.Power), 64f) &&
              Mathf.Approximately(resourceManager.GetCurrent(ResourceType.Shield), 72f));

        // DaylightManager
        Check($"{label}: daylight duration = 300",
              Mathf.Approximately(daylightManager.DurationSeconds, 300f));
        Check($"{label}: daylight restarted (remaining ~ 300)",
              daylightManager.RemainingSeconds > 299f);

        // Solar systems
        Check($"{label}: permission source allowed = {charging}",
              solarPermissionSource.IsSolarChargingAllowed == charging);
        Check($"{label}: solar recharge = 2/s", Mathf.Approximately(solarGenerator.RechargePerSecond, 2f));

        // HUD text
        string hudText = hud.CurrentWeatherText;
        Check($"{label}: HUD shows SOL {sol}", hudText.Contains("SOL " + sol));
        Check($"{label}: HUD shows pressure", hudText.Contains("PRESSURE: " + pressure + " Pa"));
        Check($"{label}: HUD shows '{weatherLine}'", hudText.Contains(weatherLine));
        Check($"{label}: HUD shows '{flareLine}'", hudText.Contains(flareLine));
        Check($"{label}: HUD shows training disclosure", hudText.Contains(WeatherManager.TrainingDisclosure));
    }

    private IEnumerator DrainTest(string label, float oxMult, float shMult)
    {
        // Measures the REAL depletion over time. Power-independent, so solar/actions cannot disturb it.
        float o0 = resourceManager.GetCurrent(ResourceType.Oxygen);
        float s0 = resourceManager.GetCurrent(ResourceType.Shield);
        float t0 = Time.time;
        yield return new WaitForSeconds(drainSampleSeconds);
        float dt = Time.time - t0;

        float oxRate = (o0 - resourceManager.GetCurrent(ResourceType.Oxygen)) / dt;
        float shRate = (s0 - resourceManager.GetCurrent(ResourceType.Shield)) / dt;
        float oxExpected = 0.5f * oxMult;
        float shExpected = 0.4f * shMult;

        Check($"{label}: measured Oxygen drain {oxRate:0.000}/s ~ {oxExpected:0.000}/s",
              Mathf.Abs(oxRate - oxExpected) <= oxExpected * drainTolerance);
        Check($"{label}: measured Shield drain {shRate:0.000}/s ~ {shExpected:0.000}/s",
              Mathf.Abs(shRate - shExpected) <= shExpected * drainTolerance);
    }

    private void Check(string name, bool condition)
    {
        if (condition) { passed++; Debug.Log("[ScenarioTest] PASS  " + name, this); }
        else { failed++; Debug.LogError("[ScenarioTest] FAIL  " + name, this); }
    }

    private bool HasAllReferences()
    {
        bool ok = weatherManager && resourceManager && daylightManager && solarPermissionSource &&
                  solarGenerator && hud && sol800 && sol852 && sol2085;
        if (!ok) Debug.LogError("[ScenarioTest] One or more serialized references are not assigned.", this);
        return ok;
    }
}
