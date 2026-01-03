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

            data = SaveSystem.LoadFromDisk(); // Load JSON saat game nyala
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
        // Langsung simpan data yang ada di memori ke Disk
        // Tidak perlu CollectFromManagers() lagi karena data sudah di-update secara real-time
        SaveSystem.SaveToDisk(data);
    }

    public void LoadGame()
    {
        data = SaveSystem.LoadFromDisk();
        // Tidak perlu ApplyToManagers() lagi karena script lain akan mengambil data sendiri via GetData()
    }

    // ❌ HAPUS method CollectFromManagers() sepenuhnya
    // ❌ HAPUS method ApplyToManagers() sepenuhnya

    // ========================================================================
    // HELPER METHODS (Memudahkan script lain akses data tanpa pegang SaveData langsung)
    // ========================================================================

    // 1. TROOP LEVEL
    public void SetTroopLevel(string troopId, int level)
    {
        data.troopLevels[troopId] = level;
    }

    public int GetTroopLevel(string troopId)
    {
        if (data.troopLevels.ContainsKey(troopId))
        {
            return data.troopLevels[troopId];
        }
        return 1; // Level default
    }

    public void SaveTroopLevel(string troopId, int newLevel)
    {
        SetTroopLevel(troopId, newLevel);
        SaveGame(); // Shortcut: Set lalu Save
        Debug.Log($"[Persistence] Saved Troop {troopId} to Level {newLevel}");
    }

    // 2. LEVEL PROGRESS
    public void SetMaxUnlockedLevel(int lvl)
    {
        data.maxUnlockedLevel = Mathf.Clamp(lvl, 1, 5);
    }

    // 3. AUDIO SETTINGS
    public void SaveAudioSettings(float master, float music, float sfx)
    {
        data.masterVolume = master;
        data.musicVolume = music;
        data.sfxVolume = sfx;
        SaveGame();
    }
    
    public float GetMasterVolume() => data.masterVolume;
    public float GetMusicVolume() => data.musicVolume;
    public float GetSFXVolume() => data.sfxVolume;

    // 4. TUTORIAL
    public bool IsTutorialCompleted() => data.isTutorialCompleted;

    public void SetTutorialCompleted(bool completed)
    {
        data.isTutorialCompleted = completed;
        SaveGame();
    }

    // 5. DIALOGUES (Termasuk Backstory)
    public bool HasSeenDialogue(string dialogueKey)
    {
        return data.seenDialogues.Contains(dialogueKey);
    }

    public void MarkDialogueSeen(string dialogueKey)
    {
        if (!data.seenDialogues.Contains(dialogueKey))
        {
            data.seenDialogues.Add(dialogueKey);
            SaveGame();
        }
    }

    public void ResetDialogueStatus(string dialogueKey)
    {
        if (data.seenDialogues.Contains(dialogueKey))
        {
            data.seenDialogues.Remove(dialogueKey);
            SaveGame();
        }
    }
}