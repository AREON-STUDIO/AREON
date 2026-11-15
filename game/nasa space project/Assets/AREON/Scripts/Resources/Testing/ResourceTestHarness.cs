using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AREON.ResourceSystem.Testing
{
    /// <summary>
    /// TEMPORARY Stage 4 test tool. Delete this component (and its GameObject) once Stage 4 is approved
    /// and the Stage 5 HUD exists.
    ///
    /// It never stores resource values. It only calls the public ResourceManager API and observes events.
    /// Two ways to use it:
    ///  1. Automated: set "Run Automated Tests On Start" or use the component's context menu / overlay button.
    ///     Results are written to the Console as "[ResourceTest] PASS ..." / "FAIL ..." lines plus a summary.
    ///  2. Manual: use the small IMGUI debug overlay (top-right) to spend, recharge, restore and reset.
    /// The overlay is a debug aid, not the Stage 5 HUD. It uses IMGUI and no Input System code.
    /// </summary>
    [DisallowMultipleComponent]
    public class ResourceTestHarness : MonoBehaviour
    {
        private const string Tag = "[ResourceTest]";
        private const int ResourceCount = 3;
        private const int MaxLogLines = 8;

        [Header("References (assign in Inspector)")]
        [SerializeField] private ResourceManager resourceManager;

        [Header("Behaviour")]
        [Tooltip("Run the full automated test sequence automatically when Play Mode starts.")]
        [SerializeField] private bool runAutomatedTestsOnStart = false;

        [Tooltip("Show the small debug overlay in the Game view.")]
        [SerializeField] private bool showOverlay = true;

        [Header("Manual test amounts")]
        [SerializeField, Min(0f)] private float spendAmount = 15f;     // Matches the science download cost.
        [SerializeField, Min(0f)] private float rechargeAmount = 10f;
        [SerializeField, Min(0f)] private float shieldRestoreAmount = 10f;

        // Observation state (counters only, never resource values used for gameplay).
        private readonly int[] changeCounts = new int[ResourceCount];
        private readonly int[] depletedCounts = new int[ResourceCount];
        private readonly float[] lastEventValue = new float[ResourceCount];
        private readonly float[] minObserved = new float[ResourceCount];
        private readonly float[] maxObserved = new float[ResourceCount];
        private int rejectedCount;
        private readonly List<string> eventLog = new List<string>();

        private bool subscribed;
        private bool testsRunning;
        private int passed;
        private int failed;
        private string lastSummary = "Tests not run yet.";

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            if (resourceManager == null)
            {
                Debug.LogError($"{Tag} ResourceManager reference is missing. Assign it in the Inspector.", this);
                return;
            }

            resourceManager.ResourceChanged += HandleResourceChanged;
            resourceManager.ResourceDepleted += HandleResourceDepleted;
            resourceManager.PowerSpendRejected += HandlePowerSpendRejected;
            subscribed = true;
            ResetObservation();
        }

        private void OnDisable()
        {
            if (subscribed && resourceManager != null)
            {
                resourceManager.ResourceChanged -= HandleResourceChanged;
                resourceManager.ResourceDepleted -= HandleResourceDepleted;
                resourceManager.PowerSpendRejected -= HandlePowerSpendRejected;
            }

            subscribed = false;
            testsRunning = false;
        }

        private void Start()
        {
            if (runAutomatedTestsOnStart)
            {
                RunAutomatedTests();
            }
        }

        // ------------------------------------------------------------------
        // Event handlers (observation only)
        // ------------------------------------------------------------------

        private void HandleResourceChanged(ResourceType type, float oldValue, float newValue)
        {
            int i = (int)type;
            changeCounts[i]++;
            lastEventValue[i] = newValue;
            minObserved[i] = Mathf.Min(minObserved[i], newValue);
            maxObserved[i] = Mathf.Max(maxObserved[i], newValue);

            // Depletion fires every frame, so only log non-depletion style changes (jumps >= 1 point).
            if (Mathf.Abs(newValue - oldValue) >= 1f)
            {
                AddLog($"{type}: {oldValue:F1} -> {newValue:F1}");
            }
        }

        private void HandleResourceDepleted(ResourceType type)
        {
            depletedCounts[(int)type]++;
            AddLog($"{type} DEPLETED");
        }

        private void HandlePowerSpendRejected(float requestedAmount, float availablePower)
        {
            rejectedCount++;
            AddLog($"Power spend REJECTED: needed {requestedAmount:F1}, had {availablePower:F1}");
        }

        private void AddLog(string line)
        {
            eventLog.Add(line);
            if (eventLog.Count > MaxLogLines)
            {
                eventLog.RemoveAt(0);
            }
        }

        private void ResetObservation()
        {
            for (int i = 0; i < ResourceCount; i++)
            {
                changeCounts[i] = 0;
                depletedCounts[i] = 0;
                lastEventValue[i] = resourceManager != null ? resourceManager.GetValue((ResourceType)i) : 0f;
                minObserved[i] = lastEventValue[i];
                maxObserved[i] = lastEventValue[i];
            }

            rejectedCount = 0;
        }

        // ------------------------------------------------------------------
        // Manual actions (public so they can be invoked from the overlay, context menu or MCP)
        // ------------------------------------------------------------------

        [ContextMenu("Test/Spend Power")]
        public void SpendPower()
        {
            if (!HasManager()) return;
            bool ok = resourceManager.TrySpendPower(spendAmount);
            Debug.Log($"{Tag} TrySpendPower({spendAmount}) -> {ok}. Power now {resourceManager.Power:F2}");
        }

        [ContextMenu("Test/Recharge Power")]
        public void RechargePower()
        {
            if (!HasManager()) return;
            bool ok = resourceManager.TryRechargePower(rechargeAmount, out float applied);
            Debug.Log($"{Tag} TryRechargePower({rechargeAmount}) -> {ok}, applied {applied:F2}. Power now {resourceManager.Power:F2}");
        }

        [ContextMenu("Test/Restore Shield")]
        public void RestoreShield()
        {
            if (!HasManager()) return;
            bool ok = resourceManager.TryRestoreShield(shieldRestoreAmount, out float applied);
            Debug.Log($"{Tag} TryRestoreShield({shieldRestoreAmount}) -> {ok}, applied {applied:F2}. Shield now {resourceManager.Shield:F2}");
        }

        [ContextMenu("Test/Toggle Depletion")]
        public void ToggleDepletion()
        {
            if (!HasManager()) return;
            resourceManager.SetDepletionEnabled(!resourceManager.DepletionEnabled);
            Debug.Log($"{Tag} Depletion enabled = {resourceManager.DepletionEnabled}");
        }

        [ContextMenu("Test/Reset To Initial Values")]
        public void ResetToInitial()
        {
            if (!HasManager()) return;
            resourceManager.ResetToInitialValues();
            ResetObservation();
            Debug.Log($"{Tag} Reset. O2 {resourceManager.Oxygen:F1} / Power {resourceManager.Power:F1} / Shield {resourceManager.Shield:F1}");
        }

        private bool HasManager()
        {
            if (resourceManager != null)
            {
                return true;
            }

            Debug.LogError($"{Tag} ResourceManager reference is missing.", this);
            return false;
        }

        // ------------------------------------------------------------------
        // Automated tests
        // ------------------------------------------------------------------

        [ContextMenu("Test/Run Automated Tests")]
        public void RunAutomatedTests()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{Tag} Automated tests need Play Mode (they rely on Time.deltaTime).", this);
                return;
            }

            if (!HasManager()) return;

            if (testsRunning)
            {
                Debug.LogWarning($"{Tag} Tests are already running.", this);
                return;
            }

            StartCoroutine(RunAutomatedTestsRoutine());
        }

        private IEnumerator RunAutomatedTestsRoutine()
        {
            if (Time.timeScale <= 0f)
            {
                Debug.LogError($"{Tag} Time.timeScale is 0, depletion tests cannot run.", this);
                yield break;
            }

            testsRunning = true;
            passed = 0;
            failed = 0;
            ResourceManager rm = resourceManager;

            // Save everything the tests will change so it can be restored afterwards.
            bool originalDepletion = rm.DepletionEnabled;
            float originalOxygenRate = rm.OxygenDrainPerSecond;
            float originalShieldRate = rm.ShieldDrainPerSecond;
            float originalOxygenMultiplier = rm.OxygenDrainMultiplier;
            float originalShieldMultiplier = rm.ShieldDrainMultiplier;
            bool originalInvalidLogging = rm.LogInvalidRequests;

            Debug.Log($"{Tag} ===== Stage 4 automated tests START =====");

            try
            {
                const int O = (int)ResourceType.Oxygen;
                const int P = (int)ResourceType.Power;
                const int S = (int)ResourceType.Shield;

                rm.SetDepletionEnabled(false);

                // T1: initial values match the Inspector configuration and are in range.
                rm.ResetToInitialValues();
                Check("T1a Initial Oxygen equals configured value", Approx(rm.Oxygen, rm.InitialOxygen), $"{rm.Oxygen} vs {rm.InitialOxygen}");
                Check("T1b Initial Power equals configured value", Approx(rm.Power, rm.InitialPower), $"{rm.Power} vs {rm.InitialPower}");
                Check("T1c Initial Shield equals configured value", Approx(rm.Shield, rm.InitialShield), $"{rm.Shield} vs {rm.InitialShield}");
                Check("T1d All initial values within 0-100", InRange(rm.Oxygen) && InRange(rm.Power) && InRange(rm.Shield), $"{rm.Oxygen}/{rm.Power}/{rm.Shield}");

                // T2: successful Power spend is exact and raises exactly one change event.
                rm.InitializeResources(80f, 60f, 70f);
                ResetObservation();
                bool ok = rm.TrySpendPower(15f);
                Check("T2a TrySpendPower(15) from 60 returns true", ok, ok.ToString());
                Check("T2b Power is exactly 45", Approx(rm.Power, 45f), rm.Power.ToString("F3"));
                Check("T2c One Power change event, value 45", changeCounts[P] == 1 && Approx(lastEventValue[P], 45f), $"count {changeCounts[P]}, last {lastEventValue[P]}");
                Check("T2d Oxygen and Shield untouched", Approx(rm.Oxygen, 80f) && Approx(rm.Shield, 70f), $"{rm.Oxygen}/{rm.Shield}");

                // T3: insufficient Power is rejected with no partial deduction and no change event.
                rm.InitializeResources(80f, 10f, 70f);
                ResetObservation();
                ok = rm.TrySpendPower(15f);
                Check("T3a TrySpendPower(15) from 10 returns false", !ok, ok.ToString());
                Check("T3b Power unchanged at 10 (no partial deduction)", Approx(rm.Power, 10f), rm.Power.ToString("F3"));
                Check("T3c PowerSpendRejected raised once", rejectedCount == 1, $"count {rejectedCount}");
                Check("T3d No Power change event", changeCounts[P] == 0, $"count {changeCounts[P]}");

                // T4: spending exactly all available Power succeeds and reaches 0 but never below.
                ResetObservation();
                ok = rm.TrySpendPower(10f);
                Check("T4a Spending exactly all Power (10) succeeds", ok, ok.ToString());
                Check("T4b Power is exactly 0", rm.Power == 0f, rm.Power.ToString("F3"));
                Check("T4c Power depleted event raised once", depletedCounts[P] == 1, $"count {depletedCounts[P]}");
                ok = rm.TrySpendPower(0.01f);
                Check("T4d Spending 0.01 at 0 Power is rejected", !ok && rm.Power == 0f, $"{ok}, power {rm.Power}");

                // T5: recharge works and reports the applied amount.
                rm.InitializeResources(80f, 20f, 70f);
                ResetObservation();
                ok = rm.TryRechargePower(10f, out float applied);
                Check("T5a TryRechargePower(10) from 20 returns true", ok, ok.ToString());
                Check("T5b Power is 30 and applied is 10", Approx(rm.Power, 30f) && Approx(applied, 10f), $"power {rm.Power}, applied {applied}");
                Check("T5c One Power change event, value 30", changeCounts[P] == 1 && Approx(lastEventValue[P], 30f), $"count {changeCounts[P]}, last {lastEventValue[P]}");

                // T6: recharge clamps at maximum capacity.
                ok = rm.TryRechargePower(500f, out applied);
                Check("T6a Over-recharge returns true and clamps to 100", ok && Approx(rm.Power, 100f), $"{ok}, power {rm.Power}");
                Check("T6b Applied amount is only the 70 that fit", Approx(applied, 70f), applied.ToString("F3"));
                int before = changeCounts[P];
                ok = rm.TryRechargePower(1f, out applied);
                Check("T6c Recharge at full returns false, Power stays 100", !ok && Approx(rm.Power, 100f) && Approx(applied, 0f), $"{ok}, power {rm.Power}");
                Check("T6d No change event when already full", changeCounts[P] == before, $"count {changeCounts[P]} vs {before}");

                // T7: invalid amounts are rejected without changing anything.
                rm.InitializeResources(80f, 50f, 70f);
                rm.SetInvalidRequestLogging(false); // Expected rejections would otherwise log warnings.
                bool negSpend = rm.TrySpendPower(-5f);
                bool negRecharge = rm.TryRechargePower(-5f);
                bool nanSpend = rm.TrySpendPower(float.NaN);
                bool infRecharge = rm.TryRechargePower(float.PositiveInfinity);
                rm.SetInvalidRequestLogging(originalInvalidLogging);
                Check("T7a Negative, NaN and infinite amounts all rejected", !negSpend && !negRecharge && !nanSpend && !infRecharge, $"{negSpend}/{negRecharge}/{nanSpend}/{infRecharge}");
                Check("T7b Power still exactly 50 after invalid requests", Approx(rm.Power, 50f), rm.Power.ToString("F3"));

                // T8: Shield restore clamps at maximum.
                rm.InitializeResources(80f, 50f, 95f);
                ok = rm.TryRestoreShield(20f, out applied);
                Check("T8a TryRestoreShield(20) from 95 clamps to 100", ok && Approx(rm.Shield, 100f) && Approx(applied, 5f), $"{ok}, shield {rm.Shield}, applied {applied}");

                // T9: InitializeResources clamps out-of-range input.
                rm.InitializeResources(150f, -20f, 100f);
                Check("T9 InitializeResources(150,-20,100) clamps to 100/0/100", Approx(rm.Oxygen, 100f) && Approx(rm.Power, 0f) && Approx(rm.Shield, 100f), $"{rm.Oxygen}/{rm.Power}/{rm.Shield}");

                // T10: event subscription and unsubscription are safe.
                rm.InitializeResources(80f, 50f, 70f);
                int localCount = 0;
                ResourceManager.ResourceChangedHandler counter = (type, oldValue, newValue) => localCount++;
                rm.ResourceChanged += counter;
                rm.TrySpendPower(5f);
                Check("T10a Subscribed handler receives the event", localCount == 1, $"count {localCount}");
                rm.ResourceChanged -= counter;
                rm.TrySpendPower(5f);
                Check("T10b Unsubscribed handler receives nothing", localCount == 1, $"count {localCount}");
                rm.ResourceChanged -= counter; // Removing twice must be harmless.
                Check("T10c Double unsubscribe causes no error", true, "no exception");

                // T11: Oxygen and Shield deplete over time at the configured base rates (multipliers = 1).
                rm.InitializeResources(100f, 50f, 100f);
                rm.SetBaseDrainRates(10f, 5f);
                rm.SetDrainMultipliers(1f, 1f);
                ResetObservation();
                float startTime = Time.time;
                rm.SetDepletionEnabled(true);
                yield return new WaitForSeconds(1f);
                float elapsed = Time.time - startTime;
                float expectedOxygen = 100f - 10f * elapsed;
                float expectedShield = 100f - 5f * elapsed;
                Check("T11a Oxygen depleted at 10/s (delta-time based)", elapsed > 0.5f && Mathf.Abs(rm.Oxygen - expectedOxygen) <= 1f, $"oxygen {rm.Oxygen:F2}, expected {expectedOxygen:F2}, elapsed {elapsed:F2}s");
                Check("T11b Shield depleted at 5/s (delta-time based)", Mathf.Abs(rm.Shield - expectedShield) <= 0.5f, $"shield {rm.Shield:F2}, expected {expectedShield:F2}");
                Check("T11c Power not affected by depletion", Approx(rm.Power, 50f), rm.Power.ToString("F3"));
                Check("T11d Change events fired and last event matches current value", changeCounts[O] > 1 && changeCounts[S] > 1 && Approx(lastEventValue[O], rm.Oxygen) && Approx(lastEventValue[S], rm.Shield), $"O2 events {changeCounts[O]}, Shield events {changeCounts[S]}");
                Check("T11e No Power events during depletion", changeCounts[P] == 0, $"count {changeCounts[P]}");

                // T12: multipliers are applied in one place (effective rate = base * multiplier).
                rm.SetDepletionEnabled(false);
                rm.InitializeResources(100f, 50f, 100f);
                rm.SetDrainMultipliers(1.5f, 2f);
                Check("T12a Effective rates are 15/s and 10/s", Approx(rm.EffectiveOxygenDrainPerSecond, 15f) && Approx(rm.EffectiveShieldDrainPerSecond, 10f), $"{rm.EffectiveOxygenDrainPerSecond}/{rm.EffectiveShieldDrainPerSecond}");
                startTime = Time.time;
                rm.SetDepletionEnabled(true);
                yield return new WaitForSeconds(1f);
                elapsed = Time.time - startTime;
                expectedOxygen = 100f - 15f * elapsed;
                expectedShield = 100f - 10f * elapsed;
                Check("T12b Oxygen depleted at 15/s with 1.5x multiplier", Mathf.Abs(rm.Oxygen - expectedOxygen) <= 1.5f, $"oxygen {rm.Oxygen:F2}, expected {expectedOxygen:F2}");
                Check("T12c Shield depleted at 10/s with 2x multiplier", Mathf.Abs(rm.Shield - expectedShield) <= 1f, $"shield {rm.Shield:F2}, expected {expectedShield:F2}");

                // T13: depletion clamps at zero, never goes negative and the depleted event fires only once.
                rm.SetDepletionEnabled(false);
                rm.SetDrainMultipliers(1f, 1f);
                rm.SetBaseDrainRates(10f, 10f);
                rm.InitializeResources(2f, 50f, 2f);
                ResetObservation();
                rm.SetDepletionEnabled(true);
                yield return new WaitForSeconds(0.6f);
                Check("T13a Oxygen and Shield clamped at exactly 0", rm.Oxygen == 0f && rm.Shield == 0f, $"{rm.Oxygen}/{rm.Shield}");
                Check("T13b Never observed below 0", minObserved[O] >= 0f && minObserved[S] >= 0f, $"min {minObserved[O]}/{minObserved[S]}");
                yield return new WaitForSeconds(0.3f);
                Check("T13c Depleted event raised exactly once per resource", depletedCounts[O] == 1 && depletedCounts[S] == 1, $"O2 {depletedCounts[O]}, Shield {depletedCounts[S]}");
                Check("T13d Still 0 and Power untouched after extra time", rm.Oxygen == 0f && rm.Shield == 0f && Approx(rm.Power, 50f), $"{rm.Oxygen}/{rm.Shield}/{rm.Power}");

                // T14: disabling depletion stops the drain.
                rm.SetDepletionEnabled(false);
                rm.InitializeResources(80f, 50f, 70f);
                yield return new WaitForSeconds(0.3f);
                Check("T14 Depletion disabled leaves Oxygen/Shield unchanged", Approx(rm.Oxygen, 80f) && Approx(rm.Shield, 70f), $"{rm.Oxygen}/{rm.Shield}");

                // T15: nothing ever left the valid range during the whole run.
                Check("T15 Observed values stayed within 0-100", maxObserved[O] <= 100f && maxObserved[P] <= 100f && maxObserved[S] <= 100f && minObserved[P] >= 0f, "max/min checked");
            }
            finally
            {
                // Always restore the original configuration, even if the run is interrupted.
                rm.SetDepletionEnabled(false);
                rm.SetInvalidRequestLogging(originalInvalidLogging);
                rm.SetBaseDrainRates(originalOxygenRate, originalShieldRate);
                rm.SetDrainMultipliers(originalOxygenMultiplier, originalShieldMultiplier);
                rm.ResetToInitialValues();
                rm.SetDepletionEnabled(originalDepletion);
                ResetObservation();
                testsRunning = false;
            }

            lastSummary = $"{passed}/{passed + failed} checks passed" + (failed == 0 ? " - ALL PASS" : $" - {failed} FAILED");

            if (failed == 0)
            {
                Debug.Log($"{Tag} ===== SUMMARY: {lastSummary} =====");
            }
            else
            {
                Debug.LogError($"{Tag} ===== SUMMARY: {lastSummary} =====");
            }
        }

        private void Check(string name, bool condition, string detail)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"{Tag} PASS {name} ({detail})");
            }
            else
            {
                failed++;
                Debug.LogError($"{Tag} FAIL {name} ({detail})");
            }
        }

        private static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }

        private static bool InRange(float value)
        {
            return value >= ResourceManager.MinValue && value <= ResourceManager.MaxValue;
        }

        // ------------------------------------------------------------------
        // Debug overlay (IMGUI, temporary)
        // ------------------------------------------------------------------

        private void OnGUI()
        {
            if (!showOverlay || resourceManager == null)
            {
                return;
            }

            const float width = 340f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, Screen.height - 20f), GUI.skin.box);

            GUILayout.Label("<b>TEMP Resource Test Harness (Stage 4)</b>", RichLabel());
            GUILayout.Label($"Oxygen: {resourceManager.Oxygen:F2}   Power: {resourceManager.Power:F2}   Shield: {resourceManager.Shield:F2}");
            GUILayout.Label($"Depletion: {(resourceManager.DepletionEnabled ? "ON" : "OFF")}   Effective drain/s  O2 {resourceManager.EffectiveOxygenDrainPerSecond:F2}  Shield {resourceManager.EffectiveShieldDrainPerSecond:F2}");
            GUILayout.Label($"Events - O2 {changeCounts[0]}  Power {changeCounts[1]}  Shield {changeCounts[2]}  Rejected {rejectedCount}");

            if (GUILayout.Button($"Spend {spendAmount:F0} Power")) SpendPower();
            if (GUILayout.Button($"Recharge {rechargeAmount:F0} Power")) RechargePower();
            if (GUILayout.Button($"Restore {shieldRestoreAmount:F0} Shield")) RestoreShield();
            if (GUILayout.Button("Toggle Depletion")) ToggleDepletion();
            if (GUILayout.Button("Reset To Initial Values")) ResetToInitial();

            GUI.enabled = !testsRunning;
            if (GUILayout.Button(testsRunning ? "Automated tests running..." : "Run Automated Tests")) RunAutomatedTests();
            GUI.enabled = true;

            GUILayout.Label(lastSummary);
            GUILayout.Label("Recent events:");
            for (int i = 0; i < eventLog.Count; i++)
            {
                GUILayout.Label(eventLog[i]);
            }

            GUILayout.EndArea();
        }

        private static GUIStyle RichLabel()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }
    }
}
