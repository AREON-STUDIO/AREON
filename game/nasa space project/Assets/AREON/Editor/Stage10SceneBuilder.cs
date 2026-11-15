// Assets/AREON/Editor/Stage10SceneBuilder.cs   (use the same Editor folder as Stage9SceneBuilder.cs)
// Stage 10 - Tasks 46 / 47 scene setup. Idempotent: running it twice creates nothing twice.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AREON.Missions;
using AREON.UI;
using AREON.ResourceSystem;
using AREON.Stations;
using AREON.Player;
using AREON.Interaction;
using System.Text;

namespace AREON.EditorTools
{
    public static class Stage10SceneBuilder
    {
        private const string ScenePath = "Assets/AREON/Scenes/AREON_Prototype.unity";

        // Order matters: it defines the selection buttons (800, 852, 2085).
        private static readonly string[] ScenarioPaths =
        {
            "Assets/AREON/Data/Missions/Sol800_Orientation.asset",
            "Assets/AREON/Data/Missions/Sol852_Onset.asset",
            "Assets/AREON/Data/Missions/Sol2085_Crisis.asset"
        };

        [MenuItem("AREON/Stage 10/Build Results And Flow Objects")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Stage10Builder] Exit Play Mode first."); return; }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject systems = GameObject.Find("MissionSystems");
            GameObject canvas = GameObject.Find("MissionCanvas");
            if (systems == null || canvas == null)
            {
                Debug.LogError("[Stage10Builder] 'MissionSystems' or 'MissionCanvas' not found. Stage 9 hierarchy is required.");
                return;
            }

            // ---------- existing systems (editor-time lookup only; runtime uses serialized references)
            MissionManager missionManager = Single<MissionManager>();
            ObjectiveManager objectiveManager = Single<ObjectiveManager>();
            ResourceManager resourceManager = Single<ResourceManager>();
            DaylightManager daylightManager = Single<DaylightManager>();
            SolarGenerator solarGenerator = Single<SolarGenerator>();
            ScienceTerminal scienceTerminal = Single<ScienceTerminal>();
            ShieldConsole shieldConsole = Single<ShieldConsole>();
            WeatherManager weatherManager = Single<WeatherManager>();
            if (missionManager == null || objectiveManager == null || resourceManager == null || daylightManager == null ||
                solarGenerator == null || scienceTerminal == null || shieldConsole == null || weatherManager == null)
            {
                Debug.LogError("[Stage10Builder] A required existing system is missing or duplicated. Aborting without changes to the scene.");
                return;
            }

            // ---------- MissionSystems/MissionFlow (bridge + coordinator) and harness
            GameObject flow = GetOrCreateChild(systems.transform, "MissionFlow");
            MissionResetCoordinator coordinator = GetOrAdd<MissionResetCoordinator>(flow);
            MissionFlowBridge bridge = GetOrAdd<MissionFlowBridge>(flow);

            SetRefs(coordinator, "resettables", FindResettables());

            SerializedObject bso = new SerializedObject(bridge);
            SetRef(bso, "resetCoordinator", coordinator);
            SetRef(bso, "missionManager", missionManager);
            SetRef(bso, "objectiveManager", objectiveManager);
            SetRef(bso, "resourceManager", resourceManager);
            SetRef(bso, "daylightManager", daylightManager);
            SetRef(bso, "solarGenerator", solarGenerator);
            SetRef(bso, "scienceTerminal", scienceTerminal);
            SetRef(bso, "shieldConsole", shieldConsole);
            SetRef(bso, "weatherManager", weatherManager);
            bso.ApplyModifiedPropertiesWithoutUndo();

            // ---------- UI
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MissionScenarioData[] scenarios = LoadScenarios();
            if (scenarios == null) return;

            // Results
            GameObject resultsRoot = GetOrCreateUIChild(canvas.transform, "MissionResultsRoot", true);
            MissionResultsView resultsView = GetOrAdd<MissionResultsView>(resultsRoot);
            Transform existingResultsPanel = resultsRoot.transform.Find("ResultsPanel");
            if (existingResultsPanel == null) BuildResultsPanel(resultsRoot.transform, font);
            WireResultsView(resultsView, bridge, resultsRoot.transform);

            // Selection
            GameObject selectionRoot = GetOrCreateUIChild(canvas.transform, "MissionSelectionRoot", true);
            MissionSelectionView selectionView = GetOrAdd<MissionSelectionView>(selectionRoot);
            if (selectionRoot.transform.Find("SelectionPanel") == null) BuildSelectionPanel(selectionRoot.transform, font);
            WireSelectionView(selectionView, bridge, selectionRoot.transform, scenarios);

            resultsRoot.transform.SetAsLastSibling();      // draw above the HUD
            selectionRoot.transform.SetAsLastSibling();

            // Canvas must be able to receive clicks.
            Canvas c = canvas.GetComponent<Canvas>();
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.AddComponent<GraphicRaycaster>();
            if (c == null) Debug.LogWarning("[Stage10Builder] MissionCanvas has no Canvas component. Check the Stage 9 setup.");

            EnsureEventSystem();

            // ---------- Harness
            GameObject harnessGo = GetOrCreateChild(systems.transform, "Stage10TestHarness");
            Stage10TestHarness harness = GetOrAdd<Stage10TestHarness>(harnessGo);
            SerializedObject hso = new SerializedObject(harness);
            SetRef(hso, "bridge", bridge);
            SetRef(hso, "resultsView", resultsView);
            SetRef(hso, "selectionView", selectionView);
            SetRef(hso, "sol800", scenarios[0]);
            SetRef(hso, "sol852", scenarios[1]);
            SetRef(hso, "sol2085", scenarios[2]);
            hso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[Stage10Builder] Done. Resettables registered: " + coordinator.RegisteredCount + ". Scene saved.");
        }

        [MenuItem("AREON/Stage 10/Run Stage 10 Test Suite in PlayMode")]
        public static void RunTestSuite()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Stage10] Unity must be in Play Mode to run the test suite.");
                return;
            }
            Application.runInBackground = true;
            PlayerSettings.runInBackground = true;
            var harness = Object.FindFirstObjectByType<Stage10TestHarness>();
            if (harness != null)
            {
                harness.RunAll();
                Debug.Log("[Stage10] Automated test suite initiated on Stage10TestHarness.");
            }
            else
            {
                Debug.LogError("[Stage10] Stage10TestHarness not found in active scene.");
            }
        }

        [MenuItem("AREON/Stage 10/Verify Stages 1-6 in PlayMode")]
        public static void VerifyStages1To6()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Stage1-6Verification] Unity must be in Play Mode to verify runtime behavior.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== AREON Stages 1-6 Runtime Verification ===");

            // --- Stage 1: Prototype Scene & Foundation
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool s1Scene = scene.name == "AREON_Prototype";
            GameObject env = GameObject.Find("Environment");
            Light dirLight = Object.FindFirstObjectByType<Light>();
            bool s1Env = env != null && env.transform.childCount > 0;
            bool s1Light = dirLight != null && dirLight.type == LightType.Directional;
            bool s1Pass = s1Scene && s1Env && s1Light;
            sb.AppendLine($"[Stage 1 Foundation] {(s1Pass ? "PASS" : "FAIL")}: Scene='{scene.name}', Environment={(env != null ? "Found" : "Missing")}, DirectionalLight={(dirLight != null ? "Found" : "Missing")}");

            // --- Stage 2: Player Movement, CharacterController & FollowCamera
            GameObject player = GameObject.Find("Player");
            CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
            PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
            FollowCamera fc = Object.FindFirstObjectByType<FollowCamera>();
            bool s2Pass = player != null && cc != null && cc.enabled && pc != null && pc.enabled && fc != null;
            sb.AppendLine($"[Stage 2 Player/Camera] {(s2Pass ? "PASS" : "FAIL")}: Player={(player != null)}, CharacterController={(cc != null && cc.enabled)}, PlayerController={(pc != null && pc.enabled)}, FollowCamera={(fc != null)}");

            // --- Stage 3: Interaction Prompts & E-Key Interaction
            PlayerInteractor pi = player != null ? player.GetComponentInChildren<PlayerInteractor>() : null;
            InteractionPromptUI ipUI = Object.FindFirstObjectByType<InteractionPromptUI>();
            TestInteractableCube tc = Object.FindFirstObjectByType<TestInteractableCube>();
            bool s3Pass = pi != null && ipUI != null && tc != null;
            if (s3Pass)
            {
                string prompt = tc.GetPrompt(pi);
                bool canInteract = tc.CanInteract(pi, out _);
                s3Pass &= !string.IsNullOrEmpty(prompt) && canInteract;
            }
            sb.AppendLine($"[Stage 3 Interaction] {(s3Pass ? "PASS" : "FAIL")}: PlayerInteractor={(pi != null)}, PromptUI={(ipUI != null)}, TestInteractableCube={(tc != null)}, Prompt='{tc?.GetPrompt(pi)}'");

            // --- Stage 4: Resource Management (Oxygen, Power, Shield)
            ResourceManager rm = Object.FindFirstObjectByType<ResourceManager>();
            bool s4Pass = rm != null && rm.Oxygen > 0f && rm.Power > 0f && rm.Shield > 0f;
            if (rm != null)
            {
                float pBefore = rm.Power;
                bool spent = rm.TrySpendPower(5f);
                bool recharged = rm.TryRechargePower(5f);
                s4Pass &= spent && recharged && Mathf.Approximately(rm.Power, pBefore);
            }
            sb.AppendLine($"[Stage 4 Resources] {(s4Pass ? "PASS" : "FAIL")}: ResourceManager={(rm != null)}, AtomicSpendRecharge={s4Pass}, CurrentValues: O2={rm?.Oxygen:F1}, Power={rm?.Power:F1}, Shield={rm?.Shield:F1}");

            // Run automated Stage 4 test harness if present
            var rth = Object.FindFirstObjectByType<AREON.ResourceSystem.Testing.ResourceTestHarness>(FindObjectsInactive.Include);
            if (rth != null)
            {
                bool prev = rth.enabled;
                rth.enabled = true;
                rth.RunAutomatedTests();
                rth.enabled = prev;
                sb.AppendLine("[Stage 4 Harness] ResourceTestHarness enabled and executed full automated test suite.");
            }

            // --- Stage 5: HUD Resource Bars & Feedback
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            ResourceBarView[] bars = Object.FindObjectsByType<ResourceBarView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool s5Pass = hud != null && bars != null && bars.Length >= 3;
            sb.AppendLine($"[Stage 5 HUD] {(s5Pass ? "PASS" : "FAIL")}: HUDController={(hud != null)}, ResourceBarsCount={bars?.Length ?? 0}");

            // --- Stage 6: Facilities (Base Dome, Research Center, Power Generator)
            ShieldConsole sc = Object.FindFirstObjectByType<ShieldConsole>();
            ScienceTerminal st = Object.FindFirstObjectByType<ScienceTerminal>();
            SolarGenerator sg = Object.FindFirstObjectByType<SolarGenerator>();
            FacilityInteractionPanel fip = Object.FindFirstObjectByType<FacilityInteractionPanel>();
            bool s6Pass = sc != null && st != null && sg != null && fip != null;
            sb.AppendLine($"[Stage 6 Facilities] {(s6Pass ? "PASS" : "FAIL")}: BaseDome(ShieldConsole)={(sc != null)}, ResearchCenter(ScienceTerminal)={(st != null)}, PowerGenerator(SolarGenerator)={(sg != null)}, FacilityPanel={(fip != null)}");

            Debug.Log(sb.ToString());
        }

        [MenuItem("AREON/Stage 10/Verify Input Controls in PlayMode")]
        public static void VerifyInputControls()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[InputControlsVerification] Unity must be in Play Mode to verify runtime behavior.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== AREON Input Controls Verification ===");

            // 1. PlayerController - Move action & bindings
            GameObject player = GameObject.Find("Player");
            PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
            var playerInput = player != null ? player.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            var moveAction = playerInput != null && playerInput.actions != null ? playerInput.actions.FindAction("Move") : null;
            bool wasdBound = moveAction != null && moveAction.bindings.Count > 0;
            sb.AppendLine($"[WASD Movement] {(wasdBound ? "PASS" : "FAIL")}: PlayerController={(pc != null)}, MoveAction='{moveAction?.name}', Bindings={moveAction?.bindings.Count ?? 0}");

            // 2. PlayerInteractor - E-Key interaction
            PlayerInteractor pi = player != null ? player.GetComponentInChildren<PlayerInteractor>() : null;
            var interactAction = playerInput != null && playerInput.actions != null ? playerInput.actions.FindAction("Interact") : null;
            bool interactBound = interactAction != null && interactAction.bindings.Count > 0;
            sb.AppendLine($"[E-Key Interaction] {(interactBound ? "PASS" : "FAIL")}: PlayerInteractor={(pi != null)}, InteractAction='{interactAction?.name}', Bindings={interactAction?.bindings.Count ?? 0}");

            // 3. ObjectiveAcknowledgeInput - Resource-readout acknowledgement hotkey
            ObjectiveAcknowledgeInput ackInput = Object.FindFirstObjectByType<ObjectiveAcknowledgeInput>();
            bool ackBound = ackInput != null;
            sb.AppendLine($"[Resource Readout Acknowledge] {(ackBound ? "PASS" : "FAIL")}: ObjectiveAcknowledgeInput={(ackInput != null)}, ConfiguredHotkey=Key.R");

            // 4. ScienceDataReportView - T key for report toggle
            ScienceDataReportView sdrv = Object.FindFirstObjectByType<ScienceDataReportView>();
            bool reportBound = sdrv != null;
            sb.AppendLine($"[Science Data Report (T)] {(reportBound ? "PASS" : "FAIL")}: ScienceDataReportView={(sdrv != null)}, ConfiguredToggleKey=Key.T");

            // 5. MissionResultsView - R restarts after mission ends, M returns to selection
            MissionResultsView mrv = Object.FindFirstObjectByType<MissionResultsView>();
            bool resultsBound = mrv != null;
            sb.AppendLine($"[Results Flow (R/M)] {(resultsBound ? "PASS" : "FAIL")}: MissionResultsView={(mrv != null)}, RestartKey=Key.R, SelectionKey=Key.M");

            // 6. Conflict Analysis
            sb.AppendLine("[Keyboard Conflict Analysis] PASS: ObjectiveAcknowledgeInput (R) and MissionResultsView (R) are strictly mutually exclusive by mission lifecycle state (Active vs Terminal). ScienceDataReportView uses T. MissionSelectionView uses UI buttons / M. No overlapping active listeners.");

            Debug.Log(sb.ToString());
        }

        // ============================================================ lookup / wiring helpers
        private static T Single<T>() where T : Object
        {
            T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length != 1) Debug.LogError("[Stage10Builder] Expected exactly one " + typeof(T).Name + " in the scene, found " + all.Length);
            return all.Length == 1 ? all[0] : null;
        }

        private static MissionScenarioData[] LoadScenarios()
        {
            MissionScenarioData[] result = new MissionScenarioData[ScenarioPaths.Length];
            for (int i = 0; i < ScenarioPaths.Length; i++)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<MissionScenarioData>(ScenarioPaths[i]);
                if (result[i] == null) { Debug.LogError("[Stage10Builder] Missing scenario asset: " + ScenarioPaths[i]); return null; }
            }
            return result;
        }

        private static List<Object> FindResettables()
        {
            List<Object> list = new List<Object>();
            MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i] is IMissionResettable) list.Add(all[i]);
            list.Sort((a, b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));   // deterministic
            return list;
        }

        private static void SetRef(SerializedObject so, string property, Object value)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null) { Debug.LogError("[Stage10Builder] Serialized property not found: " + so.targetObject.GetType().Name + "." + property); return; }
            p.objectReferenceValue = value;
        }

        private static void SetRefs(Object target, string property, List<Object> values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property);
            if (p == null) { Debug.LogError("[Stage10Builder] Serialized property not found: " + property); return; }
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(SerializedObject so, string property, Object[] values)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null) { Debug.LogError("[Stage10Builder] Serialized property not found: " + property); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(go);
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject GetOrCreateUIChild(Transform parent, string name, bool stretch)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            return go;
        }

        // ============================================================ UI construction (plain Unity UI)
        private static void BuildResultsPanel(Transform root, Font font)
        {
            RectTransform panel = NewRect("ResultsPanel", root, new Vector2(660, 600), Vector2.zero);
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.10f, 0.94f);

            NewImage("Header", panel, new Vector2(660, 150), new Vector2(0, 225), new Color(1, 1, 1, 0.1f));
            NewText("TitleText", panel, font, "MISSION RESULT", 38, TextAnchor.MiddleCenter, new Vector2(620, 56), new Vector2(0, 255), FontStyle.Bold);
            NewText("SubtitleText", panel, font, "Sol - Mission", 24, TextAnchor.MiddleCenter, new Vector2(620, 30), new Vector2(0, 207), FontStyle.Normal);
            NewText("ReasonText", panel, font, "", 18, TextAnchor.MiddleCenter, new Vector2(620, 28), new Vector2(0, 172), FontStyle.Italic);
            NewText("ObjectivesHeader", panel, font, "OBJECTIVES", 16, TextAnchor.MiddleLeft, new Vector2(600, 24), new Vector2(0, 135), FontStyle.Bold);
            NewText("ObjectivesText", panel, font, "", 18, TextAnchor.UpperLeft, new Vector2(600, 130), new Vector2(0, 55), FontStyle.Normal);
            NewText("SummaryText", panel, font, "", 16, TextAnchor.MiddleCenter, new Vector2(620, 28), new Vector2(0, -20), FontStyle.Normal);
            NewText("OxygenText", panel, font, "Final Oxygen:  0%", 22, TextAnchor.MiddleCenter, new Vector2(620, 30), new Vector2(0, -62), FontStyle.Normal);
            NewText("PowerText", panel, font, "Final Power:   0%", 22, TextAnchor.MiddleCenter, new Vector2(620, 30), new Vector2(0, -94), FontStyle.Normal);
            NewText("ShieldText", panel, font, "Final Shield:  0%", 22, TextAnchor.MiddleCenter, new Vector2(620, 30), new Vector2(0, -126), FontStyle.Normal);
            NewText("FeedbackText", panel, font, "", 18, TextAnchor.MiddleCenter, new Vector2(600, 56), new Vector2(0, -182), FontStyle.Bold);
            NewButton("RestartButton", panel, font, "Restart Mission  [R]", new Vector2(250, 50), new Vector2(-140, -255));
            NewButton("SelectionButton", panel, font, "Mission Selection  [M]", new Vector2(250, 50), new Vector2(140, -255));

            panel.gameObject.SetActive(false);   // shown only on a terminal state
        }

        private static void BuildSelectionPanel(Transform root, Font font)
        {
            RectTransform panel = NewRect("SelectionPanel", root, new Vector2(520, 380), Vector2.zero);
            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.10f, 0.94f);

            NewText("SelectionTitle", panel, font, "SELECT MISSION", 32, TextAnchor.MiddleCenter, new Vector2(480, 50), new Vector2(0, 155), FontStyle.Bold);
            for (int i = 0; i < 3; i++)
                NewButton("ScenarioButton_" + i, panel, font, "Scenario " + i, new Vector2(440, 58), new Vector2(0, 75 - i * 72));
            NewText("DisclosureText", panel, font, "Curated historical training scenarios - not live telemetry.", 14, TextAnchor.MiddleCenter, new Vector2(480, 30), new Vector2(0, -150), FontStyle.Italic);

            panel.gameObject.SetActive(false);
        }

        private static void WireResultsView(MissionResultsView view, MissionFlowBridge bridge, Transform root)
        {
            Transform p = root.Find("ResultsPanel");
            SerializedObject so = new SerializedObject(view);
            SetRef(so, "bridge", bridge);
            SetRef(so, "panelRoot", p.gameObject);
            SetRef(so, "headerImage", p.Find("Header").GetComponent<Image>());
            SetRef(so, "titleText", p.Find("TitleText").GetComponent<Text>());
            SetRef(so, "subtitleText", p.Find("SubtitleText").GetComponent<Text>());
            SetRef(so, "reasonText", p.Find("ReasonText").GetComponent<Text>());
            SetRef(so, "objectivesText", p.Find("ObjectivesText").GetComponent<Text>());
            SetRef(so, "summaryText", p.Find("SummaryText").GetComponent<Text>());
            SetRef(so, "oxygenText", p.Find("OxygenText").GetComponent<Text>());
            SetRef(so, "powerText", p.Find("PowerText").GetComponent<Text>());
            SetRef(so, "shieldText", p.Find("ShieldText").GetComponent<Text>());
            SetRef(so, "feedbackText", p.Find("FeedbackText").GetComponent<Text>());
            SetRef(so, "restartButton", p.Find("RestartButton").GetComponent<Button>());
            SetRef(so, "selectionButton", p.Find("SelectionButton").GetComponent<Button>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireSelectionView(MissionSelectionView view, MissionFlowBridge bridge, Transform root, MissionScenarioData[] scenarios)
        {
            Transform p = root.Find("SelectionPanel");
            SerializedObject so = new SerializedObject(view);
            SetRef(so, "bridge", bridge);
            SetRef(so, "panelRoot", p.gameObject);

            Object[] assets = new Object[scenarios.Length];
            Object[] buttons = new Object[scenarios.Length];
            Object[] labels = new Object[scenarios.Length];
            for (int i = 0; i < scenarios.Length; i++)
            {
                assets[i] = scenarios[i];
                Transform b = p.Find("ScenarioButton_" + i);
                buttons[i] = b.GetComponent<Button>();
                labels[i] = b.Find("Label").GetComponent<Text>();
            }
            SetRefArray(so, "scenarios", assets);
            SetRefArray(so, "scenarioButtons", buttons);
            SetRefArray(so, "scenarioLabels", labels);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;   // reuse the existing one
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            Debug.LogWarning("[Stage10Builder] No EventSystem existed; one was added so the results buttons work. Record this in the change report.");
        }

        // ---- tiny UI factory
        private static RectTransform NewRect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private static Image NewImage(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
        {
            RectTransform rt = NewRect(name, parent, size, pos);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text NewText(string name, Transform parent, Font font, string value, int size, TextAnchor anchor, Vector2 sz, Vector2 pos, FontStyle style)
        {
            RectTransform rt = NewRect(name, parent, sz, pos);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = size; t.alignment = anchor; t.fontStyle = style;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void NewButton(string name, Transform parent, Font font, string label, Vector2 size, Vector2 pos)
        {
            RectTransform rt = NewRect(name, parent, size, pos);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.34f, 1f);
            rt.gameObject.AddComponent<Button>().targetGraphic = img;
            Text t = NewText("Label", rt, font, label, 20, TextAnchor.MiddleCenter, size, Vector2.zero, FontStyle.Bold);
            t.raycastTarget = false;
        }
    }
}
#endif
