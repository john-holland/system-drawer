using System;
using UnityEngine;

public enum ChefSeasonPanMode
{
    InOven,
    OnStove,
    WipeOilAfterClean
}

[Serializable]
public sealed class KitchenTearDownSettings
{
    public bool enableTearDown = true;
    public ChefSeasonPanMode seasonPanMode = ChefSeasonPanMode.WipeOilAfterClean;
    [Range(0f, 1f)] public float oilWipeAmount01 = 0.35f;
    public bool seedDirtyDishes = true;
    public bool emitSeasonPanCards = true;
    public bool emitDishwashingCards = true;
}
