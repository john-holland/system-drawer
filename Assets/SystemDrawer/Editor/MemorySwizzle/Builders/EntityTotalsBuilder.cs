using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Memory rolled up to root GameObject entities.</summary>
public sealed class EntityTotalsBuilder : IMemorySwizzleTreeBuilder
{
    public MemorySwizzleViewMode Mode => MemorySwizzleViewMode.EntityTotals;

    public MemorySwizzleNode Build(MemorySwizzleBuildContext ctx)
    {
        var root = MemorySwizzleNode.Create("entities", "Entity Totals", 0, MemorySwizzleKind.Root, Mode);
        if (ctx.Records == null || ctx.Records.Count == 0)
            return Empty(root);

        var goParent = new Dictionary<int, int>();
        var goMeta = new Dictionary<int, (string name, string path)>();
        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (!r.IsGameObject)
                continue;
            goParent[r.InstanceId] = r.ParentInstanceId;
            goMeta[r.InstanceId] = (r.Name, r.ScenePath);
        }

        var roots = new List<int>();
        foreach (var id in goParent.Keys)
        {
            if (!goParent.TryGetValue(id, out int p) || p == 0 || !goParent.ContainsKey(p))
                roots.Add(id);
        }

        var entityTotals = new Dictionary<int, long>();
        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (r.SizeBytes <= 0)
                continue;
            int ownerGo = r.IsGameObject ? r.InstanceId : r.ParentInstanceId;
            if (ownerGo == 0)
                continue;
            int rootId = FindRoot(ownerGo, goParent);
            if (!entityTotals.ContainsKey(rootId))
                entityTotals[rootId] = 0;
            entityTotals[rootId] += r.SizeBytes;
        }

        foreach (var kv in entityTotals.OrderByDescending(x => x.Value))
        {
            if (ctx.RegisteredEntitiesOnly && !ctx.InstanceIdToRegistryKey.ContainsKey(kv.Key))
                continue;

            goMeta.TryGetValue(kv.Key, out var meta);
            string name = string.IsNullOrEmpty(meta.name) ? "GameObject" : meta.name;
            string path = meta.path ?? "";
            string label = name;
            if (ctx.InstanceIdToRegistryKey.TryGetValue(kv.Key, out var regKey))
                label = regKey + " (" + name + ")";

            root.Children.Add(MemorySwizzleNode.Create(
                "entity:" + kv.Key,
                label,
                kv.Value,
                MemorySwizzleKind.GameObject,
                Mode,
                path,
                kv.Key));
        }

        if (root.Children.Count == 0)
            return Empty(root);

        root.ComputeTotalBytes();
        root.ApplyPercentOfParent(root.SizeBytes);
        return root;
    }

    static int FindRoot(int id, Dictionary<int, int> parentMap)
    {
        int cur = id;
        int guard = 0;
        while (guard++ < 128 && parentMap.TryGetValue(cur, out int p) && p != 0)
            cur = p;
        return cur;
    }

    static MemorySwizzleNode Empty(MemorySwizzleNode root)
    {
        root.Children.Add(MemorySwizzleNode.Create("empty", "Capture a snapshot first.", 1, MemorySwizzleKind.Other, MemorySwizzleViewMode.EntityTotals));
        root.ComputeTotalBytes();
        return root;
    }
}
