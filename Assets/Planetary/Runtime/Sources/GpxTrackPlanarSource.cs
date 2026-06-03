using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Sources
{
    public sealed class GpxTrackPlanarSource : IPlanetaryPlanarSource
    {
        readonly List<Vector2> _latLonPoints = new List<Vector2>();
        readonly float _influenceDeg;

        public GpxTrackPlanarSource(float influenceDeg = 0.5f)
        {
            _influenceDeg = influenceDeg;
        }

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.Gpx;

        public void AddPoint(float latDeg, float lonDeg) => _latLonPoints.Add(new Vector2(latDeg, lonDeg));

        public float SampleHeight(float latDeg, float lonDeg)
        {
            float max = 0f;
            for (int i = 0; i < _latLonPoints.Count; i++)
            {
                float d = Vector2.Distance(_latLonPoints[i], new Vector2(latDeg, lonDeg));
                if (d < _influenceDeg)
                    max = Mathf.Max(max, (1f - d / _influenceDeg) * 10f);
            }
            return max;
        }

        public float SampleSlope(float latDeg, float lonDeg) => 0f;
        public int SampleBiome(float latDeg, float lonDeg) => SampleHeight(latDeg, lonDeg) > 0.1f ? 3 : 0;
    }
}
