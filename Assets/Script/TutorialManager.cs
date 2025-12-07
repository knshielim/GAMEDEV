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
    private static bool tutorialShownThisSession = false;
    private bool tutorialStepsFinished = false;

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

    private void ShowStep()
    {
        if (currentStep >= steps.Length)
        {
        tutorialStepsFinished = true;
        return;
        }
    var step = steps[currentStep];
    dialoguePanel.SetActive(true);
    dialogueText.text = step.text;
    dialogueImageHolder.gameObject.SetActive(step.image != null);
    dialogueImageHolder.sprite = step.image;

    // Show Skip button only on the first step
    if (SkipButton != null)
        SkipButton.SetActive(currentStep == 0);

    PauseGame();
    }


    private void Advance()
    {
        if (currentStep == 4)
        {
            EnemyDeployManager.tutorialActive = true;
            ResumeGame();
            dialoguePanel.SetActive(false);
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
            TroopInventory.Instance.AddTroop(tutorialFirstTroop);
            gaveFirstTroop = true;
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
        CoinManager.Instance.AddPlayerCoins(500);
        EnableOnly("Summon Button");
        waitingForSecondSummon = true;
        secondSummonCount = 0;
        canAdvance = false;
    }

    private IEnumerator ResetSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        DisableTutorial();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
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

        Debug.Log("[Tutorial] Completed!");
        // Disable tutorial
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;

        // Free gacha operations
        if (GachaManager.Instance != null) GachaManager.Instance.tutorialLocked = false;

        // First tutorial
        if (!TutorialDebug)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
        }

        // Show completion message
        dialoguePanel.SetActive(true);
        dialogueText.text = "Great job! Tutorial Complete!";
        dialogueImageHolder.gameObject.SetActive(false);

        // Reload scene normally
        StartCoroutine(ReloadSceneWithoutTutorial());
    }

    private IEnumerator ReloadSceneWithoutTutorial()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void OnTutorialSummonClicked()
    {
        if (waitingForSecondSummon)
        {
            TroopInventory.Instance.AddTroop(tutorialSecondTroop);
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
                SetPlayerButtons(false);
                EnableOnly("Merge Button");
                dialoguePanel.SetActive(true);
                dialogueText.text = "Great job! Now merge your troops!";
                PauseGame();
                waitingForMerge = true;
                return;
            }
        }

        if (!waitingForSummon) return;
        waitingForSummon = false;
        ResumeGame();
        GiveFirstTutorialTroop();
        EnableOnly("Deploy Button");
        currentStep++;
        ShowStep();
        waitingForDeploy = true;
        canAdvance = false;
    }

    public void OnTutorialDeployClicked()
    {
        if (!waitingForDeploy) return;

        if (mergeCompleted)
        {
            waitingForDeploy = false;
            mergeCompleted = false;

            // Show final deploy message
            dialoguePanel.SetActive(true);
            dialogueText.text = "Great work! You've deployed your merged troop!";
            dialogueImageHolder.gameObject.SetActive(false);
            ResumeGame();
            StartCoroutine(ClosePanelOnContinue());
            return;
        }

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
        dialogueText.text = "Excellent! Now deploy your merged troop!";
        PauseGame();
        EnableOnly("Deploy Button");
        waitingForDeploy = true;
    }

    private void PauseGame()
    {
        StartCoroutine(PauseNextFrame());
    }

    private IEnumerator PauseNextFrame()
    {
        yield return null;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void DisableTutorial()
    {
        tutorialActive = false;
        EnemyDeployManager.tutorialActive = false;
    }
}
