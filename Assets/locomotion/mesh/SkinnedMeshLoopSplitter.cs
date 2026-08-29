using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Result of extracting one connected region after a loop split.</summary>
public sealed class SkinnedMeshLoopSplitPiece
{
    public string name;
    public Mesh mesh;
    public int[] sourceTriangleIndices;
    public string[] loopIds;
    public int sourceMaterialIndex = -1;
}

/// <summary>
/// Partitions a mesh by authored loops. CutSeam (default) disconnects along N loops into multiple pieces.
/// </summary>
public static class SkinnedMeshLoopSplitter
{
    public static List<SkinnedMeshLoopSplitPiece> Split(Mesh mesh, SkinnedMeshLoopSectionAsset asset)
    {
        return Split(mesh, asset, null);
    }

    public static List<SkinnedMeshLoopSplitPiece> Split(
        Mesh mesh, SkinnedMeshLoopSectionAsset asset, Transform meshRoot)
    {
        var empty = new List<SkinnedMeshLoopSplitPiece>();
        if (mesh == null || asset == null)
            return empty;
        switch (asset.splitMode)
        {
            case SkinnedMeshLoopSplitMode.FloodInterior:
                return SplitFloodInterior(mesh, asset, meshRoot);
            case SkinnedMeshLoopSplitMode.NamedAssign:
                return SplitNamedAssign(mesh, asset);
            default:
                return SplitCutSeam(mesh, asset, meshRoot);
        }
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitCutSeam(Mesh mesh, SkinnedMeshLoopSectionAsset asset)
    {
        return SplitCutSeam(mesh, asset, null);
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitCutSeam(
        Mesh mesh, SkinnedMeshLoopSectionAsset asset, Transform meshRoot)
    {
        int[] tris = SkinnedMeshLoopEdgePath.AllTriangles(mesh);
        int triCount = tris.Length / 3;
        var cut = BuildCutEdges(mesh, asset, meshRoot);
        var adj = BuildTriangleAdjacency(tris, triCount, cut);
        var comps = ConnectedComponents(triCount, adj);
        var pieces = new List<SkinnedMeshLoopSplitPiece>(comps.Count);
        string[] allLoopIds = CollectLoopIds(asset);
        for (int c = 0; c < comps.Count; c++)
        {
            var triList = comps[c];
            if (triList.Count == 0)
                continue;
            string name = comps.Count == 1 ? "Piece_0" : "Piece_" + c;
            pieces.Add(MakePiece(mesh, name, triList, allLoopIds, -1));
        }
        return pieces;
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitFloodInterior(Mesh mesh, SkinnedMeshLoopSectionAsset asset)
    {
        return SplitFloodInterior(mesh, asset, null);
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitFloodInterior(
        Mesh mesh, SkinnedMeshLoopSectionAsset asset, Transform meshRoot)
    {
        int[] tris = SkinnedMeshLoopEdgePath.AllTriangles(mesh);
        int triCount = tris.Length / 3;
        var owner = new int[triCount];
        for (int i = 0; i < triCount; i++)
            owner[i] = -1;

        var loops = asset.loops;
        if (loops != null)
        {
            for (int li = 0; li < loops.Count; li++)
            {
                var loop = loops[li];
                if (loop == null || loop.seedTriangle < 0 || loop.seedTriangle >= triCount)
                    continue;
                var cut = BuildCutEdgesForLoop(mesh, loop, meshRoot);
                var adj = BuildTriangleAdjacency(tris, triCount, cut);
                FloodOwner(owner, adj, loop.seedTriangle, li);
            }
        }

        return PiecesFromOwners(mesh, asset, owner, triCount);
    }

    public static List<SkinnedMeshLoopSplitPiece> SplitNamedAssign(Mesh mesh, SkinnedMeshLoopSectionAsset asset)
    {
        int[] tris = SkinnedMeshLoopEdgePath.AllTriangles(mesh);
        int triCount = tris.Length / 3;
        var owner = new int[triCount];
        for (int i = 0; i < triCount; i++)
            owner[i] = -1;

        var loops = asset.loops;
        if (loops != null)
        {
            for (int li = 0; li < loops.Count; li++)
            {
                var loop = loops[li];
                if (loop == null || loop.assignedTriangles == null)
                    continue;
                for (int a = 0; a < loop.assignedTriangles.Count; a++)
                {
                    int t = loop.assignedTriangles[a];
                    if (t >= 0 && t < triCount)
                        owner[t] = li;
                }
            }
        }

        return PiecesFromOwners(mesh, asset, owner, triCount);
    }

    public static Mesh ExtractSubmesh(Mesh source, IReadOnlyList<int> triangleIndices, string meshName)
    {
        if (source == null || triangleIndices == null || triangleIndices.Count == 0)
            return null;

        int[] tris = SkinnedMeshLoopEdgePath.AllTriangles(source);
        Vector3[] verts = source.vertices;
        Vector3[] norms = source.normals;
        Vector2[] uv = source.uv;
        Vector4[] tans = source.tangents;
        Color[] cols = source.colors;
        BoneWeight[] bws = source.boneWeights;
        Matrix4x4[] binds = source.bindposes;

        var map = new Dictionary<int, int>();
        var newVerts = new List<Vector3>();
        var newNorms = norms != null && norms.Length == verts.Length ? new List<Vector3>() : null;
        var newUv = uv != null && uv.Length == verts.Length ? new List<Vector2>() : null;
        var newTans = tans != null && tans.Length == verts.Length ? new List<Vector4>() : null;
        var newCols = cols != null && cols.Length == verts.Length ? new List<Color>() : null;
        var newBw = bws != null && bws.Length == verts.Length ? new List<BoneWeight>() : null;
        var newTris = new List<int>();

        for (int i = 0; i < triangleIndices.Count; i++)
        {
            int triIdx = triangleIndices[i];
            if (triIdx < 0 || triIdx * 3 + 2 >= tris.Length)
                continue;
            for (int v = 0; v < 3; v++)
            {
                int orig = tris[triIdx * 3 + v];
                if (!map.TryGetValue(orig, out int ni))
                {
                    ni = newVerts.Count;
                    map[orig] = ni;
                    newVerts.Add(verts[orig]);
                    if (newNorms != null)
                        newNorms.Add(norms[orig]);
                    if (newUv != null)
                        newUv.Add(uv[orig]);
                    if (newTans != null)
                        newTans.Add(tans[orig]);
                    if (newCols != null)
                        newCols.Add(cols[orig]);
                    if (newBw != null)
                        newBw.Add(bws[orig]);
                }
                newTris.Add(ni);
            }
        }

        if (newVerts.Count < 3 || newTris.Count < 3)
            return null;

        var mesh = new Mesh { name = meshName ?? "Piece" };
        mesh.SetVertices(newVerts);
        if (newNorms != null)
            mesh.SetNormals(newNorms);
        if (newUv != null)
            mesh.SetUVs(0, newUv);
        if (newTans != null)
            mesh.SetTangents(newTans);
        if (newCols != null)
            mesh.SetColors(newCols);
        mesh.SetTriangles(newTris, 0);
        if (newBw != null && binds != null && binds.Length > 0)
        {
            mesh.boneWeights = newBw.ToArray();
            mesh.bindposes = binds;
        }
        if (newNorms == null)
            mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static SkinnedMeshLoopSplitPiece MakePiece(Mesh source, string name, List<int> triList, string[] loopIds, int materialIndex)
    {
        name = PieceObjectName(name);
        var piece = new SkinnedMeshLoopSplitPiece
        {
            name = name,
            sourceTriangleIndices = triList.ToArray(),
            loopIds = loopIds ?? Array.Empty<string>(),
            sourceMaterialIndex = materialIndex,
            mesh = ExtractSubmesh(source, triList, name)
        };
        return piece;
    }

    public static string PieceObjectName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Piece_0";
        if (name.StartsWith("Piece_", StringComparison.Ordinal))
            return name;
        return "Piece_" + name;
    }

    static List<SkinnedMeshLoopSplitPiece> PiecesFromOwners(
        Mesh mesh, SkinnedMeshLoopSectionAsset asset, int[] owner, int triCount)
    {
        var loops = asset.loops;
        int loopCount = loops != null ? loops.Count : 0;
        var buckets = new List<int>[loopCount + 1];
        for (int i = 0; i < buckets.Length; i++)
            buckets[i] = new List<int>();
        for (int t = 0; t < triCount; t++)
        {
            int o = owner[t];
            if (o < 0)
                buckets[loopCount].Add(t);
            else
                buckets[o].Add(t);
        }

        var pieces = new List<SkinnedMeshLoopSplitPiece>();
        for (int i = 0; i < loopCount; i++)
        {
            if (buckets[i].Count == 0)
                continue;
            var loop = loops[i];
            string name = loop != null && !string.IsNullOrEmpty(loop.displayName)
                ? loop.displayName
                : "Piece_" + i;
            string[] ids = loop != null && !string.IsNullOrEmpty(loop.id)
                ? new[] { loop.id }
                : Array.Empty<string>();
            pieces.Add(MakePiece(mesh, name, buckets[i], ids, loop != null ? loop.materialIndex : -1));
        }
        if (buckets[loopCount].Count > 0)
            pieces.Add(MakePiece(mesh, "Remainder", buckets[loopCount], Array.Empty<string>(), -1));
        return pieces;
    }

    static void FloodOwner(int[] owner, List<int>[] adj, int seed, int loopIndex)
    {
        if (seed < 0 || seed >= owner.Length)
            return;
        if (owner[seed] >= 0 && owner[seed] != loopIndex)
            return;
        var q = new Queue<int>();
        q.Enqueue(seed);
        owner[seed] = loopIndex;
        while (q.Count > 0)
        {
            int t = q.Dequeue();
            var nbr = adj[t];
            if (nbr == null)
                continue;
            for (int i = 0; i < nbr.Count; i++)
            {
                int n = nbr[i];
                if (owner[n] >= 0)
                    continue;
                owner[n] = loopIndex;
                q.Enqueue(n);
            }
        }
    }

    static HashSet<long> BuildCutEdges(Mesh mesh, SkinnedMeshLoopSectionAsset asset, Transform meshRoot)
    {
        var cut = new HashSet<long>();
        if (asset.loops == null)
            return cut;
        for (int i = 0; i < asset.loops.Count; i++)
            AddLoopCuts(cut, mesh, asset.loops[i], meshRoot);
        return cut;
    }

    static HashSet<long> BuildCutEdgesForLoop(
        Mesh mesh, SkinnedMeshLoopSectionAsset.LoopSection loop, Transform meshRoot)
    {
        var cut = new HashSet<long>();
        AddLoopCuts(cut, mesh, loop, meshRoot);
        return cut;
    }

    static void AddLoopCuts(
        HashSet<long> cut, Mesh mesh, SkinnedMeshLoopSectionAsset.LoopSection loop, Transform meshRoot)
    {
        if (loop == null || mesh == null)
            return;
        Vector3[] verts = mesh.vertices;
        Matrix4x4 l2w = meshRoot != null ? meshRoot.localToWorldMatrix : Matrix4x4.identity;
        var indices = loop.CombinedVertexIndices(verts, l2w);
        if (indices == null || indices.Count < 2)
            return;
        var cycle = SkinnedMeshLoopEdgePath.CloseLoop(mesh, indices);
        if (cycle == null || cycle.Count < 2)
            cycle = indices;
        int n = cycle.Count;
        bool closed = n >= 3;
        int segs = closed ? n : n - 1;
        for (int i = 0; i < segs; i++)
            cut.Add(PackEdge(cycle[i], cycle[(i + 1) % n]));
    }

    static List<int>[] BuildTriangleAdjacency(int[] tris, int triCount, HashSet<long> cut)
    {
        var edgeToTris = new Dictionary<long, List<int>>();
        for (int t = 0; t < triCount; t++)
        {
            int a = tris[t * 3];
            int b = tris[t * 3 + 1];
            int c = tris[t * 3 + 2];
            AddEdgeTri(edgeToTris, a, b, t);
            AddEdgeTri(edgeToTris, b, c, t);
            AddEdgeTri(edgeToTris, c, a, t);
        }

        var adj = new List<int>[triCount];
        for (int t = 0; t < triCount; t++)
            adj[t] = new List<int>(3);

        foreach (var kv in edgeToTris)
        {
            if (cut != null && cut.Contains(kv.Key))
                continue;
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    int ta = list[i];
                    int tb = list[j];
                    if (!adj[ta].Contains(tb))
                        adj[ta].Add(tb);
                    if (!adj[tb].Contains(ta))
                        adj[tb].Add(ta);
                }
            }
        }
        return adj;
    }

    static void AddEdgeTri(Dictionary<long, List<int>> map, int a, int b, int t)
    {
        long key = PackEdge(a, b);
        if (!map.TryGetValue(key, out var list))
        {
            list = new List<int>(2);
            map[key] = list;
        }
        list.Add(t);
    }

    static List<List<int>> ConnectedComponents(int triCount, List<int>[] adj)
    {
        var seen = new bool[triCount];
        var comps = new List<List<int>>();
        for (int t = 0; t < triCount; t++)
        {
            if (seen[t])
                continue;
            var bucket = new List<int>();
            var q = new Queue<int>();
            q.Enqueue(t);
            seen[t] = true;
            while (q.Count > 0)
            {
                int v = q.Dequeue();
                bucket.Add(v);
                var nbr = adj[v];
                if (nbr == null)
                    continue;
                for (int i = 0; i < nbr.Count; i++)
                {
                    int n = nbr[i];
                    if (seen[n])
                        continue;
                    seen[n] = true;
                    q.Enqueue(n);
                }
            }
            comps.Add(bucket);
        }
        comps.Sort((a, b) => b.Count.CompareTo(a.Count));
        return comps;
    }

    static string[] CollectLoopIds(SkinnedMeshLoopSectionAsset asset)
    {
        if (asset.loops == null || asset.loops.Count == 0)
            return Array.Empty<string>();
        var ids = new List<string>(asset.loops.Count);
        for (int i = 0; i < asset.loops.Count; i++)
            if (asset.loops[i] != null && !string.IsNullOrEmpty(asset.loops[i].id))
                ids.Add(asset.loops[i].id);
        return ids.ToArray();
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
