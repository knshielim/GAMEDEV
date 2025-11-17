using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CoinManager : MonoBehaviour
{
    [Header("Coin Settings")]
    public int playerCoins = 0;
    public int coinsPerTick = 1;
    public float coinInterval = 1f; // 1 coin per second

    private float coinTimer;

    [Header("UI")]
    public TextMeshProUGUI coinText; 

    private void Start()
    {
        coinTimer = 0f;
        UpdateCoinUI();
    }

    private void Update()
    {
        // coin ga nampah pas udah end
        if (GameManager.Instance != null && GameManager.Instance.enabled == false)
            return;

        coinTimer += Time.deltaTime;

        if (coinTimer >= coinInterval)
        {
            coinTimer -= coinInterval;
            AddCoins(coinsPerTick);
        }
    }

    public void AddCoins(int amount)
    {
        playerCoins += amount;
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = "Coins: " + playerCoins.ToString();
        }
    }
}
