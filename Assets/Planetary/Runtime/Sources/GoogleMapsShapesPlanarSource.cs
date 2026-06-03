using UnityEngine;

namespace Planetary.Sources
{
    /// <summary>Polygon masks from continuum Google Maps shapes proxy.</summary>
    public sealed class GoogleMapsShapesPlanarSource : IPlanetaryPlanarSource
    {
        readonly float[,] _mask;
        readonly int _maskRes;

        public GoogleMapsShapesPlanarSource(float[,] mask, int res)
        {
            _mask = mask;
            _maskRes = res;
        }

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.GoogleMaps;

        public float SampleHeight(float latDeg, float lonDeg)
        {
            if (_mask == null || _maskRes <= 0)
                return 0f;
            float u = (lonDeg + 180f) / 360f;
            float v = (latDeg + 90f) / 180f;
            int x = Mathf.Clamp(Mathf.FloorToInt(u * (_maskRes - 1)), 0, _maskRes - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(v * (_maskRes - 1)), 0, _maskRes - 1);
            return _mask[x, y] * 5f;
        }

        public float SampleSlope(float latDeg, float lonDeg) => 0f;
        public int SampleBiome(float latDeg, float lonDeg) => SampleHeight(latDeg, lonDeg) > 0.1f ? 4 : 0;
    }
}
