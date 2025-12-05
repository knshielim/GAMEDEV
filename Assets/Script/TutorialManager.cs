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

    // put this Awake in your TutorialManager (replace your current Awake)
    private void Awake()
    {
    Debug.Log("[TutorialManager] tutorialActive (Inspector) = " + tutorialActive);

    if (!tutorialActive)
    {
        Debug.Log("[TutorialManager] Tutorial disabled by Inspector.");

        EnemyDeployManager.tutorialActive = false;

        if (GachaManager.Instance != null)
            GachaManager.Instance.tutorialLocked = false;

        this.enabled = false;     // DISABLE
        return;
    }

    Debug.Log("[TutorialManager] Tutorial ENABLED. Starting tutorial.");

    EnemyDeployManager.tutorialActive = true;

    if (GachaManager.Instance != null)
        GachaManager.Instance.tutorialLocked = true;

    SetPlayerButtons(false); // lock gameplay
    }

    private void Start()
    {
        if (enemyDeployManager == null)
            enemyDeployManager = FindObjectOfType<EnemyDeployManager>();

        ShowStep();
    }

    private void Update()
    {
        if (waitingForSecondSummon) return;
        if (waitingForSummon) return;
        if (waitingForDeploy) return;
        if (!canAdvance) return;

        if (Input.GetKeyDown(continueKey))
            Advance();
    }

    private void ShowStep()
    {
        if (currentStep >= steps.Length)
        {
            FinishTutorial();
            return;
        }

        var step = steps[currentStep];
        dialoguePanel.SetActive(true);
        dialogueText.text = step.text;
        dialogueImageHolder.gameObject.SetActive(step.image != null);
        dialogueImageHolder.sprite = step.image;

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

    private void FinishTutorial()
    {
        dialoguePanel.SetActive(false);
        SetPlayerButtons(true);
        ResumeGame();
        Debug.Log("[Tutorial] Completed successfully.");
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

            foreach (var img in b.GetComponentsInChildren<Image>())
                img.color = tint;

            foreach (var txt in b.GetComponentsInChildren<TMP_Text>())
                txt.color = tint;
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

            foreach (var img in b.GetComponentsInChildren<Image>())
                img.color = tint;

            foreach (var txt in b.GetComponentsInChildren<TMP_Text>())
                txt.color = tint;
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
        ResumeGame();
        yield return new WaitForSeconds(5f);

        if (enemyToSpawn2 != null)
            enemyDeployManager.SpawnSpecificEnemy(enemyToSpawn2, true);

        yield return new WaitForSeconds(2f);

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

    // Disable tutorial BEFORE reloading the scene
    DisableTutorial();

    // Reload the same scene
    UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
    );
    }
  public void OnTutorialTowerDestroyed(Tower destroyedTower)
    {
    if (!tutorialActive) return;

    Debug.Log("[Tutorial] Completed!");

    // Unlock systems
    EnemyDeployManager.tutorialActive = false;
    GachaManager.Instance.tutorialLocked = false;

    // Save completion (optional)
    PlayerPrefs.SetInt("TutorialCompleted", 1);
    PlayerPrefs.Save();

    // End screen message
    dialoguePanel.SetActive(true);
    dialogueText.text = "Great job! Tutorial Complete!";
    dialogueImageHolder.gameObject.SetActive(false);

    // Reload scene after short delay
    StartCoroutine(ResetSceneAfterDelay());
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

    // If this deploy is AFTER the merge
    if (mergeCompleted)
    {
        // End merge-wait state
        waitingForDeploy = false;
        mergeCompleted = false;

        // Show the post-merge dialogue and allow advancing
        PauseGame();
        dialoguePanel.SetActive(true);
        dialogueText.text = "Great work! You've deployed your merged troop!";
        dialogueImageHolder.gameObject.SetActive(false);

        // Allow the player to press the continue key to advance the tutorial
        canAdvance = true;

        // Make sure no other waiting flags will block Update()
        waitingForSecondSummon = false;
        waitingForSummon = false;
        waitingForMerge = false;

        // Keep player buttons inactive until they continue
        SetPlayerButtons(false);

        return; 
    }

    // Normal deploy (non-merged) flow:
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

    // Dialogue freeze 
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
