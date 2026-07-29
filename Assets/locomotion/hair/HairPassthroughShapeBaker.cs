using UnityEngine;

/// <summary>
/// Bakes additional / animated curve passthrough shape textures (same radial UV layout)
/// for blend into HairPlume.shader via _PassthroughATex / _PassthroughBTex.
/// </summary>
public static class HairPassthroughShapeBaker
{
    public struct CurveDef
    {
        public float azimuth01;
        public float lengthStart01;
        public float lengthEnd01;
        public float width01;
        public float height01;
    }

    public static Texture2D Bake(HairPlumeConfig config, CurveDef[] curves, string name = "HairPassthrough")
    {
        int az = config != null ? Mathf.Max(8, config.azimuthBins) : 64;
        int len = config != null ? Mathf.Max(4, config.lengthBins) : 32;
        var pixels = new Color[az * len];
        if (curves != null)
        {
            for (int c = 0; c < curves.Length; c++)
                StampCurve(pixels, az, len, curves[c]);
        }

        var tex = new Texture2D(az, len, TextureFormat.RGBA32, false, true)
        {
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = name
        };
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    public static void BakeIntoCacheGreen(HairRadialTextureCache cache, CurveDef[] curves, float blend = 1f)
    {
        if (cache == null || curves == null) return;
        blend = Mathf.Clamp01(blend);
        int az = cache.AzimuthBins;
        int len = cache.LengthBins;
        for (int c = 0; c < curves.Length; c++)
        {
            var curve = curves[c];
            float halfW = Mathf.Max(0.01f, curve.width01) * 0.5f;
            float v0 = Mathf.Clamp01(Mathf.Min(curve.lengthStart01, curve.lengthEnd01));
            float v1 = Mathf.Clamp01(Mathf.Max(curve.lengthStart01, curve.lengthEnd01));
            for (int v = 0; v < len; v++)
            {
                float length01 = v / (float)(len - 1);
                if (length01 < v0 || length01 > v1) continue;
                for (int u = 0; u < az; u++)
                {
                    float azimuth01 = u / (float)az;
                    float d = AzimuthDelta(azimuth01, curve.azimuth01);
                    float w = 1f - Mathf.Clamp01(d / halfW);
                    if (w <= 0f) continue;
                    cache.MaxChannel(u, v, 1, curve.height01 * w * blend);
                }
            }
        }
        cache.Apply();
    }

    static void StampCurve(Color[] pixels, int az, int len, CurveDef curve)
    {
        float halfW = Mathf.Max(0.01f, curve.width01) * 0.5f;
        float v0 = Mathf.Clamp01(Mathf.Min(curve.lengthStart01, curve.lengthEnd01));
        float v1 = Mathf.Clamp01(Mathf.Max(curve.lengthStart01, curve.lengthEnd01));
        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            if (length01 < v0 || length01 > v1) continue;
            for (int u = 0; u < az; u++)
            {
                float azimuth01 = u / (float)az;
                float d = AzimuthDelta(azimuth01, curve.azimuth01);
                float w = 1f - Mathf.Clamp01(d / halfW);
                if (w <= 0f) continue;
                int idx = v * az + u;
                Color c = pixels[idx];
                c.r = Mathf.Max(c.r, curve.height01 * w);
                c.a = Mathf.Max(c.a, w);
                pixels[idx] = c;
            }
        }
    }

    static float AzimuthDelta(float a, float b)
    {
        float d = Mathf.Abs(Mathf.Repeat(a - b + 0.5f, 1f) - 0.5f);
        return d;
    }

    public static void BindLayers(Material mat, Texture2D a, Texture2D b, float weightA = 1f, float weightB = 1f)
    {
        if (mat == null) return;
        if (a != null) mat.SetTexture("_PassthroughATex", a);
        if (b != null) mat.SetTexture("_PassthroughBTex", b);
        mat.SetFloat("_PassthroughBlendA", weightA);
        mat.SetFloat("_PassthroughBlendB", weightB);
    }
}
