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

    private static bool tutorialShownThisSession = false;

    private Coroutine pauseCoroutine;

    private void Awake()
    {
        // ✅ FIX: Setup skip button with proper GameObject find
        if (SkipButton == null)
        {
            SkipButton = GameObject.Find("SkipButton");
        }
        
        if (SkipButton != null)
        {
            Button skipBtn = SkipButton.GetComponent<Button>();
            if (skipBtn != null)
            {
                skipBtn.onClick.RemoveAllListeners();
                skipBtn.onClick.AddListener(OnSkipButtonPressed);
                Debug.Log("[TutorialManager] ✅ Skip button listener added successfully");
            }
            else
            {
                Debug.LogError("[TutorialManager] ❌ Button component not found on SkipButton!");
            }
            SkipButton.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[TutorialManager] ⚠️ SkipButton GameObject not found!");
        }

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
        int currentLevel = GetCurrentLevel();
        if (currentLevel == 1)
        {
            Debug.Log("[TutorialManager] Level 1 detected - waiting for dialogue completion before starting tutorial");
            tutorialActive = false; // Don't start yet
            EnemyDeployManager.tutorialActive = false;
            if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;
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
        
        // ✅ FIX: Don't block keyboard shortcuts when tutorial is not active
        if (!tutorialActive)
        {
            return;
        }

        if (waitingForSecondSummon) return;
        if (waitingForSummon) return;
        if (waitingForDeploy) return;
        if (!canAdvance) return;

        if (Input.GetKeyDown(continueKey))
        {
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

    private void ShowStep(bool pause = true)
    {
        if (currentStep >= steps.Length)
        {
            return;
        }

        var step = steps[currentStep];

        dialoguePanel.SetActive(true);
        dialogueText.text = step.text;

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

            currentStep++;
            ShowStep(pause: false);

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
            int cost = GachaManager.Instance.GetCurrentSummonCost();

            bool success = CoinManager.Instance.TrySpendPlayerCoins(cost);
            if (!success)
            {
                Debug.LogWarning("[Tutorial] Player does not have enough coins for the tutorial summon.");
                return;
            }

            GachaManager.Instance.DebugSummonButton(); 

            // FIX: Wrap TroopData in TroopInstance
            TroopInventory.Instance.AddTroop(new TroopInstance(tutorialFirstTroop));

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
        while (!Input.GetKeyDown(continueKey))
            yield return null;

        dialoguePanel.SetActive(false);
        SetPlayerButtons(true);
        canAdvance = true;
    }

    public void OnTutorialTowerDestroyed(Tower destroyedTower)
    {
        if (!tutorialActive)
        {
            Debug.Log("[Tutorial] Tower destroyed but tutorial is not active (probably skipped) - ignoring");
            return;
        }

        Debug.Log("[Tutorial] 🏁 Tower destroyed during tutorial - Tutorial Complete!");

        // ✅ Mark tutorial as complete
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Tutorial completed - saved to PlayerPrefs");

        // ✅ FIX: Show tutorial completion message in tutorial panel (NOT victory panel)
        dialoguePanel.SetActive(true);
        dialogueText.text = "🎉 Tutorial Completed! 🎉\n\nYou've learned the basics!";
        dialogueImageHolder.gameObject.SetActive(false);
        if (SkipButton != null)
            SkipButton.SetActive(false);

        // Keep game paused
        PauseGame();

        // Wait for space then reload
        StartCoroutine(WaitForSpaceThenReload());
    }

    private IEnumerator WaitForSpaceThenReload()
    {
        Debug.Log("[Tutorial] 🔑 Waiting for SPACE key press...");
        
        // Wait until player presses space (use unscaled time since game is paused)
        bool spacePressed = false;
        while (!spacePressed)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                spacePressed = true;
                Debug.Log("[Tutorial] ✅ SPACE pressed - proceeding to reload");
            }
            yield return null;
        }

        Debug.Log("[Tutorial] 🔄 Reloading Level 1 for actual gameplay...");

        dialoguePanel.SetActive(false);
        ResumeGame();

        yield return new WaitForSeconds(0.2f);

        // Clean up all tutorial entities before reload
        CleanupTutorialEntities();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void CleanupTutorialEntities()
    {
        Debug.Log("[Tutorial] 🧹 Cleaning up tutorial entities...");
        
        // Clear all player troops
        Troops[] playerTroops = FindObjectsOfType<Troops>();
        foreach (Troops troop in playerTroops)
        {
            if (troop != null)
            {
                Destroy(troop.gameObject);
            }
        }
        Debug.Log($"[Tutorial] ✅ Destroyed {playerTroops.Length} player troops");

        // Clear all enemy units
        Enemy[] tutorialEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in tutorialEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        Debug.Log($"[Tutorial] ✅ Destroyed {tutorialEnemies.Length} tutorial enemies");
    }

    public void OnSkipButtonPressed()
    {
        Debug.Log("[Tutorial] ⏭️ SKIP BUTTON PRESSED!");
        SkipTutorial();
    }

    public void SkipTutorial()
    {
        Debug.Log("[Tutorial] ⏭️ Skipping tutorial...");

        // Mark tutorial as complete
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // ✅ FIX: Show skip message in tutorial panel (NOT start gameplay immediately)
        dialoguePanel.SetActive(true);
        dialogueText.text = "⏭️ Tutorial Skipped!\n\nYou can play the tutorial again from the main menu.\n\nPress SPACE to start Level 1...";
        dialogueImageHolder.gameObject.SetActive(false);
        if (SkipButton != null)
            SkipButton.SetActive(false);

        // Keep game paused
        PauseGame();

        // Wait for space before continuing
        StartCoroutine(WaitForSpaceThenReload());
    }

    public void OnTutorialSummonClicked()
    {
        if (waitingForSecondSummon)
        {
            int cost = GachaManager.Instance.GetCurrentSummonCost();
            bool success = CoinManager.Instance.TrySpendPlayerCoins(cost);

            if (!success)
            {
                Debug.LogWarning("[Tutorial] Not enough coins for second tutorial summons!");
                return;
            }

            // FIX: Wrap TroopData in TroopInstance
            TroopInventory.Instance.AddTroop(new TroopInstance(tutorialSecondTroop));

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
        yield return new WaitForSeconds(0.5f);

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
        // 🎡 START WEATHER ROULETTE NOW (REAL GAMEPLAY)
        if (WeatherRoulette.Instance != null)
        {
            WeatherRoulette.Instance.EnableRoulette();
        }
        else
        {
            Debug.LogWarning("[Tutorial] WeatherRoulette not found!");
        }


        StartCoroutine(StartActualLevelplay());
    }

    private int GetCurrentLevel()
    {
        if (GameManager.Instance != null)
        {
            return (int)GameManager.Instance.currentLevel;
        }

        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.GetCurrentLevel();
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level "))
        {
            string levelStr = sceneName.Substring(6);
            if (int.TryParse(levelStr, out int level))
            {
                return level;
            }
        }

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex >= 2)
        {
            return buildIndex - 1;
        }

        return 1;
    }

    public void DisableTutorial()
    {
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
    }

    public void StartTutorialAfterDialogue()
    {
        Debug.Log($"[TutorialManager] StartTutorialAfterDialogue called. tutorialActive={tutorialActive}, TutorialCompleted={PlayerPrefs.GetInt("TutorialCompleted", 0)}");

        Debug.Log("[TutorialManager] Starting tutorial after dialogue completion");

        this.enabled = true;

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

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddPlayerCoins(5);
            Debug.Log("[TutorialManager] Added 5 coins for tutorial");
        }

        ShowStep();
        Debug.Log("[TutorialManager] Tutorial started - ShowStep() called");
    }

    public void StartActualGameplayAfterVictoryDialogue()
    {
        Debug.Log("[Tutorial] StartActualGameplayAfterVictoryDialogue called - starting real gameplay");

        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // ✅ FIX: Reset coins when starting real gameplay after tutorial
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.ResetCoins();
            Debug.Log("[Tutorial] Coins reset for real gameplay start");
        }

        StartCoroutine(StartActualLevelplay());
    }

    private IEnumerator StartActualLevelplay()
    {
        Debug.Log("[Tutorial] Starting actual gameplay after story dialogue");

        yield return new WaitForSecondsRealtime(0.5f);

        CleanupTutorialEntities();

        Tower[] towers = GameObject.FindObjectsOfType<Tower>();
        Tower enemyTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Enemy);
        
        if (enemyTower != null)
        {
            enemyTower.RepairTower();
            Debug.Log("[Tutorial] Enemy tower repaired for actual gameplay");
        }
        else
        {
            Debug.LogError("[Tutorial] Could not find enemy tower to repair!");
        }

        Tower playerTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Player);
        if (playerTower != null)
        {
            playerTower.RepairTower();
            Debug.Log("[Tutorial] Player tower repaired for actual gameplay");
        }

        if (enemyDeployManager != null)
        {
            EnemyDeployManager.tutorialActive = false;
            Debug.Log("[Tutorial] Enemy deployment enabled for normal gameplay");
        }

        SetPlayerButtons(true);

        Time.timeScale = 1f;

        Debug.Log("[Tutorial] Actual level gameplay started - defeat the enemy tower to complete Level 1!");

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.ResetCoins();
        }
    }
}