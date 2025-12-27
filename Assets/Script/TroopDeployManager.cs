using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TroopDeployManager : MonoBehaviour
{
    public Transform playerTowerSpawnPoint;

    private int selectedTroopIndex = -1;
    private bool canDeploy = true;

    public void HighlightSelectedSlot(int index)
    {
        if (TroopInventory.Instance == null || TroopInventory.Instance.slotBorders == null)
        {
            Debug.LogError("[DEPLOY] TroopInventory or slotBorders is null!");
            return;
        }

        Debug.Log($"[DEPLOY] HighlightSelectedSlot called for index {index}");

        for (int i = 0; i < TroopInventory.Instance.slotBorders.Count; i++)
        {
            if (TroopInventory.Instance.slotBorders[i] != null)
            {
                bool shouldShow = (i == index);
                TroopInventory.Instance.slotBorders[i].gameObject.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    Debug.Log($"[DEPLOY] Border {i} activated");
                }
            }
            else
            {
                Debug.LogWarning($"[DEPLOY] slotBorders[{i}] is null!");
            }
        }
    }

    public void SelectTroop(int index)
    {
        if (selectedTroopIndex == index)
        {
            selectedTroopIndex = -1;
            Debug.Log($"[DEPLOY] Deselected slot {index}");
            HighlightSelectedSlot(-1);
            return;
        }

        selectedTroopIndex = index;
        Debug.Log($"[DEPLOY] Selected troop at slot: {index}");
        HighlightSelectedSlot(index);
    }

    public void DeploySelectedTroop()
    {
        Debug.Log($"[DEPLOY] DeploySelectedTroop called at {Time.time}");
        
        if (!canDeploy)
        {
            Debug.Log("[DEPLOY] Deployment on cooldown...");
            return;
        }

        if (selectedTroopIndex < 0)
        {
            Debug.Log("[DEPLOY] No troop selected!");
            return;
        }

        TroopData troop = TroopInventory.Instance.GetTroop(selectedTroopIndex);

        if (troop == null)
        {
            Debug.Log("[DEPLOY] Selected slot is empty.");
            return;
        }

        GameObject troopObj = Instantiate(troop.playerPrefab, playerTowerSpawnPoint.position, Quaternion.identity);

        if (troop.rarity == TroopRarity.Mythic)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.mythicSFX != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.mythicSFX);
        }
        else
        {
            if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);
        }

        Unit unit = troopObj.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetTroopData(troop);
            Debug.Log($"[DEPLOY] Deployed {troop.displayName} successfully");
        }
        else
        {
            Debug.LogWarning($"[DEPLOY] Spawned {troopObj.name} but it has no Unit component.");
        }

        StoredTroopDeployed(selectedTroopIndex);

        selectedTroopIndex = -1;

        HighlightSelectedSlot(-1);

        StartCoroutine(DeployCooldown());
    }

    private void StoredTroopDeployed(int index)
    {
        TroopInventory.Instance.ClearSlot(index);

        if (TroopInventory.Instance.slotBorders != null && 
            index >= 0 && index < TroopInventory.Instance.slotBorders.Count)
        {
            TroopInventory.Instance.slotBorders[index].gameObject.SetActive(false);
        }
    }

    private IEnumerator DeployCooldown()
    {
        canDeploy = false;
        yield return new WaitForSeconds(0.5f);
        canDeploy = true;
    }

    private readonly KeyCode[] troopSelectionKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Minus, KeyCode.Equals
    };

    void Update()
    {
        // ✅ FIX: Check if tutorial is active before processing keyboard shortcuts
        bool tutorialActive = false;
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null)
        {
            tutorialActive = tutorialManager.tutorialActive;
        }

        // ✅ FIX: If tutorial is active, don't process keyboard shortcuts
        if (tutorialActive)
        {
            return;
        }

        // Check for troop selection keys
        for (int i = 0; i < troopSelectionKeys.Length; i++)
        {
            if (Input.GetKeyDown(troopSelectionKeys[i]))
            {
                SelectTroop(i);
                return;
            }
        }

        // Keyboard shortcuts for game actions
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (GachaManager.Instance != null)
            {
                GachaManager.Instance.UpgradeGachaSystem();
            }
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeSFX);
            Tower[] towers = FindObjectsOfType<Tower>();
            Tower playerTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Player);
            if (playerTower != null)
            {
                playerTower.UpgradeTower();
            }
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            DeploySelectedTroop();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            if (GachaManager.Instance != null)
            {
                GachaManager.Instance.SummonTroop();
            }
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            if (TroopInventory.Instance != null && selectedTroopIndex >= 0)
            {
                TroopInventory.Instance.MergeUnits(selectedTroopIndex);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            DeploySelectedTroop();
        }
    }
}