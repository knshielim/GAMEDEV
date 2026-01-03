using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private TextMeshProUGUI[] levelTexts;
    [SerializeField] private Image[] levelImages;
    [SerializeField] private Button backButton;

    [Header("Level Data")]
    [SerializeField] private Sprite[] levelBackgrounds;
    [SerializeField] private string[] levelNames = {
        "Forest Outpost",
        "Mountain Pass",
        "Desert Ruins",
        "Wave Fortress",
        "Final Citadel"
    };
    [SerializeField] private string[] levelDescriptions = {
        "Defend against basic enemy forces in the peaceful forest.",
        "Navigate treacherous mountain terrain while fending off stronger enemies.",
        "Battle in the scorching desert where resources are scarce.",
        "Survive increasingly difficult waves of enemies.",
        "Face the ultimate challenge in the enemy's stronghold."
    };

    [Header("Level Status")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    private int maxUnlockedLevel = 1;

    private void Start()
    {
        InitializeLevelSelect();
        LoadProgress();
        UpdateLevelButtons();
    }

    private void InitializeLevelSelect()
    {
        // Ensure we have at least some buttons
        if (levelButtons == null || levelButtons.Length == 0)
        {
            Debug.LogError("[LevelSelect] No level buttons assigned!");
            return;
        }

        // Check if we have enough data for the buttons
        if (levelNames == null || levelNames.Length < levelButtons.Length)
        {
            Debug.LogError("[LevelSelect] Not enough level names for the number of buttons!");
            return;
        }

        // Set up back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => LoadMainMenu());
        }

        // Set up level buttons
        int maxButtons = Mathf.Min(levelButtons.Length, levelNames.Length);
        for (int i = 0; i < maxButtons; i++)
        {
            if (levelButtons[i] != null)
            {
                int levelIndex = i + 1; // Capture the value for the closure
                levelButtons[i].onClick.AddListener(() => LoadLevel(levelIndex));
            }
        }
    }

    private void LoadProgress()
    {
        // Load max unlocked level from JSON data
        // Starts with level 1 unlocked by default
        
        if (PersistenceManager.Instance != null)
        {
            maxUnlockedLevel = PersistenceManager.Instance.GetData().maxUnlockedLevel;
            Debug.Log($"[LevelSelect] Loaded Progress from JSON: MaxLevel {maxUnlockedLevel}");
        }
        else
        {
            maxUnlockedLevel = 1; // Default kalau belum ada save data
            Debug.LogWarning("[LevelSelect] PersistenceManager not found, defaulting to Level 1");
        }

        // Always unlock Level 5 for boss testing (no progression required)
        // maxUnlockedLevel = Mathf.Max(maxUnlockedLevel, 5);
        // Reason: If this line is active, Level 5 will always be open (Debug Mode),
        // so you won't know whether the save system was successful or not.

        // Clamp to valid range
        maxUnlockedLevel = Mathf.Clamp(maxUnlockedLevel, 1, 5);
    }

    // Public method to unlock a level (call this when a level is completed)
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > maxUnlockedLevel && levelNumber <= 5)
        {
            maxUnlockedLevel = levelNumber;
            
            UpdateLevelButtons(); // Refresh the UI

            
            if (PersistenceManager.Instance != null)
            {
                PersistenceManager.Instance.SetMaxUnlockedLevel(maxUnlockedLevel);
                PersistenceManager.Instance.SaveGame();
            }
        }


    }


    private void UpdateLevelButtons()
    {
        if (levelButtons == null || levelButtons.Length == 0)
        {
            Debug.LogError("[LevelSelect] levelButtons array is not assigned or empty!");
            return;
        }

        // Use the length of levelButtons as the maximum, but check bounds for other arrays
        int maxLevels = Mathf.Min(levelButtons.Length, levelNames.Length);

        for (int i = 0; i < maxLevels; i++)
        {
            int levelNumber = i + 1;
            bool isUnlocked = levelNumber <= maxUnlockedLevel;

            // Update button interactability
            if (levelButtons[i] != null)
            {
                levelButtons[i].interactable = isUnlocked;
            }

            // Update text color only (keep existing text content)
            if (levelTexts != null && i < levelTexts.Length && levelTexts[i] != null)
            {
                levelTexts[i].color = isUnlocked ? unlockedColor : lockedColor;
            }

            // Update background image (check bounds)
            if (levelImages != null && i < levelImages.Length && levelImages[i] != null)
            {
                if (isUnlocked)
                {
                    levelImages[i].sprite = unlockedSprite;
                    levelImages[i].color = unlockedColor;
                }
                else
                {
                    levelImages[i].sprite = lockedSprite;
                    levelImages[i].color = lockedColor;
                }
            }
        }
    }

    public void LoadLevel(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > 5)
        {
            Debug.LogError($"[LevelSelect] Invalid level number: {levelNumber}");
            return;
        }

        if (levelNumber > maxUnlockedLevel)
        {
            Debug.LogWarning($"[LevelSelect] Level {levelNumber} is locked!");
            return;
        }

        // Try different scene name formats
        string[] sceneNamesToTry = {
            $"Level {levelNumber}",  // "Level 1", "Level 2", etc.
            $"Level{levelNumber}",   // "Level1", "Level2", etc.
            $"{levelNumber}"         // "1", "2", etc. (build index)
        };

        foreach (string sceneName in sceneNamesToTry)
        {
            Debug.Log($"[LevelSelect] Attempting to load scene: '{sceneName}'");
            try
            {
                SceneManager.LoadScene(sceneName);
                Debug.Log($"[LevelSelect] Successfully loaded scene: '{sceneName}'");
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LevelSelect] Failed to load scene '{sceneName}': {e.Message}");
            }
        }

        Debug.LogError($"[LevelSelect] Could not load Level {levelNumber} with any scene name format!");
    }

    private void LoadMainMenu()
    {
        Debug.Log("[LevelSelect] Returning to Main Menu");
        SceneManager.LoadScene(0);
    }

    public string GetLevelName(int levelNumber)
    {
        if (levelNumber >= 1 && levelNumber <= levelNames.Length)
        {
            return levelNames[levelNumber - 1];
        }
        return "Unknown Level";
    }

    public string GetLevelDescription(int levelNumber)
    {
        if (levelNumber >= 1 && levelNumber <= levelDescriptions.Length)
        {
            return levelDescriptions[levelNumber - 1];
        }
        return "No description available.";
    }
}
