using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class RarityDropRate
{
    public TroopRarity rarity;
    [Range(0f, 100f)]
    public float dropPercentage;
}

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [Header("Gacha Configuration")]
    [Tooltip("The coin cost to perform one summon.")]
    [SerializeField] private int summonCost = 100;

    [Tooltip("List of ALL TroopData ScriptableObjects available to be summoned.")]
    [SerializeField] private List<TroopData> allAvailableTroops;

    [Tooltip("The percentage chance for each rarity. The total should sum to 100.")]
    [SerializeField] private List<RarityDropRate> dropRates = new List<RarityDropRate>
    {
        new RarityDropRate { rarity = TroopRarity.Common, dropPercentage = 50f },
        new RarityDropRate { rarity = TroopRarity.Rare, dropPercentage = 30f },
        new RarityDropRate { rarity = TroopRarity.Epic, dropPercentage = 15f },
        new RarityDropRate { rarity = TroopRarity.Legendary, dropPercentage = 4.5f },
        new RarityDropRate { rarity = TroopRarity.Mythic, dropPercentage = 0.5f }
    };

    [Header("Spawn Position")]
    [Tooltip("The transform where the player's summoned troops will appear.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Upgrade Settings")]
    [SerializeField, Range(0, 5)] private int upgradeLevel = 0;

    // Cache for troop filtering by rarity
    private Dictionary<TroopRarity, List<TroopData>> _troopsByRarity;

    // Properties
    public int SummonCost => summonCost;
    public int UpgradeLevel => upgradeLevel;

    private void Awake()
    {
        // Assign instance here just like original
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"Multiple GachaManager instances detected. Destroying {gameObject.name}.");
            Destroy(gameObject);
        }

        InitializeTroopCache();
        ApplyUpgradeRates();
        ValidateDropRates();
    }

    // -------------------- SUMMON FUNCTION --------------------

    public TroopData SummonTroop()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("[Gacha] Cannot summon: Game is Over.");
            return null;
        }

        // Spend coins
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendPlayerCoins(summonCost))
        {
            Debug.Log($"[Gacha] Not enough coins to summon (Cost: {summonCost}).");
            return null;
        }

        // Roll rarity using drop rates
        TroopRarity pulledRarity = DetermineRarity();

        // Pick random troop based on rarity
        TroopData newTroop = GetRandomTroopOfRarity(pulledRarity);

        if (newTroop == null)
        {
            Debug.LogError("[Gacha] No troop found for rarity: " + pulledRarity);
            return null;
        }

        // Add to inventory
        bool added = TroopInventory.Instance.AddTroop(newTroop);
        if (!added)
        {
            Debug.Log("[Gacha] Inventory full, troop not added.");
            return null;
        }

        SpawnTroop(newTroop);

        Debug.Log($"🎉 [Gacha] Pulled {newTroop.displayName} ({newTroop.rarity})");
        return newTroop;
    }

    private TroopRarity DetermineRarity()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var rate in dropRates)
        {
            cumulativeProbability += rate.dropPercentage;
            if (randomValue < cumulativeProbability)
                return rate.rarity;
        }

        return TroopRarity.Common;
    }

    private TroopData GetRandomTroopOfRarity(TroopRarity rarity)
    {
    List<TroopData> filteredTroops = allAvailableTroops
        .Where(troop => troop.rarity == rarity)
        .ToList();

    if (filteredTroops.Count == 0)
    {
        return null;
    }

    int randomIndex = UnityEngine.Random.Range(0, filteredTroops.Count);
    return filteredTroops[randomIndex];
    }


    private void SpawnTroop(TroopData troop)
    {
        if (troop.prefab == null || playerSpawnPoint == null)
        {
            Debug.LogError("[Gacha] Cannot spawn! Prefab or Spawn Point is missing.");
            return;
        }

        GameObject newUnit = Instantiate(troop.prefab, playerSpawnPoint.position, Quaternion.identity);
        Unit unitScript = newUnit.GetComponent<Unit>();

        if (unitScript != null)
        {
            InitializeUnitStats(unitScript, troop);
        }
        else
        {
            Debug.LogError($"[Gacha] Instantiated prefab '{troop.displayName}' is missing the 'Unit' component.");
        }
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
            .Where(troop => troop != null)
            .GroupBy(troop => troop.rarity)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    // -------------------- DROP RATE VALIDATION --------------------

    private void ValidateDropRates()
    {
        float totalPercentage = dropRates.Sum(rate => rate.dropPercentage);
        if (Mathf.Abs(totalPercentage - 100f) > 0.01f)
        {
            Debug.LogWarning($"[GachaManager] Drop rates DO NOT sum to 100%! Total: {totalPercentage}. Please adjust the values in the Inspector.");
        }
    }

    // -------------------- UPGRADE SYSTEM --------------------

    public void UpgradeGachaSystem()
    {
        if (upgradeLevel >= 5)
        {
            Debug.Log("[Gacha] Already at max upgrade level.");
            return;
        }

        int cost = GetUpgradeCost();

        if (!CoinManager.Instance.TrySpendPlayerCoins(cost))
        {
            Debug.Log($"[Gacha] Not enough coins to upgrade! Need {cost} coins.");
            return;
        }

        upgradeLevel++;
        ApplyUpgradeRates();
        Debug.Log($"[Gacha] Upgraded to level {upgradeLevel} (Spent {cost} coins)");
    }

    public void SetUpgradeLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 0, 5);
        ApplyUpgradeRates();
        Debug.Log($"[Gacha] Upgrade level manually set to {upgradeLevel}");
    }

    private int GetUpgradeCost()
    {
        switch (upgradeLevel)
        {
            case 0: return 500;
            case 1: return 1000;
            case 2: return 2000;
            case 3: return 4000;
            case 4: return 8000;
            default: return int.MaxValue;
        }
    }

    private void ApplyUpgradeRates()
    {
        var upgradeModifiers = new Dictionary<int, (float common, float rare, float epic, float legendary, float mythic)>
        {
            {0, (0f, 0f, 0f, 0f, 0f)},
            {1, (-8f, 5f, 2.5f, 0.4f, 0.1f)},
            {2, (-15f, 9f, 4.5f, 1f, 0.5f)},
            {3, (-22f, 13f, 6.5f, 1.2f, 1.3f)},
            {4, (-30f, 16f, 9f, 2.5f, 2.5f)},
            {5, (-35f, 18f, 11f, 3f, 3f)}
        };

        ResetDropRatesToDefault();

        if (upgradeModifiers.TryGetValue(upgradeLevel, out var modifiers))
        {
            dropRates.Find(r => r.rarity == TroopRarity.Common).dropPercentage += modifiers.common;
            dropRates.Find(r => r.rarity == TroopRarity.Rare).dropPercentage += modifiers.rare;
            dropRates.Find(r => r.rarity == TroopRarity.Epic).dropPercentage += modifiers.epic;
            dropRates.Find(r => r.rarity == TroopRarity.Legendary).dropPercentage += modifiers.legendary;
            dropRates.Find(r => r.rarity == TroopRarity.Mythic).dropPercentage += modifiers.mythic;
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
        if (total <= 0f)
        {
            Debug.LogError("[Gacha] Total drop rate is 0 or negative. Cannot normalize.");
            return;
        }

        foreach (var rate in dropRates)
        {
            rate.dropPercentage = (rate.dropPercentage / total) * 100f;
        }
    }
}
