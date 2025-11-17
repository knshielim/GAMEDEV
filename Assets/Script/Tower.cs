using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public enum TowerOwner { Player, Team }
    public TowerOwner owner;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Coin Generation")]
    public int coinsPerTick = 1;
    public float coinInterval = 1f;
    private float coinTimer = 0f;

    private void Start()
    {
        currentHealth = maxHealth;

        Debug.Log($"[{owner} TOWER] Start HP: {currentHealth}/{maxHealth}");
    }

    private void Update()
    {
        HandleCoinGeneration();
    }

    void HandleCoinGeneration()
    {
        if (CoinManager.Instance == null) return;

        coinTimer += Time.deltaTime;

        if (coinTimer >= coinInterval)
        {
            coinTimer -= coinInterval;

            if (owner == TowerOwner.Player)
            {
                CoinManager.Instance.AddPlayerCoins(coinsPerTick);
            }
            else if (owner == TowerOwner.Team)
            {
                CoinManager.Instance.AddTeamCoins(coinsPerTick);
            }
        }
    }
    
    void OnTowerDestroyed()
    {
        GameManager.Instance.TowerDestroyed(this);
    }
  
    // handle damage
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Clamp biar HP ga turun di bawah 0
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // tau hp tower habis kena damage
        Debug.Log($"[{owner} TOWER] Took {damage} damage → HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log($"[{owner} TOWER] DESTROYED!");
            OnTowerDestroyed();
        }
    }

}
