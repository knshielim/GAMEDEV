using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

        // Tutorial should run normally
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

        if (Input.GetKeyDown(continueKey)) Advance();
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

    public void SkipTutorial()
    {
        // Disable all tutorial logic
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        // Unlock gacha
        if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;

        // Save skip if NOT debug mode
        if (!TutorialDebug)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
        }

        // Hide UI
        dialoguePanel.SetActive(false);
        SkipButton.gameObject.SetActive(false);
        Time.timeScale = 1f;
        SetPlayerButtons(true);
        this.enabled = false;
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
        if (!tutorialActive) return;
        Debug.Log("[Tutorial] Tower destroyed during tutorial!");
        // Pause game immediately
        PauseGame();
        // Show tutorial completion message
        dialoguePanel.SetActive(true);
        dialogueText.text = "Great job! Tutorial Complete!";
        dialogueImageHolder.gameObject.SetActive(false);
        // Wait for player to acknowledge, then transition to story dialogue
        StartCoroutine(TransitionToStoryDialogue());
    }

    private IEnumerator TransitionToStoryDialogue()
    {
        // Wait for the user to press continue
        yield return new WaitUntil(() => Input.GetKeyDown(continueKey));

        // Hide tutorial panel
        dialoguePanel.SetActive(false);

        // Mark tutorial as complete (but keep game paused)
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        // Save tutorial completion (if not in debug mode)
        if (!TutorialDebug)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
        }

        // Now show the story dialogue (DialogueManager should handle it)
        if (DialogueManager.Instance != null)
        {
            // Get current level
            int currentLevel = 1; // Tutorial is always on level 1

            // Show level 1 end dialogue
            DialogueManager.Instance.ShowLevelEndDialogueForced(currentLevel);

            Debug.Log("[Tutorial] Transitioned to story dialogue - game remains paused");
        }
        else
        {
            Debug.LogError("[Tutorial] DialogueManager not found! Cannot show story dialogue.");
            // Fallback: just resume and reload
            ResumeGame();
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

    public void DisableTutorial()
    {
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
    }

    // Called by DialogueManager after intro dialogue completes
    public void StartTutorialAfterDialogue()
    {
        Debug.Log($"[TutorialManager] StartTutorialAfterDialogue called. tutorialActive={tutorialActive}, TutorialCompleted={PlayerPrefs.GetInt("TutorialCompleted", 0)}");

        if (tutorialActive || PlayerPrefs.GetInt("TutorialCompleted", 0) == 1)
        {
            Debug.Log("[TutorialManager] Tutorial already active or completed, not starting");
            return; // Already active or completed
        }

        Debug.Log("[TutorialManager] Starting tutorial after dialogue completion");

        // Ensure we're enabled
        this.enabled = true;

        // Initialize tutorial state
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
        // Wait a moment for tutorial cleanup
        yield return new WaitForSeconds(2f);

        // Repair the enemy tower that was "destroyed" during tutorial
        Tower enemyTower = GameObject.FindObjectOfType<Tower>();
        if (enemyTower != null && enemyTower.owner == Tower.TowerOwner.Enemy)
        {
            enemyTower.RepairTower(); // Restore full health
            Debug.Log("[Tutorial] Enemy tower repaired for actual gameplay");
        }

        // Reset enemy deployment for actual gameplay
        if (enemyDeployManager != null)
        {
            // Make sure enemies can spawn normally now
            EnemyDeployManager.tutorialActive = false;
            Debug.Log("[Tutorial] Enemy deployment enabled for normal gameplay");
        }

        // Enable all player controls
        SetPlayerButtons(true);

        // Clear any tutorial enemies
        Enemy[] tutorialEnemies = GameObject.FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in tutorialEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        Debug.Log("[Tutorial] Actual level gameplay started - defeat the enemy tower to complete the level and see end dialogue!");
    }
}
