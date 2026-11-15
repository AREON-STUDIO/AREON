// Assets/AREON/Scripts/UI/MissionResultsView.cs
// Stage 10 - Task 46 (results panel) and Task 47 (restart / selection buttons)
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using AREON.Missions;

namespace AREON.UI
{
    /// <summary>
    /// Shows the mission results panel ONLY when MissionFlowBridge reports a terminal state.
    /// It renders from the MissionResultsSnapshot and never reads, stores or calculates
    /// Oxygen / Power / Shield itself.
    ///
    /// Put this component on an ALWAYS-ACTIVE object (MissionResultsRoot). The visible panel is the
    /// inactive child referenced by panelRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionResultsView : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private MissionFlowBridge bridge;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image headerImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text reasonText;
        [SerializeField] private Text objectivesText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text oxygenText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text shieldText;
        [SerializeField] private Text feedbackText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button selectionButton;

        [Header("Colours")]
        [SerializeField] private Color successColor = new Color(0.20f, 0.65f, 0.30f, 1f);
        [SerializeField] private Color failureColor = new Color(0.75f, 0.20f, 0.20f, 1f);

        [Header("Keyboard shortcuts (only while the panel is visible)")]
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key restartKey = Key.R;
        [SerializeField] private Key selectionKey = Key.M;
#else
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode selectionKey = KeyCode.M;
#endif

        // Exposed for validation (Stage10TestHarness).
        public bool IsVisible { get { return panelRoot != null && panelRoot.activeSelf; } }
        public int ShowCount { get; private set; }
        public string RenderedSummary { get; private set; }
        public MissionResultsSnapshot DisplayedSnapshot { get; private set; }

        private bool subscribed;

        private void Awake()
        {
            if (!Validate()) { enabled = false; return; }
            Hide();
        }

        private void OnEnable()
        {
            if (bridge == null || subscribed) return;
            bridge.MissionEnded += HandleMissionEnded;
            bridge.MissionFlowReset += Hide;
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (selectionButton != null) selectionButton.onClick.AddListener(OnSelectionClicked);
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            bridge.MissionEnded -= HandleMissionEnded;
            bridge.MissionFlowReset -= Hide;
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
            if (selectionButton != null) selectionButton.onClick.RemoveListener(OnSelectionClicked);
            subscribed = false;
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (WasPressed(restartKey)) OnRestartClicked();
            else if (WasPressed(selectionKey)) OnSelectionClicked();
        }

#if ENABLE_INPUT_SYSTEM
        private static bool WasPressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }
#else
        private static bool WasPressed(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }
#endif

        private bool Validate()
        {
            bool ok = true;
            ok &= Need(bridge, "bridge");
            ok &= Need(panelRoot, "panelRoot");
            ok &= Need(titleText, "titleText");
            ok &= Need(subtitleText, "subtitleText");
            ok &= Need(reasonText, "reasonText");
            ok &= Need(objectivesText, "objectivesText");
            ok &= Need(summaryText, "summaryText");
            ok &= Need(oxygenText, "oxygenText");
            ok &= Need(powerText, "powerText");
            ok &= Need(shieldText, "shieldText");
            ok &= Need(feedbackText, "feedbackText");
            ok &= Need(restartButton, "restartButton");
            ok &= Need(selectionButton, "selectionButton");
            return ok;
        }

        private bool Need(Object reference, string field)
        {
            if (reference != null) return true;
            Debug.LogError("[MissionResultsView] Serialized reference '" + field + "' is not assigned.", this);
            return false;
        }

        // ------------------------------------------------------------ show / hide
        private void HandleMissionEnded(MissionResultsSnapshot snapshot)
        {
            Render(snapshot);
            panelRoot.SetActive(true);
            ShowCount++;
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            DisplayedSnapshot = null;
            RenderedSummary = string.Empty;
        }

        // ------------------------------------------------------------ rendering (snapshot only)
        private void Render(MissionResultsSnapshot s)
        {
            DisplayedSnapshot = s;
            bool success = s.Outcome == MissionResultOutcome.Success;
            Color color = success ? successColor : failureColor;

            titleText.text = success ? "MISSION SUCCESS" : "MISSION FAILED";
            titleText.color = color;
            if (headerImage != null) headerImage.color = new Color(color.r, color.g, color.b, 0.25f);

            subtitleText.text = "Sol " + s.SolNumber + " - " + s.MissionName;
            reasonText.text = s.EndReason;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Objectives.Count; i++)
            {
                ObjectiveResultEntry e = s.Objectives[i];
                sb.Append(e.Completed ? "[X] " : "[ ] ");
                sb.Append(e.Required ? "(Required) " : "(Optional) ");
                sb.Append(e.Title);
                if (i < s.Objectives.Count - 1) sb.Append('\n');
            }
            objectivesText.text = sb.Length > 0 ? sb.ToString() : "(no objectives configured)";

            summaryText.text = "Objectives completed: " + s.CompletedCount + " / " + s.TotalCount +
                               "   (required " + s.RequiredCompleted + " / " + s.RequiredTotal +
                               ", optional " + s.OptionalCompleted + " / " + s.OptionalTotal + ")";

            oxygenText.text = "Final Oxygen:  " + Format(s.Oxygen);
            powerText.text = "Final Power:   " + Format(s.Power);
            shieldText.text = "Final Shield:  " + Format(s.Shield);

            feedbackText.text = success
                ? "Well done! Outpost Ares is stable. You balanced science and survival."
                : "Outpost Ares was lost. Try again: spend Power on survival first, science second.";
            feedbackText.color = color;

            RenderedSummary = titleText.text + "|" + subtitleText.text + "|" + oxygenText.text + "|" +
                              powerText.text + "|" + shieldText.text + "|" + summaryText.text;
        }

        // ADAPT: use the SAME rounding as the HUD so the results panel matches what the player last saw.
        private static string Format(float value)
        {
            return Mathf.Clamp(value, 0f, 100f).ToString("0") + "%";
        }

        // ------------------------------------------------------------ buttons
        private void OnRestartClicked()
        {
            bridge.RestartCurrentMission();
        }

        private void OnSelectionClicked()
        {
            bridge.ReturnToMissionSelection();
        }
    }
}
