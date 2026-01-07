using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    // Event untuk UI per-scene
    public static event Action<int> OnTotalGemChanged;
    public static event Action<int> OnLevelGemChanged;

    public bool IsInitialized { get; private set; }

    [SerializeField] private int _levelGem;
    [SerializeField] private int _totalGem;

    [Header("Optional UI (boleh kosong kalau pakai GemBorderUI)")]
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private bool showTotalGem = true;

    public int levelGem
    {
        get => _levelGem;
        set
        {
            _levelGem = value;
            OnLevelGemChanged?.Invoke(_levelGem);
            UpdateUI();
        }
    }

    public int totalGem
    {
        get => _totalGem;
        set
        {
            _totalGem = value;
            OnTotalGemChanged?.Invoke(_totalGem);
            UpdateUI();
        }
    }

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
        StartCoroutine(InitializeGemData());
    }

    private IEnumerator InitializeGemData()
    {
        float timeout = 1f;
        while (PersistenceManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        LoadTotalGem();
        ResetLevelGem();

        // DEBUG OVERRIDE
        if (GameDebugConfig.Instance != null && GameDebugConfig.Instance.ShouldGiveDebugGem())
        {
            totalGem = GameDebugConfig.Instance.debugGemAmount;

            if (PersistenceManager.Instance != null && PersistenceManager.Instance.GetData() != null)
            {
                PersistenceManager.Instance.GetData().totalGem = totalGem;
                PersistenceManager.Instance.SaveGame();
            }

            Debug.Log($"[DEBUG] Total Gem forced to {totalGem}");
        }

        IsInitialized = true;

        // Optional: pastikan UI per-scene yang subscribe langsung dapat nilai terbaru
        OnTotalGemChanged?.Invoke(totalGem);
        OnLevelGemChanged?.Invoke(levelGem);
    }

    // ===== LEVEL GEM =====
    public void ResetLevelGem() => levelGem = 0;

    public void AddLevelGem(int amount)
    {
        levelGem += amount;
        // Immediately add to total gems and save to persistent storage
        // This ensures gems persist even if player dies or restarts
        AddTotalGem(amount);
        Debug.Log($"[GemManager] Collected {amount} gems during gameplay. Total: {totalGem} (Saved)");
    }

    public void ConvertLevelGemToTotal()
    {
        // Since AddLevelGem now immediately adds to total gems,
        // this method just resets levelGem (gems are already in totalGem)
        // This is kept for backwards compatibility and for level completion flow
        ResetLevelGem();
    }

    // ===== PERSISTENCE =====
    private void LoadTotalGem()
    {
        if (PersistenceManager.Instance != null && PersistenceManager.Instance.GetData() != null)
        {
            totalGem = PersistenceManager.Instance.GetData().totalGem;
            Debug.Log($"[GemManager] Loaded from JSON. Total Gem: {totalGem}");
        }
        else
        {
            Debug.LogWarning("[GemManager] PersistenceManager not ready yet.");
        }
    }

    // ===== OPTIONAL UI (kalau kamu masih mau pakai) =====
    private void UpdateUI()
    {
        if (gemText == null) return;
        gemText.text = showTotalGem ? totalGem.ToString() : levelGem.ToString();
    }

    // ===== SPENDING =====
    public bool HasEnoughTotalGem(int amount) => totalGem >= amount;

    public bool SpendTotalGem(int amount)
    {
        if (!HasEnoughTotalGem(amount))
            return false;

        totalGem -= amount;

        if (PersistenceManager.Instance != null && PersistenceManager.Instance.GetData() != null)
        {
            PersistenceManager.Instance.GetData().totalGem = totalGem;
            PersistenceManager.Instance.SaveGame();
        }

        return true;
    }

    public void AddTotalGem(int amount)
    {
        totalGem += amount;

        if (PersistenceManager.Instance != null && PersistenceManager.Instance.GetData() != null)
        {
            PersistenceManager.Instance.GetData().totalGem = totalGem;
            PersistenceManager.Instance.SaveGame();
            Debug.Log($"[GemManager] Added {amount} gems. New Total: {totalGem} (Saved)");
        }
        else
        {
            Debug.LogError("[GemManager] Failed to save gems! PersistenceManager is null.");
        }
    }
}