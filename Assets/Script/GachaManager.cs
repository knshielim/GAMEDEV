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

    public bool tutorialLocked = false;


    [Header("Gacha Configuration")]
    [Tooltip("The coin cost to perform one summon.")]
    [SerializeField] private int summonCost = 100;

    [Header("Gacha Cost Escalation")]
    [Tooltip("Increase cost per 1x summon.")]
    [SerializeField] private int summonCostIncrease = 25;
    private int summonsSinceReset = 0;

    [Tooltip("All TroopData ScriptableObjects available.")]
    [SerializeField] private List<TroopData> allAvailableTroops;

    [Tooltip("Drop rates for each rarity.")]
    [SerializeField] private List<RarityDropRate> dropRates = new List<RarityDropRate>
    {
        new RarityDropRate { rarity = TroopRarity.Common, dropPercentage = 50f },
        new RarityDropRate { rarity = TroopRarity.Rare, dropPercentage = 30f },
        new RarityDropRate { rarity = TroopRarity.Epic, dropPercentage = 15f },
        new RarityDropRate { rarity = TroopRarity.Legendary, dropPercentage = 4.5f },
        new RarityDropRate { rarity = TroopRarity.Mythic, dropPercentage = 0.5f }
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

    private Dictionary<TroopRarity, List<TroopData>> _troopsByRarity;
    private bool tutorialFirstSummonDone = false;

    public int SummonCost => summonCost;
    public int UpgradeLevel => upgradeLevel;

   private void Awake()
    {
    if (Instance == null) Instance = this;
    else
    {
        Destroy(gameObject);
        return;
    }

    // If TutorialManager is disabled, allow summoning normally
    tutorialLocked = false;

    InitializeTroopCache();
    ApplyUpgradeRates();
    ValidateDropRates();
    }


    // -------------------- SUMMON FUNCTION --------------------
    public TroopData SummonTroop(TroopData tutorialTroop = null)
    {
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
            return null;
        }

        bool addedToInventory = TroopInventory.Instance.AddTroop(newTroop);
        if (!addedToInventory)
        {
            Debug.LogWarning("[Gacha] Inventory full, troop not added.");
            return null;
        }

        TroopInventory.Instance.RefreshUI();
        Debug.Log($"🎉 [Gacha] Pulled {newTroop.displayName} ({newTroop.rarity})");

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
        List<TroopData> filtered = allAvailableTroops.Where(t => t.rarity == rarity).ToList();
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
        (upgradeLevel >= 10) ? int.MaxValue : (upgradeLevel + 1) * 100;

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
        dropRates.Find(r => r.rarity == TroopRarity.Common).dropPercentage = 50f;
        dropRates.Find(r => r.rarity == TroopRarity.Rare).dropPercentage = 30f;
        dropRates.Find(r => r.rarity == TroopRarity.Epic).dropPercentage = 15f;
        dropRates.Find(r => r.rarity == TroopRarity.Legendary).dropPercentage = 4.5f;
        dropRates.Find(r => r.rarity == TroopRarity.Mythic).dropPercentage = 0.5f;
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

    public void ResetGachaCost() =>
        summonsSinceReset = 0;

    // -------------------- UI UPDATE --------------------
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
        upgradeButtonText.text = $"Upgrade to Lv. {nextLevel}\nCost: {cost}";
    }
}
