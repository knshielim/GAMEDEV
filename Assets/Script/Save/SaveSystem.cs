using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class SaveSystem
{
    private const string FILE_NAME = "save.json";

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }

    public static void SaveToDisk(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(new SaveDataWrapper(data), true);
            File.WriteAllText(GetPath(), json);
            Debug.Log($"[SaveSystem] Saved to: {GetPath()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    public static SaveData LoadFromDisk()
    {
        try
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                Debug.Log("[SaveSystem] No save file found, creating new SaveData");
                return new SaveData();
            }

            string json = File.ReadAllText(path);
            var wrapper = JsonUtility.FromJson<SaveDataWrapper>(json);
            return wrapper != null ? wrapper.ToSaveData() : new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return new SaveData();
        }
    }

    public static void DeleteSave()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[SaveSystem] Save file deleted.");
        }
    }

    // JsonUtility tidak bisa serialize Dictionary langsung → pakai wrapper
    [Serializable]
    private class SaveDataWrapper
    {
        public int totalGem;
        public int maxUnlockedLevel;
        public int version;

        public List<string> troopIds = new List<string>();
        public List<int> troopLvls = new List<int>();

        public SaveDataWrapper(SaveData d)
        {
            totalGem = d.totalGem;
            maxUnlockedLevel = d.maxUnlockedLevel;
            version = d.version;

            foreach (var kvp in d.troopLevels)
            {
                troopIds.Add(kvp.Key);
                troopLvls.Add(kvp.Value);
            }
        }

        public SaveData ToSaveData()
        {
            var d = new SaveData
            {
                totalGem = totalGem,
                maxUnlockedLevel = maxUnlockedLevel,
                version = version
            };

            int n = Mathf.Min(troopIds.Count, troopLvls.Count);
            for (int i = 0; i < n; i++)
                d.troopLevels[troopIds[i]] = troopLvls[i];

            return d;
        }
    }
}
