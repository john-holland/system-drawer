using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto break-out by selected materials: submesh triangles, material vertex sets, and boundary edges.
/// </summary>
public static class SkinnedMeshLoopMaterialBreakout
{
    public static int[] TriangleSubmeshIndices(Mesh mesh)
    {
        if (mesh == null)
            return Array.Empty<int>();
        int[] all = SkinnedMeshLoopEdgePath.AllTriangles(mesh);
        int triCount = all.Length / 3;
        var map = new int[triCount];
        int cursor = 0;
        int subCount = Mathf.Max(1, mesh.subMeshCount);
        for (int s = 0; s < subCount; s++)
        {
            int[] sub = mesh.GetTriangles(s);
            int n = sub != null ? sub.Length / 3 : 0;
            for (int t = 0; t < n && cursor < triCount; t++)
                map[cursor++] = s;
        }
        return map;
    }

    public static List<int> TrianglesOfSubmesh(Mesh mesh, int submesh)
    {
        var map = TriangleSubmeshIndices(mesh);
        var list = new List<int>();
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i] == submesh)
                list.Add(i);
        }
        return list;
    }

    public static List<int> VerticesOfSubmesh(Mesh mesh, int submesh)
    {
        var verts = new List<int>();
        if (mesh == null || submesh < 0 || submesh >= Mathf.Max(1, mesh.subMeshCount))
            return verts;
        int[] tris = mesh.GetTriangles(submesh);
        if (tris == null)
            return verts;
        var seen = new HashSet<int>();
        for (int i = 0; i < tris.Length; i++)
        {
            int v = tris[i];
            if (seen.Add(v))
                verts.Add(v);
        }
        return verts;
    }

    /// <summary>Undirected edges used by exactly one triangle of this submesh (rim + material seams).</summary>
    public static List<KeyValuePair<int, int>> BoundaryEdges(Mesh mesh, int submesh)
    {
        var result = new List<KeyValuePair<int, int>>();
        if (mesh == null)
            return result;
        int[] tris = mesh.GetTriangles(submesh);
        if (tris == null || tris.Length < 3)
            return result;
        var counts = new Dictionary<long, int>();
        var ends = new Dictionary<long, KeyValuePair<int, int>>();
        for (int i = 0; i + 2 < tris.Length; i += 3)
        {
            AddEdge(counts, ends, tris[i], tris[i + 1]);
            AddEdge(counts, ends, tris[i + 1], tris[i + 2]);
            AddEdge(counts, ends, tris[i + 2], tris[i]);
        }
        foreach (var kv in counts)
        {
            if (kv.Value == 1 && ends.TryGetValue(kv.Key, out var e))
                result.Add(e);
        }
        return result;
    }

    public static List<List<int>> BoundaryLoops(Mesh mesh, int submesh)
    {
        var loops = new List<List<int>>();
        var edges = BoundaryEdges(mesh, submesh);
        if (edges.Count == 0)
            return loops;
        var adj = new Dictionary<int, List<int>>();
        for (int i = 0; i < edges.Count; i++)
        {
            int a = edges[i].Key;
            int b = edges[i].Value;
            AddAdj(adj, a, b);
            AddAdj(adj, b, a);
        }
        var used = new HashSet<long>();
        foreach (var kv in adj)
        {
            int start = kv.Key;
            var nbr = kv.Value;
            if (nbr == null)
                continue;
            for (int n = 0; n < nbr.Count; n++)
            {
                long first = PackEdge(start, nbr[n]);
                if (!used.Add(first))
                    continue;
                var cycle = Walk(adj, used, start, nbr[n]);
                if (cycle != null && cycle.Count >= 3)
                    loops.Add(cycle);
            }
        }
        loops.Sort((a, b) => b.Count.CompareTo(a.Count));
        return loops;
    }

    public static string MaterialName(Renderer renderer, int index)
    {
        if (renderer == null || renderer.sharedMaterials == null)
            return "Mat_" + index;
        if (index < 0 || index >= renderer.sharedMaterials.Length)
            return "Mat_" + index;
        var mat = renderer.sharedMaterials[index];
        return mat != null && !string.IsNullOrEmpty(mat.name) ? mat.name : "Mat_" + index;
    }

    /// <summary>
    /// Writes one NamedAssign loop per selected submesh using that material's triangles,
    /// vertices, and longest boundary-edge cycle.
    /// </summary>
    public static int ApplyToAsset(
        Mesh mesh,
        Renderer renderer,
        SkinnedMeshLoopSectionAsset asset,
        IList<int> selectedSubmeshes)
    {
        if (mesh == null || asset == null || selectedSubmeshes == null)
            return 0;
        if (asset.loops == null)
            asset.loops = new List<SkinnedMeshLoopSectionAsset.LoopSection>();
        asset.loops.Clear();
        asset.splitMode = SkinnedMeshLoopSplitMode.NamedAssign;
        if (asset.breakoutMaterialIndices == null)
            asset.breakoutMaterialIndices = new List<int>();
        asset.breakoutMaterialIndices.Clear();
        int n = 0;
        for (int i = 0; i < selectedSubmeshes.Count; i++)
        {
            int s = selectedSubmeshes[i];
            if (s < 0 || s >= Mathf.Max(1, mesh.subMeshCount))
                continue;
            asset.breakoutMaterialIndices.Add(s);
            var loop = asset.AddLoop(MaterialName(renderer, s));
            loop.submeshIndex = s;
            loop.materialIndex = s;
            loop.assignedTriangles = TrianglesOfSubmesh(mesh, s);
            loop.seedTriangle = loop.assignedTriangles.Count > 0 ? loop.assignedTriangles[0] : -1;
            var cycles = BoundaryLoops(mesh, s);
            if (cycles.Count > 0)
                loop.vertexIndices = cycles[0];
            else
                loop.vertexIndices = VerticesOfSubmesh(mesh, s);
            n++;
        }
        return n;
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitSelected(
        Mesh mesh,
        Renderer renderer,
        IList<int> selectedSubmeshes)
    {
        var pieces = new List<SkinnedMeshLoopSplitPiece>();
        if (mesh == null || selectedSubmeshes == null)
            return pieces;
        for (int i = 0; i < selectedSubmeshes.Count; i++)
        {
            int s = selectedSubmeshes[i];
            var tris = TrianglesOfSubmesh(mesh, s);
            if (tris.Count == 0)
                continue;
            string name = MaterialName(renderer, s);
            var piece = new SkinnedMeshLoopSplitPiece
            {
                name = SkinnedMeshLoopSplitter.PieceObjectName(name),
                sourceTriangleIndices = tris.ToArray(),
                loopIds = Array.Empty<string>(),
                sourceMaterialIndex = s,
                mesh = SkinnedMeshLoopSplitter.ExtractSubmesh(mesh, tris, name)
            };
            pieces.Add(piece);
        }
        return pieces;
    }

    static void AddEdge(
        Dictionary<long, int> counts,
        Dictionary<long, KeyValuePair<int, int>> ends,
        int a,
        int b)
    {
        if (a == b)
            return;
        long key = PackEdge(a, b);
        counts.TryGetValue(key, out int c);
        counts[key] = c + 1;
        if (!ends.ContainsKey(key))
            ends[key] = a < b ? new KeyValuePair<int, int>(a, b) : new KeyValuePair<int, int>(b, a);
    }

    static void AddAdj(Dictionary<int, List<int>> adj, int a, int b)
    {
        if (!adj.TryGetValue(a, out var list))
        {
            list = new List<int>(2);
            adj[a] = list;
        }
        if (!list.Contains(b))
            list.Add(b);
    }

    static List<int> Walk(Dictionary<int, List<int>> adj, HashSet<long> used, int start, int second)
    {
        var cycle = new List<int> { start, second };
        int prev = start;
        int cur = second;
        for (int guard = 0; guard < 8192; guard++)
        {
            if (!adj.TryGetValue(cur, out var nbr) || nbr == null)
                break;
            int next = -1;
            for (int i = 0; i < nbr.Count; i++)
            {
                int cand = nbr[i];
                if (cand == prev)
                    continue;
                long key = PackEdge(cur, cand);
                if (used.Contains(key) && cand != start)
                    continue;
                if (cand == start)
                {
                    used.Add(key);
                    return cycle;
                }
                if (!used.Contains(key))
                {
                    next = cand;
                    used.Add(key);
                    break;
                }
            }
            if (next < 0)
                break;
            cycle.Add(next);
            prev = cur;
            cur = next;
        }
        return cycle.Count >= 3 ? cycle : null;
    }

    static long PackEdge(int a, int b)
    {
        if (a > b)
        {
            int t = a;
            a = b;
            b = t;
        }
        return ((long)(uint)a << 32) | (uint)b;
    }
}
