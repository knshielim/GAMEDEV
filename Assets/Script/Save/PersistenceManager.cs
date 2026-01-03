using System.Collections.Generic;
using UnityEngine;

public class PersistenceManager : MonoBehaviour
{
    public static PersistenceManager Instance { get; private set; }

    private SaveData data;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            data = SaveSystem.LoadFromDisk();
            Debug.Log("[PersistenceManager] Loaded save data");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public SaveData GetData() => data;

    public void SaveGame()
    {
        // collect data from managers
        CollectFromManagers();
        SaveSystem.SaveToDisk(data);
    }

    public void LoadGame()
    {
        data = SaveSystem.LoadFromDisk();
        ApplyToManagers();
    }

    private void CollectFromManagers()
    {
        // 1) gems
        if (GemManager.Instance != null)
            data.totalGem = GemManager.Instance.totalGem;

        // 2) max unlocked level
        data.maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", data.maxUnlockedLevel);

        // 3) troop levels (ambil dari PlayerPrefs yang sudah kamu pakai di TroopInstance)
        // Kalau kamu punya daftar troopData global, lebih bagus iterate dari situ.
        // Untuk minimal changes: kita simpan ketika upgrade (lihat bagian "Hook" di bawah)
    }

    private void ApplyToManagers()
    {
        // 1) gems
        if (GemManager.Instance != null)
            GemManager.Instance.totalGem = data.totalGem;

        // 2) max unlocked level (biar level select kebaca)
        PlayerPrefs.SetInt("MaxUnlockedLevel", data.maxUnlockedLevel);
        PlayerPrefs.Save();

        // 3) troop levels
        foreach (var kvp in data.troopLevels)
        {
            PlayerPrefs.SetInt("troop_" + kvp.Key, kvp.Value);
        }
        PlayerPrefs.Save();
    }

    // helper buat update troop level ke save data
    public void SetTroopLevel(string troopId, int level)
    {
        data.troopLevels[troopId] = level;
    }

    public void SetMaxUnlockedLevel(int lvl)
    {
        data.maxUnlockedLevel = Mathf.Clamp(lvl, 1, 5);
    }

    // Helper: Ambil level troop dari SaveData (default 1 jika belum ada)
    public int GetTroopLevel(string troopId)
    {
        if (data.troopLevels.ContainsKey(troopId))
        {
            return data.troopLevels[troopId];
        }
        return 1; // Level default
    }

    // Helper: Simpan level troop
    public void SaveTroopLevel(string troopId, int newLevel)
    {
        data.troopLevels[troopId] = newLevel;
        SaveGame(); // Langsung tulis ke file JSON
        Debug.Log($"[Persistence] Saved Troop {troopId} to Level {newLevel}");
    }
}
