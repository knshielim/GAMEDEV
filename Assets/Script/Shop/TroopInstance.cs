using UnityEngine;

public class TroopInstance
{
    public TroopData data; 
    public int level;   
    public int currentHealth;
    public int currentAttack;
    public float currentMoveSpeed;

    // Increment values per level (index = level - 2, since level 1 has no increment)
    private readonly int[] statIncrements = { 2, 3, 4, 5 }; // For HP & Attack
    private readonly float moveSpeedIncrementPerLevel = 0.2f; // changed

    public TroopInstance(TroopData baseData)
    {
        data = baseData;
        // Load saved level or default to base level
        level = PlayerPrefs.GetInt("troop_" + data.id, baseData.level);

        ApplyLevelStats();
    }

    public void LevelUp()
    {
        if (level >= 5) return;

        level++;

        // Save the new level
        PlayerPrefs.SetInt("troop_" + data.id, level);
        PlayerPrefs.Save();

        ApplyLevelStats();
    }

    public void ResetLevel()
    {
        level = 1;

        PlayerPrefs.SetInt("troop_" + data.id, level);
        PlayerPrefs.Save();

        ApplyLevelStats();
    }

    private void ApplyLevelStats()
    {
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
    }
}
