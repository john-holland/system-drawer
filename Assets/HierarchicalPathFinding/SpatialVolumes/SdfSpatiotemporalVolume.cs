using System.Collections.Generic;
using SpatialVolumes;
using UnityEngine;

namespace SpatialVolumes
{
    /// <summary>SDF-backed spatiotemporal volume: inside when field ≤ 0 and t in [tMin, tMax].</summary>
    public sealed class SdfSpatiotemporalVolume : MonoBehaviour, ISpatiotemporalVolume
    {
        public SpatialVolumeProvider volumeProvider;
        public float tMin;
        public float tMax = float.MaxValue;

        void Awake()
        {
            if (volumeProvider == null)
                volumeProvider = GetComponent<SpatialVolumeProvider>();
        }

        public bool Contains(Vector3 world, float narrativeTime)
        {
            if (narrativeTime < tMin || narrativeTime > tMax)
                return false;
            if (volumeProvider == null)
                return false;
            return volumeProvider.TrySample(world, narrativeTime, out float field, out bool inside)
                   && (inside || field <= 0f);
        }

        public Bounds ApproximateBounds()
        {
            if (volumeProvider == null)
                return new Bounds(transform.position, Vector3.one);
            var col = volumeProvider.meshCollider;
            return col != null ? col.bounds : new Bounds(transform.position, Vector3.one * 4f);
        }

        public void ExportSamples(List<Vector3> surfacePoints, float narrativeTime)
        {
            if (surfacePoints == null || !Contains(transform.position, narrativeTime))
                return;
            Bounds b = ApproximateBounds();
            const int n = 8;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                surfacePoints.Add(b.center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * b.extents.x);
            }
        }
    }
}
