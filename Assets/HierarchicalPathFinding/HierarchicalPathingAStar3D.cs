using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* on <see cref="HierarchicalPathingVolumeGrid3D"/> with optional per-edge cost multiplier (physics zones).
/// </summary>
public sealed class HierarchicalPathingAStar3D
{
    public struct Settings
    {
        public bool allowDiagonalSteps;
        public int maxExpandedNodes;
        public bool returnBestEffortPathWhenNoPath;
        /// <summary>Optional step cost from cell center to neighbor cell center (defaults to Euclidean distance).</summary>
        public Func<Vector3, Vector3, float> EdgeCost;
    }

    struct Neighbor
    {
        public int x, y, z;
        public float baseDist;
    }

    public static List<Vector3> FindPath(
        HierarchicalPathingVolumeGrid3D grid,
        Vector3 startWorld,
        Vector3 goalWorld,
        Settings settings)
    {
        if (grid == null)
            return new List<Vector3>();

        if (!grid.TryWorldToCell(startWorld, out int sx, out int sy, out int sz))
            return new List<Vector3>();
        if (!grid.TryWorldToCell(goalWorld, out int gx, out int gy, out int gz))
            return new List<Vector3>();

        if (grid.IsBlocked(sx, sy, sz) || grid.IsBlocked(gx, gy, gz))
            return new List<Vector3>();

        int w = grid.width;
        int h = grid.height;
        int slice = w * h;
        int total = slice * grid.depth;

        float[] gScore = new float[total];
        float[] bestF = new float[total];
        int[] cameFrom = new int[total];
        bool[] inClosed = new bool[total];

        for (int i = 0; i < total; i++)
        {
            gScore[i] = float.PositiveInfinity;
            bestF[i] = float.PositiveInfinity;
            cameFrom[i] = -1;
        }

        int Index(int x, int y, int z) => (z * h + y) * w + x;

        int startIdx = Index(sx, sy, sz);
        int goalIdx = Index(gx, gy, gz);
        gScore[startIdx] = 0f;

        var open = new SortedSet<AStarNode>(new NodeComparer());
        float startH = Heuristic(sx, sy, sz, gx, gy, gz);
        bestF[startIdx] = startH;
        open.Add(new AStarNode(startIdx, startH));

        int expanded = 0;
        bool abortedByLimit = false;
        int bestIdx = startIdx;
        float bestH = startH;

        float EdgeCost(Vector3 a, Vector3 b)
        {
            float dist = Vector3.Distance(a, b);
            if (settings.EdgeCost != null)
                dist *= Mathf.Max(0.01f, settings.EdgeCost(a, b));
            return dist;
        }

        IEnumerable<Neighbor> Neighbors(int cx, int cy, int cz)
        {
            // 6-connected
            yield return new Neighbor { x = cx + 1, y = cy, z = cz, baseDist = 1f };
            yield return new Neighbor { x = cx - 1, y = cy, z = cz, baseDist = 1f };
            yield return new Neighbor { x = cx, y = cy + 1, z = cz, baseDist = 1f };
            yield return new Neighbor { x = cx, y = cy - 1, z = cz, baseDist = 1f };
            yield return new Neighbor { x = cx, y = cy, z = cz + 1, baseDist = 1f };
            yield return new Neighbor { x = cx, y = cy, z = cz - 1, baseDist = 1f };

            if (!settings.allowDiagonalSteps)
                yield break;

            float invSqrt2 = 1.41421356f;
            float invSqrt3 = 1.73205081f;
            // 12 edge diagonals
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dy == 0 && dz == 0) continue;
                int nonzero = (dx != 0 ? 1 : 0) + (dy != 0 ? 1 : 0) + (dz != 0 ? 1 : 0);
                float bd = nonzero == 1 ? 1f : (nonzero == 2 ? invSqrt2 : invSqrt3);
                yield return new Neighbor { x = cx + dx, y = cy + dy, z = cz + dz, baseDist = bd };
            }
        }

        while (open.Count > 0)
        {
            AStarNode currentNode = GetAndRemoveMin(open);
            int current = currentNode.index;

            if (inClosed[current])
                continue;
            if (currentNode.f > bestF[current] + 1e-6f)
                continue;

            inClosed[current] = true;
            expanded++;
            if (settings.maxExpandedNodes > 0 && expanded > settings.maxExpandedNodes)
            {
                abortedByLimit = true;
                break;
            }

            if (current == goalIdx)
                return Reconstruct(grid, cameFrom, current, w, h);

            int zz = current / slice;
            int rem = current - zz * slice;
            int cy = rem / w;
            int cx = rem - cy * w;
            int cz = zz;

            {
                float hCur = Heuristic(cx, cy, cz, gx, gy, gz);
                if (hCur < bestH)
                {
                    bestH = hCur;
                    bestIdx = current;
                }
            }

            Vector3 curCenter = grid.CellCenterWorld(cx, cy, cz);

            foreach (var n in Neighbors(cx, cy, cz))
            {
                int nx = n.x, ny = n.y, nz = n.z;
                if (!grid.IsInBounds(nx, ny, nz))
                    continue;
                if (grid.IsBlocked(nx, ny, nz))
                    continue;

                if (settings.allowDiagonalSteps)
                {
                    int dx = Mathf.Abs(nx - cx);
                    int dy = Mathf.Abs(ny - cy);
                    int dz = Mathf.Abs(nz - cz);
                    if (dx + dy + dz > 1)
                    {
                        if (dx > 0 && grid.IsBlocked(nx, cy, cz)) continue;
                        if (dy > 0 && grid.IsBlocked(cx, ny, cz)) continue;
                        if (dz > 0 && grid.IsBlocked(cx, cy, nz)) continue;
                    }
                }

                int ni = Index(nx, ny, nz);
                if (inClosed[ni])
                    continue;

                Vector3 nextCenter = grid.CellCenterWorld(nx, ny, nz);
                float step = EdgeCost(curCenter, nextCenter);
                float tentative = gScore[current] + step;
                if (tentative < gScore[ni])
                {
                    cameFrom[ni] = current;
                    gScore[ni] = tentative;
                    float f = tentative + Heuristic(nx, ny, nz, gx, gy, gz);
                    bestF[ni] = f;
                    open.Add(new AStarNode(ni, f));
                }
            }
        }

        if ((abortedByLimit || settings.returnBestEffortPathWhenNoPath) && bestIdx != startIdx && cameFrom[bestIdx] != -1)
            return Reconstruct(grid, cameFrom, bestIdx, w, h);

        return new List<Vector3>();
    }

    static float Heuristic(int x, int y, int z, int gx, int gy, int gz)
    {
        float dx = gx - x;
        float dy = gy - y;
        float dz = gz - z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    static List<Vector3> Reconstruct(HierarchicalPathingVolumeGrid3D grid, int[] cameFrom, int current, int w, int h)
    {
        var path = new List<Vector3>(64);
        int slice = w * h;

        while (current >= 0)
        {
            int zz = current / slice;
            int rem = current - zz * slice;
            int yy = rem / w;
            int xx = rem - yy * w;
            path.Add(grid.CellCenterWorld(xx, yy, zz));
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    readonly struct AStarNode
    {
        public readonly int index;
        public readonly float f;

        public AStarNode(int index, float f)
        {
            this.index = index;
            this.f = f;
        }
    }

    sealed class NodeComparer : IComparer<AStarNode>
    {
        public int Compare(AStarNode a, AStarNode b)
        {
            int cmp = a.f.CompareTo(b.f);
            if (cmp != 0) return cmp;
            return a.index.CompareTo(b.index);
        }
    }

    static AStarNode GetAndRemoveMin(SortedSet<AStarNode> open)
    {
        AStarNode min = default;
        foreach (var n in open)
        {
            min = n;
            break;
        }
        open.Remove(min);
        return min;
    }
}
