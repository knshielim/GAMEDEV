using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    // Singleton biar gampang diakses dari Tower dan script lain
    public static CoinManager Instance { get; private set; }

    [Header("Player Coins")]
    public int playerCoins = 0;
    public TextMeshProUGUI playerCoinText;   // UI buat player 

    [Header("AI Coins (no UI)")]
    public int enemyCoins = 0;               

    private void Awake()
    {
        // Setup singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdatePlayerUI();
        AddPlayerCoins(99999);
    }

    // player coin
    public void AddPlayerCoins(int amount)
    {
        if (amount <= 0) return;

        playerCoins += amount;

        // Debug di console
        // Debug.Log($"[PLAYER COIN] +{amount} → total: {playerCoins}");

        UpdatePlayerUI();
    }

    public bool TrySpendPlayerCoins(int cost)
    {
        if (playerCoins < cost)
        {
            Debug.Log($"[PLAYER COIN] NOT ENOUGH (need {cost}, has {playerCoins})");
            return false;
        }

        playerCoins -= cost;
        // Debug.Log($"[PLAYER COIN] SPEND {cost} → remaining: {playerCoins}");
        UpdatePlayerUI();
        return true;
    }

    private void UpdatePlayerUI()
    {
        if (playerCoinText != null)
        {
            playerCoinText.text = "Coins: " + playerCoins;
        }
    }

    // ai coin
    public void AddEnemyCoins(int amount)
    {
        if (amount <= 0) return;

        enemyCoins += amount;

        // buat liat cara kerja AI
        // Debug.Log($"[AI COIN] +{amount} → total: {enemyCoins}");
    }

    public bool TrySpendEnemyCoins(int cost)
    {
        if (enemyCoins < cost)
        {
            Debug.Log($"[AI COIN] NOT ENOUGH (need {cost}, has {enemyCoins})");
            return false;
        }

        enemyCoins -= cost;
        // Debug.Log($"[AI COIN] SPEND {cost} → remaining: {enemyCoins}");
        return true;
    }

    public void ResetCoins()
    {
        playerCoins = 0;
        enemyCoins = 0;
        UpdatePlayerUI();
        Debug.Log("[COIN] Reset coins for new game/level");
    }
}
