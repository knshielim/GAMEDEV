using System.Collections;
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
    
    [Header("Selection Visual Settings")]
    [Tooltip("Color for selected slot border")]
    public Color selectedBorderColor = Color.yellow;
    
    [Tooltip("Width of the selected border")]
    public float selectedBorderWidth = 4f;
    
    [Header("Merge System")]
    [Tooltip("List of merge buttons (one per slot)")]
    public List<Button> mergeButtons;
    
    [Tooltip("Maximum units per slot before merge is available")]
    public int maxUnitsPerSlot = 3;

    [Header("Animation Settings")]
    [Tooltip("Duration of the summon animation")]
    public float animationDuration = 0.4f;

    [Tooltip("Enable/disable summon animation")]
    public bool enableSummonAnimation = true;

    [Tooltip("Particle/sprite effect to play on summon")]
    public GameObject summonEffectPrefab;

    [Tooltip("Offset position for the effect (relative to slot)")]
    public Vector3 effectOffset = Vector3.zero;

    [Tooltip("How long before destroying the effect")]
    public float effectDuration = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
                mergeButtons[i].gameObject.SetActive(false);
            }
            
            // Setup borders - ensure they're properly configured
            if (slotBorders != null && i < slotBorders.Count && slotBorders[i] != null)
            {
                // Set border color
                slotBorders[i].color = selectedBorderColor;
                
                // Initially hide all borders
                slotBorders[i].gameObject.SetActive(false);
                
                Debug.Log($"[Inventory] Border {i} configured: {slotBorders[i].name}");
            }
        }

        RefreshUI();
        Debug.Log($"[Inventory] Initialized with {slotImages.Count} slots, {slotCountTexts.Count} count texts, {slotBorders.Count} borders");
    }

    public bool AddTroop(TroopData troop)
    {
        if (troop == null)
        {
            Debug.LogWarning("[Inventory] Attempted to add null troop!");
            return false;
        }

        int affectedSlot = -1;

        // 1. Try to stack first (max per slot)
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].troop == troop && storedTroops[i].count < maxUnitsPerSlot)
            {
                storedTroops[i].count++;
                affectedSlot = i;
                Debug.Log($"[TroopInventory] Stacked {troop.displayName} in slot {i}, count: {storedTroops[i].count}");
                
                // ADDED: Always try to combine immediately after adding a unit
                TryAutoCombine(); 
                
                RefreshUI();
                
                // Play animation on the slot that was updated
                if (enableSummonAnimation && affectedSlot >= 0)
                {
                    StartCoroutine(SlotSummonAnimation(affectedSlot));
                }
                
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
                affectedSlot = i;
                Debug.Log($"[TroopInventory] Added {troop.displayName} to new slot {i}");
                RefreshUI();
                
                // Play animation on the new slot
                if (enableSummonAnimation && affectedSlot >= 0)
                {
                    StartCoroutine(SlotSummonAnimation(affectedSlot));
                }
                
                return true;
            }
        }

        Debug.Log("[Inventory] FULL! Cannot add more troops.");
        return false;
    }

    // ============ SUMMON ANIMATION - POP WITH BOUNCE ============
    private IEnumerator SlotSummonAnimation(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotImages.Count)
            yield break;

        Transform slotTransform = slotImages[slotIndex].transform;
        Vector3 originalScale = slotTransform.localScale;
        
        // SPAWN THE EFFECT ON TOP
        if (summonEffectPrefab != null)
        {
            Debug.Log($"[Summon Effect] Spawning effect at slot {slotIndex}");
            
            GameObject effect = Instantiate(
                summonEffectPrefab, 
                slotTransform // Parent directly to the slot
            );
            
            // Set up as UI element
            RectTransform rt = effect.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localPosition = effectOffset; // Use offset
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling(); // Render on top
            }
            
            Destroy(effect, effectDuration);
        }
        else
        {
            Debug.LogWarning("[Summon Effect] summonEffectPrefab is NULL!");
        }
        
        // Start from zero scale
        slotTransform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Bounce effect using sine wave
            float bounce = Mathf.Sin(t * Mathf.PI);
            float scale = t + (bounce * 0.3f);
            
            slotTransform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Ensure final scale is correct
        slotTransform.localScale = originalScale;
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
        
        // Decrease count instead of clearing entirely
        if (storedTroops[index].count > 1)
        {
            storedTroops[index].count--;
        }
        else
        {
            storedTroops[index].troop = null;
            storedTroops[index].count = 0;
        }
        
        TryAutoCombine();

        RefreshUI();
    }
    
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
        
        // Consume the units
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
                Debug.Log($"✨ [Merge] Successfully merged 3x {currentRarity} into 1x {upgradedTroop.displayName} ({nextRarity})!");
            }
            else
            {
                Debug.LogWarning("[Merge] Inventory full! Could not add merged troop.");
            }
        }
        else
        {
            Debug.LogError($"[Merge] No troops found for rarity: {nextRarity}");
        }
        
        RefreshUI();
    }

    private void TryAutoCombine()
    {
        Dictionary<string, List<(int index, StoredTroopSlot slot)>> troopsById =
            new Dictionary<string, List<(int index, StoredTroopSlot slot)>>();

        for (int i = 0; i < storedTroops.Count; i++)
        {
            var slot = storedTroops[i];
            if (slot.troop != null && slot.count > 0)
            {
                string troopId = slot.troop.id; 
                
                if (!troopsById.ContainsKey(troopId))
                {
                    troopsById[troopId] = new List<(int index, StoredTroopSlot slot)>();
                }
                troopsById[troopId].Add((i, slot));
            }
        }

        foreach (var kvp in troopsById)
        {
            List<(int index, StoredTroopSlot slot)> slotsOfType = kvp.Value;
            string troopId = kvp.Key;

            if (slotsOfType.Count < 2) continue; 

            slotsOfType.Sort((a, b) => a.slot.count.CompareTo(b.slot.count));

            for (int i = 0; i < slotsOfType.Count; i++)
            {
                (int sourceIndex, StoredTroopSlot sourceSlot) = slotsOfType[i];
                
                if (sourceSlot.count <= 0 || sourceSlot.count >= maxUnitsPerSlot) continue; 

                List<(int index, StoredTroopSlot slot)> targets = new List<(int, StoredTroopSlot)>();
                for(int j = 0; j < slotsOfType.Count; j++)
                {
                    if (i != j && slotsOfType[j].slot.count < maxUnitsPerSlot && slotsOfType[j].slot.count > 0)
                    {
                        targets.Add(slotsOfType[j]);
                    }
                }
                
                targets.Sort((a, b) => b.slot.count.CompareTo(a.slot.count));

                foreach ((int targetIndex, StoredTroopSlot targetSlot) in targets)
                {
                    
                    int spaceInTarget = maxUnitsPerSlot - targetSlot.count;
                    int amountToMove = Mathf.Min(spaceInTarget, sourceSlot.count);
                    
                    if (amountToMove > 0)
                    {
                        targetSlot.count += amountToMove;
                        sourceSlot.count -= amountToMove;
                        
                        Debug.Log($"[AutoCombine] Moved {amountToMove}x {troopId} from slot {sourceIndex} (now {sourceSlot.count}) to slot {targetIndex} (now {targetSlot.count}).");
                        
                        if (sourceSlot.count <= 0)
                        {
                            sourceSlot.troop = null;
                            sourceSlot.count = 0;
                            Debug.Log($"[AutoCombine] Source slot {sourceIndex} is now empty.");
                        }
                        
                        TryAutoCombine();
                        return; 
                    }
                }
            }
        }
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
                return TroopRarity.Legendary;
            case TroopRarity.Mythic:
                return TroopRarity.Mythic;
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
        
        return GachaManager.Instance.GetRandomTroopOfRarity(rarity);
    }

    public void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            var slot = storedTroops[i];

            // Update slot image
            if (slot.troop == null)
            {
                slotImages[i].sprite = emptySlotSprite;
                slotImages[i].color = Color.white;
                
                // Clear count text
                if (i < slotCountTexts.Count && slotCountTexts[i] != null)
                {
                    slotCountTexts[i].text = "";
                }
                
                // Hide merge button
                if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
                {
                    mergeButtons[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // Set troop sprite
                SpriteRenderer sr = slot.troop.playerPrefab?.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    slotImages[i].sprite = sr.sprite;
                    slotImages[i].color = Color.white;
                }
                
                // Update count text - ALWAYS SHOW COUNT
                if (i < slotCountTexts.Count && slotCountTexts[i] != null)
                {
                    slotCountTexts[i].text = "x" + slot.count;
                    slotCountTexts[i].gameObject.SetActive(true);
                    
                    // Make text more visible
                    slotCountTexts[i].color = Color.white;
                    slotCountTexts[i].fontSize = 24;
                    slotCountTexts[i].fontStyle = FontStyles.Bold;
                    
                    Debug.Log($"[Inventory] Slot {i}: {slot.troop.displayName} x{slot.count}");
                }
                
                // Show merge button if applicable
                if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
                {
                    bool canMerge = slot.count >= maxUnitsPerSlot && slot.troop.rarity != TroopRarity.Mythic;
                    mergeButtons[i].gameObject.SetActive(canMerge);
                }
            }
        }
    }
}