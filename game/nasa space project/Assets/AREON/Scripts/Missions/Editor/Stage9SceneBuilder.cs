using AREON.Stations;
using AREON.ResourceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AREON.Missions.EditorTools
{
    /// <summary>
    /// EDIT-TIME ONLY. Creates the Stage 9 scene objects once (MissionSystems + MissionCanvas),
    /// wires every serialized reference, and reports anything that still needs manual wiring.
    /// Safe to run twice: it aborts if MissionSystems already exists.
    /// </summary>
    public static class Stage9SceneBuilder
    {
        private static Font font;

        [MenuItem("AREON/Stage 9/Build Mission Scene Objects")]
        public static void Build()
        {
            if (GameObject.Find("MissionSystems") != null)
            {
                Debug.LogWarning("[Stage9] MissionSystems already exists. Delete it first to rebuild.");
                return;
            }
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ---------------- systems
            var root = new GameObject("MissionSystems");
            Undo.RegisterCreatedObjectUndo(root, "Stage 9 Build");
            var adapter = Child<MissionGameplayAdapter>(root, "MissionGameplayAdapter");
            var objectives = Child<ObjectiveManager>(root, "ObjectiveManager");
            var telemetry = Child<TelemetryManager>(root, "TelemetryManager");
            var mission = Child<MissionManager>(root, "MissionManager");
            var harness = Child<MissionTestHarness>(root, "MissionTestHarness");

            MissionScenarioData[] scenarios = AssetDatabase.FindAssets("t:MissionScenarioData")
                .Select(g => AssetDatabase.LoadAssetAtPath<MissionScenarioData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null).OrderBy(s => s.solNumber).ToArray();
            MissionScenarioData Sol(int n) => scenarios.FirstOrDefault(s => s.solNumber == n);

            WireAdapter(adapter);
            Wire(objectives, "gameplayEvents", adapter);
            WireArray(telemetry, "knownScenarios", scenarios);
            Wire(mission, "gameplay", adapter);
            Wire(mission, "objectives", objectives);
            Wire(mission, "telemetry", telemetry);
            WireArray(mission, "availableScenarios", scenarios);
            Wire(harness, "mission", mission);
            Wire(harness, "objectives", objectives);
            Wire(harness, "telemetry", telemetry);
            Wire(harness, "gameplay", adapter);
            Wire(harness, "sol800", Sol(800));
            Wire(harness, "sol852", Sol(852));
            Wire(harness, "sol2085", Sol(2085));

            // ---------------- UI
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                Undo.RegisterCreatedObjectUndo(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)), "EventSystem");

            var canvasGo = new GameObject("MissionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "MissionCanvas");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            BuildStatusBar(canvasGo.transform, mission);
            BuildObjectivePanel(canvasGo.transform, mission, objectives);
            BuildTelemetryPanel(canvasGo.transform, telemetry);
            BuildAcknowledge(canvasGo.transform, mission, objectives);
            BuildScienceReport(canvasGo.transform, adapter, mission, telemetry);

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Stage9] Scene objects built. Check the log above for any 'MANUAL WIRING' lines.");
        }

        [MenuItem("AREON/Stage 9/Finalize Scene Configuration")]
        public static void FinalizeScene()
        {
            var wm = Object.FindFirstObjectByType<WeatherManager>();
            if (wm != null)
            {
                var so = new SerializedObject(wm);
                var prop = so.FindProperty("applyDefaultScenarioOnStart");
                if (prop != null)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(wm);
                    Debug.Log("[Stage9] WeatherManager.applyDefaultScenarioOnStart set to false.");
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Stage9] Scene configuration saved successfully.");
        }

        [MenuItem("AREON/Stage 9/Run Test Suite in PlayMode")]
        public static void RunTestSuite()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Stage9] Unity must be in Play Mode to run the test suite.");
                return;
            }
            Application.runInBackground = true;
            PlayerSettings.runInBackground = true;
            var harness = Object.FindFirstObjectByType<MissionTestHarness>();
            if (harness != null)
            {
                Debug.Log($"[Stage9] Harness found. IsRunning: {harness.IsRunning}, time: {Time.time}, runInBackground: {Application.runInBackground}");
                harness.RunAutomatedTests();
                Debug.Log("[Stage9] Automated test suite initiated on MissionTestHarness.");
            }
            else
            {
                Debug.LogError("[Stage9] MissionTestHarness not found in active scene.");
            }
        }

        [MenuItem("AREON/Stage 9/Run Stage 7 Regression Tests")]
        public static void RunStage7Tests()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Stage9] Must be in Play Mode."); return; }
            var rm = Object.FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.SetSimulationActive(true);
            var dm = Object.FindFirstObjectByType<DaylightManager>();
            if (dm != null) dm.SetRunning(true);
            var h7 = Object.FindFirstObjectByType<ActionEconomyTestHarness>();
            if (h7 != null)
            {
                h7.RunAll();
                Debug.Log("[Stage9] Stage 7 ActionEconomyTestHarness started.");
            }
            else Debug.LogWarning("[Stage9] ActionEconomyTestHarness not found.");
        }

        [MenuItem("AREON/Stage 9/Run Stage 8 Regression Tests")]
        public static void RunStage8Tests()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Stage9] Must be in Play Mode."); return; }
            var rm = Object.FindFirstObjectByType<ResourceManager>();
            if (rm != null) rm.SetSimulationActive(true);
            var dm = Object.FindFirstObjectByType<DaylightManager>();
            if (dm != null) dm.SetRunning(true);
            var h8 = Object.FindFirstObjectByType<ScenarioSwitchTestHarness>();
            if (h8 != null)
            {
                h8.RunScenarioTests();
                Debug.Log("[Stage9] Stage 8 ScenarioSwitchTestHarness started.");
            }
            else Debug.LogWarning("[Stage9] ScenarioSwitchTestHarness not found.");
        }

        [MenuItem("AREON/Stage 9/Interactive Start Sol 800")]
        public static void InteractiveSol800()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Stage9] Must be in Play Mode."); return; }
            var harness = Object.FindFirstObjectByType<MissionTestHarness>();
            if (harness != null)
            {
                harness.StartSol800();
                Debug.Log("[Stage9] Interactive Sol 800 started.");
            }
        }

        [MenuItem("AREON/Stage 9/Interactive Start Sol 852")]
        public static void InteractiveSol852()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Stage9] Must be in Play Mode."); return; }
            var harness = Object.FindFirstObjectByType<MissionTestHarness>();
            if (harness != null)
            {
                harness.StartSol852();
                Debug.Log("[Stage9] Interactive Sol 852 started.");
            }
        }

        [MenuItem("AREON/Stage 9/Interactive Start Sol 2085")]
        public static void InteractiveSol2085()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Stage9] Must be in Play Mode."); return; }
            var harness = Object.FindFirstObjectByType<MissionTestHarness>();
            if (harness != null)
            {
                harness.StartSol2085();
                Debug.Log("[Stage9] Interactive Sol 2085 started.");
            }
        }

        // ------------------------------------------------------------------ adapter wiring

        private static void WireAdapter(MissionGameplayAdapter adapter)
        {
            WireFound(adapter, "resourceManager", typeof(ResourceManager));
            WireFound(adapter, "weatherManager", typeof(WeatherManager));
            WireFound(adapter, "daylightManager", typeof(DaylightManager));
            WireFound(adapter, "solarPermission", typeof(SolarChargingPermissionSource));
            WireFound(adapter, "solarGenerator", typeof(SolarGenerator));
            WireFound(adapter, "scienceDownload", typeof(ScienceDownloadAction));
            WireFound(adapter, "shieldReinforcement", typeof(ShieldReinforcementAction));

            var stations = Object.FindObjectsByType(typeof(FacilityStation), FindObjectsSortMode.None)
                .Cast<Component>().ToList();
            var so = new SerializedObject(adapter);
            SerializedProperty list = so.FindProperty("facilities");
            list.arraySize = 3;
            Bind(list, 0, ObjectiveKeys.BaseDome, stations, "dome", "base");
            Bind(list, 1, ObjectiveKeys.ResearchCenter, stations, "research", "science", "lab");
            Bind(list, 2, ObjectiveKeys.PowerGenerator, stations, "generator", "power", "solar");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Bind(SerializedProperty list, int index, string id, List<Component> stations, params string[] keywords)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(index);
            e.FindPropertyRelative("facilityId").stringValue = id;
            Component match = stations.FirstOrDefault(c => keywords.Any(k => PathOf(c).Contains(k)));
            e.FindPropertyRelative("station").objectReferenceValue = match;
            if (match == null) Debug.LogWarning($"[Stage9] MANUAL WIRING: no FacilityStation matched '{id}'. Assign MissionGameplayAdapter.facilities[{index}].station.");
            else Debug.Log($"[Stage9] Facility '{id}' bound to {PathOf(match)}");
        }

        private static string PathOf(Component c)
        {
            string path = string.Empty;
            for (Transform t = c.transform; t != null; t = t.parent) path = t.name.ToLowerInvariant() + "/" + path;
            return path;
        }

        // ------------------------------------------------------------------ UI builders

        private static void BuildStatusBar(Transform canvas, MissionManager mission)
        {
            RectTransform bar = Panel(canvas, "MissionStatusBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(760, 70));
            var view = bar.gameObject.AddComponent<MissionStatusView>();
            Text status = Label(bar, "StatusText", "", 20, TextAnchor.MiddleCenter);
            Stretch(status.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1));
            Text banner = Label(bar, "BannerText", "", 22, TextAnchor.MiddleCenter);
            banner.fontStyle = FontStyle.Bold;
            Stretch(banner.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f));
            Wire(view, "mission", mission);
            Wire(view, "statusText", status);
            Wire(view, "bannerText", banner);
        }

        private static void BuildObjectivePanel(Transform canvas, MissionManager mission, ObjectiveManager objectives)
        {
            RectTransform panel = Panel(canvas, "ObjectivePanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(480, 560));
            var view = panel.gameObject.AddComponent<ObjectiveListView>();

            RectTransform content = NewRect("Content", panel);
            Stretch(content, Vector2.zero, Vector2.one);
            VerticalLayout(content.gameObject, 12, 8);
            Text header = Label(content, "Header", "MISSION OBJECTIVES", 22, TextAnchor.MiddleLeft);
            header.fontStyle = FontStyle.Bold;
            Text progress = Label(content, "Progress", "", 14, TextAnchor.MiddleLeft);
            RectTransform list = NewRect("ListRoot", content);
            VerticalLayout(list.gameObject, 0, 6);

            Wire(view, "objectives", objectives);
            Wire(view, "mission", mission);
            Wire(view, "contentRoot", content.gameObject);
            Wire(view, "headerText", header);
            Wire(view, "progressText", progress);
            Wire(view, "listRoot", list);
        }

        private static void BuildTelemetryPanel(Transform canvas, TelemetryManager telemetry)
        {
            RectTransform panel = Panel(canvas, "TelemetryPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(430, 330));
            var view = panel.gameObject.AddComponent<TelemetryPanelView>();
            RectTransform content = NewRect("Content", panel);
            Stretch(content, Vector2.zero, Vector2.one);
            Text body = Label(content, "BodyText", "", 18, TextAnchor.UpperLeft);
            Stretch(body.rectTransform, Vector2.zero, Vector2.one);
            body.rectTransform.offsetMin = new Vector2(14, 14);
            body.rectTransform.offsetMax = new Vector2(-14, -14);
            Wire(view, "telemetry", telemetry);
            Wire(view, "contentRoot", content.gameObject);
            Wire(view, "bodyText", body);
        }

        private static void BuildAcknowledge(Transform canvas, MissionManager mission, ObjectiveManager objectives)
        {
            RectTransform root = NewRect("AcknowledgeResourceReadout", canvas);
            Anchor(root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(520, 60));
            var input = root.gameObject.AddComponent<ObjectiveAcknowledgeInput>();
            Button button = MakeButton(root, "AcknowledgeButton", "ACKNOWLEDGE RESOURCE READOUT [R]");
            Stretch((RectTransform)button.transform, Vector2.zero, Vector2.one);
            Wire(input, "mission", mission);
            Wire(input, "objectives", objectives);
            Wire(input, "button", button);
            Wire(input, "buttonRoot", button.gameObject);
        }

        private static void BuildScienceReport(Transform canvas, MissionGameplayAdapter adapter, MissionManager mission, TelemetryManager telemetry)
        {
            RectTransform root = NewRect("ScienceDataReport", canvas);
            Stretch(root, Vector2.zero, Vector2.one);
            var view = root.gameObject.AddComponent<ScienceDataReportView>();

            RectTransform panel = Panel(root, "ReportPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 460));
            Text text = Label(panel, "ReportText", "", 18, TextAnchor.UpperLeft);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            text.rectTransform.offsetMin = new Vector2(16, 70);
            text.rectTransform.offsetMax = new Vector2(-16, -16);
            Button close = MakeButton(panel, "CloseButton", "CLOSE [T]");
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(200, 44));

            Button reopen = MakeButton(root, "ReopenButton", "VIEW SCIENCE DATA [T]");
            Anchor((RectTransform)reopen.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(320, 50));

            Wire(view, "gameplay", adapter);
            Wire(view, "mission", mission);
            Wire(view, "telemetry", telemetry);
            Wire(view, "panelRoot", panel.gameObject);
            Wire(view, "reportText", text);
            Wire(view, "closeButton", close);
            Wire(view, "reopenRoot", reopen.gameObject);
            Wire(view, "reopenButton", reopen);
        }

        // ------------------------------------------------------------------ small helpers

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform NewRect(string name, RectTransform parent) => NewRect(name, (Transform)parent);

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = NewRect(name, parent);
            var rect = rt;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = false;
            return rt;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static Text Label(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            RectTransform rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.alignment = anchor;
            t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            RectTransform rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.15f, 0.35f, 0.6f, 0.95f);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            Text t = Label(rt, "Label", label, 18, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void VerticalLayout(GameObject go, int padding, int spacing)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.spacing = spacing;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        }

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Stage9] Field '{field}' not found on {target.GetType().Name}."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (value == null) Debug.LogWarning($"[Stage9] MANUAL WIRING: {target.GetType().Name}.{field} is empty.");
        }

        private static void WireFound(Object target, string field, System.Type type) =>
            Wire(target, field, Object.FindFirstObjectByType(type));

        private static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Stage9] Field '{field}' not found on {target.GetType().Name}."); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
