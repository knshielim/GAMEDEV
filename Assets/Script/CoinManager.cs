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
    public int teamCoins = 0;               

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
    }

    // player coin
    public void AddPlayerCoins(int amount)
    {
        if (amount <= 0) return;

        playerCoins += amount;

        // Debug di console
        Debug.Log($"[PLAYER COIN] +{amount} → total: {playerCoins}");

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
        Debug.Log($"[PLAYER COIN] SPEND {cost} → remaining: {playerCoins}");
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
    public void AddTeamCoins(int amount)
    {
        if (amount <= 0) return;

        teamCoins += amount;

        // buat liat cara kerja AI
        Debug.Log($"[AI COIN] +{amount} → total: {teamCoins}");
    }

    public bool TrySpendTeamCoins(int cost)
    {
        if (teamCoins < cost)
        {
            Debug.Log($"[AI COIN] NOT ENOUGH (need {cost}, has {teamCoins})");
            return false;
        }

        teamCoins -= cost;
        Debug.Log($"[AI COIN] SPEND {cost} → remaining: {teamCoins}");
        return true;
    }

    public void ResetCoins()
    {
        playerCoins = 0;
        teamCoins = 0;
        UpdatePlayerUI();
        Debug.Log("[COIN] Reset coins for new game/level");
    }
}
