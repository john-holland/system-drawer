using System.Collections.Generic;
using SdfMax;
using SpatialVolumes;
using UnityEngine;

namespace Roads
{
    /// <summary>
    /// Bakes road ribbon or SDF corridor meshes, height stamps, and registers spatial volumes.
    /// </summary>
    [AddComponentMenu("Roads/Road Mesh Baker")]
    public class RoadMeshBaker : MonoBehaviour
    {
        public SplinePathMeshSampler sampler;
        public RoadSpline3D spline;
        public RoadBakeMode bakeMode = RoadBakeMode.RibbonMesh;
        public RoadTextureSet textureSet;
        public Material roadMaterial;
        public SpatialVolumeProvider volumeProvider;

        [Header("SDF Corridor")]
        public int sdfGridRes = 32;
        public float sdfHalfHeight = 0.15f;
        public float isoLevel = 0f;

        [Header("Height Stamp")]
        public int heightStampResolution = 64;

        [Header("Output")]
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public RoadMeshBakeData lastBakeData = new RoadMeshBakeData();

        int _buildVersion;

        public RoadMeshBakeData Bake()
        {
            if (sampler == null)
                sampler = GetComponent<SplinePathMeshSampler>();
            if (spline == null)
                spline = GetComponent<RoadSpline3D>();
            if (sampler == null || spline == null)
                return lastBakeData;

            _buildVersion++;
            Mesh mesh = bakeMode == RoadBakeMode.SdfCorridor
                ? BakeSdfCorridorMesh()
                : sampler.BuildCombinedMesh();

            lastBakeData.bakedMesh = mesh;
            lastBakeData.buildVersion = _buildVersion;
            lastBakeData.roadSegmentId = spline.roadSegmentId;
            lastBakeData.worldBounds = TransformBounds(mesh.bounds);
            lastBakeData.heightStamp = BuildHeightStamp(lastBakeData.worldBounds);
            lastBakeData.sdfSamples = BuildSdfCorridorSamples();

            ApplyToRenderers(mesh);
            RegisterVolume(mesh);
            UpdateRoadCorridorMarker(lastBakeData.worldBounds);
            StampPhysicsManifold();
            NotifyWeatherIntegration();
            var erosion = GetComponent<RoadErosionSystem>();
            erosion?.BakeErosion();
            return lastBakeData;
        }

        void UpdateRoadCorridorMarker(Bounds worldBounds)
        {
            var marker = GetComponent<RoadCorridorMarker>();
            if (marker == null)
                marker = gameObject.AddComponent<RoadCorridorMarker>();
            marker.roadSegmentId = spline != null ? spline.roadSegmentId : "";
            marker.corridorBounds.Clear();
            marker.corridorBounds.Add(worldBounds);
        }

        void StampPhysicsManifold()
        {
            var bridge = GetComponent<RoadPhysicsManifoldBridge>();
            if (bridge == null)
                bridge = gameObject.AddComponent<RoadPhysicsManifoldBridge>();
            bridge.spline = spline;
            bridge.sampler = sampler;
            bridge.StampFromBake();
        }

        void NotifyWeatherIntegration()
        {
            var weather = GetComponent<RoadWeatherIntegration>();
            if (weather != null)
                weather.OnRoadBakeComplete();
        }

        Mesh BakeSdfCorridorMesh()
        {
            var samples = sampler.BuildPathSamples();
            if (samples.Count < 2)
                return sampler.BuildCombinedMesh();

            var bounds = ComputeCorridorBounds(samples);
            var evaluator = new RoadCorridorSdfEvaluator(samples, sdfHalfHeight);
            var meshData = RoadCorridorSurfaceMesher.Build(
                evaluator.Sample,
                bounds,
                isoLevel,
                sdfGridRes,
                _buildVersion,
                true);

            if (!meshData.IsValid)
                return sampler.BuildCombinedMesh();

            var mesh = new Mesh { name = "RoadSdfCorridor" };
            meshData.ApplyToMesh(mesh, true);
            return mesh;
        }

        static Bounds ComputeCorridorBounds(List<SplinePathSample> samples)
        {
            var b = new Bounds(samples[0].position, Vector3.zero);
            foreach (var s in samples)
            {
                float hw = s.widthLeft + s.widthRight;
                b.Encapsulate(s.position + s.binormal * hw);
                b.Encapsulate(s.position - s.binormal * hw);
                b.Encapsulate(s.position + s.normal * 0.5f);
                b.Encapsulate(s.position - s.normal * 2f);
            }
            b.Expand(0.5f);
            return b;
        }

        List<RoadSdfCorridorSample> BuildSdfCorridorSamples()
        {
            var path = sampler.BuildPathSamples();
            var list = new List<RoadSdfCorridorSample>(path.Count);
            foreach (var s in path)
            {
                list.Add(new RoadSdfCorridorSample
                {
                    position = s.position,
                    halfWidth = s.widthLeft + s.widthRight,
                    halfHeight = sdfHalfHeight,
                    distanceAlong = s.distance
                });
            }
            return list;
        }

        RoadHeightStamp BuildHeightStamp(Bounds worldBounds)
        {
            int res = Mathf.Clamp(heightStampResolution, 8, 256);
            var stamp = new RoadHeightStamp { resolution = res, worldBounds = worldBounds };
            stamp.heights = new float[res * res];
            float minH = float.MaxValue;
            float maxH = float.MinValue;
            Vector3 min = worldBounds.min;
            Vector3 size = worldBounds.size;

            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float u = (x + 0.5f) / res;
                float v = (y + 0.5f) / res;
                Vector3 p = new Vector3(min.x + u * size.x, 0f, min.z + v * size.z);
                float h = spline.SampleTerrainHeight(p);
                stamp.heights[y * res + x] = h;
                minH = Mathf.Min(minH, h);
                maxH = Mathf.Max(maxH, h);
            }
            stamp.minHeight = minH;
            stamp.maxHeight = maxH;
            return stamp;
        }

        void ApplyToRenderers(Mesh mesh)
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter != null)
                meshFilter.sharedMesh = mesh;
            if (meshRenderer != null)
            {
                if (roadMaterial != null)
                    meshRenderer.sharedMaterial = roadMaterial;
                if (textureSet != null && meshRenderer.sharedMaterial != null)
                    textureSet.ApplyToMaterial(meshRenderer.sharedMaterial);
            }
        }

        void RegisterVolume(Mesh mesh)
        {
            if (volumeProvider == null)
                volumeProvider = GetComponent<SpatialVolumeProvider>();
            if (volumeProvider == null || mesh == null)
                return;

            var collider = volumeProvider.meshCollider;
            if (collider == null)
                collider = volumeProvider.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            volumeProvider.backend = VolumeBackend.MeshConvexTree;
            volumeProvider.RebuildIfDirty(true);
        }

        Bounds TransformBounds(Bounds localBounds)
        {
            var corners = new Vector3[8];
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            corners[0] = transform.TransformPoint(c + new Vector3(-e.x, -e.y, -e.z));
            corners[1] = transform.TransformPoint(c + new Vector3(e.x, -e.y, -e.z));
            corners[2] = transform.TransformPoint(c + new Vector3(-e.x, e.y, -e.z));
            corners[3] = transform.TransformPoint(c + new Vector3(e.x, e.y, -e.z));
            corners[4] = transform.TransformPoint(c + new Vector3(-e.x, -e.y, e.z));
            corners[5] = transform.TransformPoint(c + new Vector3(e.x, -e.y, e.z));
            corners[6] = transform.TransformPoint(c + new Vector3(-e.x, e.y, e.z));
            corners[7] = transform.TransformPoint(c + new Vector3(e.x, e.y, e.z));
            var wb = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                wb.Encapsulate(corners[i]);
            return wb;
        }
    }

    sealed class RoadCorridorSdfEvaluator
    {
        readonly List<SplinePathSample> _samples;
        readonly float _halfHeight;

        public RoadCorridorSdfEvaluator(List<SplinePathSample> samples, float halfHeight)
        {
            _samples = samples;
            _halfHeight = halfHeight;
        }

        public float Sample(Vector3 worldPos)
        {
            if (_samples == null || _samples.Count < 2)
                return 1000f;

            float bestDist = float.MaxValue;
            SplinePathSample nearest = _samples[0];
            for (int i = 0; i < _samples.Count; i++)
            {
                float d = (worldPos - _samples[i].position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = _samples[i];
                }
            }

            Vector3 rel = worldPos - nearest.position;
            float lateral = Mathf.Abs(Vector3.Dot(rel, nearest.binormal));
            float vertical = Mathf.Abs(Vector3.Dot(rel, nearest.normal));
            float halfW = nearest.widthLeft + nearest.widthRight;
            float dx = Mathf.Max(0f, lateral - halfW);
            float dy = Mathf.Max(0f, vertical - _halfHeight);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }

}
