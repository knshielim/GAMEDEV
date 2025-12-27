using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Settings")]
    [SerializeField] private int totalLevels = 5; // ✅ FIXED: Total levels = 5
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
        Debug.Log($"[LevelManager] Awake called for {gameObject.name} in scene {gameObject.scene.name} - totalLevels: {totalLevels}");

        if (Instance != null && Instance != this)
        {
            Debug.Log($"[LevelManager] Destroying duplicate LevelManager - existing instance has totalLevels: {Instance.totalLevels}");
            if (gameObject.scene.buildIndex > 0)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.Log($"[LevelManager] Destroying old LevelManager instance");
                Destroy(Instance.gameObject);
            }
        }

        Instance = this;
        Debug.Log($"[LevelManager] Set as Instance with totalLevels: {totalLevels}");
    }
    
    private void Start()
    {
        // ✅ CRITICAL FIX: Force totalLevels to 5 (override any Inspector settings)
        totalLevels = 5;

        // ✅ FIX: More robust level detection
        currentLevel = DetermineCurrentLevel();

        Debug.Log($"[LevelManager] ✅ Started Level {currentLevel}/{totalLevels} (Scene: {SceneManager.GetActiveScene().name}, BuildIndex: {SceneManager.GetActiveScene().buildIndex})");

        // Ensure time scale is normal
        Time.timeScale = 1f;
    }

    // ✅ NEW: Better level detection method
    private int DetermineCurrentLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        Debug.Log($"[LevelManager] 🔍 Determining level from scene: '{sceneName}' (buildIndex: {buildIndex}), totalLevels: {totalLevels}");
        
        // Method 1: Parse from scene name (most reliable)
        if (sceneName.StartsWith("Level "))
        {
            string levelStr = sceneName.Substring(6).Trim();
            Debug.Log($"[LevelManager] 🔍 Extracted level string: '{levelStr}'");
            if (int.TryParse(levelStr, out int level))
            {
                Debug.Log($"[LevelManager] ✅ Parsed level {level} from scene name (clamped to 1-{totalLevels})");
                return Mathf.Clamp(level, 1, totalLevels);
            }
            else
            {
                Debug.LogWarning($"[LevelManager] ❌ Failed to parse level from '{levelStr}'");
            }
        }
        else
        {
            Debug.LogWarning($"[LevelManager] ❌ Scene name '{sceneName}' doesn't start with 'Level '");
        }
        
        // Method 2: Use build index
        // Assuming: MainMenu=0, LevelSelect=1, Level1=2, Level2=3, Level3=4, Level4=5, Level5=6
        if (buildIndex >= 2 && buildIndex <= (totalLevels + 1))
        {
            int level = buildIndex - 1; // Level 1 = buildIndex 2, so level = 2-1 = 1
            Debug.Log($"[LevelManager] ✅ Calculated level {level} from build index {buildIndex}");
            return level;
        }
        
        Debug.LogWarning($"[LevelManager] ⚠️ Could not determine level, defaulting to 1");
        return 1;
    }
    
    // Call this when player wins the level (called by Tower when enemy tower dies)
    public void LevelCompleted()
    {
        if (isTransitioning)
            return;

        Debug.Log($"[LevelManager] 🏆 LevelCompleted called - Current: {currentLevel}, Total: {totalLevels}, IsLastLevel: {IsLastLevel()}");
        Debug.Log($"[LevelManager] 🏆 Condition check: currentLevel({currentLevel}) < totalLevels({totalLevels}) = {currentLevel < totalLevels}");

        ShowLevelEndDialogue();
        UnlockNextLevel();

        // Check if there are more levels
        if (currentLevel < totalLevels)
        {
            Debug.Log($"[LevelManager] ➡️ Next level available: Level {currentLevel + 1}");
            StartCoroutine(LoadNextLevelDelayed());
        }
        else
        {
            Debug.Log($"[LevelManager] 🎉 ALL {totalLevels} LEVELS COMPLETED! Game finished!");
            StartCoroutine(GameCompletedDelayed());
        }
    }
    
    private IEnumerator LoadNextLevelDelayed()
    {
        isTransitioning = true;

        // Wait for dialogue to complete if it's showing
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.IsDialogueActive())
        {
            Debug.Log("[LevelManager] Waiting for dialogue to complete before loading next level...");
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive());
        }

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
        // ✅ FIX: Calculate next scene index properly
        // If currentLevel = 4, next should be 5
        // Scene build indices: MainMenu=0, LevelSelect=1, Level1=2, Level2=3, Level3=4, Level4=5, Level5=6
        int nextLevel = currentLevel + 1;
        int nextSceneIndex = nextLevel + 1; // Level 5 = build index 6
        
        Debug.Log($"[LevelManager] 🔄 Loading Level {nextLevel} (scene index {nextSceneIndex})");
        
        if (nextLevel <= totalLevels)
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
        Debug.Log($"[LevelManager] 🔄 Restarting level {currentLevel}");
        
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

        Debug.Log("[LevelManager] 🏠 Loading main menu (scene 0)");

        // Reset time scale
        Time.timeScale = 1f;

        SceneManager.LoadScene(0);
    }
    
    // Call this when player fails/dies (called by Tower when player tower dies)
    public void GameOver()
    {
        Debug.Log("[LevelManager] 💀 Game Over!");
        if (AudioManager.Instance != null && AudioManager.Instance.gameOverSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSFX);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // Show end-of-level dialogue
    private void ShowLevelEndDialogue()
    {
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.ShowLevelEndDialogue(currentLevel);
        }
    }

    // Unlock the next level in level select
    private void UnlockNextLevel()
    {
        int nextLevel = currentLevel + 1;
        if (nextLevel <= totalLevels)
        {
            LevelSelectManager levelSelect = FindObjectOfType<LevelSelectManager>();
            if (levelSelect != null)
            {
                levelSelect.UnlockLevel(nextLevel);
                Debug.Log($"[LevelManager] 🔓 Unlocked Level {nextLevel} in level select!");
            }
            else
            {
                int currentMax = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);
                if (nextLevel > currentMax)
                {
                    PlayerPrefs.SetInt("MaxUnlockedLevel", nextLevel);
                    PlayerPrefs.Save();
                    Debug.Log($"[LevelManager] 💾 Saved Level {nextLevel} unlock to PlayerPrefs!");
                }
            }
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