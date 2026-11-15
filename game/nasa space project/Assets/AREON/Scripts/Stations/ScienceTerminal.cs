using UnityEngine;
using AREON.UI;
using AREON.Missions;

namespace AREON.Stations
{
    /// <summary>
    /// Research Center - Science Terminal.
    /// Stage 7: runs the science download transaction through ScienceDownloadAction
    /// and displays feedback on the shared FacilityInteractionPanel.
    /// </summary>
    public class ScienceTerminal : FacilityStation, IMissionResettable
    {
        public void ResetRuntimeState()
        {
            if (downloadAction != null) downloadAction.ResetDownloads();
        }
        [Header("Stage 7 - Action Component")]
        [SerializeField] private ScienceDownloadAction downloadAction;

        public ScienceDownloadAction DownloadAction => downloadAction;

        protected override string DefaultInteractionName => "Science Terminal";

        protected override FacilityInfo CreateDefaultInfo()
        {
            float cost = downloadAction != null ? downloadAction.PowerCost : 15f;
            return new FacilityInfo
            {
                facilityName = "Research Center",
                purpose = "Collects Mars science telemetry. Downloading data costs Power the outpost also needs to survive.",
                actionName = "Download Science Telemetry",
                powerCost = cost,
                statusText = "Terminal ready",
                accentColor = new Color(0.65f, 0.55f, 1.00f, 1f)
            };
        }

        protected override void Awake()
        {
            base.Awake();
            if (downloadAction == null)
            {
                downloadAction = GetComponent<ScienceDownloadAction>();
                if (downloadAction == null)
                {
                    Debug.LogError("[ScienceTerminal] ScienceDownloadAction reference is missing. Assign it in the Inspector.", this);
                }
            }
        }

        protected override void OnConfirm()
        {
            ConfirmDownload();
        }

        public void ConfirmDownload()
        {
            if (downloadAction == null)
            {
                Debug.LogError("[ScienceTerminal] ScienceDownloadAction reference is missing. Cannot perform download.", this);
                return;
            }

            ActionResult result = downloadAction.TryDownload();
            if (panel != null)
            {
                panel.ShowFeedback(result.Message, result.Succeeded);
            }
        }
    }
}
