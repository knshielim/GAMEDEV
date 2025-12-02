using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyDeployManager : MonoBehaviour
{
     public static bool tutorialActive = false;
    [Header("Tower Reference")]
    [SerializeField] private Tower playerTower;

    [Header("Spawn Settings")]
    [Tooltip("Where enemy troops will appear (usually in front of the enemy tower).")]
    public Transform enemySpawnPoint;

    [Tooltip("List of enemy troop data the AI can spawn.")]
    public List<TroopData> availableEnemyTroops = new List<TroopData>();

    [Header("Base Difficulty Settings")]
    [Tooltip("Base seconds between spawn attempts (will be reduced for harder levels).")]
    public float baseSpawnInterval = 3f;

    [Tooltip("Base coin cost per spawned troop.")]
    public int baseTroopCost = 50;

    [Header("Mythic Deployment")]
    [Tooltip("Mythic troops that can be deployed at higher difficulties.")]
    public List<TroopData> mythicTroops = new List<TroopData>();

    [Tooltip("Time between mythic spawns (in seconds).")]
    public float mythicSpawnInterval = 60f;


    //for testing only
    [Header("Testing Single Troop Spawn")]
    [SerializeField] private bool debugUseSingleTroop = false;
    [SerializeField] private TroopData debugSingleTroop;
    [SerializeField] private bool debugSpawnOnlyOnce = false;
    private bool debugHasSpawned = false;
    // ---------------------


    // Internal state
    private float _spawnTimer;
    private float _mythicTimer;
    private int currentLevel;
    private float currentSpawnInterval;
    private int currentTroopCost;
    
    // Level-specific configurations
    private struct LevelConfig
    {
        public int startingCoins;
        public float coinGenerationMultiplier;
        public float spawnIntervalMultiplier;
        public Dictionary<TroopRarity, float> rarityWeights;
        public bool canDeployMythic;
        public float mythicChance; // % chance to deploy mythic when available
    }

    private Dictionary<int, LevelConfig> levelConfigs;

    public static EnemyDeployManager Instance { get; private set; }

    private void Awake()
    {
        InitializeLevelConfigs();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            currentLevel = (int)GameManager.Instance.currentLevel;
        }
        else
        {
            currentLevel = 1;
            Debug.LogWarning("[EnemyDeploy] GameManager not found, defaulting to Level 1");
        }

        ApplyLevelSettings();
        _mythicTimer = mythicSpawnInterval;

    }

    private void InitializeLevelConfigs()
    {
        levelConfigs = new Dictionary<int, LevelConfig>();

        // LEVEL 1 - EASY (Tutorial-like)
        levelConfigs[1] = new LevelConfig
        {
            startingCoins = 200,
            coinGenerationMultiplier = 1.0f,
            spawnIntervalMultiplier = 1.0f,
            rarityWeights = new Dictionary<TroopRarity, float>
            {
                { TroopRarity.Common, 85f },    // 85% Common
                { TroopRarity.Rare, 15f },      // 15% Rare
                { TroopRarity.Epic, 0f },       // 0% Epic
                { TroopRarity.Legendary, 0f },  // 0% Legendary
                { TroopRarity.Mythic, 0f }      // 0% Mythic
            },
            canDeployMythic = false,
            mythicChance = 0f
        };

        // LEVEL 2 - MEDIUM (Balanced Challenge)
        levelConfigs[2] = new LevelConfig
        {
            startingCoins = 400,
            coinGenerationMultiplier = 1.5f,
            spawnIntervalMultiplier = 0.85f, // Spawns 15% faster
            rarityWeights = new Dictionary<TroopRarity, float>
            {
                { TroopRarity.Common, 50f },    // 50% Common
                { TroopRarity.Rare, 35f },      // 35% Rare
                { TroopRarity.Epic, 15f },      // 15% Epic
                { TroopRarity.Legendary, 0f },  // 0% Legendary
                { TroopRarity.Mythic, 0f }      // 0% Mythic
            },
            canDeployMythic = false,
            mythicChance = 0f
        };

        // LEVEL 3 - HARD (Final Boss)
        levelConfigs[3] = new LevelConfig
        {
            startingCoins = 600,
            coinGenerationMultiplier = 2.0f,
            spawnIntervalMultiplier = 0.7f, // Spawns 30% faster
            rarityWeights = new Dictionary<TroopRarity, float>
            {
                { TroopRarity.Common, 25f },    // 25% Common
                { TroopRarity.Rare, 35f },      // 35% Rare
                { TroopRarity.Epic, 30f },      // 30% Epic
                { TroopRarity.Legendary, 10f }, // 10% Legendary
                { TroopRarity.Mythic, 0f }      // 0% Mythic (deployed separately)
            },
            canDeployMythic = true,
            mythicChance = 100f // Always deploy mythic when timer triggers
        };
    }

    private void ApplyLevelSettings()
    {
        if (!levelConfigs.ContainsKey(currentLevel))
        {
            Debug.LogWarning($"[EnemyDeploy] No config found for level {currentLevel}, using default");
            currentLevel = 1;
        }

        LevelConfig config = levelConfigs[currentLevel];

        // Apply starting coins to AI
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddEnemyCoins(config.startingCoins);
            Debug.Log($"[EnemyDeploy] Level {currentLevel}: AI starts with {config.startingCoins} coins");
        }

        // Apply spawn interval
        currentSpawnInterval = baseSpawnInterval * config.spawnIntervalMultiplier;
        currentTroopCost = baseTroopCost;

        // Boost AI tower coin generation
        Tower aiTower = GameObject.FindObjectsOfType<Tower>()
            .FirstOrDefault(t => t.owner == Tower.TowerOwner.Enemy);

        if (aiTower != null)
        {
            // Access the private field using reflection or make it public
            // For simplicity, we'll add coins periodically instead
            Debug.Log($"[EnemyDeploy] Level {currentLevel}: AI coin generation x{config.coinGenerationMultiplier}");
        }

        Debug.Log($"[EnemyDeploy] Level {currentLevel} configured: " +
                  $"Spawn Interval={currentSpawnInterval:F2}s, " +
                  $"Mythic Enabled={config.canDeployMythic}");
    }

    private void Update()
    {
            // STOP ENEMY SPAWNING DURING TUTORIAL
         if (tutorialActive)
             return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (enemySpawnPoint == null || CoinManager.Instance == null || availableEnemyTroops.Count == 0)
            return;

        // 🔹 DEBUG: want to spawn just 1 troop for testing
        if (debugUseSingleTroop && debugSpawnOnlyOnce)
        {
            if (!debugHasSpawned)
            {
                TrySpawnEnemy();      
                debugHasSpawned = true;
            }
            return; // stop here, to not run normal AI
        }
        
        // --- AI normal ---
        // Regular spawn timer
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= currentSpawnInterval)
        {
            _spawnTimer = 0f;
            TrySpawnEnemy();
        }

        // Mythic spawn timer (only for levels that support it)
        LevelConfig config = levelConfigs[currentLevel];
        if (config.canDeployMythic && mythicTroops.Count > 0)
        {
            _mythicTimer += Time.deltaTime;
            if (_mythicTimer >= mythicSpawnInterval)
            {
                _mythicTimer = 0f;
                
                // Check if we should deploy mythic (based on chance)
                float roll = Random.Range(0f, 100f);
                if (roll < config.mythicChance)
                {
                    TrySpawnMythic();
                }
            }
        }

        // Boost AI coins periodically based on level multiplier
        BoostAICoins();
    }

    private float _coinBoostTimer = 0f;
    private const float COIN_BOOST_INTERVAL = 2f;

    private void BoostAICoins()
    {
        _coinBoostTimer += Time.deltaTime;
        if (_coinBoostTimer >= COIN_BOOST_INTERVAL)
        {
            _coinBoostTimer = 0f;

            LevelConfig config = levelConfigs[currentLevel];
            int boostAmount = Mathf.RoundToInt(10 * config.coinGenerationMultiplier);

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddEnemyCoins(boostAmount);
            }
        }
    }

    private void TrySpawnEnemy()
    {
        // Check if AI has enough coins
        if (!CoinManager.Instance.TrySpendEnemyCoins(currentTroopCost))
        {
            return;
        }

        TroopData selectedTroop = null;

        // 🔹 MODE TESTING: use 1 troop only
        if (debugUseSingleTroop && debugSingleTroop != null)
        {
            selectedTroop = debugSingleTroop;
        }
        else
        {
            // Select troop based on rarity weights for current level
            selectedTroop = SelectTroopByRarity();
        }

        // Select troop based on rarity weights for current level
        // TroopData selectedTroop = SelectTroopByRarity();

        if (selectedTroop == null)
        {
            Debug.LogWarning("[EnemyDeploy] No valid troop selected!");
            CoinManager.Instance.AddEnemyCoins(currentTroopCost); // Refund
            return;
        }

        SpawnTroop(selectedTroop);
    }

    private void TrySpawnMythic()
    {
        if (mythicTroops.Count == 0)
            return;

        // Mythic units cost more
        int mythicCost = currentTroopCost * 5;

        if (!CoinManager.Instance.TrySpendEnemyCoins(mythicCost))
        {
            Debug.Log($"[EnemyDeploy] Not enough coins for Mythic (need {mythicCost})");
            return;
        }

        // Pick random mythic
        TroopData mythicTroop = mythicTroops[Random.Range(0, mythicTroops.Count)];
        SpawnTroop(mythicTroop, true);
    }

    private TroopData SelectTroopByRarity()
    {
        LevelConfig config = levelConfigs[currentLevel];

        // Get troops grouped by rarity
        var troopsByRarity = availableEnemyTroops
            .Where(t => t != null)
            .GroupBy(t => t.rarity)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Calculate total weight
        float totalWeight = config.rarityWeights.Values.Sum();
        float roll = Random.Range(0f, totalWeight);

        // Select rarity based on weights
        float cumulative = 0f;
        TroopRarity selectedRarity = TroopRarity.Common;

        foreach (var kvp in config.rarityWeights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative)
            {
                selectedRarity = kvp.Key;
                break;
            }
        }

        // Get troops of selected rarity
        if (troopsByRarity.ContainsKey(selectedRarity) && troopsByRarity[selectedRarity].Count > 0)
        {
            List<TroopData> candidates = troopsByRarity[selectedRarity];
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Fallback to any available troop
        Debug.LogWarning($"[EnemyDeploy] No troops of rarity {selectedRarity}, using fallback");
        return availableEnemyTroops[Random.Range(0, availableEnemyTroops.Count)];
    }

    private void SpawnTroop(TroopData data, bool isMythic = false)
    {
        if (data == null || (data.enemyPrefab == null && data.playerPrefab == null))
        {
            Debug.LogWarning("[EnemyDeploy] Invalid troop data or missing prefabs.");
            return;
        }

        GameObject prefabToUse = data.enemyPrefab != null ? data.enemyPrefab : data.playerPrefab;
        GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoint.position, Quaternion.identity);

        Enemy enemyUnit = enemyObj.GetComponent<Enemy>();
        if (enemyUnit != null)
        {
            enemyUnit.SetTroopData(data);
            enemyUnit.SetTargetTower(playerTower);

            string mythicTag = isMythic ? " 🌟 MYTHIC 🌟" : "";
            Debug.Log($"[EnemyDeploy] Level {currentLevel} spawned {data.displayName} ({data.rarity}){mythicTag}");
        }
        else
        {
            Debug.LogWarning($"[EnemyDeploy] Spawned '{data.displayName}' but no Enemy component found.");
        }
    }

    
    public void SpawnSpecificEnemy(TroopData troop)
    {
    if (tutorialActive) return; // prevent random AI from spawning

    if (troop == null || enemySpawnPoint == null) return;

    GameObject prefab = troop.enemyPrefab;
    if (prefab == null)
    {
        Debug.LogWarning($"[EnemyDeployManager] TroopData '{troop.displayName}' has no prefab.");
        return;
    }

    GameObject enemyObj = Instantiate(prefab, enemySpawnPoint.position, Quaternion.identity);
    Enemy enemyUnit = enemyObj.GetComponent<Enemy>();
    if (enemyUnit != null)
    {
        enemyUnit.SetTroopData(troop);

        // 🔥 FIX: Always use the correct PLAYER tower (assigned in Inspector)
        if (playerTower != null)
            enemyUnit.SetTargetTower(playerTower);
        else
            Debug.LogError("[EnemyDeployManager] Player tower reference is missing!");
    }
    }



    // Public method to get current difficulty info (useful for UI)
    public string GetDifficultyInfo()
    {
        if (!levelConfigs.ContainsKey(currentLevel))
            return "Unknown Level";

        LevelConfig config = levelConfigs[currentLevel];
        string mythicStatus = config.canDeployMythic ? "YES" : "NO";

        return $"Level {currentLevel}\n" +
               $"Spawn Speed: {(1f / config.spawnIntervalMultiplier):F1}x\n" +
               $"AI Coins: +{config.coinGenerationMultiplier:F1}x\n" +
               $"Mythic Units: {mythicStatus}";
    }
}