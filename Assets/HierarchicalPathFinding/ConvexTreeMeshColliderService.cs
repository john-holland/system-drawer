using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caches world-space triangle octrees for convex <see cref="MeshCollider"/> instances (main thread only).
/// </summary>
public static class ConvexTreeMeshColliderService
{
    sealed class Entry
    {
        public ConvexMeshTreeCache Cache;
        public int MeshSourceId;
        public Vector3 LossyScaleKey;
    }

    static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>();
    static int _nextBuildVersion = 1;

    /// <summary>Quantized lossy scale so tiny drift does not bust cache unnecessarily.</summary>
    static Vector3 QuantizeScale(Vector3 s)
    {
        const float q = 1000f;
        return new Vector3(
            Mathf.Round(s.x * q) / q,
            Mathf.Round(s.y * q) / q,
            Mathf.Round(s.z * q) / q);
    }

    static bool KeyMatches(MeshCollider mc, Entry e)
    {
        if (mc == null || e == null || mc.sharedMesh == null)
            return false;
        return mc.sharedMesh.GetInstanceID() == e.MeshSourceId &&
               QuantizeScale(mc.transform.lossyScale) == e.LossyScaleKey;
    }

    /// <summary>Returns false if collider is null, non-convex, or has no mesh.</summary>
    public static bool EnsureBuilt(
        MeshCollider meshCollider,
        int maxDepth = ConvexMeshTreeCacheBuilder.DefaultMaxDepth,
        float minLeafExtent = ConvexMeshTreeCacheBuilder.DefaultMinExtent,
        int maxTrianglesPerLeaf = ConvexMeshTreeCacheBuilder.DefaultMaxTrianglesPerLeaf)
    {
        if (meshCollider == null || !meshCollider.convex || meshCollider.sharedMesh == null)
            return false;

        int id = meshCollider.GetInstanceID();
        if (Entries.TryGetValue(id, out Entry existing) && KeyMatches(meshCollider, existing) && existing.Cache != null && existing.Cache._leaves.Count > 0)
            return true;

        var built = ConvexMeshTreeCacheBuilder.Build(meshCollider, maxDepth, minLeafExtent, maxTrianglesPerLeaf);
        built.BuildVersion = _nextBuildVersion++;

        Entries[id] = new Entry
        {
            Cache = built,
            MeshSourceId = meshCollider.sharedMesh.GetInstanceID(),
            LossyScaleKey = QuantizeScale(meshCollider.transform.lossyScale)
        };

        return built._leaves.Count > 0;
    }

    public static bool TryGetCache(MeshCollider meshCollider, out ConvexMeshTreeCache cache)
    {
        cache = null;
        if (meshCollider == null)
            return false;
        if (!Entries.TryGetValue(meshCollider.GetInstanceID(), out Entry e) || e?.Cache == null)
            return false;
        if (!KeyMatches(meshCollider, e))
            return false;
        cache = e.Cache;
        return cache._leaves.Count > 0;
    }

    public static void Invalidate(MeshCollider meshCollider)
    {
        if (meshCollider == null)
            return;
        Entries.Remove(meshCollider.GetInstanceID());
    }

    public static void InvalidateAll()
    {
        Entries.Clear();
    }
}
