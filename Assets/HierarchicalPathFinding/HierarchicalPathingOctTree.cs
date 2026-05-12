using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recursive octree subdivision over a world axis-aligned bounds. Leaves carry blocked flags from a sampler.
/// Used as a hierarchical backing structure for 3D pathfinding over leaf adjacency (see <see cref="FindPathThroughLeaves"/>).
/// </summary>
public sealed class HierarchicalPathingOctTree
{
    public sealed class Leaf
    {
        public Bounds bounds;
        public bool blocked;
        public Vector3 Center => bounds.center;
    }

    readonly List<Leaf> leaves = new List<Leaf>();

    public IReadOnlyList<Leaf> Leaves => leaves;

    /// <summary>Build octree leaves by recursively subdividing until depth or minimum leaf extent is reached.</summary>
    public static HierarchicalPathingOctTree Build(Bounds rootBounds, int maxDepth, float minLeafExtent, Func<Vector3, bool> isBlockedAtCenter)
    {
        var tree = new HierarchicalPathingOctTree();
        BuildRecursive(tree.leaves, rootBounds, maxDepth, minLeafExtent, isBlockedAtCenter);
        return tree;
    }

    static void BuildRecursive(List<Leaf> outLeaves, Bounds bounds, int depthRemaining, float minLeafExtent, Func<Vector3, bool> isBlockedAtCenter)
    {
        float maxExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 2f;

        if (depthRemaining <= 0 || maxExtent <= minLeafExtent + 1e-5f)
        {
            outLeaves.Add(new Leaf
            {
                bounds = bounds,
                blocked = isBlockedAtCenter != null && isBlockedAtCenter(bounds.center)
            });
            return;
        }

        Vector3 halfSize = bounds.size * 0.5f;
        Vector3 quarter = halfSize * 0.5f;

        for (int i = 0; i < 8; i++)
        {
            float ox = (i & 1) != 0 ? quarter.x : -quarter.x;
            float oy = (i & 2) != 0 ? quarter.y : -quarter.y;
            float oz = (i & 4) != 0 ? quarter.z : -quarter.z;
            Vector3 childCenter = bounds.center + new Vector3(ox, oy, oz);
            Bounds child = new Bounds(childCenter, halfSize);
            BuildRecursive(outLeaves, child, depthRemaining - 1, minLeafExtent, isBlockedAtCenter);
        }
    }

    /// <summary>A* over leaf centers using face-adjacency between non-blocked leaves.</summary>
    public static List<Vector3> FindPathThroughLeaves(IReadOnlyList<Leaf> leaves, Vector3 startWorld, Vector3 goalWorld, int maxExpandedNodes = 50000)
    {
        if (leaves == null || leaves.Count == 0)
            return new List<Vector3>();

        int startIdx = FindClosestLeafIndex(leaves, startWorld, requireWalkable: true);
        int goalIdx = FindClosestLeafIndex(leaves, goalWorld, requireWalkable: true);
        if (startIdx < 0 || goalIdx < 0)
            return new List<Vector3>();

        var adj = BuildAdjacency(leaves);

        int n = leaves.Count;
        float[] gScore = new float[n];
        float[] bestF = new float[n];
        int[] cameFrom = new int[n];
        bool[] closed = new bool[n];

        for (int i = 0; i < n; i++)
        {
            gScore[i] = float.PositiveInfinity;
            bestF[i] = float.PositiveInfinity;
            cameFrom[i] = -1;
        }

        float Heuristic(int i) => Vector3.Distance(leaves[i].Center, leaves[goalIdx].Center);

        var open = new SortedSet<OctNode>(new OctNodeComparer());
        gScore[startIdx] = 0f;
        float h0 = Heuristic(startIdx);
        bestF[startIdx] = h0;
        open.Add(new OctNode(startIdx, h0));

        int expanded = 0;
        while (open.Count > 0)
        {
            OctNode cur = GetMin(open);
            int ci = cur.index;

            if (closed[ci])
                continue;
            if (cur.f > bestF[ci] + 1e-6f)
                continue;

            closed[ci] = true;
            expanded++;
            if (maxExpandedNodes > 0 && expanded > maxExpandedNodes)
                break;

            if (ci == goalIdx)
                return Reconstruct(leaves, cameFrom, ci);

            foreach (int ni in adj[ci])
            {
                if (closed[ni])
                    continue;
                float tentative = gScore[ci] + Vector3.Distance(leaves[ci].Center, leaves[ni].Center);
                if (tentative < gScore[ni])
                {
                    cameFrom[ni] = ci;
                    gScore[ni] = tentative;
                    float f = tentative + Heuristic(ni);
                    bestF[ni] = f;
                    open.Add(new OctNode(ni, f));
                }
            }
        }

        return new List<Vector3>();
    }

    static List<Vector3> Reconstruct(IReadOnlyList<Leaf> leaves, int[] cameFrom, int current)
    {
        var path = new List<Vector3>();
        while (current >= 0)
        {
            path.Add(leaves[current].Center);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    static int FindClosestLeafIndex(IReadOnlyList<Leaf> leaves, Vector3 world, bool requireWalkable)
    {
        int best = -1;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < leaves.Count; i++)
        {
            if (requireWalkable && leaves[i].blocked)
                continue;
            float d = (leaves[i].Center - world).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return best;
    }

    /// <summary>Face-adjacent non-blocked leaves (brute force; fine for moderate leaf counts).</summary>
    static List<int>[] BuildAdjacency(IReadOnlyList<Leaf> leaves)
    {
        const float tol = 1e-3f;
        int n = leaves.Count;
        var adj = new List<int>[n];
        for (int i = 0; i < n; i++)
            adj[i] = new List<int>(6);

        for (int i = 0; i < n; i++)
        {
            if (leaves[i].blocked)
                continue;
            Bounds a = leaves[i].bounds;
            for (int j = i + 1; j < n; j++)
            {
                if (leaves[j].blocked)
                    continue;
                Bounds b = leaves[j].bounds;
                if (!FaceAdjacent(a, b, tol))
                    continue;
                adj[i].Add(j);
                adj[j].Add(i);
            }
        }

        return adj;
    }

    /// <summary>True if two AABBs touch on exactly one face (sliding contact).</summary>
    public static bool FaceAdjacent(Bounds a, Bounds b, float tol)
    {
        Vector3 d = b.center - a.center;
        float dx = Mathf.Abs(d.x);
        float dy = Mathf.Abs(d.y);
        float dz = Mathf.Abs(d.z);
        float ex = a.extents.x + b.extents.x;
        float ey = a.extents.y + b.extents.y;
        float ez = a.extents.z + b.extents.z;

        bool touchX = Mathf.Abs(dx - ex) < tol && dy <= ey + tol && dz <= ez + tol;
        bool touchY = Mathf.Abs(dy - ey) < tol && dx <= ex + tol && dz <= ez + tol;
        bool touchZ = Mathf.Abs(dz - ez) < tol && dx <= ex + tol && dy <= ey + tol;

        return touchX || touchY || touchZ;
    }

    readonly struct OctNode
    {
        public readonly int index;
        public readonly float f;

        public OctNode(int index, float f)
        {
            this.index = index;
            this.f = f;
        }
    }

    sealed class OctNodeComparer : IComparer<OctNode>
    {
        public int Compare(OctNode a, OctNode b)
        {
            int cmp = a.f.CompareTo(b.f);
            return cmp != 0 ? cmp : a.index.CompareTo(b.index);
        }
    }

    static OctNode GetMin(SortedSet<OctNode> open)
    {
        OctNode min = default;
        foreach (var n in open)
        {
            min = n;
            break;
        }

        open.Remove(min);
        return min;
    }
}
