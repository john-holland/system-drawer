using System;
using System.Collections.Generic;

/// <summary>Scene → GameObject → Component hierarchy from snapshot records.</summary>
public sealed class SceneHierarchyBuilder : IMemorySwizzleTreeBuilder
{
    public MemorySwizzleViewMode Mode => MemorySwizzleViewMode.SceneHierarchy;

    public MemorySwizzleNode Build(MemorySwizzleBuildContext ctx)
    {
        var root = MemorySwizzleNode.Create("scenes", "Scene Hierarchy", 0, MemorySwizzleKind.Root, Mode);
        if (ctx.Records == null || ctx.Records.Count == 0)
            return Empty(root);

        var sceneMap = new Dictionary<string, MemorySwizzleNode>(StringComparer.OrdinalIgnoreCase);
        var goNodes = new Dictionary<int, MemorySwizzleNode>();
        var goParent = new Dictionary<int, int>();

        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (r.IsGameObject)
            {
                goParent[r.InstanceId] = r.ParentInstanceId;
                var goNode = MemorySwizzleNode.Create(
                    "go:" + r.InstanceId,
                    string.IsNullOrEmpty(r.Name) ? "GameObject" : r.Name,
                    r.SizeBytes,
                    MemorySwizzleKind.GameObject,
                    Mode,
                    r.ScenePath,
                    r.InstanceId);
                goNodes[r.InstanceId] = goNode;
            }
        }

        for (int i = 0; i < ctx.Records.Count; i++)
        {
            var r = ctx.Records[i];
            if (!r.IsComponent || r.ParentInstanceId == 0 || !goNodes.TryGetValue(r.ParentInstanceId, out var goNode))
                continue;
            goNode.Children.Add(MemorySwizzleNode.Create(
                "comp:" + r.InstanceId,
                string.IsNullOrEmpty(r.Name) ? r.TypeName : r.Name,
                r.SizeBytes,
                MemorySwizzleKind.Component,
                Mode,
                r.ScenePath,
                r.InstanceId));
            goNode.SizeBytes += r.SizeBytes;
        }

        foreach (var kv in goNodes)
        {
            int id = kv.Key;
            var goNode = kv.Value;
            string sceneName = SceneNameFromPath(goNode.Path);
            if (!sceneMap.TryGetValue(sceneName, out var sceneNode))
            {
                sceneNode = MemorySwizzleNode.Create("scene:" + sceneName, sceneName, 0, MemorySwizzleKind.Scene, Mode);
                sceneMap[sceneName] = sceneNode;
                root.Children.Add(sceneNode);
            }

            int parentId = goParent.TryGetValue(id, out var p) ? p : 0;
            if (parentId != 0 && goNodes.TryGetValue(parentId, out var parentGo) && DepthFromRoot(id, goParent) < ctx.MaxHierarchyDepth)
                parentGo.Children.Add(goNode);
            else
                sceneNode.Children.Add(goNode);
        }

        RollupSizes(root);
        SortDesc(root);
        root.ApplyPercentOfParent(root.SizeBytes);
        return root;
    }

    static int DepthFromRoot(int id, Dictionary<int, int> parentMap)
    {
        int depth = 0;
        int cur = id;
        int guard = 0;
        while (guard++ < 64 && parentMap.TryGetValue(cur, out int p) && p != 0)
        {
            depth++;
            cur = p;
        }
        return depth;
    }

    static void RollupSizes(MemorySwizzleNode n)
    {
        if (n.Children.Count == 0)
            return;
        long sum = 0;
        for (int i = 0; i < n.Children.Count; i++)
        {
            RollupSizes(n.Children[i]);
            sum += n.Children[i].SizeBytes;
        }
        if (n.Kind != MemorySwizzleKind.GameObject && n.Kind != MemorySwizzleKind.Component)
            n.SizeBytes = sum;
    }

    static string SceneNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Scene";
        int idx = path.IndexOf('/');
        return idx > 0 ? path.Substring(0, idx) : path;
    }

    static void SortDesc(MemorySwizzleNode n)
    {
        n.Children.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
        for (int i = 0; i < n.Children.Count; i++)
            SortDesc(n.Children[i]);
    }

    static MemorySwizzleNode Empty(MemorySwizzleNode root)
    {
        root.Children.Add(MemorySwizzleNode.Create("empty", "Capture a snapshot first.", 1, MemorySwizzleKind.Other, MemorySwizzleViewMode.SceneHierarchy));
        root.ComputeTotalBytes();
        return root;
    }
}
