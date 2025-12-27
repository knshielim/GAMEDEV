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
    public float baseSpawnInterval = 30f;

    [Tooltip("Base coin cost per spawned troop.")]
    public int baseTroopCost = 50;

    [Header("Mythic Deployment")]
    [Tooltip("Mythic troops that can be deployed at higher difficulties.")]
    public List<TroopData> mythicTroops = new List<TroopData>();

    [Tooltip("Time between mythic spawns (in seconds).")]
    public float mythicSpawnInterval = 60f;

    [Header("Level 4 Wave Management")]
    [Tooltip("Enable wave-based spawning for level 4")]
    [SerializeField] private bool enableWaveMode = true;

    [Tooltip("Time when Wave 2 starts (seconds from level start)")]
    [SerializeField] private float wave2StartTime = 60f;

    [Tooltip("Time when Final Wave starts (seconds from level start)")]
    [SerializeField] private float finalWaveStartTime = 60f; // Same as wave2 for now, can be adjusted

    [Tooltip("Spawn interval during Wave 2")]
    [SerializeField] private float wave2SpawnInterval = 20f;

    [Tooltip("Spawn interval during Final Wave")]
    [SerializeField] private float finalWaveSpawnInterval = 15f;

    [Header("Level 5 Boss Trigger")]
    [Tooltip("Enable boss spawning for level 5")]
    [SerializeField] private bool enableBossMode = true;

    [Tooltip("Boss unit to spawn when enemy tower reaches 50% HP")]
    [SerializeField] private TroopData bossTroop;

    [Tooltip("HP percentage threshold to trigger boss spawn (0.5 = 50%)")]
    [SerializeField] private float bossTriggerHPPercent = 0.5f;


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

    // Level 4 Wave Management
    private float _levelStartTime;
    private int _currentWave = 1;
    private bool _wave2Announced = false;
    private bool _finalWaveAnnounced = false;

    // Level 5 Boss Trigger
    private bool _bossSpawned = false;
    
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
        // Ensure singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[EnemyDeploy] Destroying duplicate EnemyDeployManager instance");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[EnemyDeploy] EnemyDeployManager instance created");

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

        // Initialize level timing
        _levelStartTime = Time.time;
        _currentWave = 1;
        _wave2Announced = false;
        _finalWaveAnnounced = false;

        // Initialize boss state
        _bossSpawned = false;

        ApplyLevelSettings();
        _mythicTimer = mythicSpawnInterval;

    }



    // --------------------------------------
    // NEW BALANCED AI SUMMON SYSTEM
    // --------------------------------------
    private TroopRarity GetBalancedRarity()
    {
        int aiRare = CountAIUnits(TroopRarity.Rare);
        int aiEpic = CountAIUnits(TroopRarity.Epic);

        bool playerHasEpic = CountPlayerUnits(TroopRarity.Epic) > 0;
        bool playerHasLegendary = CountPlayerUnits(TroopRarity.Legendary) > 0;

        // 1. If AI already has 3 Rare and player does not have Epic → force Common
        if (aiRare >= 3 && !playerHasEpic && !playerHasLegendary)
        {
            return TroopRarity.Common;
        }

        // 2. If the player has Epic → AI can summon normal Rare/Epic
        if (playerHasEpic)
        {
            return GetRandomRarityByWeights();
        }

        // 3. If the player has Legendary → AI can continue to summon Epic
        if (playerHasLegendary)
        {
            return TroopRarity.Epic;
        }

        // 4. Default: normal behavior
        return GetRandomRarityByWeights();
    }

    private TroopRarity GetRandomRarityByWeights()
    {
        // Use balanced rates from SpawnRateBalancer
        var balancedRates = SpawnRateBalancer.Instance.GetCurrentRates();
        float total = balancedRates.Values.Sum();
        float roll = Random.Range(0f, total);

        float cumulative = 0f;
        foreach (var kv in balancedRates)
        {
            cumulative += kv.Value;
            if (roll < cumulative)
                return kv.Key;
        }

        return TroopRarity.Common;
    }

    private TroopData GetTroopByRarity(TroopRarity rarity)
    {
        var list = availableEnemyTroops.Where(t => t.rarity == rarity).ToList();
        if (list.Count == 0)
            return availableEnemyTroops[Random.Range(0, availableEnemyTroops.Count)];
        return list[Random.Range(0, list.Count)];
    }
    private int CountAIUnits(TroopRarity rarity)
    {
        return Enemy.aliveEnemies.Count(e => e.GetTroopData() != null && e.GetTroopData().rarity == rarity);
    }

    private int CountPlayerUnits(TroopRarity rarity)
    {
        return Troops.aliveTroops.Count(t => t.GetTroopData() != null && t.GetTroopData().rarity == rarity);
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
                { TroopRarity.Common, 75f },    // 75% Common
                { TroopRarity.Rare, 20f },      // 20% Rare
                { TroopRarity.Epic, 5f },       // 5% Epic
                { TroopRarity.Legendary, 0f },  // 0% Legendary
                { TroopRarity.Mythic, 0f },     // 0% Mythic
                { TroopRarity.Boss, 0f }        // 0% Boss (spawned specially)
            },
            canDeployMythic = false,
            mythicChance = 0f
        };

        // LEVEL 2 - MEDIUM (Balanced Challenge)
        levelConfigs[2] = new LevelConfig
        {
            startingCoins = 300,   // awalnya 400
            coinGenerationMultiplier = 1.3f, // awalnya 1.3f
            spawnIntervalMultiplier = 0.8f, // 24 seconds between spawns
            rarityWeights = new Dictionary<TroopRarity, float>
            {

                // yg baru
                { TroopRarity.Common, 60f },
                { TroopRarity.Rare, 28f },
                { TroopRarity.Epic, 12f },
                { TroopRarity.Legendary, 5f },  // coba jadi 2 kalo masih belum balance
                { TroopRarity.Mythic, 0f },
                { TroopRarity.Boss, 0f }      
                /*
                { TroopRarity.Common, 50f },    // 50% Common
                { TroopRarity.Rare, 30f },      // 30% Rare
                { TroopRarity.Epic, 15f },      // 15% Epic
                { TroopRarity.Legendary, 5f },  // 5% Legendary
                { TroopRarity.Mythic, 0f },     // 0% Mythic
                { TroopRarity.Boss, 0f }        // 0% Boss
                */
            },
            canDeployMythic = false,
            mythicChance = 0f
        };

        // LEVEL 3 - HARD (Final Boss)
        levelConfigs[3] = new LevelConfig
        {
            startingCoins = 600,
            coinGenerationMultiplier = 2.0f,
            spawnIntervalMultiplier = 0.6f, // 18 seconds between spawns - challenging
            rarityWeights = new Dictionary<TroopRarity, float>
            {
                { TroopRarity.Common, 25f },    // 25% Common
                { TroopRarity.Rare, 35f },      // 35% Rare
                { TroopRarity.Epic, 30f },      // 30% Epic
                { TroopRarity.Legendary, 10f }, // 10% Legendary
                { TroopRarity.Mythic, 0f },     // 0% Mythic (deployed separately)
                { TroopRarity.Boss, 0f }        // 0% Boss (spawned specially)
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
        Debug.Log($"[EnemyDeploy] ⏱️  Enemy spawn timing: Every {currentSpawnInterval:F1} seconds (base: {baseSpawnInterval:F1}s × {levelConfigs[currentLevel].spawnIntervalMultiplier:F1})");
        Debug.Log($"[EnemyDeploy] 💰 Enemy coin cost per spawn: {currentTroopCost} coins");
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
        // Handle wave transitions for Level 4
        if (currentLevel == 4 && enableWaveMode)
        {
            UpdateWaveState();
        }

        // Handle boss trigger for Level 5
        if (currentLevel == 5 && enableBossMode && !_bossSpawned)
        {
            CheckBossTrigger();
        }

        // Regular spawn timer
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= currentSpawnInterval)
        {
            _spawnTimer = 0f;
            Debug.Log($"[EnemyDeploy] 🕒 SPAWN TIMER TRIGGERED (Level {currentLevel}, Wave {_currentWave})");
            Debug.Log($"[EnemyDeploy] ⏱️  Spawn interval: {currentSpawnInterval:F1} seconds");
            Debug.Log($"[EnemyDeploy] 🎯 Attempting to spawn enemy unit...");
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

    private void UpdateWaveState()
    {
        if (currentLevel != 4 || !enableWaveMode)
            return;

        float elapsedTime = Time.time - _levelStartTime;

        // Transition to Wave 2
        if (_currentWave == 1 && elapsedTime >= wave2StartTime && !_wave2Announced)
        {
            _currentWave = 2;
            currentSpawnInterval = wave2SpawnInterval;
            _wave2Announced = true;

            Debug.Log($"[EnemyDeploy] 🌊 WAVE 2 STARTED! (Level 4, {elapsedTime:F1}s elapsed)");
            Debug.Log($"[EnemyDeploy] ⚡ Increased spawn rate: Every {currentSpawnInterval:F1} seconds");

            // Optional: Show wave announcement UI or play sound
            if (AudioManager.Instance != null && AudioManager.Instance.upgradeSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSFX);
            }
        }

        // Transition to Final Wave
        if (_currentWave == 2 && elapsedTime >= finalWaveStartTime && !_finalWaveAnnounced)
        {
            _currentWave = 3;
            currentSpawnInterval = finalWaveSpawnInterval;
            _finalWaveAnnounced = true;

            Debug.Log($"[EnemyDeploy] 🔥 FINAL WAVE STARTED! (Level 4, {elapsedTime:F1}s elapsed)");
            Debug.Log($"[EnemyDeploy] ⚡ Maximum spawn rate: Every {currentSpawnInterval:F1} seconds");

            // Optional: Show final wave announcement UI or play sound
            if (AudioManager.Instance != null && AudioManager.Instance.upgradeSFX != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSFX);
            }
        }
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
            TroopRarity balanced = GetBalancedRarity();
            selectedTroop = GetTroopByRarity(balanced);
            
            //selectedTroop = SelectTroopByRarity();
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

            // Record spawn for balancing system
            SpawnRateBalancer.Instance.RecordSpawn(data.rarity);

            // Activate reactive boost if enemy spawns high-rarity unit
            if (data.rarity == TroopRarity.Epic || data.rarity == TroopRarity.Legendary)
            {
                Debug.Log($"[EnemyDeploy] 🔔 HIGH-RARITY ENEMY SPAWN DETECTED: {data.displayName} ({data.rarity})");
                Debug.Log($"[EnemyDeploy] 🎯 This will activate reactive balancing for the player!");
                SpawnRateBalancer.Instance.ActivateReactiveBoost(data.rarity);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemyDeploy] Spawned '{data.displayName}' but no Enemy component found.");
        }
    }

    
   public void SpawnSpecificEnemy(TroopData troop, bool ignoreTutorial = false)
{
    if (!ignoreTutorial && tutorialActive) return; // normal AI block

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

    private void CheckBossTrigger()
    {
        if (_bossSpawned || bossTroop == null)
            return;

        // Find the enemy tower
        Tower enemyTower = GameObject.FindObjectsOfType<Tower>()
            .FirstOrDefault(t => t.owner == Tower.TowerOwner.Enemy);

        if (enemyTower == null)
            return;

        // Check if tower HP is at or below the trigger percentage
        float currentHPPercent = (float)enemyTower.currentHealth / enemyTower.maxHealth;

        if (currentHPPercent <= bossTriggerHPPercent)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        if (_bossSpawned || bossTroop == null)
            return;

        _bossSpawned = true;

        Debug.Log($"[EnemyDeploy] 👑 BOSS SPAWN TRIGGERED! (Level 5)");
        Debug.Log($"[EnemyDeploy] 🏰 Enemy tower HP dropped to {(bossTriggerHPPercent * 100):F0}%, spawning boss!");

        // Spawn the boss (bosses are free or cost a lot)
        SpawnTroop(bossTroop, false); // isMythic = false (boss spawns as regular enemy)

        // Optional: Play dramatic music or sound effect
        if (AudioManager.Instance != null && AudioManager.Instance.upgradeSFX != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSFX);
        }

        // Optional: Show boss announcement
        Debug.Log($"[EnemyDeploy] ⚔️ BOSS {bossTroop.displayName} HAS ENTERED THE BATTLE!");
    }
}