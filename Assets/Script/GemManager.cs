using UnityEngine;
using TMPro;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    public int levelGem;  
    public int totalGem;   // gem permanent (shop)

    [SerializeField] private TextMeshProUGUI gemText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadTotalGem();
        ResetLevelGem();
    }

    // ===== LEVEL GEM =====
    public void ResetLevelGem()
    {
        levelGem = 0;
        UpdateUI();
    }

    public void AddLevelGem(int amount)
    {
        levelGem += amount;
        UpdateUI();
    }

    // ===== TOTAL GEM (permanent)=====
    public void SaveLevelGemToTotal()
    {
        // 1. Tambahkan gem level ke total
        totalGem += levelGem;
        
        // 2. 🔥 HAPUS PlayerPrefs. Save langsung ke PersistenceManager
        if (PersistenceManager.Instance != null)
        {
            // Update data di 'Otak' penyimpanan
            PersistenceManager.Instance.GetData().totalGem = totalGem; 
            
            // Tulis ke file JSON sekarang juga
            PersistenceManager.Instance.SaveGame();
            
            Debug.Log($"[GemManager] Saved to JSON via PersistenceManager. New Total: {totalGem}");
        }
        else
        {
            Debug.LogError("[GemManager] Gagal save! PersistenceManager tidak ditemukan.");
        }
    }

    private void LoadTotalGem()
    {
        // 🔥 GANTI Load dari PlayerPrefs MENJADI Load dari PersistenceManager
        if (PersistenceManager.Instance != null)
        {
            // Ambil data dari JSON yang sudah di-load di awal game
            totalGem = PersistenceManager.Instance.GetData().totalGem;
            Debug.Log($"[GemManager] Loaded from JSON. Total Gem: {totalGem}");
        }
        else
        {
            totalGem = 0;
            Debug.LogWarning("[GemManager] PersistenceManager belum siap, set totalGem = 0");
        }
    }

    // ===== UI =====
    private void UpdateUI()
    {
        if (gemText != null)
            gemText.text = levelGem.ToString(); 
    }
}
