using UnityEngine;

public class TroopDeployManager : MonoBehaviour
{
    public Transform playerTowerSpawnPoint;

    private int selectedTroopIndex = -1;

    public void SelectTroop(int index)
    {
        selectedTroopIndex = index;
        Debug.Log("Selected troop at slot: " + index);
    }

    public void DeploySelectedTroop()
    {
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

        GameObject newUnit = Instantiate(
            troop.prefab,
            playerTowerSpawnPoint.position,
            Quaternion.identity
        );

        TroopInventory.Instance.ClearSlot(selectedTroopIndex);

        Debug.Log("Deployed troop: " + troop.displayName);
    }
}
