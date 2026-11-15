// Assets/AREON/Scripts/UI/MissionSelectionView.cs
// Stage 10 - Task 47
using UnityEngine;
using UnityEngine.UI;
using AREON.Missions;
// ADAPT[0]: add the namespace of MissionScenarioData if it is not AREON.Missions.

namespace AREON.UI
{
    /// <summary>
    /// Minimal mission-selection panel. It is shown only after "Mission Selection" is chosen on the
    /// results panel. It READS the three existing scenario assets (never writes them) and asks
    /// MissionFlowBridge to start the chosen one through the existing initialization sequence.
    /// Always-active host object; the visible panel is the inactive child panelRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionSelectionView : MonoBehaviour
    {
        [SerializeField] private MissionFlowBridge bridge;
        [SerializeField] private GameObject panelRoot;

        [Header("Parallel arrays: element i of each array belongs together")]
        [SerializeField] private MissionScenarioData[] scenarios = new MissionScenarioData[0];
        [SerializeField] private Button[] scenarioButtons = new Button[0];
        [SerializeField] private Text[] scenarioLabels = new Text[0];

        public bool IsVisible { get { return panelRoot != null && panelRoot.activeSelf; } }

        private bool subscribed;

        private void Awake()
        {
            if (!Validate()) { enabled = false; return; }
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (bridge == null || subscribed) return;
            bridge.MissionSelectionRequested += Show;
            bridge.MissionStarted += HandleMissionStarted;
            bridge.MissionFlowReset += Hide;

            for (int i = 0; i < scenarioButtons.Length; i++)
            {
                int index = i;                                   // capture a copy for the closure
                scenarioButtons[i].onClick.AddListener(() => Select(index));
            }
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            bridge.MissionSelectionRequested -= Show;
            bridge.MissionStarted -= HandleMissionStarted;
            bridge.MissionFlowReset -= Hide;

            for (int i = 0; i < scenarioButtons.Length; i++)
            {
                if (scenarioButtons[i] != null) scenarioButtons[i].onClick.RemoveAllListeners(); // runtime listeners only
            }
            subscribed = false;
        }

        private bool Validate()
        {
            if (bridge == null || panelRoot == null)
            {
                Debug.LogError("[MissionSelectionView] 'bridge' and 'panelRoot' must be assigned.", this);
                return false;
            }
            if (scenarios.Length == 0 || scenarios.Length != scenarioButtons.Length || scenarios.Length != scenarioLabels.Length)
            {
                Debug.LogError("[MissionSelectionView] scenarios, scenarioButtons and scenarioLabels must be the same non-zero length.", this);
                return false;
            }
            for (int i = 0; i < scenarios.Length; i++)
            {
                if (scenarios[i] == null || scenarioButtons[i] == null || scenarioLabels[i] == null)
                {
                    Debug.LogError("[MissionSelectionView] Element " + i + " has a missing reference.", this);
                    return false;
                }
            }
            return true;
        }

        private void Show()
        {
            for (int i = 0; i < scenarios.Length; i++)
                scenarioLabels[i].text = bridge.DescribeScenario(scenarios[i]);
            panelRoot.SetActive(true);
        }

        private void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void HandleMissionStarted(MissionScenarioData started)
        {
            Hide();
        }

        /// <summary>Same code path as clicking button i. Public so the test harness can drive it.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= scenarios.Length) return;
            bridge.StartScenario(scenarios[index]);
        }
    }
}
