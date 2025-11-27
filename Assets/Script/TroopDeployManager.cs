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
        if (TroopInventory.Instance.slotBorders == null) return;

        for (int i = 0; i < TroopInventory.Instance.slotBorders.Count; i++)
        {
            TroopInventory.Instance.slotBorders[i].gameObject.SetActive(i == index);
        }
    }

    // Select a troop slot
    public void SelectTroop(int index)
    {
        selectedTroopIndex = index;
        Debug.Log("Selected troop at slot: " + index);
        HighlightSelectedSlot(index);
    }

    // Deploy the selected troop
    public void DeploySelectedTroop()
    {
        Debug.Log("[DEPLOY] DeploySelectedTroop called: " + Time.time);
        if (!canDeploy)
        {
            Debug.Log("Deployment on cooldown...");
            return;
        }

        if (selectedTroopIndex < 0)
        {
            Debug.Log("No troop selected!");
            return;
        }

        TroopData troop = TroopInventory.Instance.GetTroop(selectedTroopIndex);

        if (troop == null)
        {
            Debug.Log("Selected slot is empty.");
            return;
        }

        // Instantiate the troop
        GameObject troopObj =
            Instantiate(troop.playerPrefab, playerTowerSpawnPoint.position, Quaternion.identity);

        // Pastikan clone-nya dapat TroopData yang benar
        Unit unit = troopObj.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetTroopData(troop);
        }
        else
        {
            Debug.LogWarning($"[DEPLOY] Spawned {troopObj.name} but it has no Unit component.");
        }



        // Remove from inventory
        storedTroopDeployed(selectedTroopIndex);

        // Reset selection
        selectedTroopIndex = -1;

        // Remove border highlight
        HighlightSelectedSlot(-1);

        Debug.Log("Deployed troop: " + troop.displayName);

        StartCoroutine(DeployCooldown());
    }

    private void storedTroopDeployed(int index)
    {
        TroopInventory.Instance.ClearSlot(index);

        // remove highlight
        if (TroopInventory.Instance.slotBorders != null && 
        index >= 0 && index < TroopInventory.Instance.slotBorders.Count)
        {
            TroopInventory.Instance.slotBorders[index].gameObject.SetActive(false);
        }
    }

    private IEnumerator DeployCooldown()
    {
        canDeploy = false;
        yield return new WaitForSeconds(1f);
        canDeploy = true;
    }
}
