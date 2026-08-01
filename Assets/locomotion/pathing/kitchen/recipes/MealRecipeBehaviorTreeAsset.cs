using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MealRecipeEntry
{
    public RecipeBehaviorTreeAsset recipe;
    public float portionsMultiplier = 1f;
}

/// <summary>Meal compose: multiple recipes, actors, trays, meal-level commodities/taste.</summary>
[CreateAssetMenu(fileName = "MealRecipeBehaviorTree", menuName = "Locomotion/Kitchen/Meal Recipe Behavior Tree")]
public sealed class MealRecipeBehaviorTreeAsset : ScriptableObject
{
    public string mealId;
    public string displayName = "New Meal";
    public List<MealRecipeEntry> recipes = new List<MealRecipeEntry>();
    public List<RecipeCommoditySpec> commodities = new List<RecipeCommoditySpec>();
    public List<string> serveActorKeys = new List<string>();
    public float platesPerActor = 1f;
    public float servesAmount = 1f;
    public TrayBinSettings tray = new TrayBinSettings();
    public List<TasteNoteEntry> tasteNotes = new List<TasteNoteEntry>();
    public string tableLayoutBranchKey;
    public string assignedActorKey;
    public KitchenTearDownSettings tearDown = new KitchenTearDownSettings();

    void OnValidate()
    {
        if (string.IsNullOrEmpty(mealId))
            mealId = name;
    }

    public float TotalServes()
    {
        float s = servesAmount;
        if (recipes == null) return Mathf.Max(0f, s);
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i]?.recipe == null) continue;
            s = Mathf.Max(s, recipes[i].recipe.servesAmount * Mathf.Max(0.01f, recipes[i].portionsMultiplier));
        }
        return s;
    }
}
