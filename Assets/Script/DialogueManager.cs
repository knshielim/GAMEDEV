using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Image speakerPortrait;

    [Header("Dialogue Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float autoAdvanceDelay = 2f;

    [System.Serializable]
    public class LevelDialogue
    {
        public int levelNumber;
        [TextArea(2, 5)] public string[] startDialogueLines;
        [TextArea(2, 5)] public string[] endDialogueLines;
        public string[] startSpeakerNames;
        public string[] endSpeakerNames;
        public Sprite[] startPortraits;
        public Sprite[] endPortraits;
    }

    [SerializeField] private LevelDialogue[] levelDialogues = {
        new LevelDialogue {
            levelNumber = 1,
            startDialogueLines = new string[] {
                "Welcome, young summoner! The ancient spirits have chosen you to defend our lands.",
                "Dark forces are gathering beyond the mountains. You must use your gacha powers wisely.",
                "Each level will test your strategic mind and summoning skills. Choose your troops carefully!",
                "Remember: Common troops are numerous but weak. Rare ones are balanced. Epic and Legendary units are powerful but rare.",
                "The enemy tower grows stronger with each victory. Don't let them reach your base!",
                "Good luck, summoner! The fate of our world rests in your hands."
            },
            startSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit",
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            },
            endDialogueLines = new string[] {
                "Well done! You've defended your first outpost.",
                "The enemy grows stronger, but so do you.",
                "Prepare for the challenges ahead in the mountain pass."
            },
            endSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            }
        },
        new LevelDialogue {
            levelNumber = 2,
            startDialogueLines = new string[] {
                "The treacherous mountain pass awaits.",
                "Navigate the difficult terrain and face stronger enemies.",
                "Your strategic skills will be tested here."
            },
            startSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            },
            endDialogueLines = new string[] {
                "Impressive victory in the mountains!",
                "The desert ruins lie ahead with their own dangers.",
                "Stay vigilant, summoner."
            },
            endSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            }
        },
        new LevelDialogue {
            levelNumber = 3,
            startDialogueLines = new string[] {
                "The scorching desert ruins test your endurance.",
                "Resources are scarce here, but opportunities abound.",
                "Adapt to the harsh conditions and emerge victorious."
            },
            startSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            },
            endDialogueLines = new string[] {
                "You've conquered the desert ruins!",
                "The wave fortress presents a new challenge.",
                "Survive the relentless enemy waves."
            },
            endSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            }
        },
        new LevelDialogue {
            levelNumber = 4,
            startDialogueLines = new string[] {
                "The wave fortress unleashes enemies in relentless waves.",
                "Your timing and resource management are crucial here.",
                "Survive the escalating assault!"
            },
            startSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            },
            endDialogueLines = new string[] {
                "Outstanding! You've weathered the waves!",
                "Now face the ultimate challenge in the enemy's stronghold.",
                "The final citadel awaits your assault."
            },
            endSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            }
        },
        new LevelDialogue {
            levelNumber = 5,
            startDialogueLines = new string[] {
                "The final citadel - the enemy's last stronghold.",
                "Face their most powerful defenses and ultimate champion.",
                "This battle will decide the fate of our world!"
            },
            startSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            },
            endDialogueLines = new string[] {
                "INCREDIBLE! You've conquered the final citadel!",
                "Peace returns to our lands thanks to your bravery.",
                "You are truly the greatest summoner our world has known.",
                "Thank you for saving us all!"
            },
            endSpeakerNames = new string[] {
                "Ancient Spirit", "Ancient Spirit", "Ancient Spirit", "Ancient Spirit"
            }
        }
    };

    private Queue<string> dialogueQueue;
    private Queue<string> speakerQueue;
    private Queue<Sprite> portraitQueue;
    private bool isTyping = false;
    private bool skipRequested = false;
    private Coroutine currentTypingCoroutine;
    private bool isShowingStartDialogue = false;
    private bool isShowingEndDialogue = false;
    private bool isForcedEndDialogue = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh UI references when a new scene loads
        FindUIComponents();
    }

    private void FindUIComponents()
    {
        // Try to find UI components in the current scene
        if (dialoguePanel == null)
        {
            GameObject panelObj = GameObject.Find("DialoguePanel");
            if (panelObj != null)
            {
                dialoguePanel = panelObj;
                Debug.Log("[Dialogue] Found DialoguePanel in scene");
            }
        }

        if (dialogueText == null)
        {
            dialogueText = GameObject.Find("DialogueText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (dialogueText != null)
                Debug.Log("[Dialogue] Found DialogueText in scene");
        }

        if (speakerNameText == null)
        {
            speakerNameText = GameObject.Find("SpeakerNameText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (speakerNameText != null)
                Debug.Log("[Dialogue] Found SpeakerNameText in scene");
        }

        if (skipButton == null)
        {
            skipButton = GameObject.Find("SkipButton")?.GetComponent<UnityEngine.UI.Button>();
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipDialogue);
                Debug.Log("[Dialogue] Found SkipButton in scene");
            }
        }
    }

    private void Start()
    {
        InitializeDialogueSystem();
        FindUIComponents(); // Try to find UI components
        CheckIfIntroNeeded();
    }

    private void InitializeDialogueSystem()
    {
        dialogueQueue = new Queue<string>();
        speakerQueue = new Queue<string>();
        portraitQueue = new Queue<Sprite>();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipDialogue);
    }

    private void CheckIfIntroNeeded()
    {
        // Check if UI components are assigned
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogWarning("[Dialogue] Dialogue UI components not assigned! Skipping dialogue.");
            return;
        }

        // Get current level
        int currentLevel = GetCurrentLevel();

        if (currentLevel >= 1 && currentLevel <= 5)
        {
            // SPECIAL CASE: Check if we just completed the tutorial and should show Level 1 end dialogue
            bool tutorialJustCompleted = PlayerPrefs.GetInt("TutorialJustCompleted", 0) == 1;
            bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

            Debug.Log($"[Dialogue] CheckIfIntroNeeded - Level {currentLevel}, tutorialJustCompleted: {tutorialJustCompleted}, tutorialCompleted: {tutorialCompleted}");

            if (tutorialJustCompleted && currentLevel == 1)
            {
                Debug.Log("[Dialogue] Tutorial just completed - showing Level 1 end dialogue");
                // Clear the flag
                PlayerPrefs.SetInt("TutorialJustCompleted", 0);
                PlayerPrefs.Save();
                // Show end dialogue for Level 1
                StartCoroutine(ShowLevelEndDialogueForcedCoroutine(levelDialogues[0].endDialogueLines, levelDialogues[0].endSpeakerNames, levelDialogues[0].endPortraits));
                return;
            }

            // Check if start dialogue for this level has been seen
            string levelStartKey = $"Level{currentLevel}_StartDialogueSeen";
            bool hasSeenLevelStart = PlayerPrefs.GetInt(levelStartKey, 0) == 1;

            if (!hasSeenLevelStart)
            {
                StartCoroutine(ShowLevelStartDialogue(currentLevel));
            }
        }
    }

    private int GetCurrentLevel()
    {
        // Try to get from GameManager first
        if (GameManager.Instance != null)
        {
            return (int)GameManager.Instance.currentLevel;
        }

        // Try to get from LevelManager
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.GetCurrentLevel();
        }

        // Fallback: parse from scene name
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level "))
        {
            string levelStr = sceneName.Substring(6); // Remove "Level "
            if (int.TryParse(levelStr, out int level))
            {
                return level;
            }
        }

        // Last fallback: use build index with offset
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex >= 2)
        {
            return buildIndex - 1; // Level 1 scene (build index 2) = level 1
        }

        return 1; // Default fallback
    }

    public void StartDialogue(string[] lines, string[] speakers = null, Sprite[] portraits = null, bool isStartDialogue = true)
    {
        Debug.Log($"[Dialogue] StartDialogue called with {lines?.Length ?? 0} lines, isStartDialogue={isStartDialogue}, isForcedEndDialogue={isForcedEndDialogue}");

        dialogueQueue.Clear();
        speakerQueue.Clear();
        portraitQueue.Clear();

        foreach (string line in lines)
        {
            dialogueQueue.Enqueue(line);
            Debug.Log($"[Dialogue] Added line: '{line}'");
        }

        if (speakers != null)
        {
            foreach (string speaker in speakers)
            {
                speakerQueue.Enqueue(speaker);
            }
        }

        if (portraits != null)
        {
            foreach (Sprite portrait in portraits)
            {
                portraitQueue.Enqueue(portrait);
            }
        }

        // Set dialogue type flags
        isShowingStartDialogue = isStartDialogue;
        isShowingEndDialogue = !isStartDialogue;

        // Hide any tutorial panels before showing dialogue
        HideTutorialPanels();

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("[Dialogue] Dialogue panel activated");
        }
        else
        {
            Debug.LogError("[Dialogue] Dialogue panel is null!");
        }

        // PAUSE THE GAME during dialogue
        Time.timeScale = 0f;
        Debug.Log("[Dialogue] Game paused during dialogue (Time.timeScale = 0)");

        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        string line = dialogueQueue.Dequeue();
        string speaker = speakerQueue.Count > 0 ? speakerQueue.Dequeue() : "Narrator";
        Sprite portrait = portraitQueue.Count > 0 ? portraitQueue.Dequeue() : null;

        if (speakerNameText != null)
            speakerNameText.text = speaker;

        if (speakerPortrait != null && portrait != null)
            speakerPortrait.sprite = portrait;

        if (currentTypingCoroutine != null)
            StopCoroutine(currentTypingCoroutine);

        currentTypingCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        skipRequested = false;

        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            if (skipRequested)
            {
                dialogueText.text = text;
                break;
            }

            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed); // Use realtime instead of regular time
        }

        isTyping = false;

        // Auto-advance after delay, but allow clicking to skip the wait
        float timer = 0f;
        while (timer < autoAdvanceDelay)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled delta time
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                break;
            }
            yield return null;
        }

        DisplayNextLine();
    }

    public void SkipDialogue()
    {
        if (isTyping)
        {
            skipRequested = true;
        }
        else
        {
            // Skip entire dialogue sequence
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // RESUME THE GAME after dialogue
        Time.timeScale = 1f;
        Debug.Log("[Dialogue] Game resumed after dialogue (Time.timeScale = 1)");

        int currentLevel = GetCurrentLevel();

        // Mark appropriate dialogue as seen based on context
        if (isShowingStartDialogue)
        {
            // Mark level start dialogue as seen
            string levelStartKey = $"Level{currentLevel}_StartDialogueSeen";
            PlayerPrefs.SetInt(levelStartKey, 1);
            PlayerPrefs.Save();

            Debug.Log($"[Dialogue] Level {currentLevel} start dialogue completed");

            // After start dialogue completes, start the tutorial if it's Level 1 and hasn't been completed
            if (currentLevel == 1)
            {
                StartTutorialIfNeeded();
            }
        }
        else if (isShowingEndDialogue)
        {
            // Only mark as seen if this isn't a forced tutorial victory dialogue
            if (!isForcedEndDialogue)
            {
                string levelEndKey = $"Level{currentLevel}_EndDialogueSeen";
                PlayerPrefs.SetInt(levelEndKey, 1);
                PlayerPrefs.Save();
            }

            Debug.Log($"[Dialogue] Level {currentLevel} end dialogue completed (forced: {isForcedEndDialogue})");

            // If this was a forced tutorial victory dialogue, start actual gameplay
            if (isForcedEndDialogue && currentLevel == 1)
            {
                StartActualGameplayAfterTutorialVictory();
            }
        }

        // Reset flags
        isShowingStartDialogue = false;
        isShowingEndDialogue = false;
        isForcedEndDialogue = false;
    }

    private void HideTutorialPanels()
    {
        Debug.Log("[Dialogue] HideTutorialPanels called");
        // Find and hide any tutorial panels before showing dialogue
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            Debug.Log($"[Dialogue] Found TutorialManager, enabled={tutorialManager.enabled}");
            // Hide tutorial dialogue panel if it exists
            if (tutorialManager.dialoguePanel != null)
            {
                tutorialManager.dialoguePanel.SetActive(false);
                Debug.Log("[Dialogue] Tutorial panel hidden before showing dialogue");
            }
            else
            {
                Debug.Log("[Dialogue] TutorialManager.dialoguePanel is null");
            }

            // Hide tutorial skip button if it exists
            if (tutorialManager.SkipButton != null)
            {
                tutorialManager.SkipButton.SetActive(false);
                Debug.Log("[Dialogue] Tutorial skip button hidden before showing dialogue");
            }
            else
            {
                Debug.Log("[Dialogue] TutorialManager.SkipButton is null");
            }
        }
        else
        {
            Debug.Log("[Dialogue] TutorialManager not found in scene");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void StartTutorialIfNeeded()
    {
        // Check if tutorial needs to run
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        string currentSceneName = SceneManager.GetActiveScene().name;

        Debug.Log($"[Dialogue] Checking tutorial: completed={hasCompletedTutorial}, buildIndex={currentBuildIndex}, sceneName={currentSceneName}");

        if (!hasCompletedTutorial && GetCurrentLevel() == 1) // Check by level number instead of build index
        {
            // Find and enable the TutorialManager
            TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager != null)
            {
                Debug.Log($"[Dialogue] Found TutorialManager, enabled={tutorialManager.enabled}, calling StartTutorialAfterDialogue");
                tutorialManager.enabled = true;
                tutorialManager.StartTutorialAfterDialogue();
                Debug.Log("[Dialogue] StartTutorialAfterDialogue called successfully");
            }
            else
            {
                Debug.LogError("[Dialogue] TutorialManager not found in scene! Make sure TutorialManager component exists in Level 1 scene.");
            }
        }
        else
        {
            Debug.Log($"[Dialogue] Tutorial not started: completed={hasCompletedTutorial}, level={GetCurrentLevel()}");
        }
    }

    private void StartActualGameplayAfterTutorialVictory()
    {
        // Find TutorialManager and start actual gameplay
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            tutorialManager.StartActualGameplayAfterVictoryDialogue();
        }
        else
        {
            Debug.LogError("[Dialogue] TutorialManager not found to start actual gameplay!");
        }
    }

    private void Update()
    {
        // Allow clicking to advance dialogue (works even when Time.timeScale = 0)
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&
            dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (isTyping)
            {
                skipRequested = true;
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    private IEnumerator ShowLevelStartDialogue(int levelNumber)
    {
        yield return new WaitForSeconds(1f); // Brief delay before starting

        LevelDialogue dialogue = GetLevelDialogue(levelNumber);
        if (dialogue != null && dialogue.startDialogueLines.Length > 0)
        {
            StartDialogue(dialogue.startDialogueLines, dialogue.startSpeakerNames, dialogue.startPortraits);
        }
    }

    private LevelDialogue GetLevelDialogue(int levelNumber)
    {
        foreach (LevelDialogue dialogue in levelDialogues)
        {
            if (dialogue.levelNumber == levelNumber)
            {
                return dialogue;
            }
        }
        return null;
    }

    // Public method to show end-of-level dialogue
    public void ShowLevelEndDialogue(int levelNumber)
    {
        // Check if end dialogue for this level has been seen
        string levelEndKey = $"Level{levelNumber}_EndDialogueSeen";
        bool hasSeenLevelEnd = PlayerPrefs.GetInt(levelEndKey, 0) == 1;

        if (!hasSeenLevelEnd)
        {
            LevelDialogue dialogue = GetLevelDialogue(levelNumber);
            if (dialogue != null && dialogue.endDialogueLines.Length > 0)
            {
                StartCoroutine(ShowLevelEndDialogueCoroutine(dialogue.endDialogueLines, dialogue.endSpeakerNames, dialogue.endPortraits));
            }
        }
    }

    // Public method to force show end-of-level dialogue (for tutorial completion)
    public void ShowLevelEndDialogueForced(int levelNumber)
    {
        // Check if UI components are assigned
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("[Dialogue] Cannot show victory dialogue - UI components not assigned in DialogueManager!");
            // Fallback: try to start gameplay directly
            StartActualGameplayAfterTutorialVictory();
            return;
        }

        LevelDialogue dialogue = GetLevelDialogue(levelNumber);
        if (dialogue != null && dialogue.endDialogueLines.Length > 0)
        {
            StartCoroutine(ShowLevelEndDialogueForcedCoroutine(dialogue.endDialogueLines, dialogue.endSpeakerNames, dialogue.endPortraits));
        }
        else
        {
            Debug.LogWarning("[Dialogue] No victory dialogue found, starting gameplay directly");
            StartActualGameplayAfterTutorialVictory();
        }
    }

    private IEnumerator ShowLevelEndDialogueForcedCoroutine(string[] lines, string[] speakers, Sprite[] portraits)
    {
        yield return new WaitForSeconds(1f); // Brief delay

        Debug.Log($"[Dialogue] Showing forced end dialogue with {lines.Length} lines");

        isForcedEndDialogue = true;
        StartDialogue(lines, speakers, portraits, false); // false = end dialogue
    }

    private IEnumerator ShowLevelEndDialogueCoroutine(string[] lines, string[] speakers, Sprite[] portraits)
    {
        yield return new WaitForSeconds(2f); // Brief delay after level completion

        StartDialogue(lines, speakers, portraits, false); // false = end dialogue
    }

    // Public method to trigger intro manually (for testing)
    public void PlayIntro()
    {
        // Reset level 1 start dialogue flag
        string levelStartKey = $"Level{1}_StartDialogueSeen";
        PlayerPrefs.SetInt(levelStartKey, 0);
        StartCoroutine(ShowLevelStartDialogue(1));
    }

    // Public method to check if dialogue is currently active
    public bool IsDialogueActive()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    // Public method to show custom dialogue
    public void ShowCustomDialogue(string[] lines, string speakerName = "Narrator", Sprite portrait = null)
    {
        string[] speakers = new string[lines.Length];
        Sprite[] portraits = new Sprite[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            speakers[i] = speakerName;
            portraits[i] = portrait;
        }

        StartDialogue(lines, speakers, portraits);
    }
}
