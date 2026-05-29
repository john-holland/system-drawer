using UnityEngine;

namespace SpatialVolumes
{
    /// <summary>Spatiotemporal bounds without depending on Locomotion.Narrative (avoids assembly cycles).</summary>
    public struct SpatialVolumeBounds4
    {
        public Vector3 center;
        public Vector3 size;
        public float tMin;
        public float tMax;

        public SpatialVolumeBounds4(Vector3 center, Vector3 size, float tMin, float tMax)
        {
            this.center = center;
            this.size = size;
            this.tMin = tMin;
            this.tMax = tMax;
        }

        public Bounds ToBounds() => new Bounds(center, size);

        public static SpatialVolumeBounds4 FromBoundsAndTime(Bounds bounds, float tMin, float tMax)
        {
            return new SpatialVolumeBounds4(bounds.center, bounds.size, tMin, tMax);
        }
    }
}
