using UnityEngine;

namespace Planetary.Sources
{
    public sealed class PlanarStampPlanarSource : IPlanetaryPlanarSource
    {
        readonly PlanetaryPlanarFeatureStack _stack;
        readonly Vector3 _planetCenter;
        readonly Vector3 _poleAxis;
        readonly float _primeMeridian;

        public PlanarStampPlanarSource(
            PlanetaryPlanarFeatureStack stack,
            Vector3 planetCenter,
            Vector3 poleAxis,
            float primeMeridian)
        {
            _stack = stack;
            _planetCenter = planetCenter;
            _poleAxis = poleAxis;
            _primeMeridian = primeMeridian;
        }

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.Authored;

        public float SampleHeight(float latDeg, float lonDeg)
        {
            if (_stack == null || _stack.features == null)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < _stack.features.Count; i++)
            {
                var f = _stack.features[i];
                if (f == null || f.heightMap == null)
                    continue;
                float dLat = latDeg - f.latitudeDeg;
                float dLon = lonDeg - f.longitudeDeg;
                float dist = Mathf.Sqrt(dLat * dLat + dLon * dLon);
                if (dist > f.footprintRadiusMeters * 0.01f)
                    continue;
                float u = (dLon / (f.footprintRadiusMeters * 0.01f) + 1f) * 0.5f;
                float v = (dLat / (f.footprintRadiusMeters * 0.01f) + 1f) * 0.5f;
                sum += TextureSamplingUtility.SampleRedBilinear(f.heightMap, u, v) * f.strength * 100f;
            }
            return sum;
        }

        public float SampleSlope(float latDeg, float lonDeg)
        {
            const float e = 0.02f;
            float h = SampleHeight(latDeg, lonDeg);
            float hx = SampleHeight(latDeg, lonDeg + e) - h;
            float hy = SampleHeight(latDeg + e, lonDeg) - h;
            return Mathf.Atan(Mathf.Sqrt(hx * hx + hy * hy) / e) * Mathf.Rad2Deg;
        }

        public int SampleBiome(float latDeg, float lonDeg) => SampleHeight(latDeg, lonDeg) > 1f ? 2 : 0;
    }
}
