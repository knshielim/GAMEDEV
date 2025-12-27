using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum GameLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5
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

    [Header("Speed Control")]
    [SerializeField] private KeyCode fastForwardKey = KeyCode.F;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float fastForwardSpeed = 2f;

    private bool isGameOver = false;
    private bool isFastForward = false;

    private void Awake()
    {
        //Debug.Log($"[GameManager] Awake called for {gameObject.name} in scene {gameObject.scene.name} (buildIndex: {gameObject.scene.buildIndex}). Current Instance: {Instance}");

        // Setup Singleton with scene-aware logic
        if (Instance != null && Instance != this)
        {
            // If we're in a game scene and there's already a persistent instance, destroy this one
            // The persistent instance will get updated via OnSceneLoaded
            if (gameObject.scene.buildIndex > 0) // Game scenes (not main menu)
            {
                //Debug.Log("[GameManager] Destroying duplicate GameManager in game scene");
                Destroy(gameObject);
                return;
            }
            else // Main menu scene - destroy the old persistent instance
            {
                //Debug.Log("[GameManager] Destroying old GameManager for main menu");
                Destroy(Instance.gameObject);
            }
        }

        Instance = this;
        // Only use DontDestroyOnLoad for game scenes, not main menu
        if (gameObject.scene.buildIndex > 0)
        {
            DontDestroyOnLoad(gameObject);
            //Debug.Log("[GameManager] Set DontDestroyOnLoad for game scene");
        }

        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;

        //Debug.Log($"[GameManager] Final Instance set to: {Instance} for scene: {gameObject.scene.name}");
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

        // Make sure timeScale is normal (reset fast forward)
        Time.timeScale = normalSpeed;
        isFastForward = false;

        // Fix button references in Start() in case OnSceneLoaded ran too early
        if (gameObject.scene.buildIndex > 0)
        {
            FixButtonReferences(gameObject.scene);
        }
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    // Reset game state for new scene/level
    private void ResetGameState()
    {
        isGameOver = false;
        //Debug.Log("[GameManager] Game state reset for new scene");
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //($"[GameManager] OnSceneLoaded called for scene '{scene.name}' (buildIndex: {scene.buildIndex}), Instance: {Instance}, this: {this}");

        // If this is a game scene and we're the persistent instance, update our references
        if (scene.buildIndex > 0 && Instance == this)
        {
            // Find the scene's GameManager object and copy its references
            GameManager[] sceneManagers = FindObjectsOfType<GameManager>();
            //Debug.Log($"[GameManager] Found {sceneManagers.Length} GameManager instances");
            foreach (GameManager gm in sceneManagers)
            {
                //Debug.Log($"[GameManager] Checking GM: {gm}, scene: {gm.gameObject.scene.name}, isThis: {gm == this}");
                if (gm != this && gm.gameObject.scene == scene)
                {
                    // Transfer references from the scene GameManager to this persistent one
                    playerTower = gm.playerTower;
                    aiTower = gm.aiTower;
                    gameOverPanel = gm.gameOverPanel;
                    gameOverText = gm.gameOverText;
                    currentLevel = gm.currentLevel;

                    // Destroy the scene GameManager since we now have its references
                    Destroy(gm.gameObject);
                    //Debug.Log("[GameManager] Transferred references from scene GameManager");
                    break;
                }
            }

            // Always fix button references when loading a game scene, regardless of whether we transferred references
            FixButtonReferences(scene);
        }

        ResetGameState();
        //Debug.Log($"[GameManager] Scene '{scene.name}' loaded, game state reset. Final Instance: {Instance}");
    }

    private void FixButtonReferences(Scene scene)
    {
        // Find the summon button and ensure it calls the correct GameManager method
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (button.gameObject.scene != scene)
                continue;

            string lowerName = button.name.ToLower();

            // 1) UPGRADE SUMMON RATE button
            if (lowerName.Contains("upgrade") && lowerName.Contains("summon"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (GachaManager.Instance != null)
                        GachaManager.Instance.UpgradeGachaSystem();
                });
            }
            // 2) Regular SUMMON button
            else if (lowerName.Contains("summon"))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnSummonButtonClick);
            }
            /*
            if (button.gameObject.scene == scene && button.name.Contains("Summon"))
            {
                // Clear ALL listeners (both programmatic and persistent) to start fresh
                button.onClick.RemoveAllListeners();

                // Add our programmatic listener that calls the persistent GameManager
                button.onClick.AddListener(() => OnSummonButtonClick());

                //Debug.Log($"[GameManager] Fixed OnClick listener for summon button: {button.name}");
            }
            */
        }
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
        //Debug.Log($"Level {currentLevel} completed! Loading next level...");
        
        // The level transition is handled by Tower.cs calling LevelManager.Instance.LevelCompleted()
    }
    else
    {
        message = "GAME OVER";
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (gameOverText != null)
            gameOverText.text = message;

        Time.timeScale = 0f; // Always pause on game over, regardless of fast forward
    }
}

    private void Update()
    {
        // Handle fast forward toggle
        if (Input.GetKeyDown(fastForwardKey) && !isGameOver)
        {
            ToggleFastForward();
        }

        // ✅ DEBUG: Add 5 coins when 'C' key is pressed
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddPlayerCoins(5);
                Debug.Log("[GameManager] 🎁 DEBUG: Added 5 coins (shortcut key 'C')");
            }
        }
    }

    private void ToggleFastForward()
    {
        isFastForward = !isFastForward;
        float newSpeed = isFastForward ? fastForwardSpeed : normalSpeed;

        Time.timeScale = newSpeed;

        Debug.Log($"[GameManager] Fast forward {(isFastForward ? "ON" : "OFF")} - Speed: {newSpeed}x");

        // Optional: You could add UI feedback here
    }

    public bool IsFastForwardActive()
    {
        return isFastForward;
    }

    public float GetCurrentSpeedMultiplier()
    {
        return isFastForward ? fastForwardSpeed : normalSpeed;
    }

    private float lastSummonTime = -1f;
    private const float SUMMON_COOLDOWN = 0.1f; // Prevent summons more frequent than 0.1 seconds

    public void OnSummonButtonClick()
    {
        // Prevent duplicate summons within cooldown period
        if (Time.time - lastSummonTime < SUMMON_COOLDOWN)
        {
            //Debug.Log("[GameManager] Summon blocked - too frequent calls");
            return;
        }
        lastSummonTime = Time.time;

        //Debug.Log($"[GameManager] OnSummonButtonClick called. Instance: {Instance}, this: {this}, isGameOver: {isGameOver}, gameObject: {gameObject}, scene: {gameObject.scene.name}");
        if (isGameOver)
        {
            //Debug.Log("[GameManager] Cannot summon: Game Over.");
            return;
        }

        if (GachaManager.Instance != null)
        {
            //Debug.Log("[GameManager] Calling GachaManager.SummonTroop()");
            // Manggil Gacha Function
            GachaManager.Instance.SummonTroop();
        }
        else
        {
            Debug.LogError("[GameManager] GachaManager instance not found. Make sure the GachaManager script is active in the scene.");
        }
    }
}

