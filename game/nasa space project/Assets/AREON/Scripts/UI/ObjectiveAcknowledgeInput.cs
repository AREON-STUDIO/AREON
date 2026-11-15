using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AREON.Missions
{
    /// <summary>
    /// Explicit acknowledgement for the "learn resource monitoring" objective.
    /// The player must press the button (or hotkey) - merely showing the HUD proves nothing.
    /// The script sits on an always-active object; 'buttonRoot' (child) is shown only while an
    /// incomplete Acknowledgement objective with this key exists in an Active mission.
    /// </summary>
    public class ObjectiveAcknowledgeInput : MonoBehaviour
    {
        [SerializeField] private MissionManager mission;
        [SerializeField] private ObjectiveManager objectives;
        [SerializeField] private Button button;
        [SerializeField] private GameObject buttonRoot;
        [SerializeField] private string acknowledgementKey = ObjectiveKeys.ResourceMonitoring;
        [SerializeField] private Key hotkey = Key.R;

        private bool visible;

        private void OnEnable()
        {
            if (mission == null || objectives == null || button == null || buttonRoot == null)
            {
                Debug.LogError("[ObjectiveAcknowledgeInput] Missing serialized references.", this);
                enabled = false;
                return;
            }
            button.onClick.AddListener(Acknowledge);
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Acknowledge);
        }

        private void Update()
        {
            bool shouldShow = mission.State == MissionState.Active &&
                              objectives.HasIncompleteObjective(ObjectiveTriggerType.Acknowledgement, acknowledgementKey);
            if (shouldShow != visible) SetVisible(shouldShow);

            if (visible && Keyboard.current != null && Keyboard.current[hotkey].wasPressedThisFrame)
                Acknowledge();
        }

        private void Acknowledge() => objectives.ReportAcknowledgement(acknowledgementKey);

        private void SetVisible(bool value)
        {
            visible = value;
            buttonRoot.SetActive(value);
        }
    }
}
