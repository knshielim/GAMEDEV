using UnityEngine;
using TMPro;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    // ===== BACKING FIELDS =====
    [SerializeField] private int _levelGem;
    [SerializeField] private int _totalGem;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private bool showTotalGem = true;

    // ===== PROPERTIES =====
    public int levelGem
    {
        get => _levelGem;
        set
        {
            _levelGem = value;
            UpdateUI();
        }
    }

    public int totalGem
    {
        get => _totalGem;
        set
        {
            _totalGem = value;
            UpdateUI();
        }
    }

    private void Awake()
    {
        // Singleton setup
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
        levelGem = 0; // UI updates automatically via property
    }

    public void AddLevelGem(int amount)
    {
        levelGem += amount; // UI updates automatically via property
    }

    // ===== TOTAL GEM (permanent) =====
    public void SaveLevelGemToTotal()
    {
        totalGem += levelGem; // UI updates automatically

        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.GetData().totalGem = totalGem;
            PersistenceManager.Instance.SaveGame();
            Debug.Log($"[GemManager] Saved to JSON via PersistenceManager. New Total: {totalGem}");
        }
        else
        {
            Debug.LogError("[GemManager] PersistenceManager not found! Could not save gems.");
        }

        ResetLevelGem(); // optional: clear level gems after adding to total
    }

    private void LoadTotalGem()
    {
        if (PersistenceManager.Instance != null)
        {
            totalGem = PersistenceManager.Instance.GetData().totalGem; // property updates UI
            Debug.Log($"[GemManager] Loaded from JSON. Total Gem: {totalGem}");
        }
        else
        {
            totalGem = 0; // property updates UI
            Debug.LogWarning("[GemManager] PersistenceManager not ready, set totalGem = 0");
        }
    }

    // ===== UI =====
    private void UpdateUI()
    {
        if (gemText == null) return;
        gemText.text = showTotalGem ? totalGem.ToString() : levelGem.ToString();
    }

    // ===== SPENDING =====
    public bool HasEnoughTotalGem(int amount)
    {
        return totalGem >= amount;
    }

    public bool SpendTotalGem(int amount)
    {
        if (!HasEnoughTotalGem(amount))
            return false;

        totalGem -= amount; // property updates UI

        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.GetData().totalGem = totalGem;
            PersistenceManager.Instance.SaveGame();
        }

        return true;
    }

    // ===== HELPER: Add Gems Directly =====
    public void AddTotalGem(int amount)
    {
        totalGem += amount; // property updates UI and auto-refresh
    }

    // Optional: toggle which gem to show in UI
    public void SetShowTotalGem(bool showTotal)
    {
        showTotalGem = showTotal;
        UpdateUI();
    }
}
