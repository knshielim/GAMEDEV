using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        Debug.Log($"[LevelManager] Awake called for {gameObject.name} in scene {gameObject.scene.name} (buildIndex: {gameObject.scene.buildIndex}). Current Instance: {Instance}");

        // Setup Singleton with scene-aware logic
        if (Instance != null && Instance != this)
        {
            // If we're in a game scene and there's already a persistent instance, destroy this one
            // The persistent instance will get updated via OnSceneLoaded
            if (gameObject.scene.buildIndex > 0) // Game scenes (not main menu)
            {
                Debug.Log("[LevelManager] Destroying duplicate LevelManager in game scene");
                Destroy(gameObject);
                return;
            }
            else // Main menu scene - destroy the old persistent instance
            {
                Debug.Log("[LevelManager] Destroying old LevelManager for main menu");
                Destroy(Instance.gameObject);
            }
        }

        Instance = this;
        // Only use DontDestroyOnLoad for game scenes, not main menu
        if (gameObject.scene.buildIndex > 0)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LevelManager] Set DontDestroyOnLoad for game scene");
        }

        Debug.Log($"[LevelManager] Final Instance set to: {Instance} for scene: {gameObject.scene.name}");

        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
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

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[LevelManager] OnSceneLoaded called for scene '{scene.name}' (buildIndex: {scene.buildIndex}), Instance: {Instance}, this: {this}");

        // If this is a game scene and we're the persistent instance, update our references and fix buttons
        if (scene.buildIndex > 0 && Instance == this)
        {
            // Find the scene's LevelManager object and copy its references
            LevelManager[] sceneManagers = FindObjectsOfType<LevelManager>();
            Debug.Log($"[LevelManager] Found {sceneManagers.Length} LevelManager instances");
            foreach (LevelManager lm in sceneManagers)
            {
                Debug.Log($"[LevelManager] Checking LM: {lm}, scene: {lm.gameObject.scene.name}, isThis: {lm == this}");
                if (lm != this && lm.gameObject.scene == scene)
                {
                    // Transfer references from the scene LevelManager to this persistent one
                    totalLevels = lm.totalLevels;
                    winPanel = lm.winPanel;
                    gameOverPanel = lm.gameOverPanel;

                    // Fix button OnClick events to point to the correct LevelManager instance
                    FixButtonReferences(scene);

                    // Destroy the scene LevelManager since we now have its references
                    Destroy(lm.gameObject);
                    Debug.Log("[LevelManager] Transferred references from scene LevelManager");
                    break;
                }
            }

            // Always fix button references when loading a game scene
            FixButtonReferences(scene);
        }

        Debug.Log($"[LevelManager] Scene '{scene.name}' loaded. Final Instance: {Instance}");
    }

    private void FixButtonReferences(Scene scene)
    {
        // Find buttons that call LevelManager methods and ensure they call the correct instance
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (button.gameObject.scene == scene)
            {
                // Only fix buttons that LevelManager is responsible for
                if (button.name.Contains("Restrart") || button.name.Contains("Restart"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => RestartLevel());
                    Debug.Log($"[LevelManager] Fixed Restart button OnClick listener: {button.name}");
                }
                else if (button.name.Contains("MainMenu"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => LoadMainMenu());
                    Debug.Log($"[LevelManager] Fixed MainMenu button OnClick listener: {button.name}");
                }
                // Don't touch other buttons (like summon buttons) - let other managers handle them
            }
        }
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
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
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
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSFX);
        // Time is already frozen by Tower.cs

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
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