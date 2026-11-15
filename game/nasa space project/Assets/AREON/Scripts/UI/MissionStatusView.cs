using UnityEngine;
using UnityEngine.UI;

namespace AREON.Missions
{
    /// <summary>
    /// One-line mission status (scenario, state, time remaining) and a plain outcome line.
    /// This is NOT the Stage 10 results panel - it shows no resource summary or objective recap.
    /// </summary>
    public class MissionStatusView : MonoBehaviour
    {
        [SerializeField] private MissionManager mission;
        [SerializeField] private Text statusText;
        [SerializeField] private Text bannerText;

        private void OnEnable()
        {
            if (mission == null || statusText == null)
            {
                Debug.LogError("[MissionStatusView] Missing serialized references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            MissionScenarioData s = mission.SelectedScenario;
            string scenario = s == null ? "-" : $"SOL {s.solNumber} - {s.displayName}".ToUpperInvariant();

            switch (mission.State)
            {
                case MissionState.NotSelected:
                    statusText.text = "NO MISSION SELECTED";
                    break;
                case MissionState.Ready:
                    statusText.text = $"READY: {scenario}";
                    break;
                case MissionState.Active:
                    statusText.text = $"{scenario}   TIME LEFT {FormatTime(mission.RemainingSeconds)}";
                    break;
                default:
                    statusText.text = scenario;
                    break;
            }

            if (bannerText == null) return;
            switch (mission.State)
            {
                case MissionState.Completed: bannerText.text = "MISSION COMPLETE"; break;
                case MissionState.Failed: bannerText.text = "MISSION FAILED: " + Describe(mission.FailureReason); break;
                default: bannerText.text = string.Empty; break;
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static string Describe(MissionFailureReason reason)
        {
            switch (reason)
            {
                case MissionFailureReason.OxygenDepleted: return "OXYGEN DEPLETED";
                case MissionFailureReason.ShieldDepleted: return "SHIELD DEPLETED";
                case MissionFailureReason.TimeExpiredObjectivesIncomplete: return "TIME EXPIRED, OBJECTIVES INCOMPLETE";
                default: return "ENDED";
            }
        }
    }
}
