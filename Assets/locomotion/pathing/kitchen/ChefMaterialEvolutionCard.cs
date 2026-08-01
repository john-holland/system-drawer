using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Phase-change → smell release descriptors attached to ChefCard cook modes.</summary>
[Serializable]
public class ChefMaterialEvolutionCard
{
    public string materialId = "generic";
    public ChefActivity cookMode = ChefActivity.Sear;
    [Range(0f, 1f)] public float phaseProgress01;
    public List<string> smellDescriptors = new List<string>();
    public bool releaseSmellsOnPhaseChange = true;

    public static ChefMaterialEvolutionCard ForCook(ChefActivity mode, string materialId = "food")
    {
        var c = new ChefMaterialEvolutionCard
        {
            materialId = materialId,
            cookMode = mode,
            smellDescriptors = DefaultSmells(mode)
        };
        return c;
    }

    public static List<string> DefaultSmells(ChefActivity mode)
    {
        switch (mode)
        {
            case ChefActivity.Sear:
                return new List<string> { "fat", "heat", "protein", "carbons", "oils" };
            case ChefActivity.Broil:
                return new List<string> { "heat", "protein", "carbons", "spices" };
            case ChefActivity.Bake:
                return new List<string> { "sugar", "yeast", "water", "lactose", "heat" };
            case ChefActivity.Boil:
                return new List<string> { "water", "salt", "vegetables", "steam" };
            default:
                return new List<string> { "heat", "water" };
        }
    }

    public void Advance(float delta01, Action<IReadOnlyList<string>> onSmellRelease = null)
    {
        float before = phaseProgress01;
        phaseProgress01 = Mathf.Clamp01(phaseProgress01 + delta01);
        if (releaseSmellsOnPhaseChange && before < 0.5f && phaseProgress01 >= 0.5f)
            onSmellRelease?.Invoke(smellDescriptors);
    }
}
