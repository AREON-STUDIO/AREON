
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayNow()
    {
        SceneManager.LoadScene("Mars Scene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
