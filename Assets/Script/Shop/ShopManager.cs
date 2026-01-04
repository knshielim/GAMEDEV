using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;               // for LINQ
using System.Collections.Generic; // for Dictionary


public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Left Panel")]
    public TroopSlot[] troopSlots;        // Slots
    public TroopData[] shopTroops;        // Available troops

    [Header("Right Panel")]
    public TextMeshProUGUI troopNameText;
    public Image troopImageBig;
    public TextMeshProUGUI statsText;
    public Button upgradeButton;
    [Header("Right Panel")]
    public TextMeshProUGUI priceText;


    private TroopInstance selectedTroopInstance;
    private Dictionary<string, TroopInstance> troopInstances = new Dictionary<string, TroopInstance>();

        private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: If the ShopManager needs to move between scenes (e.g. save Gems)
        }
        else
        {
            Destroy(gameObject);
            return; 
        }
    }

    void Start()
    {
        ShowAllTroops(); // default view shows all troops
    }

    // Show all troops in the left panel
    public void ShowAllTroops()
    {
        // Hide all slots first
        foreach (var slot in troopSlots)
            slot.gameObject.SetActive(false);

        // Populate all troops
        for (int i = 0; i < shopTroops.Length && i < troopSlots.Length; i++)
        {
            if (shopTroops[i] == null) continue;

            troopSlots[i].gameObject.SetActive(true);
            troopSlots[i].Init(shopTroops[i], this);
        }

        ClearSelectedTroop();
    }

    // Filter to display only troops of the given rarity
    public void ShowRarity(TroopRarity rarity)
    {
        var filteredTroops = shopTroops.Where(t => t != null && t.rarity == rarity).ToArray();

        // Hide all slots first
        foreach (var slot in troopSlots)
            slot.gameObject.SetActive(false);

        // Populate only the filtered slots
        for (int i = 0; i < filteredTroops.Length && i < troopSlots.Length; i++)
        {
            troopSlots[i].gameObject.SetActive(true);
            troopSlots[i].Init(filteredTroops[i], this);
        }

        ClearSelectedTroop();
    }
    

    private void UpdateRightPanelUI()
    {
        if (selectedTroopInstance == null) return;

        troopNameText.text = selectedTroopInstance.data.displayName;
        troopImageBig.sprite = selectedTroopInstance.data.icon;
        statsText.text = $"Level: {selectedTroopInstance.level}\n" +
                        $"HP: {selectedTroopInstance.currentHealth}\n" +
                        $"Attack: {selectedTroopInstance.currentAttack}\n" +
                        $"Speed: {selectedTroopInstance.currentMoveSpeed}";

        // Get the upgrade cost
        int cost = selectedTroopInstance.GetUpgradeCost();

        if (cost < 0) // Max level or Boss
        {
            upgradeButton.interactable = false;
            priceText.text = "MAX"; // show MAX if cannot upgrade
        }
        else
        {
            priceText.text = cost.ToString(); // show cost in gems
            // Enable upgrade button only if player has enough gems
            upgradeButton.interactable = GemManager.Instance.HasEnoughTotalGem(cost);
        }
    }

    public void ClearSelectedTroop()
    {
        troopNameText.text = "";
        troopImageBig.sprite = null;
        statsText.text = "";
        upgradeButton.interactable = false;
    }

    public void SelectTroop(TroopSlot slot, TroopData data)
    {
        // 1. Cek apakah troop ini sudah ada di memory
        if (!troopInstances.ContainsKey(data.id))
        {
            // Buat instance baru
            TroopInstance newInstance = new TroopInstance(data);

            // 2. 🔥 LOAD: Cek ke Save System, level berapa troop ini sebenernya?
            if (PersistenceManager.Instance != null)
            {
                int savedLevel = PersistenceManager.Instance.GetTroopLevel(data.id);
                
                // Naikkan level instance ini sampai sama dengan yang disimpan
                while (newInstance.level < savedLevel)
                {
                    newInstance.LevelUp(); 
                }
            }

            troopInstances[data.id] = newInstance;
        }

        selectedTroopInstance = troopInstances[data.id];

        UpdateRightPanelUI();

        foreach (var s in troopSlots)
            s.SetSelected(s == slot);
    }

    public void OnUpgradeButtonClicked()
    {
        if (selectedTroopInstance == null) return;

        // Try to upgrade using gems
        bool upgraded = selectedTroopInstance.TryUpgradeWithGems();

        if (!upgraded)
        {
            // Optional: show a popup "Not enough gems" or play sound
            Debug.Log("Cannot upgrade: not enough gems or max level reached");
            return;
        }

        // Successfully upgraded → update UI
        UpdateRightPanelUI();
    }

    public void ResetAllTroops()
    {
        foreach (var kvp in troopInstances) // troopInstances is your dictionary
        {
            kvp.Value.ResetLevel(); // reset each TroopInstance
        }

        // If you want, also update the UI for the selected troop
        UpdateRightPanelUI();
    }
    public TroopInstance GetOrCreateInstance(TroopData data)
    {
        if (!troopInstances.ContainsKey(data.id))
        {
            TroopInstance newInstance = new TroopInstance(data);

            // Load saved level
            if (PersistenceManager.Instance != null)
            {
                int savedLevel = PersistenceManager.Instance.GetTroopLevel(data.id);
                while (newInstance.level < savedLevel)
                    newInstance.LevelUp();
            }

            troopInstances[data.id] = newInstance;
        }

        return troopInstances[data.id];
    }

    


}
