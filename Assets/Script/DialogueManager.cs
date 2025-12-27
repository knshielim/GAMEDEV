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
    private bool isShowingDialogue = false;
    private Coroutine currentTypingCoroutine;
    private bool isShowingStartDialogue = false;
    private bool isShowingEndDialogue = false;
    private bool isForcedEndDialogue = false;
    private bool hasCheckedIntroForCurrentScene = false;
    private bool sceneChangedDuringDialogue = false;

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
        Debug.Log($"[Dialogue] 📦 OnSceneLoaded called for scene '{scene.name}' at {Time.time}s");

        // ✅ FIX: Set flag to indicate scene changed - dialogue coroutines should check this
        sceneChangedDuringDialogue = true;

        // ✅ CRITICAL FIX: Don't update UI references if dialogue is currently active
        // This prevents interrupting active dialogue during scene transitions
        if (IsDialogueActive())
        {
            Debug.Log($"[Dialogue] ⏸️ Skipping UI component update - dialogue is currently active");
            return;
        }

        FindUIComponents();

        // Reset the intro check flag for the new scene
        Debug.Log($"[Dialogue] 📦 Resetting hasCheckedIntroForCurrentScene to false for scene '{scene.name}'");
        hasCheckedIntroForCurrentScene = false;

        // ✅ FIX: Don't try to show dialogue in scenes without UI components (like MainMenu)
        if (scene.name == "MainMenu" || scene.name == "LevelSelect")
        {
            Debug.Log($"[Dialogue] Skipping dialogue check for menu scene: {scene.name}");
            return;
        }

        // ✅ FIX: For level scenes, check if intro dialogue is needed
        // (Start() only runs once for DontDestroyOnLoad objects)
        // But only if UI components are properly initialized
        Debug.Log($"[Dialogue] 📦 OnSceneLoaded - checking UI readiness: Panel={dialoguePanel}, Text={dialogueText}, Speaker={speakerNameText}, Portrait={speakerPortrait}");
        if (dialoguePanel != null && dialogueText != null)
        {
            Debug.Log($"[Dialogue] 📦 About to call CheckIfIntroNeeded() for scene '{scene.name}'");
            CheckIfIntroNeeded();
        }
        else
        {
            Debug.LogWarning($"[Dialogue] ⚠️ UI components not ready yet for scene '{scene.name}' - Panel: {dialoguePanel}, Text: {dialogueText}");
        }
    }

    private void FindUIComponents()
    {
        if (dialoguePanel == null)
        {
            GameObject panelObj = GameObject.Find("DialoguePanel");
            if (panelObj != null)
            {
                dialoguePanel = panelObj;
                Debug.Log("[Dialogue] Found DialoguePanel in scene");
            }
            else
            {
                Debug.LogError("[Dialogue] ❌ CRITICAL: DialoguePanel not found in scene!");
            }
        }

        if (dialogueText == null)
        {
            dialogueText = GameObject.Find("DialogueText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (dialogueText != null)
                Debug.Log("[Dialogue] Found DialogueText in scene");
            else
                Debug.LogError("[Dialogue] ❌ CRITICAL: DialogueText not found!");
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

        // ✅ IMPROVED: DialoguePanel should now be a child of the main Canvas in each scene
        if (dialoguePanel != null)
        {
            // Ensure the panel is active and properly parented under the main Canvas
            dialoguePanel.SetActive(false); // Start hidden

            // Check if it's properly parented
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log($"[Dialogue] ✅ DialoguePanel is child of Canvas: {parentCanvas.name}");

                // ✅ FIX: Add Canvas Group to block raycasts on UI elements behind
                CanvasGroup canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
                canvasGroup.blocksRaycasts = true; // This should block clicks to buttons behind
                Debug.Log($"[Dialogue] ✅ Set DialoguePanel blocksRaycasts: {canvasGroup.blocksRaycasts}");
            }
            else
            {
                Debug.LogWarning("[Dialogue] ⚠️ DialoguePanel is not a child of any Canvas!");
            }
        }
    }

    private void Start()
    {
        Debug.Log($"[Dialogue] 🚀 Start() called at {Time.time}s - Instance: {Instance}, GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        InitializeDialogueSystem();
        FindUIComponents();
        Debug.Log($"[Dialogue] 🚀 About to call CheckIfIntroNeeded() in scene '{gameObject.scene.name}'");
        CheckIfIntroNeeded();
    }

    private void InitializeDialogueSystem()
    {
        dialogueQueue = new Queue<string>();
        speakerQueue = new Queue<string>();
        portraitQueue = new Queue<Sprite>();

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            Debug.Log("[Dialogue] DialoguePanel initially hidden");
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipDialogue);
    }

    private void CheckIfIntroNeeded()
    {
        Debug.Log($"[Dialogue] 🔍 CheckIfIntroNeeded() called at {Time.time}s - Instance: {Instance}, GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        Debug.Log($"[Dialogue] 🔍 hasCheckedIntroForCurrentScene: {hasCheckedIntroForCurrentScene}");

        // Prevent duplicate calls in the same scene
        if (hasCheckedIntroForCurrentScene)
        {
            Debug.Log($"[Dialogue] ⏭️ Skipping duplicate CheckIfIntroNeeded call for current scene");
            return;
        }

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogWarning("[Dialogue] ⚠️ Dialogue UI components not assigned! Skipping dialogue.");
            return;
        }

        int currentLevel = GetCurrentLevel();

        if (currentLevel >= 1 && currentLevel <= 5)
        {
            Debug.Log($"[Dialogue] 🎬 Level {currentLevel} loaded - showing dialogue sequence (ID: {System.Guid.NewGuid().ToString().Substring(0, 8)})");

            // Get the dialogue data to verify it's correct
            LevelDialogue dialogue = GetLevelDialogue(currentLevel);
            if (dialogue != null)
            {
                Debug.Log($"[Dialogue] ✅ Found dialogue for Level {currentLevel}: {dialogue.startDialogueLines.Length} start lines, {dialogue.endDialogueLines.Length} end lines");
            }
            else
            {
                Debug.LogError($"[Dialogue] ❌ No dialogue found for Level {currentLevel}!");
            }

            // Show the intro/start dialogue sequence for this level
            StartCoroutine(ShowLevelStartDialogueSequence(currentLevel));
            hasCheckedIntroForCurrentScene = true;
        }
        else
        {
            Debug.Log($"[Dialogue] Current level {currentLevel} is outside supported range (1-5), no dialogue will be shown");
            hasCheckedIntroForCurrentScene = true;
        }
    }

    private IEnumerator ShowLevelStartDialogueSequence(int levelNumber)
    {
        Debug.Log($"[Dialogue] Starting START dialogue sequence for Level {levelNumber}");

        LevelDialogue dialogue = GetLevelDialogue(levelNumber);
        if (dialogue == null)
        {
            Debug.LogError($"[Dialogue] No dialogue data found for Level {levelNumber}!");
            yield break;
        }

        // SPECIAL HANDLING FOR LEVEL 1
        if (levelNumber == 1)
        {
            yield return StartCoroutine(HandleLevel1StartSequence(dialogue));
            yield break;
        }

        // NORMAL HANDLING FOR OTHER LEVELS
        // Show ONLY start dialogue
        if (dialogue.startDialogueLines.Length > 0)
        {
            Debug.Log($"[Dialogue] Showing start dialogue ({dialogue.startDialogueLines.Length} lines)");
            StartDialogue(dialogue.startDialogueLines, dialogue.startSpeakerNames, dialogue.startPortraits, true);

            // Wait for start dialogue to complete
            yield return new WaitUntil(() => !isShowingDialogue);
            Debug.Log("[Dialogue] Start dialogue completed");

            // Mark start dialogue as seen
            string levelStartKey = $"Level{levelNumber}_StartDialogueSeen";
            PlayerPrefs.SetInt(levelStartKey, 1);
            PlayerPrefs.Save();
        }

        Debug.Log($"[Dialogue] Start dialogue sequence completed for Level {levelNumber} - gameplay will begin");

        // Start gameplay after intro dialogue
        StartCoroutine(StartGameplayAfterDialogue());
    }

    private IEnumerator HandleLevel1StartSequence(LevelDialogue dialogue)
    {
        Debug.Log("[Dialogue] Handling special Level 1 START sequence");

        // STEP 1: Show Level 1 start dialogue
        if (dialogue.startDialogueLines.Length > 0)
        {
            Debug.Log($"[Dialogue] Showing Level 1 start dialogue ({dialogue.startDialogueLines.Length} lines)");
            StartDialogue(dialogue.startDialogueLines, dialogue.startSpeakerNames, dialogue.startPortraits, true);

            // Wait for start dialogue to complete
            yield return new WaitUntil(() => !isShowingDialogue);
            Debug.Log("[Dialogue] Level 1 start dialogue completed");

            // Mark start dialogue as seen
            PlayerPrefs.SetInt("Level1_StartDialogueSeen", 1);
            PlayerPrefs.Save();
        }

        // STEP 2: Always start tutorial for Level 1 (reset completion status first)
        Debug.Log("[Dialogue] Resetting tutorial completion status for Level 1");
        PlayerPrefs.SetInt("TutorialCompleted", 0); // Reset so tutorial can run again
        PlayerPrefs.Save();

        Debug.Log("[Dialogue] Starting tutorial for Level 1");
        StartTutorialIfNeeded();

        // Wait for tutorial to complete (it will disable itself when done)
        yield return new WaitUntil(() => PlayerPrefs.GetInt("TutorialCompleted", 0) == 1);
        Debug.Log("[Dialogue] Tutorial completed - proceeding to real Level 1 gameplay");

        // STEP 3: Start actual Level 1 gameplay
        StartCoroutine(StartGameplayAfterDialogue());
    }

    private IEnumerator StartGameplayAfterDialogue()
    {
        Debug.Log("[Dialogue] Starting gameplay after dialogue");
        yield return new WaitForSecondsRealtime(0.5f);

        // Ensure game is running
        Time.timeScale = 1f;

        // Find TutorialManager and start gameplay if it exists
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            tutorialManager.StartActualGameplayDirectly();
        }
        else
        {
            Debug.Log("[Dialogue] No TutorialManager found, gameplay should start normally");
        }
    }

    private int GetCurrentLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        Debug.Log($"[Dialogue] GetCurrentLevel() - Scene: '{sceneName}', BuildIndex: {buildIndex}");

        // PRIORITY 1: Parse scene name (most reliable)
        if (sceneName.StartsWith("Level "))
        {
            string levelStr = sceneName.Substring(6);
            if (int.TryParse(levelStr, out int level))
            {
                Debug.Log($"[Dialogue] Level determined from scene name: {level}");
                return level;
            }
        }

        // PRIORITY 2: Build index fallback (Level 1 = build index 2, etc.)
        if (buildIndex >= 2)
        {
            int levelFromBuildIndex = buildIndex - 1;
            Debug.Log($"[Dialogue] Level determined from build index: {levelFromBuildIndex}");
            return levelFromBuildIndex;
        }

        // PRIORITY 3: LevelManager instance (if available and reliable)
        if (LevelManager.Instance != null)
        {
            int levelFromManager = LevelManager.Instance.GetCurrentLevel();
            Debug.Log($"[Dialogue] Level determined from LevelManager: {levelFromManager}");
            return levelFromManager;
        }

        // PRIORITY 4: GameManager instance (least reliable - often defaults to Level1)
        if (GameManager.Instance != null)
        {
            int levelFromGameManager = (int)GameManager.Instance.currentLevel;
            Debug.Log($"[Dialogue] Level determined from GameManager: {levelFromGameManager}");
            return levelFromGameManager;
        }

        // FALLBACK: Default to level 1
        Debug.LogWarning("[Dialogue] Could not determine level from any source, defaulting to Level 1");
        return 1;
    }

    public void StartDialogue(string[] lines, string[] speakers = null, Sprite[] portraits = null, bool isStartDialogue = true)
    {
        Debug.Log($"[Dialogue] 🎬 StartDialogue called with {lines?.Length ?? 0} lines at {Time.time}s (ID: {System.Guid.NewGuid().ToString().Substring(0, 8)})");

        dialogueQueue.Clear();
        speakerQueue.Clear();
        portraitQueue.Clear();

        foreach (string line in lines)
        {
            dialogueQueue.Enqueue(line);
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

        isShowingStartDialogue = isStartDialogue;
        isShowingEndDialogue = !isStartDialogue;
        isShowingDialogue = true;

        HideTutorialPanels();

        // Show skip button for dialogue
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(true);
            skipButton.interactable = true;
            Debug.Log($"[Dialogue] Skip button activated for dialogue - Active: {skipButton.gameObject.activeSelf}, Interactable: {skipButton.interactable}");
        }
        else
        {
            Debug.LogError("[Dialogue] Skip button is NULL - cannot show skip functionality!");
        }

        if (dialoguePanel != null && dialogueText != null)
        {
            Debug.Log($"[Dialogue] 🎬 Activating dialogue panel - current state: {dialoguePanel.activeSelf}");

            // ✅ CRITICAL FIX: Force panel to be active and visible
            dialoguePanel.SetActive(true);

            // Ensure it's at the root level or has proper parent
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log($"[Dialogue] ✅ Parent Canvas found: {parentCanvas.name}, renderMode: {parentCanvas.renderMode}");
                // Also ensure parent canvas is active
                if (!parentCanvas.gameObject.activeSelf)
                {
                    parentCanvas.gameObject.SetActive(true);
                    Debug.Log($"[Dialogue] ✅ Activated parent Canvas: {parentCanvas.name}");
                }
            }

            // Force refresh layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanel.GetComponent<RectTransform>());

            Debug.Log($"[Dialogue] ✅ Dialogue panel activated! Active: {dialoguePanel.activeSelf}, Position: {dialoguePanel.transform.position}");
        }
        else
        {
            Debug.LogError($"[Dialogue] ❌ CRITICAL ERROR: Dialogue components missing - Panel: {dialoguePanel}, Text: {dialogueText}");
            // Reset dialogue state since we can't start
            isShowingDialogue = false;
            isShowingStartDialogue = false;
            isShowingEndDialogue = false;
            // Reset the intro check flag so it can be retried when components are ready
            hasCheckedIntroForCurrentScene = false;
            return;
        }

        Time.timeScale = 0f;
        Debug.Log("[Dialogue] ⏸️  Game paused (Time.timeScale = 0)");

        Debug.Log($"[Dialogue] 🎬 Calling DisplayNextLine() with {dialogueQueue.Count} lines in queue");
        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        Debug.Log($"[Dialogue] 📝 DisplayNextLine called, queue has {dialogueQueue.Count} lines");

        if (dialogueQueue.Count == 0)
        {
            Debug.Log($"[Dialogue] 📝 No more lines, ending dialogue");
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
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;

        // Wait for spacebar input to advance (no auto-advance)
        Debug.Log("[Dialogue] Line typing complete - press SPACEBAR to continue to next line");

        // Wait indefinitely for spacebar
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DisplayNextLine();
                break;
            }
            yield return null;
        }
    }

    public void SkipDialogue()
    {
        Debug.Log($"[Dialogue] SkipDialogue called - isTyping: {isTyping}, dialoguePanel active: {dialoguePanel?.activeSelf}");

        if (isTyping)
        {
            skipRequested = true;
            Debug.Log("[Dialogue] Skip requested - will skip typing animation");
        }
        else
        {
            Debug.Log("[Dialogue] Not typing - ending dialogue");
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Hide skip button
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
        Debug.Log("[Dialogue] ▶️  Game resumed (Time.timeScale = 1)");

        int currentLevel = GetCurrentLevel();

        if (isShowingStartDialogue)
        {
            string levelStartKey = $"Level{currentLevel}_StartDialogueSeen";
            PlayerPrefs.SetInt(levelStartKey, 1);
            PlayerPrefs.Save();

            Debug.Log($"[Dialogue] Level {currentLevel} start dialogue completed");

            if (currentLevel == 1)
            {
                StartTutorialIfNeeded();
            }
        }
        else if (isShowingEndDialogue)
        {
            if (!isForcedEndDialogue)
            {
                string levelEndKey = $"Level{currentLevel}_EndDialogueSeen";
                PlayerPrefs.SetInt(levelEndKey, 1);
                PlayerPrefs.Save();
            }

            Debug.Log($"[Dialogue] Level {currentLevel} end dialogue completed (forced: {isForcedEndDialogue})");

            if (isForcedEndDialogue && currentLevel == 1)
            {
                StartActualGameplayAfterTutorialVictory();
            }
        }

        isShowingStartDialogue = false;
        isShowingEndDialogue = false;
        isForcedEndDialogue = false;
        isShowingDialogue = false;
    }

    private void HideTutorialPanels()
    {
        Debug.Log("[Dialogue] HideTutorialPanels called");
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            Debug.Log($"[Dialogue] Found TutorialManager, enabled={tutorialManager.enabled}");
            if (tutorialManager.dialoguePanel != null)
            {
                tutorialManager.dialoguePanel.SetActive(false);
                Debug.Log("[Dialogue] Tutorial panel hidden");
            }

            if (tutorialManager.SkipButton != null)
            {
                tutorialManager.SkipButton.SetActive(false);
                Debug.Log("[Dialogue] Tutorial skip button hidden");
            }
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
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        string currentSceneName = SceneManager.GetActiveScene().name;

        Debug.Log($"[Dialogue] Checking tutorial: completed={hasCompletedTutorial}, buildIndex={currentBuildIndex}, sceneName={currentSceneName}");

        if (!hasCompletedTutorial && GetCurrentLevel() == 1)
        {
            TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager != null)
            {
                Debug.Log($"[Dialogue] Found TutorialManager, calling StartTutorialAfterDialogue");
                tutorialManager.enabled = true;
                tutorialManager.StartTutorialAfterDialogue();
            }
            else
            {
                Debug.LogError("[Dialogue] TutorialManager not found!");
            }
        }
    }

    private void StartActualGameplayAfterTutorialVictory()
    {
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
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&
            dialoguePanel != null && dialoguePanel.activeSelf)
        {
            // Don't respond to input if this is tutorial completion message
            if (dialogueText != null && dialogueText.text == "Tutorial Completed!")
            {
                Debug.Log("[Dialogue] Ignoring input - tutorial completion message showing");
                return;
            }

            if (isTyping)
            {
                skipRequested = true;
                Debug.Log("[Dialogue] Skipping typing animation");
            }
            // Note: Line advancement is now handled in TypeText coroutine
        }
    }

    private IEnumerator ShowLevelStartDialogue(int levelNumber)
    {
        yield return new WaitForSeconds(1f);

        LevelDialogue dialogue = GetLevelDialogue(levelNumber);
        if (dialogue != null && dialogue.startDialogueLines.Length > 0)
        {
            Debug.Log($"[Dialogue] 🎬 Starting level {levelNumber} intro dialogue");
            StartDialogue(dialogue.startDialogueLines, dialogue.startSpeakerNames, dialogue.startPortraits);
        }
    }

    private LevelDialogue GetLevelDialogue(int levelNumber)
    {
        // Direct access to each level's dialogue (no searching)
        switch (levelNumber)
        {
            case 1:
                return (levelDialogues.Length >= 1 && levelDialogues[0].levelNumber == 1) ? levelDialogues[0] : null;
            case 2:
                return (levelDialogues.Length >= 2 && levelDialogues[1].levelNumber == 2) ? levelDialogues[1] : null;
            case 3:
                return (levelDialogues.Length >= 3 && levelDialogues[2].levelNumber == 3) ? levelDialogues[2] : null;
            case 4:
                return (levelDialogues.Length >= 4 && levelDialogues[3].levelNumber == 4) ? levelDialogues[3] : null;
            case 5:
                return (levelDialogues.Length >= 5 && levelDialogues[4].levelNumber == 5) ? levelDialogues[4] : null;
            default:
                Debug.LogError($"[Dialogue] Invalid level number: {levelNumber}");
                return null;
        }
    }

    public void ShowLevelEndDialogue(int levelNumber)
    {
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

    public void ShowLevelEndDialogueForced(int levelNumber)
    {
        Debug.Log($"[Dialogue] ShowLevelEndDialogueForced called for level {levelNumber}");
        
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("[Dialogue] ❌ Cannot show victory dialogue - UI components not assigned!");
            StartActualGameplayAfterTutorialVictory();
            return;
        }

        LevelDialogue dialogue = GetLevelDialogue(levelNumber);
        if (dialogue != null && dialogue.endDialogueLines.Length > 0)
        {
            Debug.Log($"[Dialogue] ShowLevelEndDialogueForcedCoroutine starting with {dialogue.endDialogueLines.Length} lines");
            StartCoroutine(ShowLevelEndDialogueForcedCoroutine(dialogue.endDialogueLines, dialogue.endSpeakerNames, dialogue.endPortraits, levelNumber));
        }
        else
        {
            Debug.LogWarning("[Dialogue] No victory dialogue found, starting gameplay directly");
            StartActualGameplayAfterTutorialVictory();
        }
    }

    private IEnumerator ShowLevelEndDialogueForcedCoroutine(string[] lines, string[] speakers, Sprite[] portraits, int levelNumber)
    {
        Debug.Log($"[Dialogue] ShowLevelEndDialogueForcedCoroutine starting with {lines?.Length ?? 0} lines for level {levelNumber}");

        // Reset the scene change flag at the start
        sceneChangedDuringDialogue = false;

        // ✅ FIX: For victory dialogue, start immediately without delay
        // The 1-second delay was causing the Tower to think dialogue completed before it started
        Debug.Log($"[Dialogue] Showing forced end dialogue with {lines.Length} lines");

        isForcedEndDialogue = true;
        StartDialogue(lines, speakers, portraits, false);

        Debug.Log("[Dialogue] StartDialogue called for forced end dialogue");

        // Wait for dialogue to complete
        yield return new WaitUntil(() => !isShowingDialogue || sceneChangedDuringDialogue);

        // If scene changed, don't mark as seen or do anything else
        if (sceneChangedDuringDialogue)
        {
            Debug.Log($"[Dialogue] ⏹️ Scene changed during dialogue - aborting completion for Level {levelNumber}");
            yield break;
        }

        // Mark end dialogue as seen
        string levelEndKey = $"Level{levelNumber}_EndDialogueSeen";
        PlayerPrefs.SetInt(levelEndKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Dialogue] Marked Level {levelNumber} end dialogue as seen");
    }

    private IEnumerator ShowLevelEndDialogueCoroutine(string[] lines, string[] speakers, Sprite[] portraits)
    {
        yield return new WaitForSeconds(2f);

        StartDialogue(lines, speakers, portraits, false);
    }

    public void PlayIntro()
    {
        string levelStartKey = $"Level{1}_StartDialogueSeen";
        PlayerPrefs.SetInt(levelStartKey, 0);
        StartCoroutine(ShowLevelStartDialogue(1));
    }

    public bool IsDialogueActive()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

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