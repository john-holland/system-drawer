using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    /// <summary>
    /// Extracts a watertight surface from an SDF by uniform grid sampling (occupancy face extraction / dual of marching cubes).
    /// </summary>
    public static class SdfMaxSurfaceMesher
    {
        static readonly Vector3Int[] NeighborOffsets =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1)
        };

        public static SdfMaxSurfaceMeshData Build(
            SdfMaxEvaluator evaluator,
            Bounds localBounds,
            Matrix4x4 localToWorld,
            float isoLevel,
            int gridRes,
            int buildVersion,
            bool recalculateNormals)
        {
            return Build(evaluator, localBounds, localToWorld, isoLevel, gridRes, buildVersion, recalculateNormals, null);
        }

        public static SdfMaxSurfaceMeshData Build(
            SdfMaxEvaluator evaluator,
            Bounds localBounds,
            Matrix4x4 localToWorld,
            float isoLevel,
            int gridRes,
            int buildVersion,
            bool recalculateNormals,
            SdfMaxSurfaceMeshDebugReport report)
        {
            var result = new SdfMaxSurfaceMeshData { BuildVersion = buildVersion, LocalBounds = localBounds };
            if (evaluator == null)
                return result;

            int res = Mathf.Clamp(gridRes, 4, 96);
            int nx = res;
            int ny = res;
            int nz = res;
            int count = nx * ny * nz;
            var field = new float[count];

            Vector3 origin = localBounds.min;
            Vector3 step = new Vector3(
                localBounds.size.x / (nx - 1),
                localBounds.size.y / (ny - 1),
                localBounds.size.z / (nz - 1));

            Matrix4x4 worldFromLocal = localToWorld;
            float fieldMin = float.PositiveInfinity;
            float fieldMax = float.NegativeInfinity;
            int insideCount = 0;
            for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
            {
                Vector3 localP = origin + new Vector3(x * step.x, y * step.y, z * step.z);
                Vector3 worldP = worldFromLocal.MultiplyPoint3x4(localP);
                float sample = evaluator.Sample(worldP, 0f);
                field[Index(x, y, z, nx, ny)] = sample;
                if (sample < fieldMin) fieldMin = sample;
                if (sample > fieldMax) fieldMax = sample;
                if (sample < isoLevel)
                    insideCount++;
            }

            var verts = new List<Vector3>(count * 2);
            var tris = new List<int>(count * 6);
            var vertMap = new Dictionary<long, int>(count * 2);
            int faceQuadsX = 0;
            int faceQuadsY = 0;
            int faceQuadsZ = 0;

            for (int z = 0; z < nz - 1; z++)
            for (int y = 0; y < ny - 1; y++)
            for (int x = 0; x < nx - 1; x++)
            {
                float v000 = field[Index(x, y, z, nx, ny)];
                float v100 = field[Index(x + 1, y, z, nx, ny)];
                float v010 = field[Index(x, y + 1, z, nx, ny)];
                float v110 = field[Index(x + 1, y + 1, z, nx, ny)];
                float v001 = field[Index(x, y, z + 1, nx, ny)];
                float v101 = field[Index(x + 1, y, z + 1, nx, ny)];
                float v011 = field[Index(x, y + 1, z + 1, nx, ny)];
                float v111 = field[Index(x + 1, y + 1, z + 1, nx, ny)];

                bool b000 = v000 < isoLevel;
                bool b100 = v100 < isoLevel;
                bool b010 = v010 < isoLevel;
                bool b110 = v110 < isoLevel;
                bool b001 = v001 < isoLevel;
                bool b101 = v101 < isoLevel;
                bool b011 = v011 < isoLevel;
                bool b111 = v111 < isoLevel;

                if (TryFaceX(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111))
                    faceQuadsX++;
                if (TryFaceY(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111))
                    faceQuadsY++;
                if (TryFaceZ(verts, tris, vertMap, origin, step, x, y, z, b000, b100, b010, b110, b001, b101, b011, b111))
                    faceQuadsZ++;
            }

            if (verts.Count == 0)
            {
                if (report != null)
                    PopulateDebugReport(report, evaluator, localBounds, localToWorld, isoLevel, nx, ny, nz, origin, step, field,
                        fieldMin, fieldMax, insideCount, faceQuadsX, faceQuadsY, faceQuadsZ, null, null, null);
                return result;
            }

            result.Vertices = verts.ToArray();
            result.Triangles = tris.ToArray();
            result.Uvs = BuildPlanarUvs(result.Vertices, localBounds);
            if (recalculateNormals)
            {
                result.Normals = new Vector3[result.Vertices.Length];
                ComputeNormals(result.Vertices, result.Triangles, result.Normals);
            }

            if (report != null)
            {
                PopulateDebugReport(report, evaluator, localBounds, localToWorld, isoLevel, nx, ny, nz, origin, step, field,
                    fieldMin, fieldMax, insideCount, faceQuadsX, faceQuadsY, faceQuadsZ,
                    result.Vertices, result.Triangles, result.Normals);
            }

            return result;
        }

        static void PopulateDebugReport(
            SdfMaxSurfaceMeshDebugReport report,
            SdfMaxEvaluator evaluator,
            Bounds localBounds,
            Matrix4x4 localToWorld,
            float isoLevel,
            int nx,
            int ny,
            int nz,
            Vector3 origin,
            Vector3 step,
            float[] field,
            float fieldMin,
            float fieldMax,
            int insideCount,
            int faceQuadsX,
            int faceQuadsY,
            int faceQuadsZ,
            Vector3[] meshVerts,
            int[] meshTris,
            Vector3[] meshNormals)
        {
            report.LocalBounds = localBounds;
            report.LocalToWorld = localToWorld;
            report.IsoLevel = isoLevel;
            report.GridResX = nx;
            report.GridResY = ny;
            report.GridResZ = nz;
            report.InsideCount = insideCount;
            report.OutsideCount = nx * ny * nz - insideCount;
            report.FieldMin = fieldMin;
            report.FieldMax = fieldMax;
            report.FaceQuadsX = faceQuadsX;
            report.FaceQuadsY = faceQuadsY;
            report.FaceQuadsZ = faceQuadsZ;

            report.ColumnProbes = BuildColumnProbes(field, nx, ny, nz, isoLevel);
            report.GridSamples = BuildGridSampleSubset(field, nx, ny, nz, origin, step, localToWorld, isoLevel);
            if (meshVerts == null)
            {
                report.MeshVertexCount = 0;
                report.MeshTriangleCount = 0;
                report.VertexSamples = System.Array.Empty<SdfMaxSurfaceMeshDebugReport.VertexSample>();
                return;
            }

            report.MeshVertexCount = meshVerts.Length;
            report.MeshTriangleCount = meshTris != null ? meshTris.Length : 0;
            float rMin = float.PositiveInfinity;
            float rMax = float.NegativeInfinity;
            float rSum = 0f;
            for (int i = 0; i < meshVerts.Length; i++)
            {
                Vector3 world = localToWorld.MultiplyPoint3x4(meshVerts[i]);
                float r = Vector3.Distance(world, report.ReferenceCenter);
                rMin = Mathf.Min(rMin, r);
                rMax = Mathf.Max(rMax, r);
                rSum += r;
            }
            report.VertexRadiusMin = rMin;
            report.VertexRadiusMax = rMax;
            report.VertexRadiusAvg = meshVerts.Length > 0 ? rSum / meshVerts.Length : 0f;
            report.VertexSamples = BuildVertexSampleSubset(evaluator, localToWorld, meshVerts, meshNormals, report.ReferenceCenter);
        }

        static SdfMaxSurfaceMeshDebugReport.ColumnProbe[] BuildColumnProbes(float[] field, int nx, int ny, int nz, float isoLevel)
        {
            int[] probeX = { nx / 2, 0, nx - 1, nx / 4, (3 * nx) / 4 };
            int[] probeZ = { nz / 2, 0, nz - 1, nz / 4, (3 * nz) / 4 };
            var probes = new SdfMaxSurfaceMeshDebugReport.ColumnProbe[probeX.Length];
            for (int i = 0; i < probeX.Length; i++)
            {
                int x = Mathf.Clamp(probeX[i], 0, nx - 1);
                int z = Mathf.Clamp(probeZ[i], 0, nz - 1);
                float yMin = field[Index(x, 0, z, nx, ny)];
                float yMid = field[Index(x, ny / 2, z, nx, ny)];
                float yMax = field[Index(x, ny - 1, z, nx, ny)];
                probes[i] = new SdfMaxSurfaceMeshDebugReport.ColumnProbe
                {
                    GridX = x,
                    GridZ = z,
                    FieldMinY = yMin,
                    FieldMidY = yMid,
                    FieldMaxY = yMax,
                    VariesWithY = Mathf.Abs(yMin - yMid) > 1e-4f || Mathf.Abs(yMid - yMax) > 1e-4f
                };
            }
            return probes;
        }

        static SdfMaxSurfaceMeshDebugReport.GridSample[] BuildGridSampleSubset(
            float[] field,
            int nx,
            int ny,
            int nz,
            Vector3 origin,
            Vector3 step,
            Matrix4x4 localToWorld,
            float isoLevel)
        {
            var picks = new (int x, int y, int z)[]
            {
                (nx / 2, ny / 2, nz / 2),
                (0, ny / 2, nz / 2),
                (nx - 1, ny / 2, nz / 2),
                (nx / 2, 0, nz / 2),
                (nx / 2, ny - 1, nz / 2),
                (nx / 2, ny / 2, 0),
                (nx / 2, ny / 2, nz - 1),
                (0, 0, 0),
                (nx - 1, ny - 1, nz - 1)
            };
            var samples = new SdfMaxSurfaceMeshDebugReport.GridSample[picks.Length];
            for (int i = 0; i < picks.Length; i++)
            {
                int x = picks[i].x;
                int y = picks[i].y;
                int z = picks[i].z;
                Vector3 localP = origin + new Vector3(x * step.x, y * step.y, z * step.z);
                float f = field[Index(x, y, z, nx, ny)];
                samples[i] = new SdfMaxSurfaceMeshDebugReport.GridSample
                {
                    X = x,
                    Y = y,
                    Z = z,
                    LocalPos = localP,
                    WorldPos = localToWorld.MultiplyPoint3x4(localP),
                    Field = f,
                    Inside = f < isoLevel
                };
            }
            return samples;
        }

        static SdfMaxSurfaceMeshDebugReport.VertexSample[] BuildVertexSampleSubset(
            SdfMaxEvaluator evaluator,
            Matrix4x4 localToWorld,
            Vector3[] meshVerts,
            Vector3[] meshNormals,
            Vector3 referenceCenter)
        {
            if (meshVerts == null || meshVerts.Length == 0)
                return System.Array.Empty<SdfMaxSurfaceMeshDebugReport.VertexSample>();

            int count = Mathf.Min(12, meshVerts.Length);
            var samples = new SdfMaxSurfaceMeshDebugReport.VertexSample[count];
            for (int i = 0; i < count; i++)
            {
                int idx = (i * meshVerts.Length) / count;
                Vector3 local = meshVerts[idx];
                Vector3 world = localToWorld.MultiplyPoint3x4(local);
                samples[i] = new SdfMaxSurfaceMeshDebugReport.VertexSample
                {
                    Index = idx,
                    LocalPos = local,
                    WorldPos = world,
                    FieldAtWorld = evaluator != null ? evaluator.Sample(world, 0f) : 0f,
                    RadialDistance = Vector3.Distance(world, referenceCenter),
                    Normal = meshNormals != null && idx < meshNormals.Length ? meshNormals[idx] : Vector3.zero
                };
            }
            return samples;
        }

        static bool TryFaceX(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b100)
                return false;
            int x1 = x + 1;
            Vector3 p0 = origin + new Vector3(x1 * step.x, y * step.y, z * step.z);
            Vector3 p1 = origin + new Vector3(x1 * step.x, (y + 1) * step.y, z * step.z);
            Vector3 p2 = origin + new Vector3(x1 * step.x, (y + 1) * step.y, (z + 1) * step.z);
            Vector3 p3 = origin + new Vector3(x1 * step.x, y * step.y, (z + 1) * step.z);
            AddQuad(verts, tris, map, p0, p1, p2, p3, !b100);
            return true;
        }

        static bool TryFaceY(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b010)
                return false;
            int y1 = y + 1;
            Vector3 p0 = origin + new Vector3(x * step.x, y1 * step.y, z * step.z);
            Vector3 p1 = origin + new Vector3((x + 1) * step.x, y1 * step.y, z * step.z);
            Vector3 p2 = origin + new Vector3((x + 1) * step.x, y1 * step.y, (z + 1) * step.z);
            Vector3 p3 = origin + new Vector3(x * step.x, y1 * step.y, (z + 1) * step.z);
            AddQuad(verts, tris, map, p0, p1, p2, p3, !b010);
            return true;
        }

        static bool TryFaceZ(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 origin, Vector3 step, int x, int y, int z,
            bool b000, bool b100, bool b010, bool b110, bool b001, bool b101, bool b011, bool b111)
        {
            if (b000 == b001)
                return false;
            int z1 = z + 1;
            Vector3 p0 = origin + new Vector3(x * step.x, y * step.y, z1 * step.z);
            Vector3 p1 = origin + new Vector3((x + 1) * step.x, y * step.y, z1 * step.z);
            Vector3 p2 = origin + new Vector3((x + 1) * step.x, (y + 1) * step.y, z1 * step.z);
            Vector3 p3 = origin + new Vector3(x * step.x, (y + 1) * step.y, z1 * step.z);
            AddQuad(verts, tris, map, p0, p1, p2, p3, !b001);
            return true;
        }

        static void AddQuad(List<Vector3> verts, List<int> tris, Dictionary<long, int> map,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool flip)
        {
            int i0 = GetOrAddVertex(verts, map, p0);
            int i1 = GetOrAddVertex(verts, map, p1);
            int i2 = GetOrAddVertex(verts, map, p2);
            int i3 = GetOrAddVertex(verts, map, p3);
            if (flip)
            {
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i0); tris.Add(i3); tris.Add(i2);
            }
            else
            {
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i0); tris.Add(i2); tris.Add(i3);
            }
        }

        static int GetOrAddVertex(List<Vector3> verts, Dictionary<long, int> map, Vector3 p)
        {
            long key = QuantizeKey(p);
            if (map.TryGetValue(key, out int idx))
                return idx;
            idx = verts.Count;
            verts.Add(p);
            map[key] = idx;
            return idx;
        }

        static long QuantizeKey(Vector3 p)
        {
            const float q = 10000f;
            int x = Mathf.RoundToInt(p.x * q);
            int y = Mathf.RoundToInt(p.y * q);
            int z = Mathf.RoundToInt(p.z * q);
            return ((long)x << 42) ^ ((long)y << 21) ^ (uint)z;
        }

        static Vector2[] BuildPlanarUvs(Vector3[] verts, Bounds bounds)
        {
            var uvs = new Vector2[verts.Length];
            Vector3 size = bounds.size;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 rel = verts[i] - bounds.min;
                uvs[i] = new Vector2(
                    size.x > 1e-6f ? rel.x / size.x : 0f,
                    size.z > 1e-6f ? rel.z / size.z : 0f);
            }
            return uvs;
        }

        static void ComputeNormals(Vector3[] verts, int[] tris, Vector3[] normals)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];
                Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).normalized;
                normals[a] += n;
                normals[b] += n;
                normals[c] += n;
            }
            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].sqrMagnitude > 1e-8f)
                    normals[i].Normalize();
                else
                    normals[i] = Vector3.up;
            }
        }

        static int Index(int x, int y, int z, int nx, int ny) => x + nx * (y + ny * z);

        public static int ComputeSurfaceMeshVersion(SdfMaxSolverProfile profile, SdfMaxCompositionAsset composition)
        {
            unchecked
            {
                int h = 17;
                if (profile != null)
                {
                    h = h * 31 + profile.surfaceGridRes;
                    h = h * 31 + profile.surfaceIsoLevel.GetHashCode();
                }
                if (composition != null)
                    h = h * 31 + composition.GetInstanceID();
                return h;
            }
        }
    }
}
