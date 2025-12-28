using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Backstory Content")]
    [Tooltip("Each paragraph will be displayed one at a time")]
    [SerializeField] private string[] backstoryParagraphs = new string[] {
        "Long ago, the world was in chaos as wild monsters roamed freely, destroying everything in their path.",
        
        "A group of wise summoners discovered an ancient magic—the power to merge creatures, creating stronger beings from weaker ones.",
        
        "For generations, summoners used this gift to protect humanity, building great towers as bastions of civilization.",
        
        "But now, the Shadow King has risen from the forgotten depths, commanding an army of corrupted monsters.",
        
        "He seeks to destroy the summoner towers and plunge the world into eternal darkness.",
        
        "You are the last summoner chosen by the Ancient Spirits. Only you possess the gift to merge monsters and command them in battle.",
        
        "The fate of the world rests on your shoulders."
    };

    [Header("Typing Animation Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private KeyCode advanceKey = KeyCode.Space;

    [Header("Testing")]
    [Tooltip("Check this to ALWAYS show backstory (for testing only)")]
    [SerializeField] private bool alwaysShowBackstory = false;

    [Header("UI References")]
    [Tooltip("The main menu buttons (Play, Option, Quit)")]
    public GameObject mainMenuButtons;

    [Tooltip("The options menu panel")]
    public GameObject optionsMenu;

    [Tooltip("The back button")]
    public GameObject backButton;

    [Header("Backstory UI")]
    [Tooltip("The backstory panel")]
    public GameObject backstoryPanel;

    [Tooltip("Backstory title text")]
    public TMPro.TextMeshProUGUI backstoryTitle;

    [Tooltip("Backstory content text")]
    public TMPro.TextMeshProUGUI backstoryText;

    [Tooltip("Continue button to proceed to level select")]
    public UnityEngine.UI.Button continueButton;

    [Tooltip("'Press SPACE to continue' panel - will hide on last paragraph")]
    public GameObject pressSpacePanel;

    [Tooltip("Optional: Instruction text inside the panel")]
    public TMPro.TextMeshProUGUI instructionText;

    // Internal state
    private int currentParagraphIndex = 0;
    private bool isTyping = false;
    private bool canAdvance = false;
    private Coroutine typingCoroutine;

    private void Start()
    {
        // Show main menu, hide options and back button at start
        ShowMainMenu();

        // Set up backstory continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToLevelSelect);
            continueButton.gameObject.SetActive(false); // Hide initially
        }

        // Hide backstory panel initially
        if (backstoryPanel != null)
        {
            backstoryPanel.SetActive(false);
        }

        // Hide press space panel initially
        if (pressSpacePanel != null)
        {
            pressSpacePanel.SetActive(false);
        }

        // DEBUG: Check backstory status
        bool hasSeenBackstory = PlayerPrefs.GetInt("HasSeenBackstory", 0) == 1;
        Debug.Log($"[MainMenu] 🎬 Backstory Status: HasSeenBackstory = {hasSeenBackstory}, AlwaysShow = {alwaysShowBackstory}");
    }

    private void Update()
    {
        // Only allow advancing when backstory is active
        if (backstoryPanel != null && backstoryPanel.activeSelf)
        {
            if (Input.GetKeyDown(advanceKey))
            {
                HandleAdvanceInput();
            }
        }

        // CHEAT CODE: Press R+B+S at the same time to reset backstory
        if (Input.GetKey(KeyCode.R) && Input.GetKey(KeyCode.B) && Input.GetKey(KeyCode.S))
        {
            ResetBackstoryWithFeedback();
        }

        // CHEAT CODE: Press R+E+S+E+T to reset ALL progress
        if (Input.GetKey(KeyCode.R) && Input.GetKey(KeyCode.E) && 
            Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.T))
        {
            ResetAllProgressWithFeedback();
        }
    }

    public void PlayGame()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        // Check if this is the first time playing (or testing mode)
        bool hasSeenBackstory = PlayerPrefs.GetInt("HasSeenBackstory", 0) == 1;

        Debug.Log($"[MainMenu] 🎮 PlayGame called - HasSeenBackstory: {hasSeenBackstory}, AlwaysShow: {alwaysShowBackstory}");

        if (!hasSeenBackstory || alwaysShowBackstory)
        {
            // First time playing OR testing mode - show backstory
            Debug.Log("[MainMenu] ✅ Showing backstory");
            ShowBackstory();
        }
        else
        {
            // Already seen backstory - go directly to level select
            Debug.Log("[MainMenu] ⏭️  Skipping backstory, going to level select");
            LoadLevelSelect();
        }
    }

    private void ShowBackstory()
    {
        Debug.Log("[MainMenu] 🎬 ShowBackstory called - Setting up UI...");

        // Check if backstory panel exists
        if (backstoryPanel == null)
        {
            Debug.LogError("[MainMenu] ❌ CRITICAL: backstoryPanel is NULL! Assign it in Inspector!");
            LoadLevelSelect();
            return;
        }

        // Check if backstory text exists
        if (backstoryText == null)
        {
            Debug.LogError("[MainMenu] ❌ CRITICAL: backstoryText is NULL! Assign it in Inspector!");
            LoadLevelSelect();
            return;
        }

        // Hide main menu
        if (mainMenuButtons != null)
        {
            mainMenuButtons.SetActive(false);
            Debug.Log("[MainMenu] ✅ Main menu hidden");
        }

        // Show backstory panel
        backstoryPanel.SetActive(true);

        // 🔥 CLEAR UI SELECTION SO SPACE WON'T CLICK BUTTONS
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        Debug.Log($"[MainMenu] ✅ Backstory panel activated! Active: {backstoryPanel.activeSelf}");

        if (backstoryTitle != null)
            backstoryTitle.text = "The Ancient Pact";

        // Reset state
        currentParagraphIndex = 0;
        
        // Hide continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            Debug.Log("[MainMenu] ✅ Continue button hidden");
        }

        // Show press space panel
        if (pressSpacePanel != null)
        {
            pressSpacePanel.SetActive(true);
            Debug.Log("[MainMenu] ✅ Press SPACE panel shown");
            
            // Set instruction text if available
            if (instructionText != null)
            {
                instructionText.text = "Press SPACE to continue...";
            }
        }
        else
        {
            Debug.LogWarning("[MainMenu] ⚠️ pressSpacePanel is NULL - assign it in Inspector for better UX");
        }

        // Mark as seen (only if not in testing mode)
        if (!alwaysShowBackstory)
        {
            PlayerPrefs.SetInt("HasSeenBackstory", 1);
            PlayerPrefs.Save();
            Debug.Log("[MainMenu] 💾 Backstory marked as seen");
        }

        // Start typing the first paragraph
        StartTypingParagraph(currentParagraphIndex);
    }

    private void StartTypingParagraph(int index)
    {
        if (index < 0 || index >= backstoryParagraphs.Length)
        {
            Debug.LogError($"[MainMenu] ❌ Invalid paragraph index: {index}");
            return;
        }

        // Stop any existing typing
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Reset state
        isTyping = true;
        canAdvance = false;

        // Start typing animation
        typingCoroutine = StartCoroutine(TypeText(backstoryParagraphs[index]));

        Debug.Log($"[MainMenu] ⌨️  Typing paragraph {index + 1}/{backstoryParagraphs.Length}");
    }

    private IEnumerator TypeText(string text)
    {
        if (backstoryText == null)
        {
            Debug.LogError("[MainMenu] ❌ Backstory text component is null!");
            yield break;
        }

        backstoryText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            backstoryText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Typing complete
        isTyping = false;
        canAdvance = true;

        Debug.Log($"[MainMenu] ✅ Paragraph {currentParagraphIndex + 1} typing complete");

        // Check if this is the last paragraph
        if (currentParagraphIndex >= backstoryParagraphs.Length - 1)
        {
            ShowContinueButton();
        }
    }

    private void HandleAdvanceInput()
    {
        if (isTyping)
        {
            // Skip typing animation - show full text immediately
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (backstoryText != null && currentParagraphIndex < backstoryParagraphs.Length)
            {
                backstoryText.text = backstoryParagraphs[currentParagraphIndex];
            }

            isTyping = false;
            canAdvance = true;

            Debug.Log($"[MainMenu] ⏭️  Typing skipped for paragraph {currentParagraphIndex + 1}");

            // Check if this was the last paragraph
            if (currentParagraphIndex >= backstoryParagraphs.Length - 1)
            {
                ShowContinueButton();
            }
        }
        else if (canAdvance)
        {
            // Move to next paragraph
            AdvanceToNextParagraph();
        }
    }

    private void AdvanceToNextParagraph()
    {
        currentParagraphIndex++;

        if (currentParagraphIndex < backstoryParagraphs.Length)
        {
            // Type the next paragraph
            Debug.Log($"[MainMenu] ➡️  Advancing to paragraph {currentParagraphIndex + 1}");
            StartTypingParagraph(currentParagraphIndex);
        }
        else
        {
            // All paragraphs shown
            Debug.Log("[MainMenu] 🏁 All backstory paragraphs displayed");
            ShowContinueButton();
        }
    }

    private void ShowContinueButton()
    {
        Debug.Log("[MainMenu] 🎬 Last paragraph reached - showing continue button");

        // Hide press space panel (this is the key change!)
        if (pressSpacePanel != null)
        {
            pressSpacePanel.SetActive(false);
            Debug.Log("[MainMenu] ✅ Press SPACE panel hidden");
        }

        // Show continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            Debug.Log("[MainMenu] ✅ Continue button shown");
        }

        canAdvance = false; // Disable space bar advancement
    }

    private void ContinueToLevelSelect()
    {
        Debug.Log("[MainMenu] 🚀 Player dismissed backstory, loading level select");
        
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        LoadLevelSelect();
    }

    private void LoadLevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void ShowOptions()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        // Hide main menu buttons
        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(false);

        // Show options menu
        if (optionsMenu != null)
            optionsMenu.SetActive(true);

        // Show back button
        if (backButton != null)
            backButton.SetActive(true);
    }

    public void ShowMainMenu()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);

        // Show main menu buttons
        if (mainMenuButtons != null)
            mainMenuButtons.SetActive(true);

        // Hide options menu
        if (optionsMenu != null)
            optionsMenu.SetActive(true);

        // Hide back button
        if (backButton != null)
            backButton.SetActive(true);

        // Hide backstory panel
        if (backstoryPanel != null)
            backstoryPanel.SetActive(false);

        // Hide press space panel
        if (pressSpacePanel != null)
            pressSpacePanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // ═══════════════════════════════════════════════════════
    // RESET FUNCTIONS
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Resets only the backstory flag (player will see backstory again)
    /// </summary>
    [ContextMenu("Reset Backstory Only")]
    public void ResetBackstorySeen()
    {
        PlayerPrefs.SetInt("HasSeenBackstory", 0);
        PlayerPrefs.Save();
        Debug.Log("[MainMenu] 🔄 Backstory flag reset!");
    }

    /// <summary>
    /// Resets backstory with UI feedback
    /// </summary>
    public void ResetBackstoryWithFeedback()
    {
        ResetBackstorySeen();
        
        if (AudioManager.Instance != null && AudioManager.Instance.upgradeSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSFX);
        
        Debug.Log("[MainMenu] ✅ Backstory will play again next time you press Play!");
        
        // Optional: Show a temporary message to the player
        StartCoroutine(ShowTemporaryMessage("Backstory Reset!", 2f));
    }

    /// <summary>
    /// Resets ALL player progress (backstory, tutorial, levels, etc.)
    /// </summary>
    [ContextMenu("Reset ALL Progress")]
    public void ResetAllProgress()
    {
        // Reset backstory
        PlayerPrefs.SetInt("HasSeenBackstory", 0);
        
        // Reset tutorial
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.SetInt("TutorialJustCompleted", 0);
        
        // Reset level progress
        PlayerPrefs.SetInt("MaxUnlockedLevel", 1);
        
        // Reset all level dialogues
        for (int i = 1; i <= 5; i++)
        {
            PlayerPrefs.SetInt($"Level{i}_StartDialogueSeen", 0);
            PlayerPrefs.SetInt($"Level{i}_EndDialogueSeen", 0);
        }
        
        PlayerPrefs.Save();
        Debug.Log("[MainMenu] 🔄 ALL progress reset!");
    }

    /// <summary>
    /// Resets all progress with UI feedback
    /// </summary>
    public void ResetAllProgressWithFeedback()
    {
        ResetAllProgress();
        
        if (AudioManager.Instance != null && AudioManager.Instance.upgradeSFX != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.upgradeSFX);
        
        Debug.Log("[MainMenu] ✅ ALL progress reset! Game will restart from the beginning.");
        
        // Optional: Show a temporary message to the player
        StartCoroutine(ShowTemporaryMessage("All Progress Reset!", 3f));
    }

    /// <summary>
    /// Shows a temporary message on screen (optional helper)
    /// </summary>
    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        // This is a simple console log version
        // You can replace this with actual UI if you want
        Debug.Log($"[MainMenu] 💬 {message}");
        yield return new WaitForSeconds(duration);
    }
}