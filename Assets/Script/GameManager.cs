using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3
}

public class GameManager : MonoBehaviour
{
    // Singleton, supaya bisa diakses dari mana saja lewat GameManager.Instance
    public static GameManager Instance { get; private set; }

    [Header("Game Progress / Difficulty")]
    [Tooltip("1 = easiest, 5 = hardest")]
    public GameLevel currentLevel = GameLevel.Level1;

    [Header("Tower Reference")]
    public Tower playerTower;
    public Tower aiTower;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    //public Text gameOverText; 

    private bool isGameOver = false;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // if you use a lot of scenes

        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log($"[GAME START] Current Level = {currentLevel}");
    }

    private void Start()
    {
        // Reset game state for new scene
        ResetGameState();

        // Make sure the Game Over panel is off first
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Make sure timeScale is normal
        Time.timeScale = 1f;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    // Reset game state for new scene/level
    private void ResetGameState()
    {
        isGameOver = false;
        Debug.Log("[GameManager] Game state reset for new scene");
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameState();
        Debug.Log($"[GameManager] Scene '{scene.name}' loaded, game state reset");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called from the Tower:
    // GameManager.Instance.TowerDestroyed(this);
public void TowerDestroyed(Tower destroyedTower)
{
    if (isGameOver) return;

    isGameOver = true;

    string message = "";

    // Check which tower was destroyed
    if (destroyedTower == playerTower || destroyedTower.owner == Tower.TowerOwner.Player)
    {
        message = "YOU LOSE!";
        
        // Show game over UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (gameOverText != null)
            gameOverText.text = message;
            
        Time.timeScale = 0f;
    }
    else if (destroyedTower == aiTower || destroyedTower.owner == Tower.TowerOwner.Enemy)
    {
        message = "YOU WIN!";
        
        // Don't show game over panel for wins - let LevelManager handle it
        // LevelManager will automatically load next level after 2 seconds
        Debug.Log($"Level {currentLevel} completed! Loading next level...");
        
        // The level transition is handled by Tower.cs calling LevelManager.Instance.LevelCompleted()
    }
    else
    {
        message = "GAME OVER";
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (gameOverText != null)
            gameOverText.text = message;
            
        Time.timeScale = 0f;
    }
}


    public void OnSummonButtonClick()
    {
        Debug.Log("[GameManager] OnSummonButtonClick called at " + Time.time);
        if (isGameOver)
        {
            Debug.Log("[GameManager] Cannot summon: Game Over.");
            return;
        }

        if (GachaManager.Instance != null)
        {
            // Manggil Gacha Function
            GachaManager.Instance.SummonTroop();
        }
        else
        {
            Debug.LogError("[GameManager] GachaManager instance not found. Make sure the GachaManager script is active in the scene.");
        }
    }
}

