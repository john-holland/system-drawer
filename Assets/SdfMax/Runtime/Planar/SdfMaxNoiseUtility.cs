using UnityEngine;

namespace SdfMax
{
    public static class SdfMaxNoiseUtility
    {
        public static float SampleFractal(Vector2 uv, NoiseLibrarySettings settings, int seedOffset)
        {
            if (settings == null)
                return 0f;

            float amp = settings.amplitude;
            float freq = settings.frequency;
            float sum = 0f;
            int octaves = Mathf.Max(1, settings.octaves);
            for (int i = 0; i < octaves; i++)
            {
                float n = Mathf.PerlinNoise(
                    uv.x * freq + settings.seed * 0.13f + seedOffset,
                    uv.y * freq + settings.seed * 0.71f + seedOffset);
                sum += (n * 2f - 1f) * amp;
                freq *= settings.lacunarity;
                amp *= settings.persistence;
            }
            return sum;
        }

        public static float SampleMandelbrot(Vector2 uv, SdfMaxNode node)
        {
            float scale = Mathf.Max(0.0001f, node.noiseFrequency);
            Vector2 c = uv * scale;
            Vector2 z = Vector2.zero;
            int maxIter = Mathf.Clamp(node.mandelbrotIterations, 1, 128);
            float escape = Mathf.Max(1.1f, node.mandelbrotEscape);
            int i = 0;
            for (; i < maxIter; i++)
            {
                if (z.sqrMagnitude > escape * escape)
                    break;
                z = new Vector2(z.x * z.x - z.y * z.y, 2f * z.x * z.y) + c;
            }
            float t = i / (float)maxIter;
            return (t - 0.5f) * node.radius;
        }
    }
}
