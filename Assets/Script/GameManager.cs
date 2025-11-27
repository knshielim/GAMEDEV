using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum GameLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4,
    Level5 = 5
}

public class GameManager : MonoBehaviour
{
    // Singleton, supaya bisa diakses dari mana saja lewat GameManager.Instance
    public static GameManager Instance { get; private set; }

    [Header("Game Progress / Difficulty")]
    [Tooltip("1 = easiest, 5 = hardest")]
    public GameLevel currentLevel = GameLevel.Level1;

    [Header("Tower Reference")]
    public Tower playerTower;
    public Tower aiTower;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    //public Text gameOverText; 

    private bool isGameOver = false;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // kalau pake banyak scene
        Debug.Log($"[GAME START] Current Level = {currentLevel}");
    }

    private void Start()
    {
        // Pastikan panel Game Over mati dulu di awal
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Pastikan timeScale normal
        Time.timeScale = 1f;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    // Dipanggil dari Tower:
    // GameManager.Instance.TowerDestroyed(this);
    public void TowerDestroyed(Tower destroyedTower)
    {
        if (isGameOver) return; // Biar nggak ke-trigger dua kali

        isGameOver = true;

        string message = "";

        // Cek tower siapa yang hancur
        if (destroyedTower == playerTower || destroyedTower.owner == Tower.TowerOwner.Player)
        {
            message = "YOU LOSE!";
        }
        else if (destroyedTower == aiTower || destroyedTower.owner == Tower.TowerOwner.Enemy)
        {
            message = "YOU WIN!";
        }
        else
        {
            message = "GAME OVER";
        }

        // Tampilkan UI Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = message;

        // Hentikan game (opsional, tapi enak buat Phase 1)
        Time.timeScale = 0f;
    }


    public void OnSummonButtonClick()
    {
        Debug.Log("[GameManager] OnSummonButtonClick called at " + Time.time);
        if (isGameOver)
        {
            Debug.Log("[GameManager] Cannot summon: Game Over.");
            return;
        }

        if (GachaManager.Instance != null)
        {
            // Manggil Gacha Function
            GachaManager.Instance.SummonTroop();
        }
        else
        {
            Debug.LogError("[GameManager] GachaManager instance not found. Make sure the GachaManager script is active in the scene.");
        }
    }
}

