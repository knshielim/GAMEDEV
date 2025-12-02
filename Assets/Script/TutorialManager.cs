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

    [Header("Buttons to Block (Summon + Deploy)")]
    public Button[] playerButtons;

    [Header("Tutorial Troops")]
    public TroopData tutorialFirstTroop;
    public TroopData tutorialSecondTroop;

    private bool gaveFirstTroop = false;
    private bool gaveSecondTroop = false;

    private int currentStep = 0;

    private bool canAdvance = true;
    private bool waitingForSummon = false;
    private bool waitingForDeploy = false;

    private void Awake()
    {
        // Freeze enemy AI at start so they don't move or attack
        EnemyDeployManager.tutorialActive = true;

        // Player cannot press anything at start
        SetPlayerButtons(false);
    }

    private void Start()
    {
        if (enemyDeployManager == null)
            enemyDeployManager = FindObjectOfType<EnemyDeployManager>();

        ShowStep();
    }

    private void Update()
    {
        if (!canAdvance) return;
        if (waitingForSummon) return;
        if (waitingForDeploy) return;

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
    }

    private void Advance()
    {
        // --- STEP 5: Enemy appears, tutorial unfreezes ---
        if (currentStep == 4)
        {
            EnemyDeployManager.tutorialActive = false; // enemy can move & attack normally
            enemyDeployManager.SpawnSpecificEnemy(enemyToSpawn);

            dialoguePanel.SetActive(false);

            // Only Summon is allowed
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
        EnemyDeployManager.tutorialActive = false;
        SetPlayerButtons(true);

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

    private void GiveSecondTutorialTroop()
    {
        if (!gaveSecondTroop && tutorialSecondTroop != null)
        {
            TroopInventory.Instance.AddTroop(tutorialSecondTroop);
            gaveSecondTroop = true;
        }
    }

    private void SetPlayerButtons(bool state)
    {
        foreach (var b in playerButtons)
            if (b != null)
                b.interactable = state;
    }

    private void EnableOnly(string buttonName)
    {
        foreach (var b in playerButtons)
        {
            if (b != null)
                b.interactable = (b.gameObject.name == buttonName);
        }
    }

    private IEnumerator SpawnNextEnemySequence()
    {
        // Wait 4 seconds
        yield return new WaitForSeconds(8f);

        // Spawn stronger enemy
        if (enemyToSpawn2 != null)
            enemyDeployManager.SpawnSpecificEnemy(enemyToSpawn2);

        yield return new WaitForSeconds(2f);

        // Move step forward
        currentStep++;

        // Show next tutorial dialogue
        ShowStep();

        CoinManager.Instance.AddPlayerCoins(500);

        GiveSecondTutorialTroop();

        // Allow advancing again
        canAdvance = true;
    }

    // ---------------------- BUTTON EVENTS ----------------------

    public void OnTutorialSummonClicked()
    {
        if (!waitingForSummon) return;

        waitingForSummon = false;

        // Give ONLY the blue slime (first troop)
        GiveFirstTutorialTroop();

        EnableOnly("Deploy Button");

        // Move to next dialogue
        currentStep++;
        ShowStep();

        waitingForDeploy = true;
        canAdvance = false;
    }

    public void OnTutorialDeployClicked()
    {
        if (!waitingForDeploy) return;

        waitingForDeploy = false;

        // Buttons back on
        SetPlayerButtons(true);

        // Hide dialogue instantly
        dialoguePanel.SetActive(false);

        StartCoroutine(SpawnNextEnemySequence());
    }
}
