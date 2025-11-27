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
    public List<Image> slotImages;

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
    
    [Header("Merge System")]
    [Tooltip("List of merge buttons (one per slot)")]
    public List<Button> mergeButtons;
    
    [Tooltip("Maximum units per slot before merge is available")]
    public int maxUnitsPerSlot = 3;

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
            var btn = slotImages[i].GetComponentInParent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    FindObjectOfType<TroopDeployManager>()?.SelectTroop(index);
                });
            }
            
            // Setup merge button listeners
            if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
            {
                mergeButtons[i].onClick.AddListener(() => MergeUnits(index));
                mergeButtons[i].gameObject.SetActive(false); // Hide by default
            }
        }

        RefreshUI();
    }

    public bool AddTroop(TroopData troop)
    {
        // 1. Try to stack first (max 3 per slot)
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].troop == troop && storedTroops[i].count < maxUnitsPerSlot)
            {
                storedTroops[i].count++;
                Debug.Log($"[TroopInventory] Added {troop.displayName} to slot {i}, count: {storedTroops[i].count}");
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
                Debug.Log($"[TroopInventory] Added {troop.displayName} to new slot {i}");
                RefreshUI();
                return true;
            }
        }

        Debug.Log("[Inventory] FULL!");
        return false;
    }

    public TroopData GetTroop(int index)
    {
        if (index < 0 || index >= storedTroops.Count)
            return null;
            
        return storedTroops[index].troop;
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= storedTroops.Count)
            return;
            
        storedTroops[index].troop = null;
        storedTroops[index].count = 0;
        RefreshUI();
    }
    
    /// <summary>
    /// Merge 3 units of the same rarity into 1 unit of the next rarity
    /// </summary>
    public void MergeUnits(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= storedTroops.Count)
        {
            Debug.LogWarning("[Merge] Invalid slot index!");
            return;
        }
        
        var slot = storedTroops[slotIndex];
        
        if (slot.troop == null || slot.count < maxUnitsPerSlot)
        {
            Debug.LogWarning($"[Merge] Cannot merge: slot has {slot.count} units (need {maxUnitsPerSlot})");
            return;
        }
        
        TroopRarity currentRarity = slot.troop.rarity;
        TroopRarity nextRarity = GetNextRarity(currentRarity);
        
        if (nextRarity == currentRarity)
        {
            Debug.LogWarning($"[Merge] Cannot merge {currentRarity} - already at max rarity!");
            return;
        }
        
        // Consume the 3 units
        slot.count -= maxUnitsPerSlot;
        if (slot.count <= 0)
        {
            slot.troop = null;
            slot.count = 0;
        }
        
        // Get a random troop of the next rarity
        TroopData upgradedTroop = GetRandomTroopOfRarity(nextRarity);
        
        if (upgradedTroop != null)
        {
            bool added = AddTroop(upgradedTroop);
            
            if (added)
            {
                Debug.Log($"[Merge] Successfully merged 3x {currentRarity} into 1x {upgradedTroop.displayName} ({nextRarity})!");
            }
            else
            {
                Debug.LogWarning("[Merge] Inventory full! Could not add merged troop.");
                // TODO: Refund the consumed units
            }
        }
        else
        {
            Debug.LogError($"[Merge] No troops found for rarity: {nextRarity}");
        }
        
        RefreshUI();
    }
    
    private TroopRarity GetNextRarity(TroopRarity current)
    {
        switch (current)
        {
            case TroopRarity.Common:
                return TroopRarity.Rare;
            case TroopRarity.Rare:
                return TroopRarity.Epic;
            case TroopRarity.Epic:
                return TroopRarity.Legendary;
            case TroopRarity.Legendary:
                Debug.LogWarning("[Merge] Legendary is the highest rarity that can be merged. Cannot merge further.");
                return TroopRarity.Legendary; // Max mergeable rarity
            case TroopRarity.Mythic:
                Debug.LogWarning("[Merge] Mythic troops cannot be merged.");
                return TroopRarity.Mythic; // Mythic cannot be merged
            default:
                return current;
        }
    }
    
    private TroopData GetRandomTroopOfRarity(TroopRarity rarity)
    {
        if (GachaManager.Instance == null)
        {
            Debug.LogError("[Merge] GachaManager not found!");
            return null;
        }
        
        // Use GachaManager's public method
        return GachaManager.Instance.GetRandomTroopOfRarity(rarity);
    }

    public void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            var slot = storedTroops[i];

            if (slot.troop == null)
            {
                slotImages[i].sprite = emptySlotSprite;
                if (i < slotCountTexts.Count)
                    slotCountTexts[i].text = "";
                
                // Hide merge button
                if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
                    mergeButtons[i].gameObject.SetActive(false);
            }
            else
            {
                SpriteRenderer sr = slot.troop.playerPrefab?.GetComponent<SpriteRenderer>();
                if (sr != null)
                    slotImages[i].sprite = sr.sprite;
                    
                if (i < slotCountTexts.Count)
                    slotCountTexts[i].text = "x" + slot.count;
                
                // Show merge button if count == maxUnitsPerSlot
                if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
                {
                    bool canMerge = slot.count >= maxUnitsPerSlot && slot.troop.rarity != TroopRarity.Mythic;
                    mergeButtons[i].gameObject.SetActive(canMerge);
                }
            }

            // Hide border by default
            if (slotBorders != null && i < slotBorders.Count)
                slotBorders[i].gameObject.SetActive(false);
        }
    }
}