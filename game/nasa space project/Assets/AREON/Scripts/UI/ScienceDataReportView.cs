using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AREON.Missions
{
    /// <summary>
    /// Shows the curated science data packet after a SUCCESSFUL science download.
    /// It does not touch the ScienceTerminal or its 15-Power transaction: it only listens to the
    /// neutral ScienceDownloaded event from the adapter. Reopen with the button or the toggle key.
    /// </summary>
    public class ScienceDataReportView : MonoBehaviour
    {
        [SerializeField] private MissionGameplayAdapter gameplay;
        [SerializeField] private MissionManager mission;
        [SerializeField] private TelemetryManager telemetry;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text reportText;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject reopenRoot;
        [SerializeField] private Button reopenButton;
        [SerializeField] private Key toggleKey = Key.T;

        private bool packetReceived;

        public bool PacketReceived => packetReceived;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnEnable()
        {
            if (gameplay == null || mission == null || telemetry == null || panelRoot == null ||
                reportText == null || closeButton == null || reopenRoot == null || reopenButton == null)
            {
                Debug.LogError("[ScienceDataReportView] Missing serialized references.", this);
                enabled = false;
                return;
            }
            gameplay.ScienceDownloaded += OnScienceDownloaded;
            mission.StateChanged += OnStateChanged;
            closeButton.onClick.AddListener(Close);
            reopenButton.onClick.AddListener(Open);
            ResetView();
        }

        private void OnDisable()
        {
            if (gameplay != null) gameplay.ScienceDownloaded -= OnScienceDownloaded;
            if (mission != null) mission.StateChanged -= OnStateChanged;
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (reopenButton != null) reopenButton.onClick.RemoveListener(Open);
        }

        private void Update()
        {
            if (packetReceived && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                if (IsOpen) Close(); else Open();
            }
        }

        private void OnScienceDownloaded()
        {
            if (mission.State != MissionState.Active || !telemetry.HasData) return;
            packetReceived = true;
            Open();
        }

        private void OnStateChanged(MissionState from, MissionState to)
        {
            if (to == MissionState.Active || to == MissionState.NotSelected) ResetView();
        }

        private void ResetView()
        {
            packetReceived = false;
            panelRoot.SetActive(false);
            reopenRoot.SetActive(false);
        }

        private void Open()
        {
            if (!packetReceived || !telemetry.HasData) return;
            reportText.text = telemetry.BuildScienceReportText(telemetry.Current);
            panelRoot.SetActive(true);
            reopenRoot.SetActive(false);
        }

        private void Close()
        {
            panelRoot.SetActive(false);
            reopenRoot.SetActive(packetReceived);
        }
    }
}
