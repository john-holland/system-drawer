using UnityEngine;
using SdfMax;

/// <summary>
/// Lattice waterfall baker: walks azimuth × length bins, samples gaussian / SDF Max,
/// and fills a complete radial texture cache (passthrough + hold + tip channels).
/// </summary>
public static class HairLatticeWaterfallBaker
{
    public struct BakeResult
    {
        public Color[] pixels;
        public int azimuthBins;
        public int lengthBins;
        public Texture2D texture;
    }

    public static BakeResult Bake(
        HairPlumeConfig config,
        SdfMaxCompositionAsset composition = null,
        Transform scalpRoot = null)
    {
        config ??= ScriptableObject.CreateInstance<HairPlumeConfig>();
        int az = Mathf.Max(8, config.azimuthBins);
        int len = Mathf.Max(4, config.lengthBins);
        var pixels = new Color[az * len];

        SdfMaxEvaluator evaluator = null;
        if (composition != null)
        {
            Matrix4x4 l2w = scalpRoot != null ? scalpRoot.localToWorldMatrix : Matrix4x4.identity;
            var graph = new SdfMaxExpressionGraph(composition, null, l2w);
            evaluator = new SdfMaxEvaluator(graph);
        }

        float tipHold = Mathf.Clamp01(config.plumeTipHold);
        float maxLen = Mathf.Max(0.05f, config.maxStrandLengthM);

        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            for (int u = 0; u < az; u++)
            {
                float azimuth01 = u / (float)az;
                float height01 = HairPlumeSdfComposer.SampleGaussianHeight(azimuth01, length01, config);
                // Modulate by conical flare from hairline
                float cone = config.conicalEmergenceCurve != null
                    ? Mathf.Max(0.05f, config.conicalEmergenceCurve.Evaluate(length01))
                    : 1f;
                height01 *= Mathf.Clamp01(0.85f + 0.15f * cone);

                if (evaluator != null && scalpRoot != null)
                {
                    Vector3 ring = HairLineSampler.EmergenceRingPoint(scalpRoot, config, azimuth01);
                    Vector3 dir = HairLineSampler.EmergenceDirection(scalpRoot, config, azimuth01);
                    Vector3 world = ring + dir * (length01 * maxLen);
                    float sdf = evaluator.Sample(world, 0f);
                    float shell = Mathf.Clamp01(1f - Mathf.Abs(sdf) / Mathf.Max(0.05f, maxLen));
                    height01 = Mathf.Max(height01, shell * tipHold);
                }
                else if (scalpRoot != null)
                {
                    // Analytic sample along emergence direction reinforces tip hold
                    float r = HairLineSampler.Radius(config, azimuth01);
                    height01 = Mathf.Max(height01, Mathf.Clamp01(1f - length01) * (r / Mathf.Max(1e-4f, config.scalpRadiusM)) * 0.25f);
                }

                float curveHold = height01 * tipHold * (0.7f + 0.3f * Mathf.Cos(azimuth01 * Mathf.PI * 6f));
                float tipBreak = HairPlumeSdfComposer.SampleTipBreakEnergy(length01, config);
                float passthrough = 0f;

                pixels[v * az + u] = new Color(
                    Mathf.Clamp01(height01),
                    Mathf.Clamp01(passthrough),
                    Mathf.Clamp01(curveHold),
                    Mathf.Clamp01(tipBreak));
            }
        }

        var tex = new Texture2D(az, len, TextureFormat.RGBA32, false, true)
        {
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HairLatticeWaterfallBake"
        };
        tex.SetPixels(pixels);
        tex.Apply(false, false);

        return new BakeResult
        {
            pixels = pixels,
            azimuthBins = az,
            lengthBins = len,
            texture = tex
        };
    }

    public static void ApplyToCache(BakeResult bake, HairRadialTextureCache cache)
    {
        if (cache == null || bake.pixels == null) return;
        if (cache.AzimuthBins != bake.azimuthBins || cache.LengthBins != bake.lengthBins)
            return;
        cache.CopyFrom(bake.pixels);
        cache.Apply();
    }
}
