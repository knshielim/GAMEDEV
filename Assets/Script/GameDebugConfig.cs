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

    [Header("Economy Debug")]
    public bool giveDebugGem = false;
    public int debugGemAmount = 500000;

    public bool ShouldGiveDebugGem() => enableDebugging && giveDebugGem;

    private void Awake()
    {
        // Singleton pattern supaya bisa diakses script lain
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opsional: biar setting awet antar scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Helper functions biar kodingan di script lain rapi
    public bool ShouldSkipDialogue() => enableDebugging && skipDialogue;
    public bool ShouldSkipTutorial() => enableDebugging && skipTutorial;
}