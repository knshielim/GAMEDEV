using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tower : MonoBehaviour
{
    public enum TowerOwner {Player, Enemy} 
    public TowerOwner owner;

    [Header("Health")]
    public int maxHealth = 200;
    public int currentHealth = 0;
    
    public event Action<int> OnHealthChanged;

    [Header("Upgrade & Economy")]
    public int level = 1; 
    private const int MAX_LEVEL = 10;
    private const int TOWER_BASE_RATE = 1; 
    private const int TOWER_FIRST_UPGRADE_COST = 10;
    private const int TOWER_COST_INCREMENT = 15;

    [Header("Coin Generation")]
    public int coinsPerTick = 1;
    public float coinInterval = 1f;
    private float coinTimer = 0f;

    [Header("Tutorial Reference")]
    public TutorialManager tutorialManager; 
    
    [Header("UI References (Player Only)")]
    public TextMeshProUGUI upgradeButtonText; 
    public Image upgradeButtonImage;
    public Sprite affordableSprite;
    public Sprite unaffordableSprite;

    [Header("Win/Loss UI")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI victoryText;
    public Button nextLevelButton;
    public Button mainMenuButton;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void RepairTower()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"[Tower] {owner} tower repaired to full health: {currentHealth}/{maxHealth}");
    }

    private void Start()
    {
        currentHealth = maxHealth;
        coinsPerTick = CalculateCoinRate(level); 

        Debug.Log($"[{owner} TOWER] Start HP: {currentHealth}/{maxHealth}");

        if (owner == TowerOwner.Player)
        {
            UpdateUpgradeUI();
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Ensure game stays paused when victory or game over panels are active
        if ((victoryPanel != null && victoryPanel.activeSelf && Time.timeScale != 0f) ||
            (gameOverPanel != null && gameOverPanel.activeSelf && Time.timeScale != 0f))
        {
            Time.timeScale = 0f;
            Debug.LogWarning("[Tower] ⚠️ Game was unpaused while victory/game over panel is active - forcing pause");
        }

        HandleCoinGeneration();

        if (owner == TowerOwner.Player)
        {
            UpdateUpgradeUI();
        }
    }

    public int CalculateUpgradeCost()
    {
        int nextLevel = level + 1;
        
        if (nextLevel == 2)
        {
            return TOWER_FIRST_UPGRADE_COST;
        }
        return TOWER_FIRST_UPGRADE_COST + ((nextLevel - 2) * TOWER_COST_INCREMENT);
    }

    public int CalculateCoinRate(int currentLevel)
    {
        return TOWER_BASE_RATE * currentLevel;
    }

    public void UpdateUpgradeUI()
    {
        if (owner != TowerOwner.Player) return;
        
        if (upgradeButtonText == null)
        {
            Debug.LogError("[TOWER UI ERROR] upgradeButtonText is MISSING in the Inspector.");
            return;
        }
        if (upgradeButtonImage == null)
        {
            Debug.LogError("[TOWER UI ERROR] upgradeButtonImage is MISSING in the Inspector. This is why the sprite isn't changing.");
            return;
        }

        int nextLevel = level + 1;
        int cost = CalculateUpgradeCost();
        
        UnityEngine.UI.Button button = upgradeButtonText.GetComponentInParent<UnityEngine.UI.Button>();
        
        if (level >= MAX_LEVEL)
        {
            upgradeButtonText.text = $"MAX LEVEL ({MAX_LEVEL})";
            
            if (button != null)
                button.interactable = false;

            if (upgradeButtonImage != null && unaffordableSprite != null)
                upgradeButtonImage.sprite = unaffordableSprite;

            return;
        }
        
        bool canAfford = false;

        if (CoinManager.Instance != null)
        {
            canAfford = CoinManager.Instance.playerCoins >= cost;
            
            if (button != null)
            {
                button.interactable = canAfford;
            }
        }
        
        if (canAfford)
        {
            if (affordableSprite != null)
            {
                upgradeButtonImage.sprite = affordableSprite;
            }
            else
            {
                Debug.LogWarning("[TOWER UI WARNING] affordableSprite is MISSING.");
            }
        }
        else
        {
            if (unaffordableSprite != null)
            {
                upgradeButtonImage.sprite = unaffordableSprite;
            }
            else
            {
                Debug.LogWarning("[TOWER UI WARNING] unaffordableSprite is MISSING.");
            }
        }

        upgradeButtonText.text = $"Upgrade to Lv. {nextLevel}       Cost: {cost}";        
    }

    public void UpgradeTower()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver()) return;

        if (level >= MAX_LEVEL)
        {
            Debug.Log("[PLAYER TOWER] Already at MAX LEVEL!");
            return;
        }

        int upgradeCost = CalculateUpgradeCost();

        if (owner == TowerOwner.Player && CoinManager.Instance != null && CoinManager.Instance.TrySpendPlayerCoins(upgradeCost))
        {
            level++;
            coinsPerTick = CalculateCoinRate(level);
            
            Debug.Log($"[PLAYER TOWER] Upgraded to Level {level}! New Rate: {coinsPerTick}/tick. Next cost: {CalculateUpgradeCost()}");

            UpdateUpgradeUI(); 
        }
        else if (owner == TowerOwner.Player)
        {
            Debug.Log($"[PLAYER TOWER] Cannot upgrade. Need {upgradeCost} coins.");
        }
    }

    void HandleCoinGeneration()
    {
        if (CoinManager.Instance == null) return;

        coinTimer += Time.deltaTime;

        if (coinTimer >= coinInterval)
        {
            coinTimer -= coinInterval;

            if (owner == TowerOwner.Player)
            {
                CoinManager.Instance.AddPlayerCoins(coinsPerTick);
            }
            else if (owner == TowerOwner.Enemy) 
            {
                CoinManager.Instance.AddEnemyCoins(coinsPerTick);
            }
        }
    }
    
    void OnTowerDestroyed()
    {
        TutorialManager tm = tutorialManager;

        if (tm == null)
        {
            tm = FindObjectOfType<TutorialManager>();
        }

        if (tm != null && tm.isActiveAndEnabled && tm.tutorialActive)
        {
            tm.OnTutorialTowerDestroyed(this);
            return;
        }

        if (owner == TowerOwner.Player)
        {
            ShowGameOver();
        }
        else if (owner == TowerOwner.Enemy)
        {
            // ✅ MODIFIED: Call dialogue first, then victory panel
            StartCoroutine(VictoryWithDialogueSequence());
        }
    }

    private void ShowGameOver()
    {
        Debug.Log("[TOWER] Player Tower Destroyed - GAME OVER");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverDramatic();
        }

        // Ensure game is immediately paused when game over panel appears
        Time.timeScale = 0f;
        Debug.Log("[Tower] ⏸️ Game PAUSED - Player tower destroyed, game over panel shown");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
            {
                gameOverText.text = "Your Tower was Destroyed";
            }
        }
        else
        {
            Debug.LogWarning("[TOWER] gameOverPanel is not assigned in Inspector!");
        }
    }

    // ✅ NEW: Direct victory sequence called from OnTowerDestroyed
    private IEnumerator VictoryWithDialogueSequence()
    {
        Debug.Log("[TOWER] Enemy Tower Destroyed - VICTORY! Starting dialogue sequence...");

        int currentLevel = GetCurrentLevel();

        // Play victory SFX first
        if (AudioManager.Instance != null && AudioManager.Instance.gameWinSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameWinSFX);

        // STEP 1: Show end dialogue immediately
        if (DialogueManager.Instance != null)
        {
            Debug.Log($"[Tower] Showing victory dialogue for Level {currentLevel}");
            DialogueManager.Instance.ShowLevelEndDialogueForced(currentLevel);

            // Wait for dialogue to complete
            yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive());
            Debug.Log($"[Tower] Victory dialogue completed for Level {currentLevel}");
        }
        else
        {
            Debug.LogWarning("[Tower] DialogueManager not found - skipping victory dialogue");
        }

        // STEP 2: Show victory panel after dialogue
        ShowVictoryPanel();
    }


    // ✅ REFACTORED: Separated victory panel logic
    private void ShowVictoryPanel()
    {
        // Ensure game is immediately paused when victory panel appears
        Time.timeScale = 0f;
        Debug.Log("[Tower] ⏸️ Game PAUSED - Victory panel shown");

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            if (victoryText != null)
            {
                if (LevelManager.Instance != null)
                {
                    int currentLevel = LevelManager.Instance.GetCurrentLevel();
                    int totalLevels = LevelManager.Instance.GetTotalLevels();

                    Debug.Log($"[Tower] 🎮 Victory panel - Current: {currentLevel}, Total: {totalLevels}, IsLastLevel: {LevelManager.Instance.IsLastLevel()}");

                    // ✅ FIX: Use IsLastLevel() method for clarity
                    if (LevelManager.Instance.IsLastLevel())
                    {
                        victoryText.text = "You've Completed All Levels!\nCongratulations!";
                        Debug.Log($"[Tower] ✅ Showing 'All Levels Complete' message (Level {currentLevel} is last level, total: {totalLevels})");
                    }
                    else
                    {
                        victoryText.text = $"LEVEL {currentLevel} COMPLETE!\nProceeding to Level {currentLevel + 1}...";
                        Debug.Log($"[Tower] ✅ Showing 'Next Level' message (Level {currentLevel} → {currentLevel + 1})");
                    }
                }
                else
                {
                    victoryText.text = "VICTORY!\nEnemy Tower Destroyed!";
                    Debug.LogWarning("[Tower] ⚠️ LevelManager not found - using default message");
                }
            }
        }
        else
        {
            Debug.LogWarning("[TOWER] victoryPanel is not assigned in Inspector!");
        }

        if (nextLevelButton != null)
        {
            // ✅ FIX: Hide next level button if this is the final level
            bool isLastLevel = LevelManager.Instance != null && LevelManager.Instance.IsLastLevel();
            if (isLastLevel)
            {
                nextLevelButton.gameObject.SetActive(false);
                Debug.Log("[Tower] 🎉 Final level completed - next level button hidden");
            }
            else
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
                nextLevelButton.gameObject.SetActive(true);
                Debug.Log("[Tower] ✅ Next level button activated and made interactable");
            }
        }
        else
        {
            Debug.LogWarning("[TOWER] Next level button not assigned! Player cannot proceed.");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            mainMenuButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[TOWER] Main menu button not assigned! Player cannot return to menu.");
        }
    }

    // ✅ NEW: Helper method to get current level
    private int GetCurrentLevel()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.GetCurrentLevel();
        }

        if (GameManager.Instance != null)
        {
            return (int)GameManager.Instance.currentLevel;
        }

        return 1; // Fallback
    }

    private void OnNextLevelClicked()
    {
        Debug.Log("[TOWER] Next level button clicked!");
        if (LevelManager.Instance != null)
        {
            // ✅ FIX: Call LoadNextLevel() directly instead of LevelCompleted()
            // LevelCompleted() shows dialogue again, but we already showed it
            LevelManager.Instance.LoadNextLevel();
        }
        else
        {
            Debug.LogError("[TOWER] LevelManager not found!");
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[TOWER] Main menu button clicked!");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("[TOWER] LevelManager not found!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (AudioManager.Instance != null && AudioManager.Instance.hitTowerSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.hitTowerSFX);

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"[{owner} TOWER] Took {damage} damage → HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log($"[{owner} TOWER] DESTROYED!");
            OnTowerDestroyed();
        }
    }
}