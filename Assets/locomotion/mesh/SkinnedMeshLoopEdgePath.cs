using System.Collections.Generic;
using UnityEngine;

/// <summary>Walks mesh edges so a picked vertex cycle becomes a real edge loop.</summary>
public static class SkinnedMeshLoopEdgePath
{
    public static List<int> CloseLoop(Mesh mesh, IList<int> picks)
    {
        var result = new List<int>();
        if (picks == null || picks.Count == 0)
            return result;
        if (mesh == null || picks.Count < 2)
        {
            for (int i = 0; i < picks.Count; i++)
                result.Add(picks[i]);
            return result;
        }

        var adj = BuildAdjacency(mesh);
        int count = picks.Count;
        bool closed = count >= 3;
        int segments = closed ? count : count - 1;
        for (int i = 0; i < segments; i++)
        {
            int a = picks[i];
            int b = picks[(i + 1) % count];
            var path = ShortestPath(adj, a, b);
            if (path == null || path.Count == 0)
            {
                if (result.Count == 0 || result[result.Count - 1] != a)
                    result.Add(a);
                continue;
            }
            int start = result.Count == 0 ? 0 : 1;
            for (int p = start; p < path.Count; p++)
                result.Add(path[p]);
        }

        if (result.Count > 1 && result[result.Count - 1] == result[0])
            result.RemoveAt(result.Count - 1);
        return result;
    }

    public static Dictionary<int, List<int>> BuildAdjacency(Mesh mesh)
    {
        var adj = new Dictionary<int, List<int>>();
        if (mesh == null)
            return adj;
        int[] tris = AllTriangles(mesh);
        for (int i = 0; i + 2 < tris.Length; i += 3)
        {
            Link(adj, tris[i], tris[i + 1]);
            Link(adj, tris[i + 1], tris[i + 2]);
            Link(adj, tris[i + 2], tris[i]);
        }
        return adj;
    }

    public static int[] AllTriangles(Mesh mesh)
    {
        if (mesh == null)
            return System.Array.Empty<int>();
        if (mesh.subMeshCount <= 1)
            return mesh.triangles ?? System.Array.Empty<int>();
        var list = new List<int>();
        for (int i = 0; i < mesh.subMeshCount; i++)
            list.AddRange(mesh.GetTriangles(i));
        return list.ToArray();
    }

    static void Link(Dictionary<int, List<int>> adj, int a, int b)
    {
        if (a == b)
            return;
        Add(adj, a, b);
        Add(adj, b, a);
    }

    static void Add(Dictionary<int, List<int>> adj, int a, int b)
    {
        if (!adj.TryGetValue(a, out var list))
        {
            list = new List<int>(6);
            adj[a] = list;
        }
        if (!list.Contains(b))
            list.Add(b);
    }

    static List<int> ShortestPath(Dictionary<int, List<int>> adj, int start, int end)
    {
        if (start == end)
            return new List<int> { start };
        var q = new Queue<int>();
        var prev = new Dictionary<int, int>();
        var seen = new HashSet<int> { start };
        q.Enqueue(start);
        prev[start] = start;
        while (q.Count > 0)
        {
            int v = q.Dequeue();
            if (!adj.TryGetValue(v, out var nbr))
                continue;
            for (int i = 0; i < nbr.Count; i++)
            {
                int n = nbr[i];
                if (!seen.Add(n))
                    continue;
                prev[n] = v;
                if (n == end)
                    return Reconstruct(prev, start, end);
                q.Enqueue(n);
            }
        }
        return null;
    }

    static List<int> Reconstruct(Dictionary<int, int> prev, int start, int end)
    {
        var path = new List<int>();
        int v = end;
        path.Add(v);
        while (v != start)
        {
            v = prev[v];
            path.Add(v);
        }
        path.Reverse();
        return path;
    }
}
