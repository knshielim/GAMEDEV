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
    [Range(0f, 100f)]
    public float dropPercentage;
}

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [Header("Gacha Configuration")]
    [Tooltip("The coin cost to perform one summon.")]
    [SerializeField] private int summonCost = 100;

    [Header("Gacha Cost Escalation")]
    [Tooltip("Kenaikan harga setiap kali melakukan 1x summon.")]
    [SerializeField] private int summonCostIncrease = 25;
    private int summonsSinceReset = 0;

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
    [SerializeField, Range(0, 10)] private int upgradeLevel = 0;

    [Header("UI References (Player Only)")]
    [Tooltip("The Text component on the Upgrade button that shows the price.")]
    public TextMeshProUGUI upgradeButtonText;

    [Tooltip("The Image component of the Upgrade Button to change its visual.")]
    public Image upgradeButtonImage;

    [Tooltip("The sprite to display when the upgrade CAN be afforded (original).")]
    public Sprite affordableSprite;

    [Tooltip("The sprite to display when the upgrade CANNOT be afforded (different sprite).")]
    public Sprite unaffordableSprite;

    // Cache for troop filtering by rarity
    private Dictionary<TroopRarity, List<TroopData>> _troopsByRarity;

    // Properties
    public int SummonCost => summonCost;
    public int UpgradeLevel => upgradeLevel;

    private void Awake()
    {
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

    private void Update()
    {
        UpdateUpgradeUI();
    }

    // -------------------- SUMMON FUNCTION --------------------
    public TroopData SummonTroop()
    {
        Debug.Log("[Gacha] SummonTroop is called: " + Time.time);
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("[Gacha] Cannot summon: Game is Over.");
            return null;
        }

        // Calculate current cost
        int currentCost = GetCurrentSummonCost();

        // Spend coins
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendPlayerCoins(currentCost))
        {
            Debug.Log($"[Gacha] Not enough coins to summon (Cost: {currentCost}).");
            return null;
        }

        // Increase counter
        summonsSinceReset++;
        Debug.Log($"[Gacha] Summon: {summonsSinceReset} (Cost: {currentCost})");

        // Determine rarity
        TroopRarity pulledRarity = DetermineRarity();

        // Pick random troop
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

        // Refresh UI and spawn troop
        TroopInventory.Instance.RefreshUI();
        // SpawnTroop(newTroop);

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

    public TroopData GetRandomTroopOfRarity(TroopRarity rarity)
    {
        List<TroopData> filteredTroops = allAvailableTroops
            .Where(troop => troop.rarity == rarity)
            .ToList();

        if (filteredTroops.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, filteredTroops.Count);
        return filteredTroops[randomIndex];
    }

    private void SpawnTroop(TroopData troop)
    {
        Debug.Log("[Gacha] SpawnTroop called for: " + troop.displayName + " | time: " + Time.time);
        if (troop.playerPrefab == null || playerSpawnPoint == null)
        {
            Debug.LogError("[Gacha] Cannot spawn! Player prefab or Spawn Point is missing.");
            return;
        }

        GameObject newUnit = Instantiate(troop.playerPrefab, playerSpawnPoint.position, Quaternion.identity);
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
        if (upgradeLevel >= 10)
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
        UpdateUpgradeUI();
    }

    public void SetUpgradeLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 0, 10);
        ApplyUpgradeRates();
        UpdateUpgradeUI();
        Debug.Log($"[Gacha] Upgrade level manually set to {upgradeLevel}");
    }

    private int GetUpgradeCost()
    {
        if (upgradeLevel >= 10)
            return int.MaxValue;

        return (upgradeLevel + 1) * 100;
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
            {5, (-35f, 18f, 11f, 3f, 3f)},
            {6, (-38f, 20f, 12f, 3.5f, 3.5f)},
            {7, (-41f, 22f, 13f, 4f, 4f)},
            {8, (-44f, 24f, 14f, 4.5f, 4.5f)},
            {9, (-47f, 26f, 15f, 5f, 5f)},
            {10, (-50f, 28f, 16f, 5.5f, 5.5f)}
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

    // -------------------- SUMMON COST SYSTEM --------------------
    public int GetCurrentSummonCost()
    {
        return summonCost + summonsSinceReset * summonCostIncrease;
    }

    public void ResetGachaCost()
    {
        summonsSinceReset = 0;
        Debug.Log("[Gacha] Summon cost escalation reset (stage/level baru).");
    }

    // -------------------- UI UPDATE METHOD --------------------
    public void UpdateUpgradeUI()
    {
        if (upgradeButtonText == null)
        {
            Debug.LogError("[GACHA UI ERROR] upgradeButtonText is MISSING in the Inspector.");
            return;
        }
        if (upgradeButtonImage == null)
        {
            Debug.LogError("[GACHA UI ERROR] upgradeButtonImage is MISSING in the Inspector. This is why the sprite isn't changing.");
            return;
        }

        // Check if already at max level
        if (upgradeLevel >= 10)
        {
            upgradeButtonText.text = "Max level reached";

            // Disable the button
            UnityEngine.UI.Button button = upgradeButtonText.GetComponentInParent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.interactable = false;
            }

            // Set to unaffordable sprite if available
            if (unaffordableSprite != null)
            {
                upgradeButtonImage.sprite = unaffordableSprite;
            }

            return;
        }

        int nextLevel = upgradeLevel + 1;
        int cost = GetUpgradeCost();
        bool canAfford = false;

        if (CoinManager.Instance != null)
        {
            canAfford = CoinManager.Instance.playerCoins >= cost;

            UnityEngine.UI.Button button = upgradeButtonText.GetComponentInParent<UnityEngine.UI.Button>();
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
                Debug.LogWarning("[GACHA UI WARNING] affordableSprite is MISSING.");
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
                Debug.LogWarning("[GACHA UI WARNING] unaffordableSprite is MISSING.");
            }
        }

        upgradeButtonText.text = $"Upgrade to Lv. {nextLevel}\nCost: {cost}";
    }
}
