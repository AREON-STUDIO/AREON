using UnityEngine;
using UnityEngine.UI;

namespace AREON.Missions
{
    /// <summary>
    /// Always-visible telemetry panel (Sol, pressure, solar event, weather, disclosure).
    /// Script sits on the panel ROOT; 'contentRoot' child is shown only while telemetry has data.
    /// </summary>
    public class TelemetryPanelView : MonoBehaviour
    {
        [SerializeField] private TelemetryManager telemetry;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Text bodyText;

        private void OnEnable()
        {
            if (telemetry == null || bodyText == null)
            {
                Debug.LogError("[TelemetryPanelView] Missing serialized references.", this);
                enabled = false;
                return;
            }
            telemetry.TelemetryChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (telemetry != null) telemetry.TelemetryChanged -= Refresh;
        }

        private void Refresh()
        {
            if (contentRoot != null) contentRoot.SetActive(telemetry.HasData);
            if (telemetry.HasData) bodyText.text = telemetry.BuildPanelText(telemetry.Current);
        }
    }
}
