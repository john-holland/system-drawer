using System.Collections.Generic;
using UnityEngine;

namespace DestructibleEnvironment
{
    public sealed class DestructibleTreeBuildResult
    {
        public Bounds RootBounds;
        public float RootVolume;
        public readonly List<DestructibleTreeLeaf> Leaves = new List<DestructibleTreeLeaf>();
    }

    public sealed class DestructibleTreeLeaf
    {
        public Bounds WorldBounds;
        public int Depth;
        public int ParentLeafIndex = -1;
        public readonly List<int> TriangleIndices = new List<int>();
    }

    /// <summary>Builds an octree over mesh triangles without requiring a convex MeshCollider.</summary>
    public static class DestructibleConvexTreeBuilder
    {
        public static DestructibleTreeBuildResult BuildFromMesh(
            Mesh mesh,
            Matrix4x4 localToWorld,
            int maxDepth = ConvexMeshTreeCacheBuilder.DefaultMaxDepth,
            float minLeafExtent = ConvexMeshTreeCacheBuilder.DefaultMinExtent,
            int maxTrianglesPerLeaf = ConvexMeshTreeCacheBuilder.DefaultMaxTrianglesPerLeaf)
        {
            var result = new DestructibleTreeBuildResult();
            if (mesh == null)
                return result;

            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            if (verts == null || tris == null || tris.Length < 3)
                return result;

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

            result.RootBounds = rootBounds;
            result.RootVolume = Mathf.Max(Volume(rootBounds), 1e-8f);

            var allIndices = new List<int>(triCount);
            for (int i = 0; i < triCount; i++)
                allIndices.Add(i);

            BuildRecursive(
                result.Leaves,
                rootBounds,
                allIndices,
                triBounds,
                maxDepth,
                minLeafExtent,
                maxTrianglesPerLeaf,
                0,
                -1);

            return result;
        }

        static void BuildRecursive(
            List<DestructibleTreeLeaf> outLeaves,
            Bounds nodeBounds,
            List<int> triangleIndices,
            Bounds[] triBounds,
            int depthRemaining,
            float minLeafExtent,
            int maxTrianglesPerLeaf,
            int depth,
            int parentLeafIndex)
        {
            float ext = Mathf.Max(nodeBounds.extents.x, nodeBounds.extents.y, nodeBounds.extents.z) * 2f;

            if (depthRemaining <= 0 ||
                ext <= minLeafExtent + 1e-5f ||
                triangleIndices.Count <= maxTrianglesPerLeaf)
            {
                var leaf = new DestructibleTreeLeaf
                {
                    WorldBounds = nodeBounds,
                    Depth = depth,
                    ParentLeafIndex = parentLeafIndex
                };
                leaf.TriangleIndices.AddRange(triangleIndices);
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

            int leafIndexBefore = outLeaves.Count;
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
                BuildRecursive(
                    outLeaves,
                    childBounds,
                    perOctant[oct],
                    triBounds,
                    depthRemaining - 1,
                    minLeafExtent,
                    maxTrianglesPerLeaf,
                    depth + 1,
                    leafIndexBefore);
            }

            if (!anyChild)
            {
                var leaf = new DestructibleTreeLeaf
                {
                    WorldBounds = nodeBounds,
                    Depth = depth,
                    ParentLeafIndex = parentLeafIndex
                };
                leaf.TriangleIndices.AddRange(triangleIndices);
                outLeaves.Add(leaf);
            }
        }

        static int OctantIndex(Vector3 parentCenter, Vector3 point)
        {
            int ix = point.x >= parentCenter.x ? 1 : 0;
            int iy = point.y >= parentCenter.y ? 2 : 0;
            int iz = point.z >= parentCenter.z ? 4 : 0;
            return ix | iy | iz;
        }

        static float Volume(Bounds b) => b.size.x * b.size.y * b.size.z;
    }

    public static class DestructibleMeshExtractor
    {
        public static Mesh ExtractSubmesh(
            Mesh source,
            Matrix4x4 localToWorld,
            Matrix4x4 worldToLocal,
            IReadOnlyList<int> triangleIndices,
            string meshName)
        {
            if (source == null || triangleIndices == null || triangleIndices.Count == 0)
                return null;

            Vector3[] verts = source.vertices;
            int[] tris = source.triangles;
            var pieceVerts = new List<Vector3>();
            var pieceTris = new List<int>();
            var map = new Dictionary<int, int>();

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
                        ni = pieceVerts.Count;
                        Vector3 world = localToWorld.MultiplyPoint3x4(verts[orig]);
                        pieceVerts.Add(worldToLocal.MultiplyPoint3x4(world));
                        map[orig] = ni;
                    }
                    pieceTris.Add(ni);
                }
            }

            if (pieceVerts.Count < 3 || pieceTris.Count < 3)
                return null;

            var mesh = new Mesh { name = meshName };
            mesh.SetVertices(pieceVerts);
            mesh.SetTriangles(pieceTris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
