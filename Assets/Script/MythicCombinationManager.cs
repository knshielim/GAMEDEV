using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class MythicCombinationManager : MonoBehaviour
{
    public static MythicCombinationManager Instance { get; private set; }
    
    [Header("Mythic Recipes")]
    [Tooltip("All available Mythic combination recipes")]
    public List<MythicRecipe> mythicRecipes = new List<MythicRecipe>();
    
    [Header("UI References")]
    public GameObject mythicCombinationPanel;
    public Transform recipeListContainer;
    public GameObject recipeButtonPrefab;
    public TextMeshProUGUI recipeDescriptionText;
    public Button craftButton;
    
    private MythicRecipe selectedRecipe;
    
    [Header("Icon Display")]
    public Transform iconContainer; 
    public GameObject iconPrefab; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (mythicCombinationPanel != null)
            mythicCombinationPanel.SetActive(false);
            
        if (craftButton != null)
            craftButton.onClick.AddListener(CraftSelectedRecipe);
            
        RefreshRecipeList();
    }
    
    public void OpenMythicPanel()
    {
        if (mythicCombinationPanel != null)
        {
            mythicCombinationPanel.SetActive(true);
            RefreshRecipeList();
        }
    }
    
    public void CloseMythicPanel()
    {
        if (mythicCombinationPanel != null)
            mythicCombinationPanel.SetActive(false);
    }
    
    private void RefreshRecipeList()
    {
        if (recipeListContainer == null || recipeButtonPrefab == null)
            return;
            
        // Clear existing buttons
        foreach (Transform child in recipeListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Get available troops from inventory
        Dictionary<TroopData, int> availableTroops = GetAvailableTroopsFromInventory();
        
        // Create button for each recipe
        foreach (var recipe in mythicRecipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = recipe.resultMythicTroop.displayName;
            
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                MythicRecipe r = recipe; // Capture for closure
                btn.onClick.AddListener(() => SelectRecipe(r));
                
                // Recipe buttons should always be interactable
                btn.interactable = true; 
                
                // Visual feedback (keep visual feedback based on canCraft)
                bool canCraft = recipe.CanCraft(availableTroops);
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = canCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }
    }
    
    private void SelectRecipe(MythicRecipe recipe)
    {
        selectedRecipe = recipe;
        
        // Clear old icons
        foreach (Transform child in iconContainer)
            Destroy(child.gameObject);
        
        // Build description (only header now)
        string desc = $"<b>Create {recipe.resultMythicTroop.displayName}</b>\n\n<b>Required:</b>\n"; 
        
        foreach (var ingredient in recipe.ingredients)
        {
            // Create icon-name pair
            GameObject iconPair = Instantiate(iconPrefab, iconContainer);

            // Get Image component (assuming it's on the root or a child)
            Image img = iconPair.GetComponent<Image>();
            if (img == null) // Fallback if Image is a child
            {
                 img = iconPair.GetComponentInChildren<Image>();
            }
            
            if (ingredient.requiredTroop?.playerPrefab != null)
            {
                var sr = ingredient.requiredTroop.playerPrefab.GetComponent<SpriteRenderer>();
                if (sr?.sprite != null)
                    img.sprite = sr.sprite;
            }
            
            // Get TextMeshProUGUI component (assuming it's a child)
            TextMeshProUGUI nameText = iconPair.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = $"{ingredient.quantity}x {ingredient.requiredTroop.displayName}";
            }
        }
        
        if (recipeDescriptionText != null)
            recipeDescriptionText.text = desc; // Assign only the header text
    }
    
    private void CraftSelectedRecipe()
    {
        if (selectedRecipe == null)
        {
            Debug.LogWarning("[MythicCombination] No recipe selected!");
            return;
        }
        
        Dictionary<TroopData, int> availableTroops = GetAvailableTroopsFromInventory();
        
        if (!selectedRecipe.CanCraft(availableTroops))
        {
            Debug.LogWarning("[MythicCombination] Cannot craft: missing ingredients!");
            return;
        }
        
        // Consume ingredients
        foreach (var ingredient in selectedRecipe.ingredients)
        {
            for (int i = 0; i < ingredient.quantity; i++)
            {
                RemoveTroopFromInventory(ingredient.requiredTroop);
            }
        }
        
        // Add Mythic result
        bool added = TroopInventory.Instance.AddTroop(selectedRecipe.resultMythicTroop);
        
        if (added)
        {
            Debug.Log($"[MythicCombination] Successfully crafted {selectedRecipe.resultMythicTroop.displayName}!");
            TroopInventory.Instance.RefreshUI();
            RefreshRecipeList();
        }
        else
        {
            Debug.LogWarning("[MythicCombination] Inventory full! Could not add Mythic troop.");
            // TODO: Return ingredients to player
        }
    }
    
    private Dictionary<TroopData, int> GetAvailableTroopsFromInventory()
    {
        Dictionary<TroopData, int> result = new Dictionary<TroopData, int>();
        
        if (TroopInventory.Instance == null)
            return result;
        
        foreach (var slot in TroopInventory.Instance.storedTroops)
        {
            if (slot.troop != null)
            {
                if (result.ContainsKey(slot.troop))
                    result[slot.troop] += slot.count;
                else
                    result[slot.troop] = slot.count;
            }
        }
        
        return result;
    }
    
    private void RemoveTroopFromInventory(TroopData troop)
    {
        if (TroopInventory.Instance == null)
            return;
            
        // Find first slot with this troop
        for (int i = 0; i < TroopInventory.Instance.storedTroops.Count; i++)
        {
            var slot = TroopInventory.Instance.storedTroops[i];
            
            if (slot.troop == troop && slot.count > 0)
            {
                slot.count--;
                
                if (slot.count <= 0)
                {
                    slot.troop = null;
                    slot.count = 0;
                }
                
                return;
            }
        }
    }
}