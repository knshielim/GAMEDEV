using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Settings")]
    [SerializeField] private int totalLevels = 3;
    private int currentLevel = 1;
    
    [Header("UI References (Optional - Tower handles these)")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Transition Settings")]
    [Tooltip("Delay before loading next level after victory")]
    [SerializeField] private float levelTransitionDelay = 2f;
    
    private bool isTransitioning = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Get current level from scene build index
        currentLevel = SceneManager.GetActiveScene().buildIndex;
        
        // If scene 0 is main menu, adjust
        if (currentLevel == 0)
            currentLevel = 1;
            
        Debug.Log($"[LevelManager] Started Level {currentLevel}/{totalLevels}");
        
        // Ensure time scale is normal
        Time.timeScale = 1f;
    }
    
    // Call this when player wins the level (called by Tower when enemy tower dies)
    public void LevelCompleted()
    {
        if (isTransitioning)
            return;
            
        Debug.Log($"[LevelManager] Level {currentLevel} completed!");
        
        // Check if there are more levels
        if (currentLevel < totalLevels)
        {
            StartCoroutine(LoadNextLevelDelayed());
        }
        else
        {
            // All levels completed
            Debug.Log("[LevelManager] 🎉 All levels completed! Game finished!");
            StartCoroutine(GameCompletedDelayed());
        }
    }
    
    private IEnumerator LoadNextLevelDelayed()
    {
        isTransitioning = true;
        
        Debug.Log($"[LevelManager] Loading next level in {levelTransitionDelay} seconds...");
        
        // Wait before transitioning
        yield return new WaitForSeconds(levelTransitionDelay);
        
        LoadNextLevel();
    }
    
    private IEnumerator GameCompletedDelayed()
    {
        isTransitioning = true;
        
        Debug.Log("[LevelManager] Game completed! Returning to main menu in 4 seconds...");
        
        // Wait longer for final victory
        yield return new WaitForSeconds(4f);
        
        LoadMainMenu();
    }
    
    // Load the next level
    public void LoadNextLevel()
    {
        int nextSceneIndex = currentLevel + 1;
        
        Debug.Log($"[LevelManager] Loading scene index {nextSceneIndex}");
        
        if (nextSceneIndex <= totalLevels)
        {
            // Reset time scale before loading
            Time.timeScale = 1f;
            
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("[LevelManager] No more levels! Returning to main menu.");
            LoadMainMenu();
        }
    }
    
    // Restart current level (called by Restart button)
    public void RestartLevel()
    {
        Debug.Log($"[LevelManager] Restarting level {currentLevel}");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Go to main menu (scene 0)
    public void LoadMainMenu()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        Debug.Log("[LevelManager] Loading main menu (scene 0)");
        
        // Reset time scale
        Time.timeScale = 1f;
        
        SceneManager.LoadScene(0);
    }
    
    // Call this when player fails/dies (called by Tower when player tower dies)
    public void GameOver()
    {
        Debug.Log("[LevelManager] Game Over!");
        AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSFX);
        // Time is already frozen by Tower.cs
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
    
    // Public getters
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    public int GetTotalLevels()
    {
        return totalLevels;
    }
    
    public bool IsLastLevel()
    {
        return currentLevel >= totalLevels;
    }
}