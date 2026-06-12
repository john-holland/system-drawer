using System;
using System.Collections.Generic;
using SdfMax;
using UnityEngine;

namespace Roads
{
    /// <summary>Corridor-aligned SDF surface extraction using delegate sampling.</summary>
    static class RoadCorridorSurfaceMesher
    {
        public static SdfMaxSurfaceMeshData Build(
            Func<Vector3, float> sample,
            Bounds localBounds,
            float isoLevel,
            int gridRes,
            int buildVersion,
            bool recalculateNormals)
        {
            var result = new SdfMaxSurfaceMeshData { BuildVersion = buildVersion, LocalBounds = localBounds };
            if (sample == null)
                return result;

            int res = Mathf.Clamp(gridRes, 4, 96);
            int nx = res, ny = res, nz = res;
            int count = nx * ny * nz;
            var field = new float[count];

            Vector3 origin = localBounds.min;
            Vector3 step = new Vector3(
                localBounds.size.x / Mathf.Max(1, nx - 1),
                localBounds.size.y / Mathf.Max(1, ny - 1),
                localBounds.size.z / Mathf.Max(1, nz - 1));

            for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
            {
                Vector3 p = origin + new Vector3(x * step.x, y * step.y, z * step.z);
                field[Index(x, y, z, nx, ny)] = sample(p);
            }

            var verts = new List<Vector3>(count * 2);
            var tris = new List<int>(count * 6);
            var vertMap = new Dictionary<long, int>(count * 2);

            for (int z = 0; z < nz - 1; z++)
            for (int y = 0; y < ny - 1; y++)
            for (int x = 0; x < nx - 1; x++)
            {
                bool b000 = field[Index(x, y, z, nx, ny)] < isoLevel;
                bool b100 = field[Index(x + 1, y, z, nx, ny)] < isoLevel;
                bool b010 = field[Index(x, y + 1, z, nx, ny)] < isoLevel;
                bool b110 = field[Index(x + 1, y + 1, z, nx, ny)] < isoLevel;
                bool b001 = field[Index(x, y, z + 1, nx, ny)] < isoLevel;
                bool b101 = field[Index(x + 1, y, z + 1, nx, ny)] < isoLevel;
                bool b011 = field[Index(x, y + 1, z + 1, nx, ny)] < isoLevel;
                bool b111 = field[Index(x + 1, y + 1, z + 1, nx, ny)] < isoLevel;

                TryFaceX(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111);
                TryFaceY(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111);
                TryFaceZ(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111);
            }

            if (verts.Count == 0)
                return result;

            result.Vertices = verts.ToArray();
            result.Triangles = tris.ToArray();
            result.Uvs = BuildPlanarUvs(result.Vertices, localBounds);
            if (recalculateNormals)
            {
                result.Normals = new Vector3[result.Vertices.Length];
                ComputeNormals(result.Vertices, result.Triangles, result.Normals);
            }
            return result;
        }

        static void TryFaceX(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b100) return;
            int x1 = x + 1;
            AddQuad(verts, tris, map,
                origin + new Vector3(x1 * step.x, y * step.y, z * step.z),
                origin + new Vector3(x1 * step.x, (y + 1) * step.y, z * step.z),
                origin + new Vector3(x1 * step.x, (y + 1) * step.y, (z + 1) * step.z),
                origin + new Vector3(x1 * step.x, y * step.y, (z + 1) * step.z),
                !b100);
        }

        static void TryFaceY(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b010) return;
            int y1 = y + 1;
            AddQuad(verts, tris, map,
                origin + new Vector3(x * step.x, y1 * step.y, z * step.z),
                origin + new Vector3((x + 1) * step.x, y1 * step.y, z * step.z),
                origin + new Vector3((x + 1) * step.x, y1 * step.y, (z + 1) * step.z),
                origin + new Vector3(x * step.x, y1 * step.y, (z + 1) * step.z),
                !b010);
        }

        static void TryFaceZ(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b001) return;
            int z1 = z + 1;
            AddQuad(verts, tris, map,
                origin + new Vector3(x * step.x, y * step.y, z1 * step.z),
                origin + new Vector3((x + 1) * step.x, y * step.y, z1 * step.z),
                origin + new Vector3((x + 1) * step.x, (y + 1) * step.y, z1 * step.z),
                origin + new Vector3(x * step.x, (y + 1) * step.y, z1 * step.z),
                !b001);
        }

        static void AddQuad(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool flip)
        {
            int i0 = GetOrAddVertex(verts, map, p0);
            int i1 = GetOrAddVertex(verts, map, p1);
            int i2 = GetOrAddVertex(verts, map, p2);
            int i3 = GetOrAddVertex(verts, map, p3);
            if (flip) { tris.Add(i0); tris.Add(i2); tris.Add(i1); tris.Add(i0); tris.Add(i3); tris.Add(i2); }
            else { tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i0); tris.Add(i2); tris.Add(i3); }
        }

        static int GetOrAddVertex(List<Vector3> verts, Dictionary<long, int> map, Vector3 p)
        {
            long key = QuantizeKey(p);
            if (map.TryGetValue(key, out int idx)) return idx;
            idx = verts.Count;
            verts.Add(p);
            map[key] = idx;
            return idx;
        }

        static long QuantizeKey(Vector3 p)
        {
            const float q = 10000f;
            return ((long)Mathf.RoundToInt(p.x * q) << 42) ^ ((long)Mathf.RoundToInt(p.y * q) << 21) ^ (uint)Mathf.RoundToInt(p.z * q);
        }

        static Vector2[] BuildPlanarUvs(Vector3[] verts, Bounds bounds)
        {
            var uvs = new Vector2[verts.Length];
            Vector3 size = bounds.size;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 rel = verts[i] - bounds.min;
                uvs[i] = new Vector2(size.x > 1e-6f ? rel.x / size.x : 0f, size.z > 1e-6f ? rel.z / size.z : 0f);
            }
            return uvs;
        }

        static void ComputeNormals(Vector3[] verts, int[] tris, Vector3[] normals)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).normalized;
                normals[a] += n; normals[b] += n; normals[c] += n;
            }
            for (int i = 0; i < normals.Length; i++)
                normals[i] = normals[i].sqrMagnitude > 1e-8f ? normals[i].normalized : Vector3.up;
        }

        static int Index(int x, int y, int z, int nx, int ny) => x + nx * (y + ny * z);
    }
}
