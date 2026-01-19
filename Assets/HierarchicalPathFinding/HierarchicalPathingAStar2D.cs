using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* pathfinding over HierarchicalPathingGrid2D (XZ plane).
/// </summary>
public sealed class HierarchicalPathingAStar2D
{
    public struct Settings
    {
        public bool allowDiagonals;
        public int maxExpandedNodes;
        public bool returnBestEffortPathWhenNoPath;
    }

    public static List<Vector3> FindPath(
        HierarchicalPathingGrid2D grid,
        Vector3 startWorld,
        Vector3 goalWorld,
        float sampleY,
        Settings settings)
    {
        if (grid == null)
            return new List<Vector3>();

        if (!grid.TryWorldToCell(startWorld, out int sx, out int sz))
            return new List<Vector3>();
        if (!grid.TryWorldToCell(goalWorld, out int gx, out int gz))
            return new List<Vector3>();

        if (grid.IsBlocked(sx, sz) || grid.IsBlocked(gx, gz))
            return new List<Vector3>();

        int w = grid.width;
        int h = grid.height;
        int total = w * h;

        // Arrays
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

        int startIdx = grid.Index(sx, sz);
        int goalIdx = grid.Index(gx, gz);
        gScore[startIdx] = 0f;

        // Priority queue via SortedSet (good enough for MVP)
        var open = new SortedSet<Node>(new NodeComparer());
        float startH = Heuristic(sx, sz, gx, gz);
        bestF[startIdx] = startH;
        open.Add(new Node(startIdx, startH));

        int expanded = 0;
        bool abortedByLimit = false;

        // Best-effort fallback target (used only if we abort by limit)
        int bestIdx = startIdx;
        float bestH = startH;

        while (open.Count > 0)
        {
            Node currentNode = GetAndRemoveMin(open);
            int current = currentNode.index;

            // Skip stale entries (we allow duplicates instead of decrease-key).
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
                return Reconstruct(grid, cameFrom, current, sampleY);

            int cx = current % w;
            int cz = current / w;

            // Track best candidate (closest to goal) for limit-abort fallback.
            {
                float hCur = Heuristic(cx, cz, gx, gz);
                if (hCur < bestH)
                {
                    bestH = hCur;
                    bestIdx = current;
                }
            }

            foreach (var n in Neighbors(cx, cz, settings.allowDiagonals))
            {
                int nx = n.x;
                int nz = n.z;
                if (!grid.IsInBounds(nx, nz))
                    continue;
                if (grid.IsBlocked(nx, nz))
                    continue;

                // Prevent "corner cutting" when moving diagonally.
                if (settings.allowDiagonals && nx != cx && nz != cz)
                {
                    if (grid.IsBlocked(nx, cz) || grid.IsBlocked(cx, nz))
                        continue;
                }

                int ni = grid.Index(nx, nz);
                if (inClosed[ni])
                    continue;

                float step = n.cost;
                float tentative = gScore[current] + step;
                if (tentative < gScore[ni])
                {
                    cameFrom[ni] = current;
                    gScore[ni] = tentative;

                    float f = tentative + Heuristic(nx, nz, gx, gz);
                    bestF[ni] = f;
                    open.Add(new Node(ni, f));
                }
            }
        }

        // Optional: return best-effort partial path toward the goal (useful for audio "awareness").
        if ((abortedByLimit || settings.returnBestEffortPathWhenNoPath) && bestIdx != startIdx && cameFrom[bestIdx] != -1)
            return Reconstruct(grid, cameFrom, bestIdx, sampleY);

        return new List<Vector3>();
    }

    private static float Heuristic(int x, int z, int gx, int gz)
    {
        // Euclidean in grid space
        float dx = gx - x;
        float dz = gz - z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static List<Vector3> Reconstruct(HierarchicalPathingGrid2D grid, int[] cameFrom, int current, float y)
    {
        var path = new List<Vector3>(64);
        int w = grid.width;

        while (current >= 0)
        {
            int x = current % w;
            int z = current / w;
            path.Add(grid.CellCenterWorld(x, z, y));
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    private struct Neighbor
    {
        public int x;
        public int z;
        public float cost;
    }

    private static IEnumerable<Neighbor> Neighbors(int x, int z, bool diagonals)
    {
        yield return new Neighbor { x = x + 1, z = z, cost = 1f };
        yield return new Neighbor { x = x - 1, z = z, cost = 1f };
        yield return new Neighbor { x = x, z = z + 1, cost = 1f };
        yield return new Neighbor { x = x, z = z - 1, cost = 1f };

        if (!diagonals) yield break;

        const float d = 1.41421356f;
        yield return new Neighbor { x = x + 1, z = z + 1, cost = d };
        yield return new Neighbor { x = x + 1, z = z - 1, cost = d };
        yield return new Neighbor { x = x - 1, z = z + 1, cost = d };
        yield return new Neighbor { x = x - 1, z = z - 1, cost = d };
    }

    private readonly struct Node
    {
        public readonly int index;
        public readonly float f;

        public Node(int index, float f)
        {
            this.index = index;
            this.f = f;
        }
    }

    private sealed class NodeComparer : IComparer<Node>
    {
        public int Compare(Node a, Node b)
        {
            int cmp = a.f.CompareTo(b.f);
            if (cmp != 0) return cmp;
            // tie-breaker: stable ordering by index so SortedSet can store distinct nodes
            return a.index.CompareTo(b.index);
        }
    }

    private static Node GetAndRemoveMin(SortedSet<Node> open)
    {
        Node min = default;
        foreach (var n in open)
        {
            min = n;
            break;
        }
        open.Remove(min);
        return min;
    }
}

