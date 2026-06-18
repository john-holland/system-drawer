using UnityEngine;

namespace Weather.Lod
{
    /// <summary>Prolate spheroid egg bounds for player weather LOD zones.</summary>
    public static class WeatherEggBounds
    {
        public static bool Contains(Vector3 center, Vector3 radii, Vector3 point)
        {
            Vector3 d = point - center;
            float nx = radii.x > 0f ? d.x / radii.x : 0f;
            float ny = radii.y > 0f ? d.y / radii.y : 0f;
            float nz = radii.z > 0f ? d.z / radii.z : 0f;
            return nx * nx + ny * ny + nz * nz <= 1f;
        }

        public static float NormalizedDistance(Vector3 center, Vector3 radii, Vector3 point)
        {
            Vector3 d = point - center;
            float nx = radii.x > 0f ? d.x / radii.x : 0f;
            float ny = radii.y > 0f ? d.y / radii.y : 0f;
            float nz = radii.z > 0f ? d.z / radii.z : 0f;
            return Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
        }

        /// <summary>1 inside egg, smooth falloff outside shell.</summary>
        public static float ShellWeight(Vector3 center, Vector3 radii, Vector3 point, float shellThickness = 2f)
        {
            float r = NormalizedDistance(center, radii, point);
            if (r <= 1f)
                return 1f;
            float scale = Mathf.Min(radii.x, Mathf.Min(radii.y, radii.z));
            float overshoot = (r - 1f) * scale;
            return Mathf.Clamp01(1f - overshoot / Mathf.Max(0.01f, shellThickness));
        }

        public static Bounds GetAabb(Vector3 center, Vector3 radii) =>
            new Bounds(center, radii * 2f);

        public static float OverlapGradientWeight(Vector3 centerA, Vector3 radiiA, Vector3 centerB, Vector3 radiiB, Vector3 point)
        {
            float wa = ShellWeight(centerA, radiiA, point);
            float wb = ShellWeight(centerB, radiiB, point);
            float sum = wa + wb;
            return sum > 0.0001f ? wa / sum : 0.5f;
        }
    }
}
