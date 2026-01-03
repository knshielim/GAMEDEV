using UnityEngine;

public class TroopInstance
{
    public TroopData data; 
    public int level;   
    public int currentHealth;
    public int currentAttack;
    public float currentMoveSpeed;

    public TroopInstance(TroopData baseData)
    {
        data = baseData;
        // Load saved level or default to base level

        if (PersistenceManager.Instance != null)
        {
            level = PersistenceManager.Instance.GetTroopLevel(data.id);
        }
        else
        {
            // Fallback kalau test scene tanpa Manager
            level = baseData.level; 
            // Debug.LogWarning("PersistenceManager tidak ditemukan! Menggunakan level default.");
        }

        ApplyLevelStats();
    }

    public void LevelUp()
    {
        if (level >= 5) return;

        level++;

        // Update stats in this object
        ApplyLevelStats();
        
        // Save to JSON
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SetTroopLevel(data.id, level);
            PersistenceManager.Instance.SaveGame();
        }

    }

    public void ResetLevel()
    {
        level = 1;

        // --- UPDATE STATS ---
        ApplyLevelStats();

        // --- Save to JSON ---
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SetTroopLevel(data.id, level);
            PersistenceManager.Instance.SaveGame();
            // Debug.Log($"[TroopInstance] Reset {data.id} to Level 1 (Saved to JSON)");
        }
    }

    private void ApplyLevelStats()
    {
        var stats = GetStatsForLevel(data, level);
        currentHealth = Mathf.RoundToInt(stats.hp);
        currentAttack = Mathf.RoundToInt(stats.atk);
        currentMoveSpeed = stats.spd;
        /*
        // Start from base stats
        currentHealth = data.maxHealth;
        currentAttack = data.attack;
        currentMoveSpeed = data.moveSpeed;

        // Apply cumulative HP & Attack increments
        for (int i = 2; i <= level; i++)
        {
            int incrementIndex = i - 2; // level 2 -> index 0
            if (incrementIndex < statIncrements.Length)
            {
                currentHealth += statIncrements[incrementIndex];
                currentAttack += statIncrements[incrementIndex];
            }

            // Movement speed increments per level above 1
            currentMoveSpeed += moveSpeedIncrementPerLevel;
        }
        */
    }

    public static (float hp, float atk, float spd) GetStatsForLevel(TroopData data, int lvl)
    {
        // Rumus: Base * (1 + (Level - 1) * 20%)
        // Level 1 = 100% (Base)
        // Level 2 = 120%
        // ...
        // Level 5 = 180%
        float multiplier = 1.0f + ((lvl - 1) * 0.2f); 

        float finalHp = data.maxHealth * multiplier;
        float finalAtk = data.attack * multiplier;
        
        // Speed dikasih bonus dikit aja (5% per level) biar animasi jalan gak aneh
        float finalSpd = data.moveSpeed * (1.0f + ((lvl - 1) * 0.05f));

        return (finalHp, finalAtk, finalSpd);
    }
}
