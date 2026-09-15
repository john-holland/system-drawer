using SdfMax;
using UnityEngine;

/// <summary>Homogeneous bolt UV: polar pin weights + linear Mandelbrot bunch amplitude.</summary>
public static class ClothMandelbrotScrunch
{
    public static float PinWeight(Vector2 uv, Vector2 pinUv, float radius)
    {
        float r = Mathf.Max(1e-4f, radius);
        float d = Vector2.Distance(uv, pinUv);
        return Mathf.Clamp01(1f - d / r);
    }

    public static float NearestPinWeight(Vector2 uv, ClothSplinePath path, float radius)
    {
        if (path?.grabbers == null || path.grabbers.Count == 0)
            return 0f;
        float best = 0f;
        for (int i = 0; i < path.grabbers.Count; i++)
        {
            var g = path.grabbers[i];
            if (g == null) continue;
            best = Mathf.Max(best, PinWeight(uv, g.uv, radius) * g.weight01);
        }
        return best;
    }

    public static float BunchAmplitude(Vector2 uv, int iterations, float muFriction01)
    {
        var node = new SdfMaxNode
        {
            noiseFrequency = 2.2f,
            mandelbrotIterations = Mathf.Clamp(iterations, 1, 128),
            mandelbrotEscape = 2f,
            radius = 1f
        };
        float raw = SdfMaxNoiseUtility.SampleMandelbrot(uv, node);
        float iterScale = iterations / 32f;
        float amp = Mathf.Abs(raw) * iterScale;
        return amp * (1f - Mathf.Clamp01(muFriction01) * 0.65f);
    }

    public static float WeightedBunch(Vector2 uv, ClothSplinePath path, int iterations, float muFriction01, float pinRadius)
    {
        float pin = NearestPinWeight(uv, path, pinRadius);
        float bunch = BunchAmplitude(uv, iterations, muFriction01);
        return bunch * (1f - pin);
    }
}
