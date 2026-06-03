using System.Collections.Generic;

/// <summary>Resolves builders by view mode.</summary>
public static class MemorySwizzleTreeBuilderRegistry
{
    static readonly IMemorySwizzleTreeBuilder[] Builders =
    {
        new UnitySystemsBuilder(),
        new ComponentTotalsBuilder(),
        new EntityTotalsBuilder(),
        new TypeTreeBuilder(),
        new SceneHierarchyBuilder()
    };

    public static IMemorySwizzleTreeBuilder Get(MemorySwizzleViewMode mode)
    {
        for (int i = 0; i < Builders.Length; i++)
        {
            if (Builders[i].Mode == mode)
                return Builders[i];
        }
        return Builders[0];
    }

    public static bool RequiresSnapshot(MemorySwizzleViewMode mode) =>
        mode != MemorySwizzleViewMode.UnitySystems;
}
