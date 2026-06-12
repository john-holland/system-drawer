using SdfMax;
using UnityEngine;

namespace Planetary
{
    public sealed class PlanetPlanarEvaluationContext : IPlanarEvaluationContext
    {
        readonly PlanetBody _body;
        readonly PlanetaryPlanarBase _planar;

        public PlanetPlanarEvaluationContext(PlanetBody body, PlanetaryPlanarBase planar)
        {
            _body = body;
            _planar = planar;
        }

        public bool TryWorldToLatLon(Vector3 worldPos, out float latDeg, out float lonDeg)
        {
            latDeg = lonDeg = 0f;
            if (_body == null)
                return false;
            var s = SphericalCoordinates.FromWorldPosition(
                worldPos,
                _body.PlanetCenter,
                _body.StablePoleAxis,
                _body.PrimeMeridianOffsetDeg);
            latDeg = s.LatitudeDeg;
            lonDeg = s.LongitudeDeg;
            return true;
        }

        public float SampleHeightAtLatLon(float latDeg, float lonDeg) =>
            _planar != null ? _planar.SampleHeight(latDeg, lonDeg) : 0f;

        public bool TryWorldToPlanarUV(Vector3 worldPos, out int featureIndex, out Vector2 uv)
        {
            featureIndex = -1;
            uv = Vector2.zero;
            if (_body == null || _planar == null || _planar.featureStack == null)
                return false;
            if (!TryWorldToLatLon(worldPos, out float lat, out float lon))
                return false;

            float best = float.MaxValue;
            for (int i = 0; i < _planar.featureStack.features.Count; i++)
            {
                var f = _planar.featureStack.features[i];
                if (f == null)
                    continue;
                float d = Vector2.Distance(new Vector2(lat, lon), new Vector2(f.latitudeDeg, f.longitudeDeg));
                if (d < best)
                {
                    best = d;
                    featureIndex = i;
                    float span = Mathf.Max(0.01f, f.footprintRadiusMeters * 0.01f);
                    uv = new Vector2((lon - f.longitudeDeg) / span + 0.5f, (lat - f.latitudeDeg) / span + 0.5f);
                }
            }
            return featureIndex >= 0;
        }

        public float SampleStampHeight(int featureIndex, Vector2 uv)
        {
            if (_planar?.featureStack == null || featureIndex < 0 || featureIndex >= _planar.featureStack.features.Count)
                return 0f;
            var f = _planar.featureStack.features[featureIndex];
            if (f?.heightMap == null)
                return 0f;
            return TextureSamplingUtility.SampleRedBilinear(
                f.heightMap, Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y)) * f.strength * 100f;
        }

        public float SampleProceduralNoise(Vector2 uv, float narrativeTime, NoiseKind kind)
        {
            if (_planar == null)
                return 0f;
            var settings = _planar.proceduralNoise ?? new NoiseLibrarySettings();
            return kind == NoiseKind.Mandelbrot
                ? SdfMaxNoiseUtility.SampleMandelbrot(uv, new SdfMaxNode { noiseFrequency = 1f, mandelbrotIterations = 24, radius = 50f })
                : SdfMaxNoiseUtility.SampleFractal(uv, settings, (int)(narrativeTime * 10f));
        }
    }
}
