using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cached axis-aligned octree over mesh triangle indices in world space (for convex <see cref="MeshCollider"/>).
/// </summary>
public sealed class ConvexMeshTreeCache
{
    public Bounds RootBounds { get; internal set; }
    public IReadOnlyList<ConvexMeshTreeLeaf> Leaves => _leaves;
    public int BuildVersion { get; internal set; }
    public int MeshInstanceId { get; internal set; }
    public int TriangleCount { get; internal set; }

    internal readonly List<ConvexMeshTreeLeaf> _leaves = new List<ConvexMeshTreeLeaf>();

    internal void ClearLeaves()
    {
        _leaves.Clear();
    }
}

/// <summary>Leaf cell containing triangle indices whose world AABB overlaps this node's bounds.</summary>
public sealed class ConvexMeshTreeLeaf
{
    public Bounds Bounds { get; internal set; }
    public IReadOnlyList<int> TriangleIndices => _triangleIndices;
    internal readonly List<int> _triangleIndices = new List<int>();
}

/// <summary>Builds an octree over triangle AABBs for a convex mesh collider.</summary>
public static class ConvexMeshTreeCacheBuilder
{
    public const int DefaultMaxDepth = 10;
    public const float DefaultMinExtent = 0.05f;
    public const int DefaultMaxTrianglesPerLeaf = 24;

    public static ConvexMeshTreeCache Build(
        MeshCollider meshCollider,
        int maxDepth = DefaultMaxDepth,
        float minLeafExtent = DefaultMinExtent,
        int maxTrianglesPerLeaf = DefaultMaxTrianglesPerLeaf)
    {
        var cache = new ConvexMeshTreeCache();
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return cache;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (verts == null || tris == null || tris.Length < 3)
            return cache;

        Matrix4x4 localToWorld = meshCollider.transform.localToWorldMatrix;

        int triCount = tris.Length / 3;
        var triBounds = new Bounds[triCount];
        var rootBounds = new Bounds();

        bool first = true;
        for (int t = 0; t < triCount; t++)
        {
            Vector3 a = localToWorld.MultiplyPoint3x4(verts[tris[t * 3]]);
            Vector3 b = localToWorld.MultiplyPoint3x4(verts[tris[t * 3 + 1]]);
            Vector3 c = localToWorld.MultiplyPoint3x4(verts[tris[t * 3 + 2]]);
            Bounds bb = new Bounds(a, Vector3.zero);
            bb.Encapsulate(b);
            bb.Encapsulate(c);
            triBounds[t] = bb;
            if (first)
            {
                rootBounds = bb;
                first = false;
            }
            else
                rootBounds.Encapsulate(bb);
        }

        cache.RootBounds = rootBounds;
        cache.TriangleCount = triCount;
        cache.MeshInstanceId = mesh.GetInstanceID();

        var allIndices = new List<int>(triCount);
        for (int i = 0; i < triCount; i++)
            allIndices.Add(i);

        BuildRecursive(
            cache._leaves,
            rootBounds,
            allIndices,
            triBounds,
            maxDepth,
            minLeafExtent,
            maxTrianglesPerLeaf);

        return cache;
    }

    static void BuildRecursive(
        List<ConvexMeshTreeLeaf> outLeaves,
        Bounds nodeBounds,
        List<int> triangleIndices,
        Bounds[] triBounds,
        int depthRemaining,
        float minLeafExtent,
        int maxTrianglesPerLeaf)
    {
        float ext = Mathf.Max(nodeBounds.extents.x, nodeBounds.extents.y, nodeBounds.extents.z) * 2f;

        if (depthRemaining <= 0 ||
            ext <= minLeafExtent + 1e-5f ||
            triangleIndices.Count <= maxTrianglesPerLeaf)
        {
            var leaf = new ConvexMeshTreeLeaf { Bounds = nodeBounds };
            leaf._triangleIndices.AddRange(triangleIndices);
            outLeaves.Add(leaf);
            return;
        }

        Vector3 halfSize = nodeBounds.size * 0.5f;
        Vector3 quarter = halfSize * 0.5f;

        bool anyChild = false;
        var perOctant = new List<int>[8];
        for (int o = 0; o < 8; o++)
            perOctant[o] = new List<int>();

        for (int i = 0; i < triangleIndices.Count; i++)
        {
            int ti = triangleIndices[i];
            Vector3 center = triBounds[ti].center;
            int oct = OctantIndex(nodeBounds.center, center);
            perOctant[oct].Add(ti);
        }

        for (int oct = 0; oct < 8; oct++)
        {
            if (perOctant[oct].Count == 0)
                continue;

            float ox = (oct & 1) != 0 ? quarter.x : -quarter.x;
            float oy = (oct & 2) != 0 ? quarter.y : -quarter.y;
            float oz = (oct & 4) != 0 ? quarter.z : -quarter.z;
            Vector3 childCenter = nodeBounds.center + new Vector3(ox, oy, oz);
            Bounds childBounds = new Bounds(childCenter, halfSize);

            anyChild = true;
            BuildRecursive(outLeaves, childBounds, perOctant[oct], triBounds, depthRemaining - 1, minLeafExtent, maxTrianglesPerLeaf);
        }

        if (!anyChild)
        {
            var leaf = new ConvexMeshTreeLeaf { Bounds = nodeBounds };
            leaf._triangleIndices.AddRange(triangleIndices);
            outLeaves.Add(leaf);
        }
    }

    /// <summary>Octant bitmask from point relative to parent center (split planes through center).</summary>
    static int OctantIndex(Vector3 parentCenter, Vector3 point)
    {
        int ix = point.x >= parentCenter.x ? 1 : 0;
        int iy = point.y >= parentCenter.y ? 2 : 0;
        int iz = point.z >= parentCenter.z ? 4 : 0;
        return ix | iy | iz;
    }
}
