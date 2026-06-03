using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Memory grouped by component / Unity object type.</summary>
public sealed class ComponentTotalsBuilder : IMemorySwizzleTreeBuilder
{
    public MemorySwizzleViewMode Mode => MemorySwizzleViewMode.ComponentTotals;

    public MemorySwizzleNode Build(MemorySwizzleBuildContext ctx)
    {
        var root = MemorySwizzleNode.Create("components", "Component Totals", 0, MemorySwizzleKind.Root, Mode);
        if (ctx.Records == null || ctx.Records.Count == 0)
            return Empty(root);

        var byType = new Dictionary<string, List<MemorySwizzleObjectRecord>>(StringComparer.Ordinal);
        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (r.SizeBytes <= 0)
                continue;
            string key = string.IsNullOrEmpty(r.TypeName) ? "Unknown" : r.TypeName;
            if (!byType.TryGetValue(key, out var list))
            {
                list = new List<MemorySwizzleObjectRecord>();
                byType[key] = list;
            }
            list.Add(r);
        }

        foreach (var kv in byType.OrderByDescending(k => k.Value.Sum(x => x.SizeBytes)))
        {
            long typeTotal = kv.Value.Sum(x => x.SizeBytes);
            var typeNode = MemorySwizzleNode.Create("ctype:" + kv.Key, kv.Key, typeTotal, MemorySwizzleKind.Type, Mode);
            var ordered = kv.Value.OrderByDescending(x => x.SizeBytes).ToList();
            int cap = Math.Max(1, ctx.MaxInstancesPerType);
            long other = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                if (i < cap)
                {
                    typeNode.Children.Add(MemorySwizzleNode.Create(
                        "inst:" + r.InstanceId,
                        string.IsNullOrEmpty(r.Name) ? r.TypeName : r.Name,
                        r.SizeBytes,
                        MemorySwizzleKind.Instance,
                        Mode,
                        r.ScenePath,
                        r.InstanceId));
                }
                else
                    other += r.SizeBytes;
            }
            if (other > 0)
                typeNode.Children.Add(MemorySwizzleNode.Create("other:" + kv.Key, "Other instances", other, MemorySwizzleKind.Other, Mode));
            root.Children.Add(typeNode);
        }

        root.ComputeTotalBytes();
        root.ApplyPercentOfParent(root.SizeBytes);
        return root;
    }

    static MemorySwizzleNode Empty(MemorySwizzleNode root)
    {
        root.Children.Add(MemorySwizzleNode.Create("empty", "Capture a snapshot first.", 1, MemorySwizzleKind.Other, MemorySwizzleViewMode.ComponentTotals));
        root.ComputeTotalBytes();
        return root;
    }
}
