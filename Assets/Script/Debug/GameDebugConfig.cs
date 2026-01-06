using UnityEngine;

public class GameDebugConfig : MonoBehaviour
{
    public static GameDebugConfig Instance;

    [Header("🔧 DEBUGGING CONTROL")]
    [Tooltip("Check this to enable the debug features below")]
    public bool enableDebugging = false; 

    [Header("Sub-Options (Only active if enableDebugging = YES)")]
    public bool skipDialogue = false;
    public bool skipTutorial = false;

    [Header("Progress Debug")]
    public bool unlockAllLevels = false;

    [Header("Economy Debug")]
    public bool giveDebugGem = false; // for shop
    public int debugGemAmount = 500000;

    [Header("Drop Debug")]
    public bool forceGemDrop100 = false;

    public bool ShouldGiveDebugGem() => enableDebugging && giveDebugGem;
    public bool ShouldUnlockAllLevels() => enableDebugging && unlockAllLevels;
    public bool ShouldForceGemDrop100() => enableDebugging && forceGemDrop100;

    private void Awake()
    {
        Debug.Log("[GameDebugConfig] Awake called on: " + gameObject.name);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 🔥 INI YANG KURANG
    }


    // Helper functions biar kodingan di script lain rapi
    public bool ShouldSkipDialogue() => enableDebugging && skipDialogue;
    public bool ShouldSkipTutorial() => enableDebugging && skipTutorial;
}