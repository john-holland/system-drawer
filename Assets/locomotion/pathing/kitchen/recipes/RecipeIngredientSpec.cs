using System;
using System.Collections.Generic;
using UnityEngine;

public enum NarrativeMealPrepActionKind
{
    None,
    PrepFetch,
    PrepWash,
    PrepCook,
    PrepPlate,
    PrepServe,
    TearDown,
    WashDish
}

[Serializable]
public sealed class RecipeIngredientSpec
{
    public RecipeCommoditySpec commodity = new RecipeCommoditySpec();
    public float amount = 1f;
    public string unit = "ea";
    public List<TasteNoteEntry> tasteNotes = new List<TasteNoteEntry>();
}

[Serializable]
public sealed class RecipeStepSpec
{
    public string label = "Step";
    public ChefActivity chefActivity = ChefActivity.Place;
    public NarrativeMealPrepActionKind narrativeAction = NarrativeMealPrepActionKind.PrepCook;
    public string stationHint;
    public float durationSeconds = 4f;
    public List<string> dutyChecklistOverride = new List<string>();
}
