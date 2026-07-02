using UnityEngine;

namespace Planetary.AsteroidBelt
{
    /// <summary>Statistical angular density field for far LOD asteroid belt rendering.</summary>
    public sealed class AsteroidBeltStatisticalManifold : MonoBehaviour
    {
        public float innerRadiusM = 1e8f;
        public float outerRadiusM = 4e8f;
        public Vector3 orbitalPlaneNormal = Vector3.up;
        public float meanDensity = 0.35f;
        public float densityVariance = 0.15f;
        public int seed = 12345;
        public Transform parentPlanet;
        public int angularBins = 64;
        public int radialBins = 8;

        public float SampleDensity(Vector3 worldPos)
        {
            Vector3 center = parentPlanet != null ? parentPlanet.position : transform.position;
            Vector3 local = worldPos - center;
            float dist = local.magnitude;
            if (dist < innerRadiusM || dist > outerRadiusM)
                return 0f;
            float theta = Mathf.Atan2(
                Vector3.Dot(local, OrbitalTangent(local)),
                Vector3.Dot(local, OrbitalBitangent(local)));
            int tBin = Mathf.Abs(Mathf.FloorToInt((theta / (Mathf.PI * 2f) + 0.5f) * angularBins)) % angularBins;
            int rBin = Mathf.Clamp(Mathf.FloorToInt((dist - innerRadiusM) / (outerRadiusM - innerRadiusM) * radialBins), 0, radialBins - 1);
            float hash = Hash01(seed, tBin, rBin);
            return Mathf.Clamp01(meanDensity + (hash - 0.5f) * 2f * densityVariance);
        }

        public float SampleAngularOpacity(Vector3 viewDir, float distKm)
        {
            float density = meanDensity;
            if (parentPlanet != null)
            {
                Vector3 sample = parentPlanet.position + viewDir.normalized * (innerRadiusM + outerRadiusM) * 0.5f;
                density = SampleDensity(sample);
            }
            float limb = 1f - Mathf.Abs(Vector3.Dot(viewDir.normalized, orbitalPlaneNormal.normalized));
            return Mathf.Clamp01(1f - density * limb);
        }

        Vector3 OrbitalTangent(Vector3 local)
        {
            Vector3 n = orbitalPlaneNormal.normalized;
            return Vector3.Cross(n, local).normalized;
        }

        Vector3 OrbitalBitangent(Vector3 local)
        {
            Vector3 n = orbitalPlaneNormal.normalized;
            Vector3 t = OrbitalTangent(local);
            return Vector3.Cross(t, n).normalized;
        }

        static float Hash01(int seed, int a, int b)
        {
            uint h = (uint)(seed ^ (a * 73856093) ^ (b * 19349663));
            h = (h ^ (h >> 16)) * 2246822507u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
