// PATH: Assets/AREON/Editor/MissionScenarioAssetCreator.cs
// Editor-only one-shot tool. Creates (or idempotently updates) the three curated scenario assets.
// Run through the menu:  AREON > Stage 8 > Create Scenario Assets

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MissionScenarioAssetCreator
{
    private const string Folder = "Assets/AREON/Data/Missions";

    [MenuItem("AREON/Stage 8/Create Scenario Assets")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/AREON");
        EnsureFolder("Assets/AREON/Data");
        EnsureFolder(Folder);

        //            file name              sol   display name        Pa    flare                 storm                    oxMult shMult charging
        CreateOrUpdate("Sol800_Orientation", 800,  "Orientation Sol",  887f, SolarFlareClass.None, StormStatus.Clear,        1.0f,  1.0f,  true);
        CreateOrUpdate("Sol852_Onset",       852,  "Onset Sol",        924f, SolarFlareClass.C,    StormStatus.Regional,     1.0f,  2.0f,  true);
        CreateOrUpdate("Sol2085_Crisis",     2085, "Crisis Sol",       772f, SolarFlareClass.X,    StormStatus.SevereGlobal, 1.5f,  2.0f,  false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AREON Stage 8] Scenario assets created/updated in " + Folder);
    }

    private static void CreateOrUpdate(string fileName, int sol, string displayName, float pressurePa,
                                       SolarFlareClass flare, StormStatus storm,
                                       float oxMult, float shieldMult, bool solarAllowed)
    {
        string path = $"{Folder}/{fileName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<MissionScenarioData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MissionScenarioData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        var so = new SerializedObject(asset);

        // Historical / scenario-defining values (blueprint 1.5)
        so.FindProperty("solNumber").intValue = sol;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("pressurePa").floatValue = pressurePa;
        so.FindProperty("solarFlareClass").enumValueIndex = (int)flare;
        so.FindProperty("stormStatus").enumValueIndex = (int)storm;
        so.FindProperty("oxygenDrainMultiplier").floatValue = oxMult;
        so.FindProperty("shieldDrainMultiplier").floatValue = shieldMult;
        so.FindProperty("solarChargingAllowed").boolValue = solarAllowed;

        // Prototype tuning defaults (identical in every scenario; NOT historical values)
        so.FindProperty("initialOxygen").floatValue = 88f;
        so.FindProperty("initialPower").floatValue = 64f;
        so.FindProperty("initialShield").floatValue = 72f;
        so.FindProperty("oxygenDrainPerSecond").floatValue = 0.5f;
        so.FindProperty("shieldDrainPerSecond").floatValue = 0.4f;
        so.FindProperty("scienceDownloadPowerCost").floatValue = 15f;
        so.FindProperty("shieldReinforcePowerCost").floatValue = 10f;
        so.FindProperty("shieldRestoreAmount").floatValue = 25f;
        so.FindProperty("daylightDurationSeconds").floatValue = 300f;
        so.FindProperty("solarRechargePerSecond").floatValue = 2f;

        // Objective lists intentionally left empty: configured in Stage 9 (Tasks 43-44).

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
