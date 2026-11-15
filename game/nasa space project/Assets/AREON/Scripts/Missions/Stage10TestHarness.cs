// Assets/AREON/Scripts/Missions/Stage10TestHarness.cs
// Stage 10 - Tasks 46, 47, 48 (automated flow / regression assertions)
//
// Run in Play Mode:  right-click the component > "Run Stage 10 Suite"   (or call RunAll() through Unity MCP).
// Results: Console lines prefixed "[Stage10]" and the public LastReport string.
//
// It uses ONLY MissionFlowBridge (probe + test hooks) and the Stage 10 views. It never edits assets.
// Temporary overrides (timeScale, elapsed-time jump) are runtime-only and restored in the finally block.
using System.Collections;
using System.Text;
using UnityEngine;
using AREON.UI;
// ADAPT[0]: add the namespace of MissionScenarioData if it is not AREON.Missions.

namespace AREON.Missions
{
    [DisallowMultipleComponent]
    public sealed class Stage10TestHarness : MonoBehaviour
    {
        [Header("Stage 10 systems")]
        [SerializeField] private MissionFlowBridge bridge;
        [SerializeField] private MissionResultsView resultsView;
        [SerializeField] private MissionSelectionView selectionView;

        [Header("Existing scenario assets (READ ONLY - never modified)")]
        [SerializeField] private MissionScenarioData sol800;
        [SerializeField] private MissionScenarioData sol852;
        [SerializeField] private MissionScenarioData sol2085;

        [Header("Run options")]
        [Tooltip("Temporary Time.timeScale while waiting. Restored afterwards. 1 = real time.")]
        [SerializeField, Range(1f, 20f)] private float testTimeScale = 1f;
        [SerializeField] private bool runOnStart = false;

        public bool IsRunning { get; private set; }
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int Skipped { get; private set; }
        public string LastReport { get; private set; }

        private readonly StringBuilder report = new StringBuilder();
        private int endedCount;
        private MissionResultsSnapshot lastEnded;
        private MissionRuntimeProbe liveAtEnd;

        // Independent oracle: blueprint Section 1.5 + Stage 9 baseline recorded in the handoff.
        private sealed class Case
        {
            public string Label;
            public MissionScenarioData Asset;
            public int Sol;
            public float Pressure;
            public string Storm, Flare;
            public float OxygenMult, ShieldMult;
            public bool Solar;
            // Stage 9 baseline (verify in Phase 0; update only with a written justification).
            public float BaseDuration;
            public bool FailOxygen, FailShield, FailTimeout;
        }

        private Case[] cases;

        private void Start()
        {
            if (runOnStart) RunAll();
        }

        [ContextMenu("Run Stage 10 Suite")]
        public void RunAll()
        {
            if (IsRunning) { Debug.LogWarning("[Stage10] Suite already running."); return; }
            if (!Application.isPlaying) { Debug.LogError("[Stage10] Enter Play Mode first."); return; }
            if (bridge == null || resultsView == null || selectionView == null ||
                sol800 == null || sol852 == null || sol2085 == null)
            {
                Debug.LogError("[Stage10] Harness references are not fully assigned.", this);
                return;
            }
            StartCoroutine(RunSuite());
        }

        // ================================================================ suite
        private IEnumerator RunSuite()
        {
            IsRunning = true;
            Passed = 0; Failed = 0; Skipped = 0;
            report.Length = 0;
            BuildCases();
            float originalScale = Time.timeScale;
            endedCount = 0; lastEnded = null; liveAtEnd = null;
            bridge.MissionEnded += OnEnded;

            try
            {
                yield return TestScenarioConfiguration();
                yield return TestSuccessFlow();
                yield return TestFailureFlows();
                yield return TestTimeoutBehaviour();
                yield return TestRestartCycles();
                yield return TestReturnToSelectionAndSwitching();
                yield return TestSubscriptionIntegrity();
                yield return TestResourceRange();
            }
            finally
            {
                bridge.MissionEnded -= OnEnded;
                Time.timeScale = originalScale;
                IsRunning = false;
            }

            string summary = "[Stage10] RESULT: " + Passed + " passed, " + Failed + " failed, " + Skipped + " skipped (of " + (Passed + Failed + Skipped) + ")";
            report.AppendLine(summary);
            LastReport = report.ToString();
            if (Failed == 0) Debug.Log(summary); else Debug.LogError(summary);
        }

        private void BuildCases()
        {
            cases = new Case[]
            {
                new Case { Label = "Sol 800",  Asset = sol800,  Sol = 800,  Pressure = 887f, Storm = "Clear",        Flare = "None", OxygenMult = 1f,   ShieldMult = 1f, Solar = true,  BaseDuration = 180f, FailOxygen = true, FailShield = false, FailTimeout = false },
                new Case { Label = "Sol 852",  Asset = sol852,  Sol = 852,  Pressure = 924f, Storm = "Regional",     Flare = "C",    OxygenMult = 1f,   ShieldMult = 2f, Solar = true,  BaseDuration = 240f, FailOxygen = true, FailShield = true,  FailTimeout = true  },
                new Case { Label = "Sol 2085", Asset = sol2085, Sol = 2085, Pressure = 772f, Storm = "SevereGlobal", Flare = "X",    OxygenMult = 1.5f, ShieldMult = 2f, Solar = false, BaseDuration = 240f, FailOxygen = true, FailShield = true,  FailTimeout = true  },
            };
        }

        private void OnEnded(MissionResultsSnapshot snapshot)
        {
            endedCount++;
            lastEnded = snapshot;
            liveAtEnd = bridge.CaptureProbe();   // live values read in the same call stack as the capture
        }

        // ================================================================ tests
        private IEnumerator TestScenarioConfiguration()
        {
            Section("A. Scenario configuration, assets unchanged, selection applies its own data");
            foreach (Case c in cases)
            {
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                AssertConfig(c, "start");

                float o, p, s;
                bridge.ReadScenarioStartingResources(c.Asset, out o, out p, out s);
                MissionRuntimeProbe pr = bridge.CaptureProbe();
                Check(c.Label + ": starting resources come from its asset (+-1)",
                    Near(pr.Oxygen, o, 1f) && Near(pr.Power, p, 1f) && Near(pr.Shield, s, 1f),
                    "probe " + pr.Oxygen.ToString("0.0") + "/" + pr.Power.ToString("0.0") + "/" + pr.Shield.ToString("0.0") + " asset " + o + "/" + p + "/" + s);

                // Baseline guard: detects accidental asset edits.
                Check(c.Label + ": duration equals recorded Stage 9 baseline", Near(bridge.ReadScenarioDuration(c.Asset), c.BaseDuration, 0.001f),
                    "asset " + bridge.ReadScenarioDuration(c.Asset) + " baseline " + c.BaseDuration);
                bool fo, fs, ft;
                bridge.ReadScenarioFailureFlags(c.Asset, out fo, out fs, out ft);
                Check(c.Label + ": failure flags equal recorded Stage 9 baseline",
                    fo == c.FailOxygen && fs == c.FailShield && ft == c.FailTimeout,
                    "asset O2=" + fo + " Shield=" + fs + " Timeout=" + ft);
            }
        }

        private IEnumerator TestSuccessFlow()
        {
            Section("B. Success flow and results accuracy (required objectives only; optional must not block)");
            foreach (Case c in cases)
            {
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                int before = endedCount;
                Check(c.Label + ": results hidden at mission start", !resultsView.IsVisible);

                bridge.TestCompleteRequiredObjectives();
                yield return Frames(3);

                bool neededTime = false;
                if (endedCount == before)
                {
                    // The configured survival/time condition may also be required (blueprint 2.6).
                    neededTime = true;
                    bridge.TestJumpElapsed(bridge.ReadScenarioDuration(c.Asset));
                    yield return Real(1.5f);
                }
                Info(c.Label + ": success needed the time condition = " + neededTime);

                Check(c.Label + ": exactly one terminal notification", endedCount == before + 1, "count delta " + (endedCount - before));
                if (lastEnded == null) continue;

                MissionResultsSnapshot snap = lastEnded;
                Check(c.Label + ": outcome is Success", snap.Outcome == MissionResultOutcome.Success);
                Check(c.Label + ": all required objectives completed", snap.RequiredCompleted == snap.RequiredTotal && snap.RequiredTotal > 0,
                    snap.RequiredCompleted + "/" + snap.RequiredTotal);
                if (snap.OptionalTotal > 0)
                    Check(c.Label + ": success reached with optional objectives incomplete", snap.OptionalCompleted < snap.OptionalTotal,
                        snap.OptionalCompleted + "/" + snap.OptionalTotal);
                else Skip(c.Label + ": optional-objective check", "scenario has no optional objectives");

                Check(c.Label + ": snapshot Sol/name correct", snap.SolNumber == c.Sol && !string.IsNullOrEmpty(snap.MissionName));
                CheckResultsPanel(c, snap, true);

                // Values must persist after completion.
                string shown = resultsView.RenderedSummary;
                yield return Real(1.0f);
                Check(c.Label + ": panel text unchanged one second later", resultsView.RenderedSummary == shown);
                Check(c.Label + ": panel still visible", resultsView.IsVisible);
                MissionRuntimeProbe now = bridge.CaptureProbe();
                Check(c.Label + ": live resources frozen after terminal state (informational for stage 9 policy)",
                    Near(now.Oxygen, snap.Oxygen, 0.01f) && Near(now.Shield, snap.Shield, 0.01f),
                    "live " + now.Oxygen.ToString("0.00") + "/" + now.Shield.ToString("0.00") + " snap " + snap.Oxygen.ToString("0.00") + "/" + snap.Shield.ToString("0.00"));
            }
        }

        private IEnumerator TestFailureFlows()
        {
            Section("C. Failure flows follow each scenario's configured rules");
            foreach (Case c in cases)
            {
                // Oxygen
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                int before = endedCount;
                bridge.TestDepleteOxygen();
                yield return Frames(4);
                if (c.FailOxygen)
                {
                    Check(c.Label + ": Oxygen depletion ends mission once", endedCount == before + 1);
                    if (lastEnded != null)
                    {
                        Check(c.Label + ": outcome is Failure", lastEnded.Outcome == MissionResultOutcome.Failure);
                        Check(c.Label + ": reason mentions Oxygen", lastEnded.EndReason != null && lastEnded.EndReason.Contains("Oxygen"));
                        CheckResultsPanel(c, lastEnded, false);
                        Check(c.Label + ": final Oxygen is 0", lastEnded.Oxygen <= 0.001f, lastEnded.Oxygen.ToString("0.000"));
                    }
                }
                else Check(c.Label + ": Oxygen failure disabled -> mission continues", endedCount == before);

                // Shield
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                before = endedCount;
                bridge.TestDepleteShield();
                yield return Frames(4);
                if (c.FailShield)
                {
                    Check(c.Label + ": Shield depletion ends mission once", endedCount == before + 1);
                    if (lastEnded != null)
                    {
                        Check(c.Label + ": Shield failure outcome", lastEnded.Outcome == MissionResultOutcome.Failure);
                        Check(c.Label + ": final Shield is 0", lastEnded.Shield <= 0.001f);
                    }
                }
                else
                {
                    Check(c.Label + ": Shield failure disabled -> mission continues", endedCount == before);
                    Check(c.Label + ": mission still running", bridge.CaptureProbe().MissionRunning);
                }
            }
        }

        private IEnumerator TestTimeoutBehaviour()
        {
            Section("D. Mission timeout behaviour (runtime time jump, assets untouched)");
            foreach (Case c in cases)
            {
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                int before = endedCount;
                Time.timeScale = testTimeScale;
                bridge.TestJumpElapsed(Mathf.Max(0f, c.BaseDuration - 0.2f));
                yield return Real(1.5f);
                Time.timeScale = 1f;

                if (c.FailTimeout)
                {
                    Check(c.Label + ": timeout with incomplete required objectives -> Failure", endedCount == before + 1 && lastEnded != null && lastEnded.Outcome == MissionResultOutcome.Failure);
                    if (lastEnded != null) Check(c.Label + ": reason mentions time", lastEnded.EndReason != null && lastEnded.EndReason.ToLower().Contains("time"));
                }
                else
                {
                    bool failed = endedCount > before && lastEnded != null && lastEnded.Outcome == MissionResultOutcome.Failure;
                    Check(c.Label + ": timeout failure disabled -> no timeout Failure", !failed);
                }
            }
        }

        private IEnumerator TestRestartCycles()
        {
            Section("E. Restart resets every runtime system; no duplicate events (3 cycles per scenario)");
            foreach (Case c in cases)
            {
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                bool dirtyWarned = false;

                for (int cycle = 1; cycle <= 3; cycle++)
                {
                    string tag = c.Label + " cycle " + cycle;

                    bool dirty = bridge.TestDirtyRuntimeState();
                    if (!dirty && !dirtyWarned) { Skip(c.Label + ": dirty-state precondition", "hook could not change state (Power/objectives)"); dirtyWarned = true; }
                    MissionRuntimeProbe dirtyProbe = bridge.CaptureProbe();
                    Info(tag + " dirty: science=" + dirtyProbe.ScienceDownloaded + " reinforce=" + dirtyProbe.ShieldReinforcements + " daylight=" + dirtyProbe.DaylightActive);

                    int endedBefore = endedCount;
                    int showBefore = resultsView.ShowCount;
                    bridge.TestDepleteOxygen();
                    yield return Frames(4);
                    Check(tag + ": terminal -> exactly one notification", endedCount == endedBefore + 1, "delta " + (endedCount - endedBefore));
                    Check(tag + ": results shown exactly once", resultsView.ShowCount == showBefore + 1 && resultsView.IsVisible, "delta " + (resultsView.ShowCount - showBefore));

                    bool restarted = bridge.RestartCurrentMission();
                    yield return Frames(3);
                    Check(tag + ": restart accepted", restarted);
                    CheckFreshMission(c, tag);
                }
            }
        }

        private IEnumerator TestReturnToSelectionAndSwitching()
        {
            Section("F. Return to selection, select another scenario, switching leaks nothing");
            Case first = cases[0];
            bridge.StartScenario(first.Asset);
            yield return Frames(3);
            bridge.TestDepleteOxygen();
            yield return Frames(4);
            Check("results visible before returning to selection", resultsView.IsVisible);

            bridge.ReturnToMissionSelection();
            yield return Frames(3);
            Check("selection panel visible", selectionView.IsVisible);
            Check("results panel hidden", !resultsView.IsVisible);
            MissionRuntimeProbe idle = bridge.CaptureProbe();
            Check("no mission running while selecting", !idle.MissionRunning && !idle.MissionTerminal);
            yield return Real(1.0f);
            MissionRuntimeProbe idle2 = bridge.CaptureProbe();
            Check("resources do not drain while in selection", Near(idle.Oxygen, idle2.Oxygen, 0.001f) && Near(idle.Shield, idle2.Shield, 0.001f),
                idle.Oxygen.ToString("0.000") + " -> " + idle2.Oxygen.ToString("0.000"));

            selectionView.Select(2);   // third button = Sol 2085 (order assigned by the builder)
            yield return Frames(3);
            Check("selection panel hides after choosing", !selectionView.IsVisible);
            AssertConfig(cases[2], "chosen from selection");
            CheckFreshMission(cases[2], "Sol 2085 from selection");

            // Switching sequence: nothing from the previous scenario may remain.
            int[] order = { 0, 2, 1, 0, 2, 1 };
            for (int i = 0; i < order.Length; i++)
            {
                Case c = cases[order[i]];
                bridge.TestDirtyRuntimeState();
                bridge.StartScenario(c.Asset);
                yield return Frames(3);
                AssertConfig(c, "switch #" + (i + 1));
                CheckFreshMission(c, "switch #" + (i + 1) + " " + c.Label);
            }
        }

        private IEnumerator TestSubscriptionIntegrity()
        {
            Section("G. Disable/enable cycles do not duplicate subscriptions");
            for (int i = 0; i < 5; i++)
            {
                bridge.enabled = false; resultsView.enabled = false; selectionView.enabled = false;
                yield return null;
                bridge.enabled = true; resultsView.enabled = true; selectionView.enabled = true;
                yield return null;
            }
            Check("bridge still enabled after toggling (references valid)", bridge.enabled);

            bridge.StartScenario(cases[1].Asset);
            yield return Frames(3);
            int endedBefore = endedCount;
            int showBefore = resultsView.ShowCount;
            bridge.TestDepleteOxygen();
            yield return Frames(4);
            Check("after 5 toggles: one terminal notification", endedCount == endedBefore + 1, "delta " + (endedCount - endedBefore));
            Check("after 5 toggles: results shown once", resultsView.ShowCount == showBefore + 1, "delta " + (resultsView.ShowCount - showBefore));
        }

        private IEnumerator TestResourceRange()
        {
            Section("H. Resources stay within 0-100 while running and at depletion");
            foreach (Case c in cases)
            {
                bridge.StartScenario(c.Asset);
                Time.timeScale = testTimeScale;
                bool inRange = true;
                for (int f = 0; f < 150; f++)
                {
                    MissionRuntimeProbe p = bridge.CaptureProbe();
                    if (p.Oxygen < 0f || p.Oxygen > 100f || p.Power < 0f || p.Power > 100f || p.Shield < 0f || p.Shield > 100f) inRange = false;
                    yield return null;
                }
                Time.timeScale = 1f;
                bridge.TestDepleteOxygen(); bridge.TestDepleteShield();
                yield return Frames(3);
                MissionRuntimeProbe z = bridge.CaptureProbe();
                if (z.Oxygen < 0f || z.Shield < 0f) inRange = false;
                Check(c.Label + ": resources within 0-100 (150 frames + depletion)", inRange);
            }

            // Leave the prototype in a clean, playable state.
            bridge.StartScenario(cases[0].Asset);
            yield return Frames(2);
        }

        // ================================================================ shared assertions
        private void AssertConfig(Case c, string when)
        {
            MissionRuntimeProbe p = bridge.CaptureProbe();
            string t = c.Label + " (" + when + ")";
            Check(t + ": mission Running", p.MissionRunning);
            Check(t + ": Sol", p.Sol == c.Sol, p.Sol.ToString());
            Check(t + ": pressure " + c.Pressure + " Pa", Near(p.PressurePa, c.Pressure, 0.01f), p.PressurePa.ToString());
            Check(t + ": storm " + c.Storm, p.StormStatus == c.Storm, p.StormStatus);
            Check(t + ": flare " + c.Flare, p.FlareClass == c.Flare, p.FlareClass);
            Check(t + ": oxygen multiplier " + c.OxygenMult, Near(p.OxygenMultiplier, c.OxygenMult, 0.001f), p.OxygenMultiplier.ToString());
            Check(t + ": shield multiplier " + c.ShieldMult, Near(p.ShieldMultiplier, c.ShieldMult, 0.001f), p.ShieldMultiplier.ToString());
            Check(t + ": solar charging allowed = " + c.Solar, p.SolarChargingAllowed == c.Solar);
            if (!c.Solar) Check(t + ": solar charging never active", !p.SolarChargingActive);
        }

        private void CheckFreshMission(Case c, string tag)
        {
            MissionRuntimeProbe p = bridge.CaptureProbe();
            float o, pw, s;
            bridge.ReadScenarioStartingResources(c.Asset, out o, out pw, out s);

            Check(tag + ": mission Running, not terminal", p.MissionRunning && !p.MissionTerminal);
            Check(tag + ": timer restarted (<1.5s)", p.MissionElapsed < 1.5f, p.MissionElapsed.ToString("0.00"));
            Check(tag + ": resources = scenario start (+-1)", Near(p.Oxygen, o, 1f) && Near(p.Power, pw, 1f) && Near(p.Shield, s, 1f),
                p.Oxygen.ToString("0.0") + "/" + p.Power.ToString("0.0") + "/" + p.Shield.ToString("0.0"));
            Check(tag + ": objectives all reset", p.ObjectivesCompleted == 0, p.ObjectivesCompleted + "/" + p.ObjectivesTotal);
            Check(tag + ": science download flag reset", !p.ScienceDownloaded);
            Check(tag + ": shield reinforcement count reset", p.ShieldReinforcements == 0);
            Check(tag + ": daylight active again with a full window", p.DaylightActive && p.DaylightRemaining > 250f, p.DaylightRemaining.ToString("0.0"));
            Check(tag + ": solar permission matches scenario", p.SolarChargingAllowed == c.Solar && (!c.Solar ? !p.SolarChargingActive : p.SolarChargingActive));
            Check(tag + ": results panel hidden", !resultsView.IsVisible);
        }

        private void CheckResultsPanel(Case c, MissionResultsSnapshot snap, bool success)
        {
            Check(c.Label + ": results panel visible", resultsView.IsVisible);
            string text = resultsView.RenderedSummary ?? string.Empty;
            Check(c.Label + ": panel shows " + (success ? "SUCCESS" : "FAILED"), text.Contains(success ? "MISSION SUCCESS" : "MISSION FAILED"));
            Check(c.Label + ": panel shows Sol and name", text.Contains("Sol " + snap.SolNumber + " - " + snap.MissionName));
            Check(c.Label + ": panel Oxygen = snapshot", text.Contains("Final Oxygen:  " + Mathf.Clamp(snap.Oxygen, 0f, 100f).ToString("0") + "%"));
            Check(c.Label + ": panel Power = snapshot", text.Contains("Final Power:   " + Mathf.Clamp(snap.Power, 0f, 100f).ToString("0") + "%"));
            Check(c.Label + ": panel Shield = snapshot", text.Contains("Final Shield:  " + Mathf.Clamp(snap.Shield, 0f, 100f).ToString("0") + "%"));
            Check(c.Label + ": panel objective count = snapshot", text.Contains("Objectives completed: " + snap.CompletedCount + " / " + snap.TotalCount));
            if (liveAtEnd != null)
                Check(c.Label + ": snapshot = ResourceManager value at the moment of ending",
                    Near(snap.Oxygen, liveAtEnd.Oxygen, 0.001f) && Near(snap.Power, liveAtEnd.Power, 0.001f) && Near(snap.Shield, liveAtEnd.Shield, 0.001f));
        }

        // ================================================================ helpers
        private static bool Near(float a, float b, float tolerance) { return Mathf.Abs(a - b) <= tolerance; }

        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private static IEnumerator Real(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        private void Section(string title)
        {
            string line = "[Stage10] ---- " + title;
            report.AppendLine(line);
            Debug.Log(line);
        }

        private void Info(string message)
        {
            string line = "[Stage10] INFO  " + message;
            report.AppendLine(line);
            Debug.Log(line);
        }

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) { Passed++; string l = "[Stage10] PASS  " + name; report.AppendLine(l); Debug.Log(l); }
            else
            {
                Failed++;
                string l = "[Stage10] FAIL  " + name + (detail.Length > 0 ? "   (" + detail + ")" : "");
                report.AppendLine(l);
                Debug.LogError(l, this);
            }
        }

        private void Skip(string name, string why)
        {
            Skipped++;
            string l = "[Stage10] SKIP  " + name + " - " + why;
            report.AppendLine(l);
            Debug.LogWarning(l, this);
        }
    }
}
