using SdfMax;
using UnityEngine;

namespace Planetary.Sources
{
    public sealed class ProceduralPlanarSource : IPlanetaryPlanarSource
    {
        readonly NoiseLibrarySettings _noise;
        readonly float _mandelbrotScale;

        public ProceduralPlanarSource(NoiseLibrarySettings noise, float mandelbrotScale = 0.002f)
        {
            _noise = noise ?? new NoiseLibrarySettings();
            _mandelbrotScale = mandelbrotScale;
        }

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.Procedural;

        public float SampleHeight(float latDeg, float lonDeg)
        {
            Vector2 uv = new Vector2(lonDeg * 0.01f, latDeg * 0.01f);
            float n = SdfMaxNoiseUtility.SampleFractal(uv, _noise, 0);
            float m = SdfMaxNoiseUtility.SampleMandelbrot(uv * _mandelbrotScale, new SdfMax.SdfMaxNode
            {
                noiseFrequency = 1f,
                mandelbrotIterations = 24,
                radius = 50f
            });
            return n * 20f + m;
        }

        public float SampleSlope(float latDeg, float lonDeg)
        {
            const float e = 0.05f;
            float h = SampleHeight(latDeg, lonDeg);
            float hx = SampleHeight(latDeg, lonDeg + e) - h;
            float hy = SampleHeight(latDeg + e, lonDeg) - h;
            return Mathf.Atan(Mathf.Sqrt(hx * hx + hy * hy) / e) * Mathf.Rad2Deg;
        }

        public int SampleBiome(float latDeg, float lonDeg) => SampleHeight(latDeg, lonDeg) > 0f ? 1 : 0;
    }
}
