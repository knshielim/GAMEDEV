using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
