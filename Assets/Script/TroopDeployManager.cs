using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TroopDeployManager : MonoBehaviour
{
    public Transform playerTowerSpawnPoint;

    private int selectedTroopIndex = -1;
    private bool canDeploy = true; // cooldown

    // Highlight the selected slot border
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

    // Select a troop slot
    public void SelectTroop(int index)
    {
        // If clicking the same slot, deselect
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

    // Deploy the selected troop
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

        // Instantiate the troop
        GameObject troopObj = Instantiate(troop.playerPrefab, playerTowerSpawnPoint.position, Quaternion.identity);

        // Play different SFX depending on rarity
        if (troop.rarity == TroopRarity.Mythic)
        {
            // Special SFX for Mythic deploy
            if (AudioManager.Instance != null && AudioManager.Instance.mythicSFX != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.mythicSFX);
        }
        else
        {
            // Default summon SFX for non-Mythic troops
            if (AudioManager.Instance != null && AudioManager.Instance.summonSFX != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.summonSFX);
        }


        // Set TroopData
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

        // Remove from inventory
        StoredTroopDeployed(selectedTroopIndex);

        // Reset selection
        selectedTroopIndex = -1;

        // Remove border highlight
        HighlightSelectedSlot(-1);

        StartCoroutine(DeployCooldown());
    }

    private void StoredTroopDeployed(int index)
    {
        TroopInventory.Instance.ClearSlot(index);

        // Remove highlight
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
        // Check for troop selection keys
        for (int i = 0; i < troopSelectionKeys.Length; i++)
        {
            if (Input.GetKeyDown(troopSelectionKeys[i]))
            {
                // This 'i' is the inventory slot index (0 to 11)
                SelectTroop(i);
                // Stop checking after the first key press is handled
                return;
            }
        }

        // Keyboard shortcuts for game actions
        if (Input.GetKeyDown(KeyCode.U))
        {
            // Upgrade summon rate
            if (GachaManager.Instance != null)
            {
                GachaManager.Instance.UpgradeGachaSystem();
            }
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeSFX);
            // Upgrade tower
            Tower[] towers = FindObjectsOfType<Tower>();
            Tower playerTower = System.Array.Find(towers, t => t.owner == Tower.TowerOwner.Player);
            if (playerTower != null)
            {
                playerTower.UpgradeTower();
            }
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            // Deploy selected troop
            DeploySelectedTroop();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            // Summon troop
            if (GachaManager.Instance != null)
            {
                GachaManager.Instance.SummonTroop();
            }
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            // Merge units in selected slot
            if (TroopInventory.Instance != null && selectedTroopIndex >= 0)
            {
                TroopInventory.Instance.MergeUnits(selectedTroopIndex);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            // Alternative deploy shortcut (existing functionality)
            DeploySelectedTroop();
        }
    }
}