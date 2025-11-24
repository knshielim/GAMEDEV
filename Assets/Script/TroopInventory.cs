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
    
    [Header("UI Inventory Slot Borders")]
    [Tooltip("Border Images for each inventory slot (used for selection highlight).")]
    public List<Image> slotBorders; 


    private void Awake()
    {
        Instance = this;

        // Ensure list matches slot count
        while (storedTroops.Count < slotImages.Count)
            storedTroops.Add(new StoredTroopSlot());

        // Add button listeners for selecting troops
        for (int i = 0; i < slotImages.Count; i++)
        {
            int index = i;
            var btn = slotImages[i].GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() =>
            {
                FindObjectOfType<TroopDeployManager>().SelectTroop(index);
            });
        }

        RefreshUI();
    }

    public bool AddTroop(TroopData troop)
    {
        // 1. Try to stack first (max 3 per slot)
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].troop == troop && storedTroops[i].count < 3)
            {
                storedTroops[i].count++;
                RefreshUI();
                return true;
            }
        }

        // 2. Find the first empty slot
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

    public void RefreshUI()
    {
    for (int i = 0; i < slotImages.Count; i++)
    {
        var slot = storedTroops[i];

        if (slot.troop == null)
        {
            slotImages[i].sprite = emptySlotSprite; // reset to empty image
            slotCountTexts[i].text = "";
        }
        else
        {
            slotImages[i].sprite = slot.troop.prefab.GetComponent<SpriteRenderer>().sprite;
            slotCountTexts[i].text = "x" + slot.count;
        }

        // Hide the border if nothing is selected
        if (slotBorders != null && i < slotBorders.Count)
            slotBorders[i].gameObject.SetActive(false);
    }
    }

}
