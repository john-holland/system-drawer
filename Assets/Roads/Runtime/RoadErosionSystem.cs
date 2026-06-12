using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    /// <summary>Procedural road erosion from water flow with PREBAKE/CACHE/NOCACHE modes.</summary>
    [AddComponentMenu("Roads/Road Erosion System")]
    public class RoadErosionSystem : MonoBehaviour
    {
        public RoadFlowSampler flowSampler;
        public RoadMeshBaker meshBaker;
        public SplinePathMeshSampler sampler;
        public RoadErosionCacheMode cacheMode = RoadErosionCacheMode.CACHE;
        public float breakIntensityScale = 1f;
        public float scatterSpread = 1.5f;
        public Material erosionMaterial;

        [Header("Cache")]
        public RoadFlowCell[] cachedFlowCells;
        public bool debrisCached;

        Transform _debrisRoot;
        readonly Dictionary<int, Mesh> _lodDebrisCache = new Dictionary<int, Mesh>();

        public void BakeErosion()
        {
            if (flowSampler == null)
                flowSampler = GetComponent<RoadFlowSampler>();
            if (meshBaker == null)
                meshBaker = GetComponent<RoadMeshBaker>();

            cachedFlowCells = flowSampler != null ? flowSampler.SampleFlow() : null;

            if (cacheMode == RoadErosionCacheMode.PREBAKE)
                GenerateErosionDebris(cachedFlowCells, force: true);
            else if (cacheMode == RoadErosionCacheMode.CACHE)
                debrisCached = false;
        }

        public void UpdateRuntimeErosion(int lodTier = 0)
        {
            if (cacheMode == RoadErosionCacheMode.PREBAKE && debrisCached)
                return;

            if (cacheMode == RoadErosionCacheMode.CACHE)
            {
                if (_lodDebrisCache.TryGetValue(lodTier, out _))
                    return;
                var cells = cachedFlowCells ?? flowSampler?.SampleFlow();
                GenerateErosionDebris(cells, force: false, lodTier: lodTier);
                return;
            }

            // NOCACHE
            GenerateErosionDebris(flowSampler?.SampleFlow(), force: true, lodTier: lodTier);
        }

        void GenerateErosionDebris(RoadFlowCell[] cells, bool force, int lodTier = 0)
        {
            if (cells == null || cells.Length == 0 || meshBaker?.lastBakeData?.bakedMesh == null)
                return;

            var peak = flowSampler != null ? flowSampler.FindPeakFlow(cells) : cells[0];
            if (peak.intensity <= flowSampler?.flowThreshold)
                return;

            if (_debrisRoot == null)
            {
                _debrisRoot = new GameObject("RoadErosionDebris").transform;
                _debrisRoot.SetParent(transform, false);
            }

            if (!force && debrisCached)
                return;

            ClearDebris();
            var sourceMesh = meshBaker.lastBakeData.bakedMesh;
            var pieces = RoadMeshTriangleSplitter.SplitByFlow(
                sourceMesh, peak, breakIntensityScale, scatterSpread);

            var combined = new Mesh { name = "ErosionDebris" };
            var instances = new CombineInstance[pieces.Count];
            for (int i = 0; i < pieces.Count; i++)
            {
                instances[i] = new CombineInstance
                {
                    mesh = pieces[i],
                    transform = Matrix4x4.TRS(
                        peak.flowDir * Random.Range(0f, scatterSpread),
                        Quaternion.LookRotation(peak.flowDir, Vector3.up),
                        Vector3.one)
                };
            }
            combined.CombineMeshes(instances, true, true);
            combined.RecalculateBounds();

            var go = new GameObject($"Debris_LOD{lodTier}");
            go.transform.SetParent(_debrisRoot, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = combined;
            if (erosionMaterial != null)
                mr.sharedMaterial = erosionMaterial;

            if (cacheMode != RoadErosionCacheMode.NOCACHE)
            {
                _lodDebrisCache[lodTier] = combined;
                debrisCached = true;
            }
        }

        void ClearDebris()
        {
            if (_debrisRoot == null)
                return;
            for (int i = _debrisRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_debrisRoot.GetChild(i).gameObject);
        }
    }

    /// <summary>Splits road mesh triangles using convex tree partitioning, oriented to flow.</summary>
    public static class RoadMeshTriangleSplitter
    {
        public static List<Mesh> SplitByFlow(Mesh source, RoadFlowCell peak, float intensityScale, float spread)
        {
            var result = new List<Mesh>();
            if (source == null)
                return result;

            var verts = source.vertices;
            var tris = source.triangles;
            if (verts == null || tris == null || tris.Length < 3)
                return result;

            var rng = new System.Random(Mathf.RoundToInt(peak.arcLength * 1000f));
            int triCount = tris.Length / 3;
            int pieces = Mathf.Clamp(Mathf.RoundToInt(peak.intensity * intensityScale * 3f), 1, 12);

            for (int p = 0; p < pieces; p++)
            {
                int startTri = rng.Next(0, triCount);
                var pieceVerts = new List<Vector3>();
                var pieceTris = new List<int>();
                var map = new Dictionary<int, int>();

                for (int t = 0; t < 3; t++)
                {
                    int triIdx = (startTri + t) % triCount;
                    for (int v = 0; v < 3; v++)
                    {
                        int orig = tris[triIdx * 3 + v];
                        if (!map.TryGetValue(orig, out int ni))
                        {
                            ni = pieceVerts.Count;
                            Vector3 offset = peak.flowDir * (float)rng.NextDouble() * spread;
                            pieceVerts.Add(verts[orig] + offset);
                            map[orig] = ni;
                        }
                        pieceTris.Add(ni);
                    }
                }

                if (pieceVerts.Count < 3)
                    continue;

                var mesh = new Mesh { name = $"ErosionPiece_{p}" };
                mesh.SetVertices(pieceVerts);
                mesh.SetTriangles(pieceTris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                result.Add(mesh);
            }
            return result;
        }
    }
}
