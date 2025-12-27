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
    [Tooltip("The current level of the tower. Starts at 1.")]
    public int level = 1; 
    private const int MAX_LEVEL = 10;

    [Tooltip("Base coins generated per tick at Level 1")]
    private const int TOWER_BASE_RATE = 1; 
    
    [Tooltip("The cost of the first upgrade (Lv 1->2).")]
    private const int TOWER_FIRST_UPGRADE_COST = 10;

    [Tooltip("The amount the cost increases by each level (300, 400, etc.)")]
    private const int TOWER_COST_INCREMENT = 15;

    [Header("Coin Generation")]
    public int coinsPerTick = 1;
    public float coinInterval = 1f;
    private float coinTimer = 0f;

    [Header("Tutorial Reference")]
    public TutorialManager tutorialManager; 
    
    [Header("UI References (Player Only)")]
    [Tooltip("The Text component on the Upgrade button that shows the price.")]
    public TextMeshProUGUI upgradeButtonText; 

    [Tooltip("The Image component of the Upgrade Button to change its visual.")]
    public Image upgradeButtonImage;
    
    [Tooltip("The sprite to display when the upgrade CAN be afforded (original).")]
    public Sprite affordableSprite;
    
    [Tooltip("The sprite to display when the upgrade CANNOT be afforded (different sprite).")]
    public Sprite unaffordableSprite;

    [Header("Win/Loss UI")]
    [Tooltip("Panel to show when player loses (player tower destroyed)")]
    public GameObject gameOverPanel;
    
    [Tooltip("Panel to show when player wins (enemy tower destroyed)")]
    public GameObject victoryPanel;
    
    [Tooltip("Text to display game over message")]
    public TextMeshProUGUI gameOverText;
    
    [Tooltip("Text to display victory message")]
    public TextMeshProUGUI victoryText;

    [Tooltip("Button for player to proceed to next level")]
    public Button nextLevelButton;

    [Tooltip("Button for player to return to main menu")]
    public Button mainMenuButton;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Method to repair/restore tower to full health (used after tutorial)
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

        // Ensure UI panels are hidden at start
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
        HandleCoinGeneration();

        if (owner == TowerOwner.Player)
        {
            UpdateUpgradeUI(); 
        }
    }

    public int CalculateUpgradeCost()
    {
        int nextLevel = level + 1;
        
        // Cost for Lv 2 (Next Level = 2) is the first upgrade cost.
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
        
        // If tower is max level, disable the upgrade button entirely
        if (level >= MAX_LEVEL)
        {
            upgradeButtonText.text = $"MAX LEVEL ({MAX_LEVEL})";
            
            if (button != null)
                button.interactable = false;

            // Use unaffordable sprite for grey-out
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

        // Prevent upgrading beyond maximum level
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
    // Check assigned tutorial manager first
    TutorialManager tm = tutorialManager;

    // If not assigned, try to find it
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
            // PLAYER LOST - Show Game Over screen
            ShowGameOver();
        }
        else if (owner == TowerOwner.Enemy)
        {
            // PLAYER WON - Show Victory screen and proceed to next level
            ShowVictory();
        }
    }



    private void ShowGameOver()
    {
        Debug.Log("[TOWER] Player Tower Destroyed - GAME OVER");

        // Play dramatic Game Over SFX (duck music + boost SFX)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverDramatic();
        }
        // Stop game time
        Time.timeScale = 0f;

        // Show game over panel
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

    private void ShowVictory()
    {
        Debug.Log("[TOWER] Enemy Tower Destroyed - VICTORY!");

        // Play victory SFX
        if (AudioManager.Instance != null && AudioManager.Instance.gameWinSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameWinSFX);

        // Freeze the screen
        Time.timeScale = 0f;

        // Show victory panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            if (victoryText != null)
            {
                if (LevelManager.Instance != null)
                {
                    int currentLevel = LevelManager.Instance.GetCurrentLevel();
                    int totalLevels = LevelManager.Instance.GetTotalLevels();

                    if (currentLevel >= totalLevels)
                    {
                        victoryText.text = "You've Completed All Levels!\nCongratulations!";
                    }
                    else
                    {
                        victoryText.text = $"LEVEL {currentLevel} COMPLETE!\nProceeding to Level {currentLevel + 1}...";
                    }
                }
                else
                {
                    victoryText.text = "VICTORY!\nEnemy Tower Destroyed!";
                }
            }
        }
        else
        {
            Debug.LogWarning("[TOWER] victoryPanel is not assigned in Inspector!");
        }

        // Set up next level button (player must click to proceed)
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            nextLevelButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[TOWER] Next level button not assigned! Player cannot proceed.");
        }

        // Set up main menu button (player can click to return to menu)
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            mainMenuButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[TOWER] Main menu button not assigned! Player cannot return to menu.");
        }
    }

    // Called when player clicks the Next Level button
    private void OnNextLevelClicked()
    {
        Debug.Log("[TOWER] Next level button clicked!");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LevelCompleted();
        }
        else
        {
            Debug.LogError("[TOWER] LevelManager not found!");
        }
    }

    // Called when player clicks the Main Menu button
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