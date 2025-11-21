using UnityEngine;
using System.Collections.Generic;
using System.Linq;


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
    public int summonCost = 100;

    [Tooltip("List of ALL TroopData ScriptableObjects available to be summoned.")]
    public List<TroopData> allAvailableTroops;

    [Tooltip("The percentage chance for each rarity. The total should sum to 100.")]
    public List<RarityDropRate> dropRates = new List<RarityDropRate>
    {
        
        new RarityDropRate { rarity = TroopRarity.Common, dropPercentage = 50f },
        new RarityDropRate { rarity = TroopRarity.Rare, dropPercentage = 30f },
        new RarityDropRate { rarity = TroopRarity.Epic, dropPercentage = 15f },
        new RarityDropRate { rarity = TroopRarity.Legendary, dropPercentage = 4.5f },
        new RarityDropRate { rarity = TroopRarity.Mythic, dropPercentage = 0.5f }
    };

    [Header("Spawn Position")]
    [Tooltip("The transform where the player's summoned troops will appear.")]
    public Transform playerSpawnPoint; 

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        

        ValidateDropRates();
    } 

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

        // Roll rarity using your drop rates
        TroopRarity pulledRarity = DetermineRarity();

        // Pick random troop based on rarity
        TroopData newTroop = GetRandomTroopOfRarity(pulledRarity);

        if (newTroop == null)
        {
            Debug.LogError("[Gacha] No troop found for rarity: " + pulledRarity);
            return null;
        }

        // ADD TO INVENTORY
        bool added = TroopInventory.Instance.AddTroop(newTroop);

        if (!added)
        {
            Debug.Log("[Gacha] Inventory full, troop not added.");
            return null;
        }

        Debug.Log($"🎉 [Gacha] Pulled {newTroop.displayName} ({newTroop.rarity})");

        return newTroop;
    }


    private TroopRarity DetermineRarity()
    {
      
        float randomValue = Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var rate in dropRates)
        {
            cumulativeProbability += rate.dropPercentage;

            if (randomValue < cumulativeProbability)
            {
                return rate.rarity;
            }
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

        int randomIndex = Random.Range(0, filteredTroops.Count);
        return filteredTroops[randomIndex];
    }

    private void SpawnTroop(TroopData troop)
    {
        if (troop.prefab == null || playerSpawnPoint == null)
        {
            Debug.LogError($"[Gacha] Cannot spawn! Prefab or Spawn Point is missing.");
            return;
        }

        GameObject newUnit = Instantiate(
            troop.prefab,
            playerSpawnPoint.position,
            Quaternion.identity
        );

        Troops unitScript = newUnit.GetComponent<Troops>();
        
        if (unitScript != null) 
        {

            unitScript.maxHealth = troop.maxHealth;
            unitScript.currentHealth = troop.maxHealth;
            unitScript.attackPoints = troop.attack;
            unitScript.moveSpeed = troop.moveSpeed;
            unitScript.attackSpeed = troop.attackInterval; 
        }
        else
        {
             Debug.LogError($"[Gacha] Instantiated prefab '{troop.displayName}' is missing the 'Troops' script.");
        }
    }

    private void ValidateDropRates()
    {
        float totalPercentage = dropRates.Sum(rate => rate.dropPercentage);
        if (Mathf.Abs(totalPercentage - 100f) > 0.01f)
        {
            Debug.LogWarning($"[GachaManager] Drop rates DO NOT sum to 100%! Total: {totalPercentage}. Please adjust the values in the Inspector.");
        }
    }
}