using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MythicIngredient
{
    public TroopData requiredTroop;
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "NewMythicRecipe", menuName = "Game/Mythic Recipe")]
public class MythicRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName;
    
    [Header("Result")]
    public TroopData resultMythicTroop;
    
    [Header("Required Ingredients")]
    public List<MythicIngredient> ingredients = new List<MythicIngredient>();

    /// Check if the given inventory can fulfill this recipe
    public bool CanCraft(Dictionary<TroopData, int> availableTroops)
    {
        foreach (var ingredient in ingredients)
        {
            if (!availableTroops.ContainsKey(ingredient.requiredTroop))
                return false;
                
            if (availableTroops[ingredient.requiredTroop] < ingredient.quantity)
                return false;
        }
        
        return true;
    }

    /// Get a human-readable description of the recipe
    public string GetRecipeDescription()
    {
        string desc = $"Create {resultMythicTroop.displayName}:\n";
        
        foreach (var ingredient in ingredients)
        {
            desc += $"• {ingredient.quantity}x {ingredient.requiredTroop.displayName}\n";
        }
        
        return desc;
    }
}