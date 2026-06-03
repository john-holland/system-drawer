using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One node in the memory swizzle hierarchy (sizes roll up to parent).</summary>
[Serializable]
public sealed class MemorySwizzleNode
{
    public string Id = "";
    public string Label = "";
    public long SizeBytes;
    public MemorySwizzleKind Kind = MemorySwizzleKind.Category;
    public MemorySwizzleViewMode OriginMode;
    public string Path = "";
    public int InstanceId;
    public readonly List<MemorySwizzleNode> Children = new List<MemorySwizzleNode>();

    [NonSerialized] public Rect LayoutRect;
    [NonSerialized] public float PercentOfParent;

    public long ComputeTotalBytes()
    {
        if (Children.Count == 0)
            return Math.Max(0, SizeBytes);

        long sum = 0;
        for (int i = 0; i < Children.Count; i++)
            sum += Children[i].ComputeTotalBytes();
        SizeBytes = sum;
        return sum;
    }

    public void ApplyPercentOfParent(long parentBytes)
    {
        PercentOfParent = parentBytes > 0 ? (float)SizeBytes / parentBytes : 0f;
        for (int i = 0; i < Children.Count; i++)
            Children[i].ApplyPercentOfParent(SizeBytes > 0 ? SizeBytes : parentBytes);
    }

    public static MemorySwizzleNode Create(string id, string label, long bytes, MemorySwizzleKind kind,
        MemorySwizzleViewMode mode, string path = "", int instanceId = 0)
    {
        return new MemorySwizzleNode
        {
            Id = id ?? "",
            Label = label ?? "",
            SizeBytes = Math.Max(0, bytes),
            Kind = kind,
            OriginMode = mode,
            Path = path ?? "",
            InstanceId = instanceId
        };
    }
}
