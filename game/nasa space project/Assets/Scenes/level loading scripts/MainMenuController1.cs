
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController1 : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Mars Scene";

    public void PlayNow()
    {
        Debug.Log("Play Now button clicked!");

        if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError("Scene not found in Build Profiles: " + gameplaySceneName);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit button clicked!");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
