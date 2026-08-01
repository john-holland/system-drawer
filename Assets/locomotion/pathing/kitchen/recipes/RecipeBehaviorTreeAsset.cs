using System.Collections.Generic;
using UnityEngine;

/// <summary>Authorable recipe: ingredients, steps, commodities, taste notes, serves.</summary>
[CreateAssetMenu(fileName = "RecipeBehaviorTree", menuName = "Locomotion/Kitchen/Recipe Behavior Tree")]
public sealed class RecipeBehaviorTreeAsset : ScriptableObject
{
    public string recipeId;
    public string displayName = "New Recipe";
    public List<RecipeIngredientSpec> ingredients = new List<RecipeIngredientSpec>();
    public List<RecipeStepSpec> steps = new List<RecipeStepSpec>();
    public List<RecipeCommoditySpec> commodities = new List<RecipeCommoditySpec>();
    public float servesAmount = 1f;
    public List<TasteNoteEntry> tasteNotes = new List<TasteNoteEntry>();
    [Range(0f, 1f)] public float tasteIntensity01 = 0.5f;
    public ChefDutyMode defaultDutyMode = ChefDutyMode.Line;
    public string defaultStationKind = "Cooking";
    public string tableLayoutBranchKey;
    public KitchenTearDownSettings tearDown = new KitchenTearDownSettings();

    void OnValidate()
    {
        if (string.IsNullOrEmpty(recipeId))
            recipeId = name;
        if (string.IsNullOrEmpty(displayName))
            displayName = name;
    }

    public bool ValidateAmounts(out string error)
    {
        if (servesAmount <= 0f)
        {
            error = "servesAmount must be > 0";
            return false;
        }
        if (ingredients == null || ingredients.Count == 0)
        {
            error = "recipe needs at least one ingredient";
            return false;
        }
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] == null || ingredients[i].amount <= 0f)
            {
                error = $"ingredient[{i}] amount must be > 0";
                return false;
            }
        }
        error = null;
        return true;
    }
}
