using System;
using System.Collections.Generic;

/// <summary>Assembly → namespace → type rollup from snapshot records.</summary>
public sealed class TypeTreeBuilder : IMemorySwizzleTreeBuilder
{
    public MemorySwizzleViewMode Mode => MemorySwizzleViewMode.TypeTree;

    public MemorySwizzleNode Build(MemorySwizzleBuildContext ctx)
    {
        var root = MemorySwizzleNode.Create("types", "Managed & Native Types", 0, MemorySwizzleKind.Root, Mode);
        if (ctx.Records == null || ctx.Records.Count == 0)
            return Empty(root, "Capture a snapshot first.");

        var asmMap = new Dictionary<string, MemorySwizzleNode>(StringComparer.OrdinalIgnoreCase);
        long miscBytes = 0;
        long total = 0;

        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (r.SizeBytes <= 0)
                continue;
            total += r.SizeBytes;

            string typeName = string.IsNullOrEmpty(r.TypeName) ? "Unknown" : r.TypeName;
            var t = r.SystemType;
            string asm = t?.Assembly.GetName().Name ?? "Unknown";
            string ns = t?.Namespace ?? "";
            if (string.IsNullOrEmpty(ns))
                ns = "(global)";

            if (!asmMap.TryGetValue(asm, out var asmNode))
            {
                asmNode = MemorySwizzleNode.Create("asm:" + asm, asm, 0, MemorySwizzleKind.Assembly, Mode);
                asmMap[asm] = asmNode;
                root.Children.Add(asmNode);
            }

            if (!TryGetChild(asmNode, "ns:" + ns, out var nsNode))
            {
                nsNode = MemorySwizzleNode.Create("ns:" + asm + "/" + ns, ns, 0, MemorySwizzleKind.Namespace, Mode);
                asmNode.Children.Add(nsNode);
            }

            if (!TryGetChild(nsNode, "type:" + typeName, out var typeNode))
            {
                typeNode = MemorySwizzleNode.Create("type:" + typeName, typeName, 0, MemorySwizzleKind.Type, Mode);
                nsNode.Children.Add(typeNode);
            }

            typeNode.SizeBytes += r.SizeBytes;
        }

        root.ComputeTotalBytes();
        long rootTotal = root.SizeBytes;
        miscBytes = CollapseSmallTypes(root, rootTotal, ctx.MiscTypeThreshold);
        if (miscBytes > 0)
            root.Children.Add(MemorySwizzleNode.Create("misc", "Misc types", miscBytes, MemorySwizzleKind.Other, Mode));

        root.ComputeTotalBytes();
        SortDesc(root);
        root.ApplyPercentOfParent(root.SizeBytes);
        return root;
    }

    static long CollapseSmallTypes(MemorySwizzleNode node, long rootTotal, float threshold)
    {
        long misc = 0;
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            var c = node.Children[i];
            if (c.Kind == MemorySwizzleKind.Type)
            {
                float frac = rootTotal > 0 ? (float)c.SizeBytes / rootTotal : 0f;
                if (frac < threshold)
                {
                    misc += c.SizeBytes;
                    node.Children.RemoveAt(i);
                    continue;
                }
            }
            misc += CollapseSmallTypes(c, rootTotal, threshold);
        }
        return misc;
    }

    static bool TryGetChild(MemorySwizzleNode parent, string id, out MemorySwizzleNode child)
    {
        for (int i = 0; i < parent.Children.Count; i++)
        {
            if (parent.Children[i].Id == id)
            {
                child = parent.Children[i];
                return true;
            }
        }
        child = null;
        return false;
    }

    static void SortDesc(MemorySwizzleNode n)
    {
        n.Children.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
        for (int i = 0; i < n.Children.Count; i++)
            SortDesc(n.Children[i]);
    }

    static MemorySwizzleNode Empty(MemorySwizzleNode root, string msg)
    {
        root.Children.Add(MemorySwizzleNode.Create("empty", msg, 1, MemorySwizzleKind.Other, MemorySwizzleViewMode.TypeTree));
        root.ComputeTotalBytes();
        return root;
    }
}
