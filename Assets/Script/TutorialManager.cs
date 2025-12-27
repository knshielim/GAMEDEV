using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 6)] public string text;
        public Sprite image;
    }

    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public Image dialogueImageHolder;

    [Header("Tutorial Steps")]
    public TutorialStep[] steps;

    [Header("Controls")]
    public KeyCode continueKey = KeyCode.Space;

    [Header("Debug")]
    public bool TutorialDebug = false;

    [Header("Enemy Spawn")]
    public TroopData enemyToSpawn;
    public TroopData enemyToSpawn2;
    public EnemyDeployManager enemyDeployManager;
    public GachaManager gachaManager;

    [Header("Buttons to Block (Summon + Deploy)")]
    public Button[] playerButtons;

    [Header("Tutorial Troops")]
    public TroopData tutorialFirstTroop;
    public TroopData tutorialSecondTroop;
    public GameObject SkipButton;

    private bool waitingForSecondSummon = false;
    private int secondSummonCount = 0;
    private bool gaveFirstTroop = false;
    private int currentStep = 0;
    private bool canAdvance = true;
    private bool waitingForSummon = false;
    private bool waitingForDeploy = false;
    private bool waitingForMerge = false;
    private bool mergeCompleted = false;
    public bool tutorialActive = true;
    private bool waitingForSlotClick = false;
    private bool showingTutorialCompletion = false;

    private static bool tutorialShownThisSession = false;

    // --- NEW: track the pause coroutine so we can cancel it if needed
    private Coroutine pauseCoroutine;

    private void Awake()
    {
        if (SkipButton != null) SkipButton.SetActive(false);

        // --- Debug to always show tutorial
        if (TutorialDebug && !tutorialShownThisSession)
        {
            tutorialActive = true;
            tutorialShownThisSession = true;
            EnemyDeployManager.tutorialActive = true;
            if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = true;
            SetPlayerButtons(false);
            if (SkipButton != null) SkipButton.SetActive(true);
            return;
        }

        // --- NORMAL MODE: Use PlayerPrefs ---
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        if (hasCompletedTutorial || !tutorialActive)
        {
            tutorialActive = false;
            EnemyDeployManager.tutorialActive = false;
            if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;
            this.enabled = false;
            return;
        }

        // For Level 1, don't start tutorial yet - wait for dialogue to complete
        // For other levels, tutorial should run normally
        int currentLevel = GetCurrentLevel();
        if (currentLevel == 1)
        {
            Debug.Log("[TutorialManager] Level 1 detected - waiting for dialogue completion before starting tutorial");
            tutorialActive = false; // Don't start yet
            EnemyDeployManager.tutorialActive = false;
            if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;
            // Stay enabled so we can start later
            return;
        }

        // For other levels, tutorial should run normally
        tutorialActive = true;
        EnemyDeployManager.tutorialActive = true;
        if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = true;
        SetPlayerButtons(false);
        if (SkipButton != null) SkipButton.SetActive(true);
    }

    private void Start()
    {
        if (enemyDeployManager == null) enemyDeployManager = FindObjectOfType<EnemyDeployManager>();

        // Debugging mode
        bool hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        if (hasCompletedTutorial && !TutorialDebug)
        {
            Debug.Log("[TutorialManager] Tutorial already completed. Auto-disabling.");
            tutorialActive = false;
            if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;
            this.enabled = false;
            return;
        }
        CoinManager.Instance.AddPlayerCoins(5);
        ShowStep();
    }

    private void Update()
    {
        EnemyDeployManager.tutorialActive = tutorialActive;
        if (waitingForSecondSummon) return;
        if (waitingForSummon) return;
        if (waitingForDeploy) return;
        if (!canAdvance) return;

        if (Input.GetKeyDown(continueKey))
        {
            // Don't advance tutorial if DialogueManager is currently showing dialogue
            bool dialogueManagerActive = false;
            if (DialogueManager.Instance != null)
            {
                dialogueManagerActive = DialogueManager.Instance.IsDialogueActive();
            }

            if (!dialogueManagerActive)
            {
                Advance();
            }
            else
            {
                Debug.Log("[Tutorial] Not advancing - DialogueManager is active");
            }
        }
    }

    // changed to optional pause flag (default true) so caller can show text without pausing
        private void ShowStep(bool pause = true)
    {
        if (currentStep >= steps.Length)
        {
            return;
        }

        var step = steps[currentStep];

        dialoguePanel.SetActive(true);
        dialogueText.text = step.text;

        // Clean image handling
        if (step.image != null)
        {
            dialogueImageHolder.gameObject.SetActive(true);
            dialogueImageHolder.sprite = step.image;
        }
        else
        {
            dialogueImageHolder.gameObject.SetActive(false);
            dialogueImageHolder.sprite = null;
        }

        // Show Skip button only on the first step
        if (SkipButton != null)
            SkipButton.SetActive(currentStep == 0);

        if (pause)
            PauseGame();
    }


    private void Advance()
    {
        if (currentStep == 4)
        {
            EnemyDeployManager.tutorialActive = true;

            // Show the summon instruction first
            currentStep++;
            ShowStep(pause: false); // Show step 5 without pausing

            // Then prepare for summon
            ResumeGame();
            EnableOnly("Summon Button");
            waitingForSummon = true;
            canAdvance = false;
            return;
        }

        currentStep++;
        ShowStep();
    }

    private void GiveFirstTutorialTroop()
    {
        if (!gaveFirstTroop && tutorialFirstTroop != null)
        {
            // -Deduct summon cost
            int cost = GachaManager.Instance.GetCurrentSummonCost();

            bool success = CoinManager.Instance.TrySpendPlayerCoins(cost);
            if (!success)
            {
                Debug.LogWarning("[Tutorial] Player does not have enough coins for the tutorial summon.");
                return;
            }
            GachaManager.Instance.DebugSummonButton(); 
            TroopInventory.Instance.AddTroop(tutorialFirstTroop);
            gaveFirstTroop = true;

            GachaManager.Instance.IncreaseSummonCost();
            GachaManager.Instance.UpdateSummonCostUI();

            Debug.Log("[Tutorial] First tutorial troop granted and cost deducted.");
        }
    }


    private void SetPlayerButtons(bool state)
    {
        foreach (var b in playerButtons)
        {
            if (b == null) continue;
            b.interactable = state;
            Color tint = state ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            foreach (var img in b.GetComponentsInChildren<Image>()) img.color = tint;
            foreach (var txt in b.GetComponentsInChildren<TMP_Text>()) txt.color = tint;
        }
    }

    private void EnableOnly(string buttonName)
    {
        foreach (var b in playerButtons)
        {
            if (b == null) continue;
            bool enable = (b.gameObject.name == buttonName);
            b.interactable = enable;
            Color tint = enable ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            foreach (var img in b.GetComponentsInChildren<Image>()) img.color = tint;
            foreach (var txt in b.GetComponentsInChildren<TMP_Text>()) txt.color = tint;
        }
    }

    private IEnumerator SpawnFirstEnemyAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        enemyDeployManager.SpawnSpecificEnemy(enemyToSpawn, true);
    }

    private IEnumerator SpawnNextEnemySequence()
    {
        EnemyDeployManager.tutorialActive = true;
        yield return new WaitForSecondsRealtime(5f);
        if (enemyToSpawn2 != null) enemyDeployManager.SpawnSpecificEnemy(enemyToSpawn2, true);
        yield return new WaitForSecondsRealtime(2f);
        canAdvance = false;
        dialoguePanel.SetActive(true);
        currentStep++;
        ShowStep();
        yield return new WaitUntil(() => Input.GetKeyDown(continueKey));
        currentStep++;
        ShowStep();
        yield return new WaitUntil(() => Input.GetKeyDown(continueKey));
        ResumeGame();
        CoinManager.Instance.AddPlayerCoins(50);
        EnableOnly("Summon Button");
        waitingForSecondSummon = true;
        secondSummonCount = 0;
        canAdvance = false;
    }

    private IEnumerator ClosePanelOnContinue()
    {
        // Do NOT pause game. Keep playing.
        while (!Input.GetKeyDown(continueKey))
            yield return null;

        dialoguePanel.SetActive(false);
        // Tutorial is done with this section
        SetPlayerButtons(true);
        canAdvance = true;
    }

    public void OnTutorialTowerDestroyed(Tower destroyedTower)
    {
        Debug.Log($"[Tutorial] OnTutorialTowerDestroyed called, tutorialActive = {tutorialActive}");

        if (!tutorialActive)
        {
            Debug.Log("[Tutorial] Tower destroyed but tutorial is not active (probably skipped) - ignoring");
            return;
        }

        Debug.Log("[Tutorial] Tower destroyed during tutorial - showing completion message");

        // Mark tutorial as complete
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        // Save tutorial completion
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Tutorial completed - saved to PlayerPrefs");

        // Show tutorial completion message
        dialoguePanel.SetActive(true);
        dialogueText.text = "Tutorial Completed!";
        dialogueImageHolder.gameObject.SetActive(false);
        if (SkipButton != null)
            SkipButton.SetActive(false);

        // Set flag to prevent DialogueManager from interfering
        showingTutorialCompletion = true;

        // Pause game briefly for the message
        PauseGame();

        // Start gameplay immediately after player presses space
        StartCoroutine(StartGameplayAfterTutorialCompletion());
    }

    private IEnumerator StartGameplayAfterTutorialCompletion()
    {
        Debug.Log("[Tutorial] Waiting for player to press spacebar to acknowledge completion...");

        // Wait for player to acknowledge completion
        yield return new WaitUntil(() => Input.GetKeyDown(continueKey));

        Debug.Log("[Tutorial] Spacebar pressed - player acknowledged tutorial completion");

        // Clear the tutorial completion flag
        showingTutorialCompletion = false;

        // Hide tutorial panel
        dialoguePanel.SetActive(false);

        // Resume game briefly for scene reload
        ResumeGame();
        Debug.Log($"[Tutorial] Game resumed, Time.timeScale = {Time.timeScale}");

        // Small delay before reload
        yield return new WaitForSeconds(0.2f);

        Debug.Log("[Tutorial] Reloading Level 1 scene after tutorial completion");

        // Reset/reload Level 1 scene
        // Now that tutorial is completed, Level 1 will go directly to gameplay
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void SkipTutorial()
    {
        Debug.Log("[Tutorial] Tutorial skipped by player");

        // Disable all tutorial logic
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        // Unlock gacha
        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        // Save skip as completed
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // Show "Tutorial Skipped!" message briefly
        dialoguePanel.SetActive(true);
        dialogueText.text = "Tutorial Skipped!";
        dialogueImageHolder.gameObject.SetActive(false);
        if (SkipButton != null)
            SkipButton.SetActive(false);

        // Pause game briefly for the message
        PauseGame();

        // Reset scene after player acknowledges
        StartCoroutine(ResetSceneAfterSkip());
    }

    private IEnumerator ResetSceneAfterSkip()
    {
        // Wait for player to acknowledge skip
        yield return new WaitUntil(() => Input.GetKeyDown(continueKey));

        Debug.Log("[Tutorial] Player acknowledged tutorial skip - resetting Level 1");

        // Hide tutorial panel
        dialoguePanel.SetActive(false);

        // Resume game briefly for scene reload
        ResumeGame();

        // Small delay before reload
        yield return new WaitForSeconds(0.2f);

        // Reset/reload Level 1 scene
        // Now that tutorial is completed (skipped), Level 1 will go directly to gameplay
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }


    private IEnumerator ShowStoryDialogueAfterTutorial()
    {
        // Small delay to ensure clean transition
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[Tutorial] Starting story dialogue after tutorial completion");

        // Show the story dialogue (DialogueManager will pause the game)
        if (DialogueManager.Instance != null)
        {
            // Get current level (tutorial is always on level 1)
            int currentLevel = 1;
            
            // Show level 1 end dialogue (forced mode for tutorial)
            DialogueManager.Instance.ShowLevelEndDialogueForced(currentLevel);
            
            Debug.Log("[Tutorial] Story dialogue started - DialogueManager will handle pausing");
        }
        else
        {
            Debug.LogError("[Tutorial] DialogueManager not found! Cannot show story dialogue.");
            // Fallback: just reload the scene
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    public void OnTutorialSummonClicked()
    {
        if (waitingForSecondSummon)
        {
            // --- Deduct cost ---
            int cost = GachaManager.Instance.GetCurrentSummonCost();
            bool success = CoinManager.Instance.TrySpendPlayerCoins(cost);

            if (!success)
            {
                Debug.LogWarning("[Tutorial] Not enough coins for second tutorial summons!");
                return;
            }

            // --- Perform actual summon ---
            TroopInventory.Instance.AddTroop(tutorialSecondTroop);

            // Update cost 
            GachaManager.Instance.IncreaseSummonCost();
            GachaManager.Instance.UpdateSummonCostUI();

            secondSummonCount++;

            if (secondSummonCount == 1)
            {
                dialogueText.text = "Good! Summon two more!";
                return;
            }
            if (secondSummonCount == 2)
            {
                dialogueText.text = "Nice! One more!";
                return;
            }
            if (secondSummonCount >= 3)
            {
                waitingForSecondSummon = false;
                StartCoroutine(ShowMergeAfterDelay());
                return;
            }
        }

        if (!waitingForSummon) return;

        waitingForSummon = false;
        ResumeGame();
        GiveFirstTutorialTroop();
        waitingForSlotClick = true;

        EnableOnly("Slot Button");
        canAdvance = false;
    }

    private IEnumerator ShowMergeAfterDelay()
    {
    yield return new WaitForSeconds(0.5f); // delay for animation

    SetPlayerButtons(false);
    EnableOnly("Merge Button");

    dialoguePanel.SetActive(true);
    dialogueText.text = "Great job! Now merge your troops!";

    PauseGame();
    waitingForMerge = true;
    }   


    public void OnTutorialDeployClicked()
    {
        if (!waitingForDeploy) return;

        if (mergeCompleted)
        {
            waitingForDeploy = false;
            mergeCompleted = false;

            dialoguePanel.SetActive(true);
            dialogueText.text = "Great work! Now destroy the towers!";
            dialogueImageHolder.gameObject.SetActive(false);
            ResumeGame();
            StartCoroutine(ClosePanelOnContinue());
            return;
        }
        currentStep++;
        ShowStep(false);

        waitingForDeploy = false;
        SetPlayerButtons(true);
        dialoguePanel.SetActive(false);
        ResumeGame();

        StartCoroutine(SpawnFirstEnemyAfterDelay());
        StartCoroutine(SpawnNextEnemySequence());
    }

    public void OnTutorialMergeClicked()
    {
        if (!waitingForMerge) return;
        waitingForMerge = false;
        mergeCompleted = true;
        dialoguePanel.SetActive(true);
        dialogueText.text = "Excellent! You just merged your troops!";
        PauseGame();
        EnableOnly("Deploy Button");
        waitingForDeploy = true;
    }

    public void OnSlotClicked()
    {
        if (!waitingForSlotClick) return;

        waitingForSlotClick = false;

        EnableOnly("Deploy Button");
        dialoguePanel.SetActive(true);
        dialogueText.text = "Great! Now press Deploy!";

        waitingForDeploy = true;
        PauseGame();
    }

    private void PauseGame()
    {
        if (pauseCoroutine != null)
        {
            StopCoroutine(pauseCoroutine);
            pauseCoroutine = null;
        }
        pauseCoroutine = StartCoroutine(PauseNextFrame());
    }

    private IEnumerator PauseNextFrame()
    {
        yield return null;
        Time.timeScale = 0f;
        pauseCoroutine = null;
    }

    private void ResumeGame()
    {
        // Resume the game normally 
        if (pauseCoroutine != null)
        {
            StopCoroutine(pauseCoroutine);
            pauseCoroutine = null;
        }
        Time.timeScale = 1f;
    }

    public void StartActualGameplayDirectly()
    {
        Debug.Log("[Tutorial] Starting gameplay directly (no dialogue available)");

        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        dialoguePanel.SetActive(false);
        if (SkipButton != null)
            SkipButton.SetActive(false);

        SetPlayerButtons(true);
        Time.timeScale = 1f;

        StartCoroutine(StartActualLevelplay());
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
            string levelStr = sceneName.Substring(6);
            if (int.TryParse(levelStr, out int level))
            {
                return level;
            }
        }

        // Last fallback
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex >= 2)
        {
            return buildIndex - 1; // Level 1 scene (build index 2) = level 1
        }

        return 1; // Default fallback
    }

    public void DisableTutorial()
    {
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
    }

    // Called by DialogueManager after intro dialogue completes
    public void StartTutorialAfterDialogue()
    {
        Debug.Log($"[TutorialManager] StartTutorialAfterDialogue called. tutorialActive={tutorialActive}, TutorialCompleted={PlayerPrefs.GetInt("TutorialCompleted", 0)}");

        // Don't check tutorialActive here - this method is specifically called to start tutorial after dialogue
        // The Awake method might have set tutorialActive, but we override it here

        Debug.Log("[TutorialManager] Starting tutorial after dialogue completion");

        // Ensure we're enabled
        this.enabled = true;

        // Initialize tutorial state (force it active)
        tutorialActive = true;
        EnemyDeployManager.tutorialActive = true;
        if (GachaManager.Instance != null)
        {
            GachaManager.Instance.tutorialLocked = true;
            Debug.Log("[TutorialManager] Gacha locked for tutorial");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] GachaManager not found!");
        }

        SetPlayerButtons(false);
        if (SkipButton != null)
        {
            SkipButton.SetActive(true);
            Debug.Log("[TutorialManager] Skip button enabled");
        }

        // Give starting coins and start tutorial
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddPlayerCoins(5);
            Debug.Log("[TutorialManager] Added 5 coins for tutorial");
        }

        ShowStep();
        Debug.Log("[TutorialManager] Tutorial started - ShowStep() called");
    }

    // Called by DialogueManager after tutorial victory dialogue completes
    public void StartActualGameplayAfterVictoryDialogue()
    {
        Debug.Log("[Tutorial] StartActualGameplayAfterVictoryDialogue called - starting real gameplay");

        // Disable tutorial systems completely
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        // Ensure tutorial UI is hidden
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Start actual level gameplay
        StartCoroutine(StartActualLevelplay());
    }

    private IEnumerator StartActualLevelplay()
    {
        Debug.Log("[Tutorial] Starting actual gameplay after story dialogue");

        // Wait a moment for dialogue cleanup (use realtime since game might be paused)
        yield return new WaitForSecondsRealtime(0.5f);

        // Clear any tutorial enemies first
        Enemy[] tutorialEnemies = GameObject.FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in tutorialEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        Debug.Log($"[Tutorial] Cleared {tutorialEnemies.Length} tutorial enemies");

        // Find and repair the enemy tower
        Tower[] towers = GameObject.FindObjectsOfType<Tower>();
        Tower enemyTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Enemy);
        
        if (enemyTower != null)
        {
            enemyTower.RepairTower(); // Restore full health
            Debug.Log("[Tutorial] Enemy tower repaired for actual gameplay");
        }
        else
        {
            Debug.LogError("[Tutorial] Could not find enemy tower to repair!");
        }

        // Find and repair the player tower too (in case it was damaged)
        Tower playerTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Player);
        if (playerTower != null)
        {
            playerTower.RepairTower();
            Debug.Log("[Tutorial] Player tower repaired for actual gameplay");
        }

        // Reset enemy deployment for actual gameplay
        if (enemyDeployManager != null)
        {
            EnemyDeployManager.tutorialActive = false;
            Debug.Log("[Tutorial] Enemy deployment enabled for normal gameplay");
        }

        // Enable all player controls
        SetPlayerButtons(true);

        // Ensure game is running
        Time.timeScale = 1f;

        Debug.Log("[Tutorial] Actual level gameplay started - defeat the enemy tower to complete Level 1!");
    }
}
