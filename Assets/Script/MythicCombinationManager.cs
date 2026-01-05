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
    public Image mythicRecipeImage; // Shows the combined recipe sprite
    public Button vampireButton;
    public Button samuraiButton;
    public Button craftMythicButton;
    public Button closeButton; // ✅ NEW: Close button
    public TextMeshProUGUI messageText;
    
    [Header("Recipe Sprites")]
    [Tooltip("Combined sprite showing Vampire recipe")]
    public Sprite vampireRecipeSprite;
    [Tooltip("Combined sprite showing Samurai recipe")]
    public Sprite samuraiRecipeSprite;
    
    private MythicRecipe selectedRecipe;
    private int currentRecipeIndex = 0; // 0 = Vampire, 1 = Samurai
    
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
        
        // Setup button listeners
        if (vampireButton != null)
            vampireButton.onClick.AddListener(() => SelectRecipe(0)); // Vampire
        
        if (samuraiButton != null)
            samuraiButton.onClick.AddListener(() => SelectRecipe(1)); // Samurai
        
        if (craftMythicButton != null)
            craftMythicButton.onClick.AddListener(CraftSelectedRecipe);
        
        // ✅ NEW: Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMythicPanel);
    }
    
    public void OpenMythicPanel()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (mythicCombinationPanel != null)
        {
            mythicCombinationPanel.SetActive(true);
            
            // Default to Vampire recipe (index 0)
            currentRecipeIndex = 0;
            SelectRecipe(0);
            
            DisplayMessage("");
        }
    }
    
    public void CloseMythicPanel()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        if (mythicCombinationPanel != null)
            mythicCombinationPanel.SetActive(false);
    }
    
    private void SelectRecipe(int recipeIndex)
    {
        AudioManager.Instance?.PlayButtonClick();

        currentRecipeIndex = recipeIndex;
        
        // Find the recipe by index
        if (recipeIndex >= 0 && recipeIndex < mythicRecipes.Count)
        {
            selectedRecipe = mythicRecipes[recipeIndex];
            
            // Update the recipe image based on selection
            if (mythicRecipeImage != null)
            {
                if (recipeIndex == 0 && vampireRecipeSprite != null)
                {
                    mythicRecipeImage.sprite = vampireRecipeSprite;
                    Debug.Log("[MythicCombination] Showing Vampire recipe");
                }
                else if (recipeIndex == 1 && samuraiRecipeSprite != null)
                {
                    mythicRecipeImage.sprite = samuraiRecipeSprite;
                    Debug.Log("[MythicCombination] Showing Samurai recipe");
                }
            }
            
            // Update button visual states
            UpdateButtonStates();
            
            // Check if player can craft this recipe
            UpdateCraftButtonState();
            
            DisplayMessage("");
        }
        else
        {
            Debug.LogError($"[MythicCombination] Invalid recipe index: {recipeIndex}");
        }
    }
    
    private void UpdateButtonStates()
    {
        // Visual feedback - highlight selected button
        if (vampireButton != null)
        {
            var vampireColors = vampireButton.colors;
            vampireColors.normalColor = (currentRecipeIndex == 0) ? Color.yellow : Color.white;
            vampireButton.colors = vampireColors;
        }
        
        if (samuraiButton != null)
        {
            var samuraiColors = samuraiButton.colors;
            samuraiColors.normalColor = (currentRecipeIndex == 1) ? Color.yellow : Color.white;
            samuraiButton.colors = samuraiColors;
        }
    }
    
    private void UpdateCraftButtonState()
    {
        if (craftMythicButton == null || selectedRecipe == null) return;
        
        Dictionary<TroopData, int> availableTroops = GetAvailableTroopsFromInventory();
        bool canCraft = selectedRecipe.CanCraft(availableTroops);
        
        craftMythicButton.interactable = canCraft;
        
        // Visual feedback
        var craftColors = craftMythicButton.colors;
        craftColors.normalColor = canCraft ? Color.green : Color.gray;
        craftMythicButton.colors = craftColors;
        
        if (!canCraft)
        {
            DisplayMessage("Not enough ingredients!");
        }
    }
    
    private void CraftSelectedRecipe()
    {
        AudioManager.Instance?.PlayButtonClick();

        DisplayMessage("");
        
        if (selectedRecipe == null)
        {
            DisplayMessage("Select a recipe!");
            Debug.LogWarning("[MythicCombination] No recipe selected!");
            return;
        }
        
        Dictionary<TroopData, int> availableTroops = GetAvailableTroopsFromInventory();
        
        // Check if the player has enough ingredients
        if (!selectedRecipe.CanCraft(availableTroops))
        {
            DisplayMessage("Not enough ingredients!");
            Debug.LogWarning($"[MythicCombination] Failed to craft {selectedRecipe.recipeName}: Not enough ingredients.");
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
        
        // Add Mythic result to inventory
        bool added = TroopInventory.Instance.AddTroop(new TroopInstance(selectedRecipe.resultMythicTroop));
        
        if (added)
        {
            Debug.Log($"[MythicCombination] Successfully crafted {selectedRecipe.resultMythicTroop.displayName}!");
            DisplayMessage($"✅ Crafted {selectedRecipe.resultMythicTroop.displayName}!");
            
            // Play mythic sound effect
            if (AudioManager.Instance != null && AudioManager.Instance.mythicSFX != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.mythicSFX);
            
            TroopInventory.Instance.RefreshUI();
            
            // Update craft button state after crafting
            UpdateCraftButtonState();
        }
        else
        {
            DisplayMessage("Inventory full!");
            Debug.LogWarning("[MythicCombination] Inventory full! Could not add Mythic troop.");
            
            // Refund ingredients since crafting failed
            foreach (var ingredient in selectedRecipe.ingredients)
            {
                for (int i = 0; i < ingredient.quantity; i++)
                {
                    TroopInventory.Instance.AddTroop(new TroopInstance(ingredient.requiredTroop));
                }
            }
        }
    }
    
    private Dictionary<TroopData, int> GetAvailableTroopsFromInventory()
    {
        Dictionary<TroopData, int> result = new Dictionary<TroopData, int>();
        
        if (TroopInventory.Instance == null)
            return result;
        
        foreach (var slot in TroopInventory.Instance.storedTroops)
        {
            if (slot.Data != null)
            {
                if (result.ContainsKey(slot.Data))
                    result[slot.Data] += slot.count;
                else
                    result[slot.Data] = slot.count;
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
            
            if (slot.Data == troop && slot.count > 0)
            {
                slot.count--;
                
                if (slot.count <= 0)
                {
                    slot.troopInstance = null;
                    slot.count = 0;
                }
                
                return;
            }
        }
    }
    
    private void DisplayMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}