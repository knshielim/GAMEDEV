using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class RarityDropRate
{
    public TroopRarity rarity;
    [Range(0f, 100f)] public float dropPercentage;
}

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    public bool tutorialLocked = true;


    [Header("Gacha Configuration")]
    [Tooltip("The coin cost to perform one summon.")]
    [SerializeField] private int summonCost = 2;

    [Header("Gacha Cost Escalation")]
    [Tooltip("Increase cost per 1x summon.")]
    [SerializeField] private int summonCostIncrease = 2;
    private int summonsSinceReset = 0;

    [Tooltip("All TroopData ScriptableObjects available.")]
    [SerializeField] private List<TroopData> allAvailableTroops;

    [Tooltip("Drop rates for each rarity.")]
    [SerializeField] private List<RarityDropRate> dropRates = new List<RarityDropRate>
    {
        new RarityDropRate { rarity = TroopRarity.Common, dropPercentage = 80f },
        new RarityDropRate { rarity = TroopRarity.Rare, dropPercentage = 15f },
        new RarityDropRate { rarity = TroopRarity.Epic, dropPercentage = 4f },
        new RarityDropRate { rarity = TroopRarity.Legendary, dropPercentage = 1f },
        new RarityDropRate { rarity = TroopRarity.Mythic, dropPercentage = 0f }
    };

    [Header("Spawn Position")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("UI References")]
    public TextMeshProUGUI upgradeButtonText;
    public Image upgradeButtonImage;
    public Sprite affordableSprite;
    public Sprite unaffordableSprite;

    [Tooltip("Text to display current summon cost")]
    public TextMeshProUGUI summonCostText;

    [Header("Summon Rate Tooltip")]
    [Tooltip("Panel to show summon rates on hover")]
    public GameObject summonRateTooltip;

    [Tooltip("Text component to display the summon rate information")]
    public TextMeshProUGUI summonRateText;

    private Dictionary<TroopRarity, List<TroopData>> _troopsByRarity;
    private bool tutorialFirstSummonDone = false;

    public int SummonCost => summonCost;

    public List<TroopData> GetAllTroopData()
    {
        return allAvailableTroops;
    }
    public int UpgradeLevel => SpawnRateBalancer.Instance?.GetUpgradeLevel() ?? 0;

    private void Awake()
    {
        // ✅ CHANGED: Allow one GachaManager per scene (not persistent)
        if (Instance == null)
        {
            Instance = this;
            // REMOVED DontDestroyOnLoad - this was causing stale references
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ✅ ADDED: Reset tutorial flags when GachaManager is created
        tutorialLocked = false;
        tutorialFirstSummonDone = false;

        InitializeTroopCache();
        ValidateDropRates();

        // Hide tooltip initially
        if (summonRateTooltip != null)
        {
            summonRateTooltip.SetActive(false);
        }
    }

    // ✅ ADDED: Clean up instance reference on destroy
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void DebugSummonButton()
    {
        Debug.Log($"=== SUMMON BUTTON DEBUG ===");
        Debug.Log($"Tutorial Locked: {tutorialLocked}");
        Debug.Log($"Current Cost: {GetCurrentSummonCost()}");
        Debug.Log($"Player Coins: {(CoinManager.Instance != null ? CoinManager.Instance.playerCoins : -1)}");
        Debug.Log($"CoinManager exists: {CoinManager.Instance != null}");
        Debug.Log($"TroopInventory exists: {TroopInventory.Instance != null}");
        Debug.Log($"GameManager exists: {GameManager.Instance != null}");
        if (GameManager.Instance != null)
            Debug.Log($"Game Over: {GameManager.Instance.IsGameOver()}");

        // Add spawn rate balancer debug info
        Debug.Log($"=== SPAWN RATE BALANCER DEBUG ===");
        Debug.Log(SpawnRateBalancer.Instance.GetBalancingInfo());
    }

    // Summon button hover functionality
    public void OnSummonButtonHoverEnter()
    {
        if (summonRateTooltip != null)
        {
            summonRateTooltip.SetActive(true);
            UpdateSummonRateTooltip();
        }
    }

    public void OnSummonButtonHoverExit()
    {
        if (summonRateTooltip != null)
        {
            summonRateTooltip.SetActive(false);
        }
    }

    private void UpdateSummonRateTooltip()
    {
        if (summonRateText == null || SpawnRateBalancer.Instance == null) return;

        var currentRates = SpawnRateBalancer.Instance.GetCurrentRates();

        string tooltipText = $"Common: {currentRates[TroopRarity.Common]:F1}%\n";
        tooltipText += $"Rare: {currentRates[TroopRarity.Rare]:F1}%\n";
        tooltipText += $"Epic: {currentRates[TroopRarity.Epic]:F1}%\n";
        tooltipText += $"Legendary: {currentRates[TroopRarity.Legendary]:F1}%";

        summonRateText.text = tooltipText;
    }
    
    private void Start()
    {
        // Check if TutorialManager exists in current scene
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        
        if (tutorialManager != null && tutorialManager.enabled)
        {
            // Tutorial scene - lock summoning until tutorial allows it
            tutorialLocked = true;
            Debug.Log("[GachaManager] Tutorial detected - summoning locked");
        }
        else
        {
            // Normal gameplay - allow summoning
            tutorialLocked = false;
            tutorialFirstSummonDone = false; // Reset tutorial flag
            Debug.Log("[GachaManager] No tutorial - summoning enabled");
        }

        // ✅ ADDED: Update UI to show correct initial upgrade level and cost
        UpdateUpgradeUI();
        UpdateSummonCostUI();
    }

    // -------------------- SUMMON FUNCTION --------------------
    public TroopData SummonTroop(TroopData tutorialTroop = null)
    {
        Debug.Log($"[Gacha] SummonTroop called - tutorialLocked: {tutorialLocked}, Instance exists: {Instance != null}, TroopInventory exists: {TroopInventory.Instance != null}, CoinManager exists: {CoinManager.Instance != null}");
        
        if (tutorialLocked)
        {
            Debug.LogWarning("[Gacha] Summoning is locked by tutorial");
            return null;
        }

        // -------------------- TUTORIAL OVERRIDE --------------------
        if (!tutorialFirstSummonDone && tutorialTroop != null)
            {
            tutorialFirstSummonDone = true;

            int cost = GetCurrentSummonCost();

            // Spend coins for tutorial summon
            if (!CoinManager.Instance.TrySpendPlayerCoins(cost))
            {
                Debug.LogWarning("[Tutorial Gacha] Not enough coins for tutorial summon!");
                return null;
            }

            // Escalate cost normally
            summonsSinceReset++;

            // Add tutorial troop to inventory
            if (TroopInventory.Instance != null)
            {
                bool added = TroopInventory.Instance.AddTroop(tutorialTroop);
                if (added)
                    Debug.Log($"[Tutorial Gacha] Added tutorial troop to inventory: {tutorialTroop.displayName}");
                else
                    Debug.LogWarning("[Tutorial Gacha] Failed to add tutorial troop, inventory may be full.");
            }

            // Update summon cost UI after spending coins
            UpdateSummonCostUI();

            return tutorialTroop;
        }


        // -------------------- NORMAL SUMMON --------------------
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("[Gacha] Cannot summon: Game is over.");
            return null;
        }

        int currentCost = GetCurrentSummonCost();
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendPlayerCoins(currentCost))
        {
            Debug.Log($"[Gacha] Not enough coins to summon (Cost: {currentCost}).");
            return null;
        }

        summonsSinceReset++;
        Debug.Log($"[Gacha] Summon #{summonsSinceReset} (Cost: {currentCost})");

        TroopRarity pulledRarity = DetermineRarity();
        TroopData newTroop = GetRandomTroopOfRarity(pulledRarity);

        if (newTroop == null)
        {
            Debug.LogError($"[Gacha] No troop found for rarity: {pulledRarity}");
            CoinManager.Instance.AddPlayerCoins(currentCost);
            return null;
        }
        if (TroopInventory.Instance == null)
        {
            Debug.LogError("[Gacha] TroopInventory.Instance is NULL!");
            CoinManager.Instance.AddPlayerCoins(currentCost);
            return null;
        }

        bool addedToInventory = TroopInventory.Instance.AddTroop(newTroop);
        if (!addedToInventory)
        {
            Debug.LogWarning("[Gacha] Inventory full, troop not added - refunding coins.");
            CoinManager.Instance.AddPlayerCoins(currentCost);
            return null;
        }

        Debug.Log($"[Gacha] Successfully added {newTroop.displayName} to inventory, refreshing UI...");
        TroopInventory.Instance.RefreshUI();
        Debug.Log($"🎉 [Gacha] Pulled {newTroop.displayName} ({newTroop.rarity})");

        // Record spawn for balancing system
        SpawnRateBalancer.Instance.RecordSpawn(newTroop.rarity);

        // Activate reactive boost if player spawns high-rarity unit (vice versa)
        if (newTroop.rarity == TroopRarity.Epic || newTroop.rarity == TroopRarity.Legendary)
        {
            Debug.Log($"[Gacha] 🔔 HIGH-RARITY PLAYER SUMMON DETECTED: {newTroop.displayName} ({newTroop.rarity})");
            Debug.Log($"[Gacha] 🎯 This activates reactive balancing (vice versa effect)!");
            SpawnRateBalancer.Instance.ActivateReactiveBoost(newTroop.rarity);
        }

        // Update summon cost display after successful summon
        UpdateSummonCostUI();

        return newTroop;
    }

    private TroopRarity DetermineRarity()
    {
        // Use balanced rates from SpawnRateBalancer
        var balancedRates = SpawnRateBalancer.Instance.GetCurrentRates();
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var rate in balancedRates)
        {
            cumulative += rate.Value;
            if (randomValue < cumulative)
                return rate.Key;
        }

        return TroopRarity.Common;
    }

    public TroopData GetRandomTroopOfRarity(TroopRarity rarity)
    {
        // Exclude Samurai and Vampire from regular summoning (only obtainable via mythic combination)
        List<TroopData> filtered = allAvailableTroops
            .Where(t => t.rarity == rarity &&
                       t.id != "Samurai" &&
                       t.id != "Vampire")
            .ToList();

        if (filtered.Count == 0) return null;

        return filtered[UnityEngine.Random.Range(0, filtered.Count)];
    }

    private void SpawnTroop(TroopData troop)
    {
        if (troop.playerPrefab == null || playerSpawnPoint == null)
        {
            Debug.LogError("[Gacha] Cannot spawn! Player prefab or Spawn Point is missing.");
            return;
        }

        GameObject newUnit = Instantiate(troop.playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        Unit unitScript = newUnit.GetComponent<Unit>();

        if (unitScript != null)
            InitializeUnitStats(unitScript, troop);
    }

    private static void InitializeUnitStats(Unit unit, TroopData troopData)
    {
        unit.MaxHealth = troopData.maxHealth;
        unit.currentHealth = troopData.maxHealth;
        unit.attackPoints = troopData.attack;
        unit.moveSpeed = troopData.moveSpeed;
        unit.attackSpeed = troopData.attackInterval;
    }

    // -------------------- TROOP CACHE --------------------
    private void InitializeTroopCache()
    {
        _troopsByRarity = allAvailableTroops
            .Where(t => t != null)
            .GroupBy(t => t.rarity)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // -------------------- DROP RATE VALIDATION --------------------
    private void ValidateDropRates()
    {
        float total = dropRates.Sum(r => r.dropPercentage);
        if (Mathf.Abs(total - 100f) > 0.01f)
            Debug.LogWarning($"[GachaManager] Drop rates DO NOT sum to 100%! Total: {total}");
    }

    // -------------------- UPGRADE SYSTEM --------------------
    public void UpgradeGachaSystem()
    {
        if (SpawnRateBalancer.Instance == null) return;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeSFX);
        if (SpawnRateBalancer.Instance.GetUpgradeLevel() >= 10) return;

        int cost = SpawnRateBalancer.Instance.GetUpgradeCost();
        if (!CoinManager.Instance.TrySpendPlayerCoins(cost)) return;

        SpawnRateBalancer.Instance.UpgradeSummonSystem();
        UpdateUpgradeUI();

        Debug.Log($"[Gacha] Upgraded to level {SpawnRateBalancer.Instance.GetUpgradeLevel()} (Spent {cost} coins)");
    }

    public void SetUpgradeLevel(int level)
    {
        if (SpawnRateBalancer.Instance == null) return;

        SpawnRateBalancer.Instance.SetUpgradeLevel(level);
        UpdateUpgradeUI();
    }

    private int GetUpgradeCost()
    {
        return SpawnRateBalancer.Instance?.GetUpgradeCost() ?? int.MaxValue;
    }

    // -------------------- SUMMON COST SYSTEM --------------------
    public int GetCurrentSummonCost() =>
        summonCost + summonsSinceReset * summonCostIncrease;

    public void ResetGachaCost()
    {
        summonsSinceReset = 0;
        UpdateSummonCostUI();
    }

    public void IncreaseSummonCost()
    {
    summonsSinceReset++;
    UpdateSummonCostUI();   
    }


    // -------------------- UI UPDATE --------------------
    public void UpdateSummonCostUI()
    {
        if (summonCostText != null)
        {
            int currentCost = GetCurrentSummonCost();
            summonCostText.text = $"Cost: {currentCost}";
        }
    }

    public void UpdateUpgradeUI()
    {
        if (SpawnRateBalancer.Instance == null || upgradeButtonText == null || upgradeButtonImage == null)
            return;

        int currentLevel = SpawnRateBalancer.Instance.GetUpgradeLevel();

        if (currentLevel >= 10)
        {
            upgradeButtonText.text = "Max level reached";
            upgradeButtonImage.sprite = unaffordableSprite;

            var button = upgradeButtonText.GetComponentInParent<Button>();
            if (button != null) button.interactable = false;

            return;
        }

        int nextLevel = currentLevel + 1;
        int cost = GetUpgradeCost();

        bool canAfford = CoinManager.Instance != null &&
                         CoinManager.Instance.playerCoins >= cost;

        var btn = upgradeButtonText.GetComponentInParent<Button>();
        if (btn != null)
            btn.interactable = canAfford;

        upgradeButtonImage.sprite = canAfford ? affordableSprite : unaffordableSprite;
        upgradeButtonText.text = $"Upgrade to Lv. {nextLevel}       Cost: {cost}";

        // Keep button gray when unaffordable
        if (!canAfford && btn != null)
        {
            SetButtonGrayColor(btn);
        }
    }

    private void SetButtonGrayColor(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        Color grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Consistent gray color

        // Set all color states to gray to prevent hover/press color changes
        colors.normalColor = grayColor;
        colors.highlightedColor = grayColor;
        colors.pressedColor = grayColor;
        colors.selectedColor = grayColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Darker gray for disabled

        button.colors = colors;
    }
}