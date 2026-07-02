using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Galactic coordinates use scene meters; 1 AU ≈ 1.496e11 stored in registry, scaled for rendering.</summary>
    public static class GalacticFrame
    {
        public const double MetersPerAu = 1.496e11;
        public const float RenderScale = 1e-9f;

        public static Vector3 GalacticToWorld(Vector3 galacticMeters, Transform origin = null)
        {
            Vector3 scaled = galacticMeters * RenderScale;
            return origin != null ? origin.TransformPoint(scaled) : scaled;
        }

        public static Vector3 WorldToGalactic(Vector3 world, Transform origin = null)
        {
            Vector3 local = origin != null ? origin.InverseTransformPoint(world) : world;
            return local / RenderScale;
        }
    }
}
