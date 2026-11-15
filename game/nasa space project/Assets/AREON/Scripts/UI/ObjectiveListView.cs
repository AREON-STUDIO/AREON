using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AREON.Missions
{
    /// <summary>
    /// Simple objective list. Display only - it reads ObjectiveManager and never changes progress.
    /// Place this script on the panel ROOT (always active). 'contentRoot' is a child that is shown
    /// only while a mission is Active / Completed / Failed.
    /// </summary>
    public class ObjectiveListView : MonoBehaviour
    {
        [SerializeField] private ObjectiveManager objectives;
        [SerializeField] private MissionManager mission;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Text headerText;
        [SerializeField] private Text progressText;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private bool showDescriptions = true;
        [SerializeField] private int rowFontSize = 16;
        [SerializeField] private Color incompleteColor = Color.white;
        [SerializeField] private Color optionalColor = new Color(0.65f, 0.85f, 1f);
        [SerializeField] private Color completedColor = new Color(0.45f, 1f, 0.55f);

        private readonly List<Text> rows = new List<Text>();

        private void OnEnable()
        {
            if (objectives == null || mission == null || listRoot == null || headerText == null)
            {
                Debug.LogError("[ObjectiveListView] Missing serialized references.", this);
                enabled = false;
                return;
            }
            objectives.ObjectivesChanged += Rebuild;
            objectives.ObjectiveProgressed += OnProgress;
            mission.StateChanged += OnStateChanged;
            headerText.text = "MISSION OBJECTIVES";
            Rebuild();
            ApplyVisibility();
        }

        private void OnDisable()
        {
            if (objectives != null)
            {
                objectives.ObjectivesChanged -= Rebuild;
                objectives.ObjectiveProgressed -= OnProgress;
            }
            if (mission != null) mission.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(MissionState from, MissionState to) => ApplyVisibility();

        private void ApplyVisibility()
        {
            if (contentRoot != null)
                contentRoot.SetActive(mission.State == MissionState.Active ||
                                      mission.State == MissionState.Completed ||
                                      mission.State == MissionState.Failed);
        }

        private void OnProgress(ObjectiveRuntime _) => Refresh();

        private void Rebuild()
        {
            foreach (Text row in rows) if (row != null) Destroy(row.gameObject);
            rows.Clear();

            foreach (ObjectiveRuntime r in objectives.All)
            {
                var go = new GameObject("Row_" + r.Id, typeof(RectTransform));
                go.transform.SetParent(listRoot, false);
                Text t = go.AddComponent<Text>();
                t.font = headerText.font;                  // reuse the panel's font, no Resources lookup
                t.fontSize = rowFontSize;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                rows.Add(t);
            }
            Refresh();
        }

        private void Refresh()
        {
            IReadOnlyList<ObjectiveRuntime> list = objectives.All;
            for (int i = 0; i < rows.Count && i < list.Count; i++)
            {
                ObjectiveRuntime r = list[i];
                string mark = r.IsCompleted ? "[X]" : "[ ]";
                string tag = r.IsRequired ? "REQUIRED" : "OPTIONAL";
                string progress = r.ProgressLabel;
                string text = $"{mark} {r.Title}  ({tag})" + (progress.Length > 0 ? "  " + progress : "");
                if (showDescriptions && !string.IsNullOrEmpty(r.Description)) text += "\\n      " + r.Description;
                rows[i].text = text;
                rows[i].color = r.IsCompleted ? completedColor : (r.IsRequired ? incompleteColor : optionalColor);
            }

            if (progressText != null)
            {
                int pct = Mathf.RoundToInt(objectives.RequiredProgress01 * 100f);
                progressText.text =
                    $"REQUIRED {objectives.RequiredCompleted}/{objectives.RequiredTotal} ({pct}%)   " +
                    $"OPTIONAL {objectives.OptionalCompleted}/{objectives.OptionalTotal}";
            }
        }
    }
}
