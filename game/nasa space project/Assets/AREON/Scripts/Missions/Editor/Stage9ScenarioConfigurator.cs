using AREON.ResourceSystem;
using System;
using UnityEditor;
using UnityEngine;

// ADAPT: add the using for ResourceType's namespace if it has one.

namespace AREON.Missions.EditorTools
{
    /// <summary>
    /// EDIT-TIME ONLY. Writes the Stage 9 objectives, durations and mission policies into the three
    /// existing scenario assets (matched by solNumber). Never runs during gameplay.
    /// All numbers below are PROTOTYPE TUNING VALUES that need playtesting - they are not NASA data.
    /// Re-running the menu item overwrites only the Stage 9 fields, so it is safe to repeat.
    /// </summary>
    public static class Stage9ScenarioConfigurator
    {
        private struct Spec
        {
            public string Id, Title, Description, Key;
            public ObjectiveTriggerType Trigger;
            public float Amount;
            public ResourceType Resource;
            public float Threshold;
        }

        private static Spec O(string id, string title, string desc, ObjectiveTriggerType trigger,
                              string key = "", float amount = 1f, ResourceType res = ResourceType.Power, float threshold = 0f) =>
            new Spec { Id = id, Title = title, Description = desc, Trigger = trigger, Key = key, Amount = amount, Resource = res, Threshold = threshold };

        [MenuItem("AREON/Stage 9/Configure Scenario Objectives")]
        public static void ConfigureAll()
        {
            int configured = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:MissionScenarioData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<MissionScenarioData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var so = new SerializedObject(data);
                SerializedProperty sol = so.FindProperty("solNumber");
                if (sol == null) { Debug.LogError("[Stage9] solNumber field not found on MissionScenarioData."); return; }

                bool ok = true;
                switch (sol.intValue)
                {
                    case 800: ok = Sol800(so); break;
                    case 852: ok = Sol852(so); break;
                    case 2085: ok = Sol2085(so); break;
                    default: continue;
                }
                if (!ok) return;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                configured++;
                Debug.Log($"[Stage9] Configured Sol {sol.intValue} ({data.name}).");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage9] Scenario configuration finished: {configured}/3 assets updated.");
        }

        // ------------------------------------------------------------------ Sol 800 - tutorial (order = list order)

        private static bool Sol800(SerializedObject so)
        {
            var required = new[]
            {
                O("s800_visit_base_dome", "Visit Base Dome", "Walk to the Base Dome and interact with it.", ObjectiveTriggerType.FacilityInteracted, ObjectiveKeys.BaseDome),
                O("s800_visit_research_center", "Visit Research Center", "Interact with the Research Center terminal.", ObjectiveTriggerType.FacilityInteracted, ObjectiveKeys.ResearchCenter),
                O("s800_visit_power_generator", "Visit Power Generator", "Interact with the Power Generator.", ObjectiveTriggerType.FacilityInteracted, ObjectiveKeys.PowerGenerator),
                O("s800_resource_monitoring", "Monitor Resource Levels", "Read the Oxygen, Power and Shield bars, then press ACKNOWLEDGE (R).", ObjectiveTriggerType.Acknowledgement, ObjectiveKeys.ResourceMonitoring),
                O("s800_recharge_power", "Recharge Power", "Charge Power at the generator during daylight (10 Power).", ObjectiveTriggerType.PowerRecharged, "", 10f),
                O("s800_first_science_upload", "Download Science Telemetry", "Spend 15 Power to download your first science packet.", ObjectiveTriggerType.ScienceDownloaded),
            };
            var optional = new[]
            {
                O("s800_reinforce_shield", "Reinforce the Shield", "Optional: spend 10 Power at the Base Dome shield console.", ObjectiveTriggerType.ShieldReinforced),
            };
            return Apply(so, 180f, MissionCompletionPolicy.ObjectivesAndSurvival, false, true, false, required, optional);
        }

        // ------------------------------------------------------------------ Sol 852 - onset, resource triage

        private static bool Sol852(SerializedObject so)
        {
            var required = new[]
            {
                O("s852_reinforce_shield", "Reinforce the Shield", "Shield drains 2x faster. Reinforce it once (10 Power).", ObjectiveTriggerType.ShieldReinforced),
                O("s852_recharge_power", "Recharge Power", "Restore 20 Power at the generator before daylight ends.", ObjectiveTriggerType.PowerRecharged, "", 20f),
                O("s852_hold_shield", "Keep Shield Above 20%", "Keep the Shield at or above 20% for the whole mission.", ObjectiveTriggerType.ResourceHeld, "", 0f, ResourceType.Shield, 20f),
                O("s852_survive", "Survive the Storm", "Stay alive until the mission timer ends.", ObjectiveTriggerType.MissionTimeElapsed, "", 0f),
            };
            var optional = new[]
            {
                O("s852_science_upload", "Download Science Telemetry", "Optional: spend 15 Power on a science packet.", ObjectiveTriggerType.ScienceDownloaded),
                O("s852_power_reserve", "Keep Power Above 15%", "Optional: never let Power fall below 15% during the mission.", ObjectiveTriggerType.ResourceHeld, "", 0f, ResourceType.Power, 15f),
            };
            return Apply(so, 240f, MissionCompletionPolicy.ObjectivesAndSurvival, true, true, true, required, optional);
        }

        // ------------------------------------------------------------------ Sol 2085 - crisis, limited resources

        private static bool Sol2085(SerializedObject so)
        {
            var required = new[]
            {
                O("s2085_reinforce_shield", "Reinforce the Shield", "Solar charging is offline. Spend Power wisely: reinforce the Shield once.", ObjectiveTriggerType.ShieldReinforced),
                O("s2085_power_reserve", "Keep Power Above 10%", "Do not let Power drop below 10% during the mission.", ObjectiveTriggerType.ResourceHeld, "", 0f, ResourceType.Power, 10f),
                O("s2085_survive", "Survive the Global Storm", "Stay alive until the mission timer ends.", ObjectiveTriggerType.MissionTimeElapsed, "", 0f),
            };
            var optional = new[]
            {
                O("s2085_science_upload", "Download Science Telemetry", "Optional: spend 15 Power on a science packet.", ObjectiveTriggerType.ScienceDownloaded),
                O("s2085_reinforce_twice", "Reinforce the Shield Twice", "Optional: reinforce the Shield two times.", ObjectiveTriggerType.ShieldReinforced, "", 2f),
            };
            return Apply(so, 240f, MissionCompletionPolicy.ObjectivesAndSurvival, true, true, true, required, optional);
        }

        // ------------------------------------------------------------------ writers

        private static bool Apply(SerializedObject so, float duration, MissionCompletionPolicy policy,
                                  bool failIfIncomplete, bool failOxygen, bool failShield,
                                  Spec[] required, Spec[] optional)
        {
            return SetFloat(so, "missionDurationSeconds", duration)
                && SetEnum(so, "completionPolicy", policy)
                && SetBool(so, "failIfObjectivesIncompleteAtTimeout", failIfIncomplete)
                && SetBool(so, "failAtZeroOxygen", failOxygen)
                && SetBool(so, "failAtZeroShield", failShield)
                && WriteList(so, "requiredObjectives", required)
                && WriteList(so, "optionalObjectives", optional);
        }

        private static bool WriteList(SerializedObject so, string field, Spec[] specs)
        {
            SerializedProperty list = Find(so, field);
            if (list == null) return false;
            list.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                Spec s = specs[i];
                if (!(Rel(e, "id", p => p.stringValue = s.Id)
                    && Rel(e, "title", p => p.stringValue = s.Title)
                    && Rel(e, "description", p => p.stringValue = s.Description)
                    && Rel(e, "trigger", p => SetEnumIndex(p, s.Trigger))
                    && Rel(e, "triggerKey", p => p.stringValue = s.Key ?? string.Empty)
                    && Rel(e, "targetAmount", p => p.floatValue = s.Amount)
                    && Rel(e, "resourceType", p => SetEnumIndex(p, s.Resource))
                    && Rel(e, "resourceThreshold", p => p.floatValue = s.Threshold)))
                    return false;
            }
            return true;
        }

        private static bool Rel(SerializedProperty element, string name, Action<SerializedProperty> set)
        {
            SerializedProperty p = element.FindPropertyRelative(name);
            if (p == null) { Debug.LogError($"[Stage9] MissionObjectiveDefinition has no serialized field '{name}'. Merge Stage 9 fields first."); return false; }
            set(p);
            return true;
        }

        private static SerializedProperty Find(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) Debug.LogError($"[Stage9] MissionScenarioData has no serialized field '{name}'.");
            return p;
        }

        private static bool SetFloat(SerializedObject so, string n, float v) { var p = Find(so, n); if (p == null) return false; p.floatValue = v; return true; }
        private static bool SetBool(SerializedObject so, string n, bool v) { var p = Find(so, n); if (p == null) return false; p.boolValue = v; return true; }
        private static bool SetEnum(SerializedObject so, string n, Enum v) { var p = Find(so, n); if (p == null) return false; SetEnumIndex(p, v); return true; }

        private static void SetEnumIndex(SerializedProperty p, Enum value)
        {
            int index = Array.IndexOf(p.enumNames, value.ToString());
            if (index < 0) Debug.LogError($"[Stage9] Enum value '{value}' not found on '{p.propertyPath}'.");
            else p.enumValueIndex = index;
        }
    }
}
