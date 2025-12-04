using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main menu buttons (Play, Option, Quit)")]
    public GameObject mainMenuButtons;
    
    [Tooltip("The options menu panel")]
    public GameObject optionsMenu;
    
    [Tooltip("The back button")]
    public GameObject backButton;

    private void Start()
    {
        // Show main menu, hide options and back button at start
        ShowMainMenu();
    }

    public void PlayGame()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ShowOptions()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        // Hide main menu buttons
        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(false);

        // Show options menu
        if (optionsMenu != null)
            optionsMenu.SetActive(true);

        // Show back button
        if (backButton != null)
            backButton.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        // Show main menu buttons
        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(true);

        // Hide options menu
        if (optionsMenu != null)
            optionsMenu.SetActive(false);

        // Hide back button
        if (backButton != null)
            backButton.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}