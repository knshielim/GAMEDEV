using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnRecord
{
    public TroopRarity rarity;
    public float timestamp;

    public SpawnRecord(TroopRarity rarity, float timestamp)
    {
        this.rarity = rarity;
        this.timestamp = timestamp;
    }
}

public class SpawnRateBalancer : MonoBehaviour
{
    public static SpawnRateBalancer Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance first
                _instance = FindObjectOfType<SpawnRateBalancer>();

                // If not found, create new one
                if (_instance == null)
                {
                    GameObject balancerObj = new GameObject("SpawnRateBalancer");
                    _instance = balancerObj.AddComponent<SpawnRateBalancer>();
                    DontDestroyOnLoad(balancerObj); // Make it persistent
                    Debug.Log("[SpawnRateBalancer] ✅ Created new persistent instance");
                }
                else
                {
                    Debug.Log("[SpawnRateBalancer] Found existing instance");
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    private static SpawnRateBalancer _instance;

    [Header("Balancing Thresholds")]
    [Tooltip("Number of units of same rarity in last 10 draws to trigger balancing")]
    [SerializeField] private int drawBalancingThreshold = 5;

    [Tooltip("How many recent draws to check for balancing")]
    [SerializeField] private int drawBalancingWindow = 10;

    [Tooltip("Percentage adjustment for draw-based balancing")]
    [SerializeField] private float drawBalancingAdjustment = 5f;

    [Tooltip("Number of units in 60s to trigger time-based balancing")]
    [SerializeField] private int balancingThreshold = 8;

    [Tooltip("Time window in seconds for frequency balancing")]
    [SerializeField] private float balancingWindow = 60f;

    [Tooltip("Percentage adjustment for time-based balancing")]
    [SerializeField] private float balancingAdjustment = 1f;

    [Header("Base Drop Rates")]
    [SerializeField] private float baseCommonRate = 78f;
    [SerializeField] private float baseRareRate = 16f;
    [SerializeField] private float baseEpicRate = 4f; // Balanced rate - obtainable but not easy
    [SerializeField] private float baseLegendaryRate = 2f; // Slightly increased for better accessibility
    [SerializeField] private float baseMythicRate = 0f;
    [SerializeField] private float baseBossRate = 0f; // Boss units are spawned specially, not through gacha

    [Header("Upgrade System")]
    [SerializeField, Range(0, 10)] private int upgradeLevel = 0;

    [Header("Reactive Balancing")]
    [Tooltip("Number of summons the reactive boost lasts")]
    [SerializeField] private int reactiveBoostDuration = 5;

    [Tooltip("Rate boost when enemy spawns high rarity")]
    [SerializeField] private float reactiveBoostAmount = 10f;

    [Tooltip("Rate reduction for Common/Rare during reactive boost")]
    [SerializeField] private float reactivePenaltyAmount = 5f;

    // Track spawn history
    private List<SpawnRecord> spawnHistory = new List<SpawnRecord>();

    // Track recent draws for draw-based balancing
    private List<TroopRarity> recentDraws = new List<TroopRarity>();

    // Current adjusted rates
    private Dictionary<TroopRarity, float> currentRates = new Dictionary<TroopRarity, float>();

    // Reactive balancing system
    private bool isReactiveBoostActive = false;
    private TroopRarity boostedRarity = TroopRarity.Common;
    private int remainingReactiveSummons = 0;

    // Rarity order for cascading adjustments
    private TroopRarity[] rarityOrder = {
        TroopRarity.Common,
        TroopRarity.Rare,
        TroopRarity.Epic,
        TroopRarity.Legendary,
        TroopRarity.Mythic,
        TroopRarity.Boss
    };

    private void Awake()
    {
        // Initialize rates when the component starts
        InitializeRates();
    }

    private void InitializeRates()
    {
        // Start with base rates
        currentRates[TroopRarity.Common] = baseCommonRate;
        currentRates[TroopRarity.Rare] = baseRareRate;
        currentRates[TroopRarity.Epic] = baseEpicRate;
        currentRates[TroopRarity.Legendary] = baseLegendaryRate;
        currentRates[TroopRarity.Mythic] = baseMythicRate;
        currentRates[TroopRarity.Boss] = baseBossRate;

        // Apply upgrade modifiers
        ApplyUpgradeModifiers();
    }

    public void RecordSpawn(TroopRarity rarity)
    {
        Debug.Log($"[SpawnRateBalancer] RecordSpawn called for {rarity}");

        float currentTime = Time.time;
        spawnHistory.Add(new SpawnRecord(rarity, currentTime));

        // Add to recent draws for draw-based balancing
        recentDraws.Add(rarity);
        if (recentDraws.Count > drawBalancingWindow)
        {
            recentDraws.RemoveAt(0); // Remove oldest draw
        }

        // Clean old records outside the balancing window
        CleanOldRecords(currentTime);

        // Apply draw-based balancing adjustments (primary)
        ApplyDrawBalancingAdjustments();

        // Apply time-based balancing adjustments (secondary)
        ApplyBalancingAdjustments(currentTime);

        // Apply reactive boost modifiers if active
        ApplyReactiveModifiers();

        Debug.Log($"[SpawnRateBalancer] Recorded {rarity} spawn. Current rates: " +
                 $"Common:{currentRates[TroopRarity.Common]:F1}% " +
                 $"Rare:{currentRates[TroopRarity.Rare]:F1}% " +
                 $"Epic:{currentRates[TroopRarity.Epic]:F1}% " +
                 $"Legendary:{currentRates[TroopRarity.Legendary]:F1}% " +
                 $"Mythic:{currentRates[TroopRarity.Mythic]:F1}%");
    }

    private void CleanOldRecords(float currentTime)
    {
        spawnHistory.RemoveAll(record => currentTime - record.timestamp > balancingWindow);
    }

    private void ApplyDrawBalancingAdjustments()
    {
        // Reset to base rates first (includes upgrades)
        InitializeRates();

        if (recentDraws.Count < drawBalancingWindow) return; // Need full window

        // Count occurrences of each rarity in recent draws
        Dictionary<TroopRarity, int> drawCounts = new Dictionary<TroopRarity, int>();
        foreach (var rarity in rarityOrder)
        {
            drawCounts[rarity] = 0;
        }

        foreach (var draw in recentDraws)
        {
            drawCounts[draw]++;
        }

        // Check for rarities that exceed the threshold
        bool drawBalancingTriggered = false;
        for (int i = 0; i < rarityOrder.Length - 1; i++) // Don't adjust Mythic (last in array)
        {
            TroopRarity currentRarity = rarityOrder[i];

            if (drawCounts[currentRarity] >= drawBalancingThreshold)
            {
                TroopRarity nextRarity = rarityOrder[i + 1];
                drawBalancingTriggered = true;

                // Apply draw-based adjustments
                currentRates[currentRarity] = Mathf.Max(0f, currentRates[currentRarity] - drawBalancingAdjustment);
                currentRates[nextRarity] = Mathf.Min(100f, currentRates[nextRarity] + drawBalancingAdjustment);

                Debug.Log($"[SpawnRateBalancer] 📊 DRAW BALANCING: {currentRarity} appeared {drawCounts[currentRarity]}/{drawBalancingThreshold} times in last {drawBalancingWindow} draws!");
                Debug.Log($"[SpawnRateBalancer] 📈 Adjusted: {currentRarity} -{drawBalancingAdjustment}%, {nextRarity} +{drawBalancingAdjustment}%");
            }
        }

        if (!drawBalancingTriggered && recentDraws.Count >= drawBalancingWindow)
        {
            Debug.Log($"[SpawnRateBalancer] ✅ DRAW BALANCING: All rarities within limits (checked last {drawBalancingWindow} draws)");
        }

        // Normalize after draw balancing
        NormalizeRates();
    }

    private void ApplyUpgradeModifiers()
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

        if (modifiers.TryGetValue(upgradeLevel, out var m))
        {
            currentRates[TroopRarity.Common] += m.common;
            currentRates[TroopRarity.Rare] += m.rare;
            currentRates[TroopRarity.Epic] += m.epic;
            currentRates[TroopRarity.Legendary] += m.legendary;
            currentRates[TroopRarity.Mythic] += m.mythic;

            Debug.Log($"[SpawnRateBalancer] Applied upgrade level {upgradeLevel}: " +
                     $"Common: {m.common:+0.0;-0.0}, Rare: {m.rare:+0.0;-0.0}, " +
                     $"Epic: {m.epic:+0.0;-0.0}, Legendary: {m.legendary:+0.0;-0.0}, " +
                     $"Mythic: {m.mythic:+0.0;-0.0}");
        }

        // Ensure rates don't go below 0 or above 100
        foreach (var rarity in rarityOrder)
        {
            currentRates[rarity] = Mathf.Clamp(currentRates[rarity], 0f, 100f);
        }
    }

    private void ApplyBalancingAdjustments(float currentTime)
    {
        // Reset to base rates first (includes upgrades)
        InitializeRates();

        // Count spawns in the current window for each rarity
        Dictionary<TroopRarity, int> recentCounts = new Dictionary<TroopRarity, int>();
        foreach (var rarity in rarityOrder)
        {
            recentCounts[rarity] = 0;
        }

        foreach (var record in spawnHistory)
        {
            if (currentTime - record.timestamp <= balancingWindow)
            {
                recentCounts[record.rarity]++;
            }
        }

        // Debug: Show current spawn counts
        Debug.Log($"[SpawnRateBalancer] 📊 Recent spawns in last {balancingWindow}s: " +
                 $"Common:{recentCounts[TroopRarity.Common]} " +
                 $"Rare:{recentCounts[TroopRarity.Rare]} " +
                 $"Epic:{recentCounts[TroopRarity.Epic]} " +
                 $"Legendary:{recentCounts[TroopRarity.Legendary]} " +
                 $"Mythic:{recentCounts[TroopRarity.Mythic]}");

        bool anyBalancingTriggered = false;

        // Apply cascading adjustments for each rarity that exceeds threshold
        for (int i = 0; i < rarityOrder.Length - 1; i++) // Don't adjust Mythic (last in array)
        {
            TroopRarity currentRarity = rarityOrder[i];
            TroopRarity nextRarity = rarityOrder[i + 1];

            if (recentCounts[currentRarity] >= balancingThreshold)
            {
                anyBalancingTriggered = true;
                // Reduce current rarity
                currentRates[currentRarity] = Mathf.Max(0f, currentRates[currentRarity] - balancingAdjustment);

                // Increase next rarity
                currentRates[nextRarity] = Mathf.Min(100f, currentRates[nextRarity] + balancingAdjustment);

                Debug.Log($"[SpawnRateBalancer] ⚡ BALANCING ACTIVATED: {currentRarity} threshold exceeded ({recentCounts[currentRarity]}/{balancingThreshold}). " +
                         $"Reduced {currentRarity} by {balancingAdjustment}%, increased {nextRarity} by {balancingAdjustment}%.");
            }
        }

        // Summary of balancing state
        if (!anyBalancingTriggered)
        {
            Debug.Log($"[SpawnRateBalancer] ✅ NO BALANCING TRIGGERED - Using base rates (no rarity exceeded {balancingThreshold} spawns in {balancingWindow}s)");
        }
        else
        {
            Debug.Log($"[SpawnRateBalancer] 🔄 BALANCING APPLIED - Rates adjusted based on recent spawn patterns");
        }

        // Normalize rates to ensure they sum to 100%
        NormalizeRates();
    }

    private void ApplyReactiveModifiers()
    {
        if (!isReactiveBoostActive) return;

        // Store original rates for comparison
        float originalBoostedRate = currentRates[boostedRarity];
        float originalCommonRate = currentRates[TroopRarity.Common];
        float originalRareRate = currentRates[TroopRarity.Rare];

        // Apply boost to the target rarity
        currentRates[boostedRarity] = Mathf.Min(100f, currentRates[boostedRarity] + reactiveBoostAmount);

        // Apply penalties to Common and Rare
        currentRates[TroopRarity.Common] = Mathf.Max(0f, currentRates[TroopRarity.Common] - reactivePenaltyAmount);
        currentRates[TroopRarity.Rare] = Mathf.Max(0f, currentRates[TroopRarity.Rare] - reactivePenaltyAmount);

        // Decrement counter
        remainingReactiveSummons--;

        Debug.Log($"[SpawnRateBalancer] 🎯 APPLYING REACTIVE MODIFIERS:");
        Debug.Log($"[SpawnRateBalancer] 📊 Before: {boostedRarity} {originalBoostedRate:F1}%, Common {originalCommonRate:F1}%, Rare {originalRareRate:F1}%");
        Debug.Log($"[SpawnRateBalancer] ⚡ After:  {boostedRarity} {currentRates[boostedRarity]:F1}% (+{reactiveBoostAmount}%), Common {currentRates[TroopRarity.Common]:F1}% (-{reactivePenaltyAmount}%), Rare {currentRates[TroopRarity.Rare]:F1}% (-{reactivePenaltyAmount}%)");

        if (remainingReactiveSummons <= 0)
        {
            isReactiveBoostActive = false;
            Debug.Log($"[SpawnRateBalancer] 🏁 REACTIVE BOOST COMPLETE! All {reactiveBoostDuration} summons used.");
            Debug.Log($"[SpawnRateBalancer] 🔄 Rates reverted to normal balancing.");
        }
        else
        {
            Debug.Log($"[SpawnRateBalancer] ⏳ Reactive boost: {remainingReactiveSummons}/{reactiveBoostDuration} summons remaining");
            Debug.Log($"[SpawnRateBalancer] 🎲 Next summon will have boosted {boostedRarity} rates!");
        }

        // Renormalize after reactive modifiers
        NormalizeRates();
    }

    private void NormalizeRates()
    {
        float total = 0f;
        foreach (var rate in currentRates.Values)
        {
            total += rate;
        }

        if (total <= 0f) return;

        foreach (var rarity in rarityOrder)
        {
            currentRates[rarity] = (currentRates[rarity] / total) * 100f;
        }
    }

    public Dictionary<TroopRarity, float> GetCurrentRates()
    {
        return new Dictionary<TroopRarity, float>(currentRates);
    }

    public float GetRateForRarity(TroopRarity rarity)
    {
        return currentRates.ContainsKey(rarity) ? currentRates[rarity] : 0f;
    }

    public string GetBalancingInfo()
    {
        float currentTime = Time.time;
        CleanOldRecords(currentTime);

        Dictionary<TroopRarity, int> recentCounts = new Dictionary<TroopRarity, int>();
        foreach (var rarity in rarityOrder)
        {
            recentCounts[rarity] = 0;
        }

        foreach (var record in spawnHistory)
        {
            if (currentTime - record.timestamp <= balancingWindow)
            {
                recentCounts[record.rarity]++;
            }
        }

        string info = $"🎯 DRAW-BASED BALANCING (5+ of same rarity in last 10 draws):\n";
        info += $"• Common (5+) → Common -5%, Rare +5% (temporary)\n";
        info += $"• Rare (5+) → Rare -5%, Epic +5% (temporary)\n";
        info += $"• Epic (5+) → Epic -5%, Legendary +5% (temporary)\n";
        info += $"• Legendary (5+) → Legendary -5%, Mythic +5% (temporary)\n\n";
        info += $"🎯 TIME-BASED BALANCING (8+ spawns in 60s - secondary):\n";
        info += $"• Same logic as draw-based but gentler (-1%/+1%)\n\n";

        info += $"🎯 REACTIVE BALANCING (High-rarity spawns trigger counter-boosts):\n";
        info += $"• When enemy spawns Epic/Legendary → Player gets +{reactiveBoostAmount}% to that rarity\n";
        info += $"• Common/Rare each -{reactivePenaltyAmount}% for next {reactiveBoostDuration} summons\n";
        info += $"• Vice versa: Player high-rarity spawns boost enemy rates\n\n";

        // Show recent draws for draw-based balancing
        info += $"Recent draws (last {drawBalancingWindow} summons):\n";
        Dictionary<TroopRarity, int> drawCounts = new Dictionary<TroopRarity, int>();
        foreach (var rarity in rarityOrder)
        {
            drawCounts[rarity] = 0;
        }
        foreach (var draw in recentDraws)
        {
            drawCounts[draw]++;
        }
        foreach (var rarity in rarityOrder)
        {
            string drawStatus = drawCounts[rarity] >= drawBalancingThreshold ? "⚠️ DRAW BALANCE ACTIVE" : "✓ OK";
            info += $"{rarity}: {drawCounts[rarity]}/{drawBalancingThreshold} {drawStatus}\n";
        }
        info += $"\nRecent time spawns (last {balancingWindow}s):\n";
        foreach (var rarity in rarityOrder)
        {
            string timeStatus = recentCounts[rarity] >= balancingThreshold ? "⚠️ TIME BALANCE ACTIVE" : "✓ OK";
            info += $"{rarity}: {recentCounts[rarity]}/{balancingThreshold} {timeStatus}\n";
        }

        info += $"\nReactive Boost: ";
        if (isReactiveBoostActive)
        {
            info += $"🎯 ACTIVE - {boostedRarity} +{reactiveBoostAmount}%, Common/Rare -{reactivePenaltyAmount}% ({remainingReactiveSummons} summons left)\n";
        }
        else
        {
            info += $"❌ INACTIVE\n";
        }

        info += $"\nCurrent rates:\n";
        foreach (var rarity in rarityOrder)
        {
            info += $"{rarity}: {currentRates[rarity]:F1}%\n";
        }

        return info;
    }

    public void ResetBalancing()
    {
        spawnHistory.Clear();
        recentDraws.Clear();
        InitializeRates();
        isReactiveBoostActive = false;
        remainingReactiveSummons = 0;
        Debug.Log("[SpawnRateBalancer] Balancing reset to base rates");
    }

    /// <summary>
    /// Resets upgrade level to 0 and clears all balancing data.
    /// Call this when starting a new game or level.
    /// </summary>
    public void ResetUpgrades()
    {
        upgradeLevel = 0;
        ResetBalancing();
        Debug.Log("[SpawnRateBalancer] 🔄 All upgrades and balancing reset for new game/level");
    }

    // Reactive balancing system - called when enemy spawns high-rarity unit
    public void ActivateReactiveBoost(TroopRarity rarity)
    {
        if (rarity != TroopRarity.Epic && rarity != TroopRarity.Legendary && rarity != TroopRarity.Boss) return;

        isReactiveBoostActive = true;
        boostedRarity = rarity;
        remainingReactiveSummons = reactiveBoostDuration;

        Debug.Log($"[SpawnRateBalancer] 🎯 REACTIVE BOOST ACTIVATED!");
        Debug.Log($"[SpawnRateBalancer] 📈 Trigger: High-rarity spawn detected ({rarity})");
        Debug.Log($"[SpawnRateBalancer] ⏱️  Duration: Next {reactiveBoostDuration} summons");
        Debug.Log($"[SpawnRateBalancer] 🎯 Boost: {rarity} rate +{reactiveBoostAmount}%");
        Debug.Log($"[SpawnRateBalancer] 📉 Penalty: Common/Rare rates -{reactivePenaltyAmount}% each");
        Debug.Log($"[SpawnRateBalancer] 💡 Strategy: Summon {rarity} units now to counter!");
    }

    // Debug method to test the draw-based balancing system
    [ContextMenu("Test Draw Balancing (5 Commons in 10 draws)")]
    public void TestDrawBalancing()
    {
        Debug.Log("[SpawnRateBalancer] 🧪 TEST: Simulating 5 Common spawns in 10 draws...");

        // Clear history first
        spawnHistory.Clear();
        recentDraws.Clear();

        // Simulate 10 draws: 5 Common, 3 Rare, 2 Epic
        TroopRarity[] testDraws = {
            TroopRarity.Common, TroopRarity.Rare, TroopRarity.Common,
            TroopRarity.Epic, TroopRarity.Common, TroopRarity.Rare,
            TroopRarity.Common, TroopRarity.Common, TroopRarity.Rare,
            TroopRarity.Epic
        };

        foreach (var draw in testDraws)
        {
            RecordSpawn(draw);
        }

        Debug.Log("[SpawnRateBalancer] 🧪 TEST: 10 draws completed (5 Common, 3 Rare, 2 Epic). Should trigger draw balancing!");
        Debug.Log("[SpawnRateBalancer] 📊 Expected: Common -5%, Rare +5% due to 5+ Commons in 10 draws");
    }

    [ContextMenu("Test Reactive Boost (Epic)")]
    public void TestReactiveBoostEpic()
    {
        Debug.Log("[SpawnRateBalancer] 🧪 TEST MODE: Simulating enemy Epic spawn...");
        Debug.Log("[SpawnRateBalancer] 🎯 This would happen when enemy spawns an Epic unit");
        Debug.Log("[SpawnRateBalancer] 📈 Expected result: Player gets Epic +10%, Common/Rare -5% for 5 summons");

        // Activate reactive boost as if enemy spawned Epic
        ActivateReactiveBoost(TroopRarity.Epic);

        Debug.Log("[SpawnRateBalancer] ✅ TEST COMPLETE: Reactive boost activated!");
        Debug.Log("[SpawnRateBalancer] 🎮 Try summoning units now to see the boosted rates in action!");
    }

    [ContextMenu("Test Reactive Boost (Legendary)")]
    public void TestReactiveBoostLegendary()
    {
        Debug.Log("[SpawnRateBalancer] 🧪 TEST MODE: Simulating enemy Legendary spawn...");
        Debug.Log("[SpawnRateBalancer] 🎯 This would happen when enemy spawns a Legendary unit");
        Debug.Log("[SpawnRateBalancer] 📈 Expected result: Player gets Legendary +10%, Common/Rare -5% for 5 summons");

        // Activate reactive boost as if enemy spawned Legendary
        ActivateReactiveBoost(TroopRarity.Legendary);

        Debug.Log("[SpawnRateBalancer] ✅ TEST COMPLETE: Reactive boost activated!");
        Debug.Log("[SpawnRateBalancer] 🎮 Try summoning units now to see the boosted rates in action!");
    }

    // Upgrade System Methods
    public void UpgradeSummonSystem()
    {
        if (upgradeLevel >= 10) return;

        upgradeLevel++;
        InitializeRates(); // Reapply upgrades
        Debug.Log($"[SpawnRateBalancer] Upgraded to level {upgradeLevel}");
    }

    public void SetUpgradeLevel(int level)
    {
        upgradeLevel = Mathf.Clamp(level, 0, 10);
        InitializeRates(); // Reapply upgrades
        Debug.Log($"[SpawnRateBalancer] Upgrade level set to {upgradeLevel}");
    }

    public int GetUpgradeLevel()
    {
        return upgradeLevel;
    }

    public int GetUpgradeCost()
    {
        if (upgradeLevel >= 10) return int.MaxValue;
        return 20 + (upgradeLevel * 15);
    }
}
