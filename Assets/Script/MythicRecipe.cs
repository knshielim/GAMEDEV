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
    
    /// <summary>
    /// Check if the given inventory can fulfill this recipe
    /// </summary>
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
    
    public string GetRecipeDescription()
    {
        string desc = $"<b>Create {resultMythicTroop.displayName}:</b>\n\n";
        
        foreach (var ingredient in ingredients)
        {
            string spriteName = GetSpriteNameFromTroop(ingredient.requiredTroop);
            
            if (!string.IsNullOrEmpty(spriteName))
            {
                desc += $"• <sprite name=\"{spriteName}\"> {ingredient.quantity}x {ingredient.requiredTroop.displayName}\n";
            }
            else
            {
                desc += $"• {ingredient.quantity}x {ingredient.requiredTroop.displayName}\n";
            }
        }
        
        return desc;
    }
    
    private string GetSpriteNameFromTroop(TroopData troop)
    {
        if (troop == null || troop.playerPrefab == null)
            return null;
            
        SpriteRenderer sr = troop.playerPrefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return sr.sprite.name;
        }
        
        return null;
    }
}