using System.Collections.Generic;
using UnityEngine;

namespace Planetary
{
    /// <summary>Welds cube-sphere face corners for closed manifold meshes.</summary>
    public static class VolleyballCornerStitcher
    {
        public static void WeldCorners(
            IList<Vector3>[] faceVertices,
            IList<int>[] faceTriangles,
            float weldEpsilon = 0.001f)
        {
            if (faceVertices == null || faceVertices.Length < 6)
                return;

            var cornerBuckets = new Dictionary<long, List<(int face, int vert)>>();
            for (int f = 0; f < 6; f++)
            {
                if (faceVertices[f] == null)
                    continue;
                int n = faceVertices[f].Count;
                int[] cornerIndices =
                {
                    0, n - 1, faceVertices[f].Count > 0 ? faceVertices[f].Count / 2 : 0
                };
                for (int c = 0; c < cornerIndices.Length; c++)
                {
                    int vi = Mathf.Clamp(cornerIndices[c], 0, n - 1);
                    Vector3 p = faceVertices[f][vi];
                    long key = QuantizeKey(p, weldEpsilon);
                    if (!cornerBuckets.TryGetValue(key, out var list))
                    {
                        list = new List<(int, int)>();
                        cornerBuckets[key] = list;
                    }
                    list.Add((f, vi));
                }
            }

            foreach (var kv in cornerBuckets)
            {
                if (kv.Value.Count < 2)
                    continue;
                Vector3 avg = Vector3.zero;
                for (int i = 0; i < kv.Value.Count; i++)
                    avg += faceVertices[kv.Value[i].face][kv.Value[i].vert];
                avg /= kv.Value.Count;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var (face, vert) = kv.Value[i];
                    var list = faceVertices[face] as List<Vector3>;
                    if (list != null)
                        list[vert] = avg;
                }
            }
        }

        static long QuantizeKey(Vector3 p, float eps)
        {
            float inv = 1f / Mathf.Max(eps, 1e-6f);
            long x = Mathf.RoundToInt(p.x * inv);
            long y = Mathf.RoundToInt(p.y * inv);
            long z = Mathf.RoundToInt(p.z * inv);
            return (x * 73856093L) ^ (y * 19349663L) ^ (z * 83492791L);
        }

        public static int CountOpenBoundaryEdges(IList<int>[] faceTriangles)
        {
            var edgeCount = new Dictionary<long, int>();
            for (int f = 0; f < faceTriangles.Length; f++)
            {
                var tris = faceTriangles[f];
                if (tris == null)
                    continue;
                for (int i = 0; i < tris.Count; i += 3)
                {
                    AddEdge(edgeCount, tris[i], tris[i + 1]);
                    AddEdge(edgeCount, tris[i + 1], tris[i + 2]);
                    AddEdge(edgeCount, tris[i + 2], tris[i]);
                }
            }
            int open = 0;
            foreach (var c in edgeCount.Values)
                if (c == 1)
                    open++;
            return open;
        }

        static void AddEdge(Dictionary<long, int> map, int a, int b)
        {
            if (a > b) (a, b) = (b, a);
            long key = ((long)a << 32) | (uint)b;
            map.TryGetValue(key, out int c);
            map[key] = c + 1;
        }
    }
}
