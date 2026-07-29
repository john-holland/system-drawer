using UnityEngine;

/// <summary>
/// Bakes fiberglass-like diffuse / specular textures along lattice tangents (radial UV).
/// </summary>
public static class HairFiberMaterialBaker
{
    public static void Bake(
        HairPlumeConfig config,
        Color rootColor,
        Color tipColor,
        out Texture2D diffuse,
        out Texture2D specular)
    {
        int az = config != null ? Mathf.Max(8, config.azimuthBins) : 64;
        int len = config != null ? Mathf.Max(4, config.lengthBins) : 32;
        float tipHold = config != null ? Mathf.Clamp01(config.plumeTipHold) : 0.55f;

        var diffPixels = new Color[az * len];
        var specPixels = new Color[az * len];

        for (int v = 0; v < len; v++)
        {
            float length01 = v / (float)(len - 1);
            Color baseCol = Color.Lerp(rootColor, tipColor, length01);
            for (int u = 0; u < az; u++)
            {
                float azimuth01 = u / (float)az;
                // Fiber strand modulation along azimuth (anisotropy proxy)
                float fiber = 0.85f + 0.15f * Mathf.Sin((azimuth01 * 48f + length01 * 6f) * Mathf.PI * 2f);
                float clump = 0.9f + 0.1f * Mathf.Sin(azimuth01 * Mathf.PI * 8f);
                Color d = baseCol * fiber * clump;
                d.a = 1f;
                diffPixels[v * az + u] = d;

                // Specular lobe intensity: higher near mid-shaft, stronger with tipHold (glassy hold)
                float lobe = Mathf.Sin(length01 * Mathf.PI) * Mathf.Lerp(0.35f, 0.85f, tipHold);
                lobe *= 0.7f + 0.3f * fiber;
                specPixels[v * az + u] = new Color(lobe, lobe * 0.9f, lobe * 0.8f, 1f);
            }
        }

        diffuse = MakeTex(az, len, diffPixels, "HairFiberDiffuse");
        specular = MakeTex(az, len, specPixels, "HairFiberSpecular");
    }

    public static void Bind(Material mat, Texture2D diffuse, Texture2D specular)
    {
        if (mat == null) return;
        if (diffuse != null) mat.SetTexture("_HairDiffuseTex", diffuse);
        if (specular != null) mat.SetTexture("_HairSpecTex", specular);
    }

    static Texture2D MakeTex(int w, int h, Color[] pixels, string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true)
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
}
