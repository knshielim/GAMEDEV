using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    // Singleton, supaya bisa diakses dari mana saja lewat GameManager.Instance
    public static GameManager Instance { get; private set; }

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
        // Pastikan panel Game Over mati dulu di awal
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Pastikan timeScale normal
        Time.timeScale = 1f;
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
        else if (destroyedTower == aiTower || destroyedTower.owner == Tower.TowerOwner.Team)
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
}

