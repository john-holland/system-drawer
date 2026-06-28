using System.Collections.Generic;

/// <summary>Registry of active rope pathing footprints for multibody clearance.</summary>
public static class RopePathingFootprintRegistry
{
    static readonly List<RopePathingFootprint> s_all = new List<RopePathingFootprint>(8);

    public static IReadOnlyList<RopePathingFootprint> All => s_all;

    public static void Register(RopePathingFootprint footprint)
    {
        if (footprint != null && !s_all.Contains(footprint))
            s_all.Add(footprint);
    }

    public static void Unregister(RopePathingFootprint footprint)
    {
        if (footprint != null)
            s_all.Remove(footprint);
    }
}
