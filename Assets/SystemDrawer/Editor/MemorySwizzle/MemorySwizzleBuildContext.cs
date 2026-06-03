using System.Collections.Generic;

/// <summary>Input for tree builders.</summary>
public sealed class MemorySwizzleBuildContext
{
    public MemorySwizzleViewMode Mode;
    public IReadOnlyList<MemorySwizzleObjectRecord> Records = System.Array.Empty<MemorySwizzleObjectRecord>();
    public bool RegisteredEntitiesOnly;
    public float MiscTypeThreshold = 0.001f;
    public int MaxInstancesPerType = 50;
    public int MaxHierarchyDepth = 8;
    public Dictionary<int, string> InstanceIdToRegistryKey = new Dictionary<int, string>();
}
