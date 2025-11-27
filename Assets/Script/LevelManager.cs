using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Settings")]
    [SerializeField] private int totalLevels = 3;
    private int currentLevel = 1;
    
    [Header("UI References (Optional)")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    
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
        }
    }
    
    private void Start()
    {
        // Get current level from scene build index
        currentLevel = SceneManager.GetActiveScene().buildIndex;
    }
    
    // Call this when player wins the level
    public void LevelCompleted()
    {
        Debug.Log("Level " + currentLevel + " completed!");
        
        // Show win panel if assigned
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        
        // Check if there are more levels
        if (currentLevel < totalLevels)
        {
            // Wait a bit before loading next level (optional)
            Invoke("LoadNextLevel", 2f);
        }
        else
        {
            Debug.Log("All levels completed! Game finished!");
            Invoke("GameCompleted", 2f);
        }
    }
    
    // Load the next level
    public void LoadNextLevel()
    {
        int nextLevel = currentLevel + 1;
        
        if (nextLevel <= totalLevels)
        {
            SceneManager.LoadScene(nextLevel);
        }
    }
    
    // Restart current level
    public void RestartLevel()
    {
        SceneManager.LoadScene(currentLevel);
    }
    
    // Go to main menu (scene 0)
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    
    // Called when all levels are completed
    private void GameCompleted()
    {
        // You can load a "Victory" scene or main menu
        LoadMainMenu();
    }
    
    // Call this when player fails/dies
    public void GameOver()
    {
        Debug.Log("Game Over!");
        
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
}