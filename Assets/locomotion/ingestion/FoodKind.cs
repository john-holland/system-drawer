using System;
using System.Collections.Generic;
using UnityEngine;

public enum FoodKind
{
    Meat,
    Cheese,
    FruitVegetable
}

[Serializable]
public sealed class FoodNutrientProfile
{
    public float bloodSugarDelta01;
    public float vitaminsDelta01;
    public float hydrationDelta01;
    public float lipidsDelta01;
    public bool useExplicitNutrients;
}

[Serializable]
public sealed class FoodSmellTag
{
    public string signature = "garlic";
    [Range(0f, 2f)] public float intensity = 1f;
}

/// <summary>World food item for chew bake + digest.</summary>
[AddComponentMenu("Locomotion/Ingestion/Food Item")]
public sealed class FoodItem : MonoBehaviour
{
    public FoodKind kind = FoodKind.Meat;
    public FoodNutrientProfile nutrients = new FoodNutrientProfile();
    public Texture2D maskInedible;
    public UnityEngine.Object openCloseTopologyAsset;
    public float mouthfeelLongevitySeconds = 8f;
    public List<FoodSmellTag> smellTags = new List<FoodSmellTag>();
    public MeshFilter meshFilter;
    public float biteFitRadius = 0.02f;
    public bool createPoopContribution = true;

    void Awake()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
    }
}
