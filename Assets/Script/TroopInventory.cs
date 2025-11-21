using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TroopInventory : MonoBehaviour
{
    public static TroopInventory Instance;

    [Header("UI Inventory Slot Images")]
    public List<Image> slotImages; // gray boxes
    public Sprite emptySlotSprite;

    [Header("Stored Troops")]
    public List<TroopData> storedTroops = new List<TroopData>();

    private void Awake()
    {
        Instance = this;

        // Ensure list matches slot count
        while (storedTroops.Count < slotImages.Count)
            storedTroops.Add(null);

        RefreshUI();
    }

    public bool AddTroop(TroopData troop)
    {
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i] == null)
            {
                storedTroops[i] = troop;
                RefreshUI();
                return true;
            }
        }

        Debug.Log("[Inventory] FULL!");
        return false;
    }

    public TroopData GetTroop(int index)
    {
        return storedTroops[index];
    }

    public void ClearSlot(int index)
    {
        storedTroops[index] = null;
        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (storedTroops[i] == null)
                slotImages[i].sprite = emptySlotSprite;
            else
                slotImages[i].sprite = storedTroops[i].prefab.GetComponent<SpriteRenderer>().sprite;
        }
    }
}
