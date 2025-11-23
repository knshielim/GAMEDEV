using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
    public class StoredTroopSlot
    {
        public TroopData troop;
        public int count;

        public StoredTroopSlot()
        {
            troop = null;
            count = 0;
        }
    }

public class TroopInventory : MonoBehaviour
{
    

    public static TroopInventory Instance;

    [Header("UI Inventory Slot Images")]
    [Tooltip("List of Image components representing the inventory slots.")]
    public List<Image> slotImages; // gray 

    [Tooltip("The sprite to display when a slot is empty.")]
    public Sprite emptySlotSprite;

    [Header("UI Inventory Slot Count Text")]
    [Tooltip("Text for displaying how many units are in each slot.")]
    public List<TextMeshProUGUI> slotCountTexts;

    [Header("Stored Troops")]
    public List<StoredTroopSlot> storedTroops = new List<StoredTroopSlot>();


    private void Awake()
    {
        Instance = this;

        // Ensure list matches slot count
       while (storedTroops.Count < slotImages.Count)
        storedTroops.Add(new StoredTroopSlot());


        RefreshUI();
    }

    public bool AddTroop(TroopData troop)
    {
        // 1. Try stacking (max 3)
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].troop == troop && storedTroops[i].count < 3)
            {
                storedTroops[i].count++;
                RefreshUI();
                return true;
            }
        }

        // 2. Find first empty slot
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].troop == null)
            {
                storedTroops[i].troop = troop;
                storedTroops[i].count = 1;
                RefreshUI();
                return true;
            }
        }

        Debug.Log("[Inventory] FULL!");
        return false;
    }



    public TroopData GetTroop(int index)
    {
        return storedTroops[index].troop;
    }


    public void ClearSlot(int index)
    {
        storedTroops[index].troop = null;
        storedTroops[index].count = 0;

        RefreshUI();
    }

    private void RefreshUI()
    {
    for (int i = 0; i < slotImages.Count; i++)
    {
        var slot = storedTroops[i];

        if (slot.troop == null)
        {
            slotImages[i].sprite = emptySlotSprite;
            slotCountTexts[i].text = "";
        }
        else
        {
            slotImages[i].sprite = slot.troop.prefab.GetComponent<SpriteRenderer>().sprite;
            slotCountTexts[i].text = slot.count > 1 ? "x" + slot.count : "";
        }
    }
    }

}
