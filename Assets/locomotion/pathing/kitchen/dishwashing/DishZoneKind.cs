using System;
using UnityEngine;

/// <summary>Soil gradient: Dirty &lt; Sink &lt; Dishwasher &lt; Dry. Compost optional (scraps only).</summary>
public enum DishZoneKind
{
    Dirty = 0,
    Sink = 1,
    Dishwasher = 2,
    Dry = 3,
    Compost = 4
}

public enum DishFinishPreference
{
    Dishwasher,
    DryingRack,
    Either
}

public enum DishScrubMode
{
    Timing,
    FloodProxy,
    TimingAndFlood
}

public enum DishToolKind
{
    Sponge,
    Spray,
    Either
}

[Serializable]
public sealed class DishZoneBinding
{
    public DishZoneKind kind = DishZoneKind.Dirty;
    public Transform anchor;
    public readonly System.Collections.Generic.List<string> stack = new System.Collections.Generic.List<string>();
}
