using UnityEngine;

namespace Planetary.Sources
{
    public sealed class AuthoredHeightmapPlanarSource : IPlanetaryPlanarSource
    {
        readonly Texture2D _height;
        readonly float _heightScale;

        public AuthoredHeightmapPlanarSource(Texture2D height, float heightScale = 100f)
        {
            _height = height;
            _heightScale = heightScale;
        }

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.Authored;

        public float SampleHeight(float latDeg, float lonDeg)
        {
            if (_height == null)
                return 0f;
            float u = (lonDeg + 180f) / 360f;
            float v = (latDeg + 90f) / 180f;
            return TextureSamplingUtility.SampleRedBilinear(_height, u, v) * _heightScale;
        }

        public float SampleSlope(float latDeg, float lonDeg)
        {
            const float e = 0.5f;
            float h = SampleHeight(latDeg, lonDeg);
            float hx = SampleHeight(latDeg, lonDeg + e) - h;
            float hy = SampleHeight(latDeg + e, lonDeg) - h;
            return Mathf.Atan(Mathf.Sqrt(hx * hx + hy * hy) / e) * Mathf.Rad2Deg;
        }

        public int SampleBiome(float latDeg, float lonDeg) => 0;
    }
}
