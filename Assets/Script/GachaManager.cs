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

    [Header("Upgrade Settings")]
    [SerializeField, Range(0, 10)] private int upgradeLevel = 0;

    [Header("UI References")]
    public TextMeshProUGUI upgradeButtonText;
    public Image upgradeButtonImage;
    public Sprite affordableSprite;
    public Sprite unaffordableSprite;

    [Tooltip("Text to display current summon cost")]
    public TextMeshProUGUI summonCostText;

    private Dictionary<TroopRarity, List<TroopData>> _troopsByRarity;
    private bool tutorialFirstSummonDone = false;

    public int SummonCost => summonCost;

    public List<TroopData> GetAllTroopData()
    {
        return allAvailableTroops;
    }
    public int UpgradeLevel => upgradeLevel;

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
        ApplyUpgradeRates();
        ValidateDropRates();
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
        
        // ✅ ADDED: Check if summoning is locked by tutorial
        if (tutorialLocked)
        {
            Debug.LogWarning("[Gacha] Summoning is locked by tutorial");
            return null;
        }

        // -------------------- TUTORIAL OVERRIDE --------------------
        if (!tutorialFirstSummonDone && tutorialTroop != null)
        {
            tutorialFirstSummonDone = true;

            // Add tutorial troop to inventory only (no spawn)
            if (TroopInventory.Instance != null)
            {
                bool added = TroopInventory.Instance.AddTroop(tutorialTroop);
                if (added)
                    Debug.Log($"[Tutorial Gacha] Added tutorial troop to inventory: {tutorialTroop.displayName}");
                else
                    Debug.LogWarning("[Tutorial Gacha] Failed to add tutorial troop, inventory may be full.");
            }

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
            // ✅ ADDED: Refund coins if no troop found
            CoinManager.Instance.AddPlayerCoins(currentCost);
            return null;
        }

        // ✅ ADDED: Check if TroopInventory exists before adding
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
            // ✅ ADDED: Refund coins if inventory is full
            CoinManager.Instance.AddPlayerCoins(currentCost);
            return null;
        }

        Debug.Log($"[Gacha] Successfully added {newTroop.displayName} to inventory, refreshing UI...");
        TroopInventory.Instance.RefreshUI();
        Debug.Log($"🎉 [Gacha] Pulled {newTroop.displayName} ({newTroop.rarity})");

        // Update summon cost display after successful summon
        UpdateSummonCostUI();

        return newTroop;
    }

    private TroopRarity DetermineRarity()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var rate in dropRates)
        {
            cumulative += rate.dropPercentage;
            if (randomValue < cumulative)
                return rate.rarity;
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
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeSFX);
        if (upgradeLevel >= 10) return;

        int cost = GetUpgradeCost();
        if (!CoinManager.Instance.TrySpendPlayerCoins(cost)) return;

        upgradeLevel++;
        ApplyUpgradeRates();
        UpdateUpgradeUI();

        Debug.Log($"[Gacha] Upgraded to level {upgradeLevel} (Spent {cost} coins)");
    }

    public void SetUpgradeLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 0, 10);
        ApplyUpgradeRates();
        UpdateUpgradeUI();
    }

    private int GetUpgradeCost() =>
        (upgradeLevel >= 10) ? int.MaxValue : 20 + (upgradeLevel * 15);

    private void ApplyUpgradeRates()
    {
        var modifiers = new Dictionary<int, (float common, float rare, float epic, float legendary, float mythic)>
        {
            {0, (0f,0f,0f,0f,0f)},
            {1,(-8f,5f,2.5f,0.4f,0.1f)},
            {2,(-15f,9f,4.5f,1f,0.5f)},
            {3,(-22f,13f,6.5f,1.2f,1.3f)},
            {4,(-30f,16f,9f,2.5f,2.5f)},
            {5,(-35f,18f,11f,3f,3f)},
            {6,(-38f,20f,12f,3.5f,3.5f)},
            {7,(-41f,22f,13f,4f,4f)},
            {8,(-44f,24f,14f,4.5f,4.5f)},
            {9,(-47f,26f,15f,5f,5f)},
            {10,(-50f,28f,16f,5.5f,5.5f)}
        };

        ResetDropRatesToDefault();

        if (modifiers.TryGetValue(upgradeLevel, out var m))
        {
            dropRates.Find(r => r.rarity == TroopRarity.Common).dropPercentage += m.common;
            dropRates.Find(r => r.rarity == TroopRarity.Rare).dropPercentage += m.rare;
            dropRates.Find(r => r.rarity == TroopRarity.Epic).dropPercentage += m.epic;
            dropRates.Find(r => r.rarity == TroopRarity.Legendary).dropPercentage += m.legendary;
            dropRates.Find(r => r.rarity == TroopRarity.Mythic).dropPercentage += m.mythic;
        }

        NormalizeDropRates();
    }

    private void ResetDropRatesToDefault()
    {
        dropRates.Find(r => r.rarity == TroopRarity.Common).dropPercentage = 80f;
        dropRates.Find(r => r.rarity == TroopRarity.Rare).dropPercentage = 15f;
        dropRates.Find(r => r.rarity == TroopRarity.Epic).dropPercentage = 4f;
        dropRates.Find(r => r.rarity == TroopRarity.Legendary).dropPercentage = 1f;
        dropRates.Find(r => r.rarity == TroopRarity.Mythic).dropPercentage = 0f;
    }

    private void NormalizeDropRates()
    {
        float total = dropRates.Sum(r => r.dropPercentage);
        if (total <= 0f) return;

        foreach (var rate in dropRates)
            rate.dropPercentage = (rate.dropPercentage / total) * 100f;
    }

    // -------------------- SUMMON COST SYSTEM --------------------
    public int GetCurrentSummonCost() =>
        summonCost + summonsSinceReset * summonCostIncrease;

    public void ResetGachaCost()
    {
        summonsSinceReset = 0;
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
        if (upgradeButtonText == null || upgradeButtonImage == null)
            return;

        if (upgradeLevel >= 10)
        {
            upgradeButtonText.text = "Max level reached";
            upgradeButtonImage.sprite = unaffordableSprite;

            var button = upgradeButtonText.GetComponentInParent<Button>();
            if (button != null) button.interactable = false;

            return;
        }

        int nextLevel = upgradeLevel + 1;
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