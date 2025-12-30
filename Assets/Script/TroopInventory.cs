using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class StoredTroopSlot
{
    public TroopInstance troopInstance;
    public int count;

    public TroopData Data => troopInstance != null ? troopInstance.data : null;
    public bool IsEmpty => troopInstance == null || count <= 0;
}

public class TroopInventory : MonoBehaviour
{
    public static TroopInventory Instance;

    private Dictionary<int, Coroutine> runningAnimations = new Dictionary<int, Coroutine>();

    [Header("UI Inventory Slot Images")]
    public List<Image> slotImages;
    public Sprite emptySlotSprite;

    [Header("UI Inventory Slot Count Text")]
    public List<TextMeshProUGUI> slotCountTexts;

    [Header("Stored Troops")]
    public List<StoredTroopSlot> storedTroops = new List<StoredTroopSlot>();

    [Header("UI Inventory Slot Borders")]
    public List<Image> slotBorders;

    [Header("Selection Visual Settings")]
    public Color selectedBorderColor = Color.yellow;
    public float selectedBorderWidth = 4f;

    [Header("Merge System")]
    public List<Button> mergeButtons;
    public int maxUnitsPerSlot = 3;

    [Header("Animation Settings")]
    public float animationDuration = 0.4f;
    public bool enableSummonAnimation = true;
    public GameObject summonEffectPrefab;
    public Vector3 effectOffset = Vector3.zero;
    public float effectDuration = 1f;

    [Header("Merge Animation Settings")]
    public GameObject mergeEffectPrefab;
    public float mergeAnimationDuration = 0.6f;
    public float mergeEffectDuration = 1.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        while (storedTroops.Count < slotImages.Count)
            storedTroops.Add(new StoredTroopSlot());

        for (int i = 0; i < slotImages.Count; i++)
        {
            int index = i;

            var btn = slotImages[i].GetComponentInParent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    FindObjectOfType<TroopDeployManager>()?.SelectTroop(index);
                });
            }

            if (mergeButtons != null && i < mergeButtons.Count && mergeButtons[i] != null)
            {
                mergeButtons[i].onClick.AddListener(() => MergeUnits(index));
                mergeButtons[i].gameObject.SetActive(false);
            }

            if (slotBorders != null && i < slotBorders.Count && slotBorders[i] != null)
            {
                slotBorders[i].color = selectedBorderColor;
                slotBorders[i].gameObject.SetActive(false);
            }
        }

        RefreshUI();
    }

    // ================= ADD TROOP =================
    public bool AddTroop(TroopInstance instance, bool isMerge = false)
    {
        if (instance == null || instance.data == null)
        {
            Debug.LogWarning("[Inventory] Attempted to add null troop!");
            return false;
        }

        int affectedSlot = -1;

        // Try stacking
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (!storedTroops[i].IsEmpty &&
                storedTroops[i].troopInstance.data.id == instance.data.id &&
                storedTroops[i].count < maxUnitsPerSlot)
            {
                storedTroops[i].count++;
                affectedSlot = i;
                RefreshUI();
                if (enableSummonAnimation) StartSlotAnimation(affectedSlot, isMerge);
                return true;
            }
        }

        // Find empty slot
        for (int i = 0; i < storedTroops.Count; i++)
        {
            if (storedTroops[i].IsEmpty)
            {
                storedTroops[i].troopInstance = instance;
                storedTroops[i].count = 1;
                affectedSlot = i;
                RefreshUI();
                if (enableSummonAnimation) StartSlotAnimation(affectedSlot, isMerge);
                return true;
            }
        }

        Debug.Log("[Inventory] FULL!");
        return false;
    }

    // ================= GET TROOP =================
    // Original GetTroop method (returns TroopData)
    public TroopData GetTroop(int index)
    {
        if (index < 0 || index >= storedTroops.Count)
            return null;

        return storedTroops[index].Data;
    }

    // ================= GET INSTANCE =================
    public TroopInstance GetTroopInstance(int index)
    {
        if (index < 0 || index >= storedTroops.Count)
            return null;

        return storedTroops[index].troopInstance;
    }

    // ================= CLEAR =================
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= storedTroops.Count) return;

        if (storedTroops[index].count > 1)
            storedTroops[index].count--;
        else
        {
            storedTroops[index].troopInstance = null;
            storedTroops[index].count = 0;
        }

        RefreshUI();
    }

    // ================= MERGE =================
    public void MergeUnits(int slotIndex)
    {
        var slot = storedTroops[slotIndex];
        if (slot.IsEmpty || slot.count < maxUnitsPerSlot) return;

        TroopRarity next = GetNextRarity(slot.Data.rarity);
        if (next == slot.Data.rarity) return;

        slot.count -= maxUnitsPerSlot;
        if (slot.count <= 0)
            slot.troopInstance = null;

        TroopData upgradedData = GetRandomTroopOfRarity(next);
        if (upgradedData != null)
        {
            AddTroop(new TroopInstance(upgradedData), true);
        }

        RefreshUI();
    }

    private TroopRarity GetNextRarity(TroopRarity current)
    {
        switch (current)
        {
            case TroopRarity.Common: return TroopRarity.Rare;
            case TroopRarity.Rare: return TroopRarity.Epic;
            case TroopRarity.Epic: return TroopRarity.Legendary;
            default: return current;
        }
    }

    private TroopData GetRandomTroopOfRarity(TroopRarity rarity)
    {
        return GachaManager.Instance?.GetRandomTroopOfRarity(rarity);
    }

    // ================= UI =================
    public void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            var slot = storedTroops[i];

            if (slot.IsEmpty)
            {
                slotImages[i].sprite = emptySlotSprite;
                slotCountTexts[i].text = "";
                mergeButtons[i].gameObject.SetActive(false);
            }
            else
            {
                SpriteRenderer sr = slot.Data.playerPrefab.GetComponent<SpriteRenderer>();
                slotImages[i].sprite = sr.sprite;
                slotCountTexts[i].text = "x" + slot.count;

                bool canMerge = slot.count >= maxUnitsPerSlot &&
                                slot.Data.rarity != TroopRarity.Mythic &&
                                slot.Data.rarity != TroopRarity.Boss;

                mergeButtons[i].gameObject.SetActive(canMerge);
            }
        }
    }

    // ================= ANIMATIONS =================
    private void StartSlotAnimation(int slotIndex, bool isMerge)
    {
        if (runningAnimations.ContainsKey(slotIndex))
            StopCoroutine(runningAnimations[slotIndex]);

        runningAnimations[slotIndex] =
            StartCoroutine(isMerge ? SlotMergeAnimation(slotIndex) : SlotSummonAnimation(slotIndex));
    }

    private IEnumerator SlotMergeAnimation(int slotIndex)
    {
        Transform t = slotImages[slotIndex].transform;
        Vector3 original = t.localScale;

        float elapsed = 0f;
        while (elapsed < mergeAnimationDuration)
        {
            elapsed += Time.deltaTime;
            t.localScale = original * (1f + Mathf.Sin(elapsed * 10f) * 0.2f);
            yield return null;
        }

        t.localScale = original;
    }

    private IEnumerator SlotSummonAnimation(int slotIndex)
    {
        Transform t = slotImages[slotIndex].transform;
        Vector3 original = t.localScale;
        t.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.zero, original, elapsed / animationDuration);
            yield return null;
        }

        t.localScale = original;
    }
}
