
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AREONPlayModeStartScene
{
    static AREONPlayModeStartScene()
    {
        EditorApplication.delayCall += () =>
        {
            SceneAsset mainMenu =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    "Assets/Scenes/AREON_MainMenu.unity"
                );

            if (mainMenu != null)
            {
                EditorSceneManager.playModeStartScene = mainMenu;

                Debug.Log("AREON Main Menu configured!");
            }
            else
            {
                Debug.LogError("AREON_MainMenu scene not found!");
            }
        };
    }
}
#endif
