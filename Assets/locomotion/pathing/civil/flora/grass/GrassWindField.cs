using UnityEngine;

/// <summary>CPU mirror of GrassWindBend. Dual-octave scroll, gust mix, and height-squared tip warp.</summary>
public static class GrassWindField
{
    public static Vector2 ScrollUv(Vector2 p, float scale, Vector2 direction, float time, float velocity)
    {
        return p * scale + direction * time * velocity;
    }

    public static float MixedStrength(float macroGust, float microFlutter)
    {
        return macroGust + microFlutter * 0.25f;
    }

    public static Vector2 HeightWarp(Vector2 windDisplacement, float strength, float layerHeight)
    {
        float sharpened = Mathf.Pow(Mathf.Max(0f, strength), 2f);
        float height = layerHeight * layerHeight;
        return windDisplacement * sharpened * height;
    }

    public static Vector2 Hash2(Vector2 p)
    {
        float x = Mathf.Sin(Vector2.Dot(p, new Vector2(127.1f, 311.7f))) * 43758.5453f;
        float y = Mathf.Sin(Vector2.Dot(p, new Vector2(269.5f, 183.3f))) * 43758.5453f;
        return -Vector2.one + 2f * new Vector2(x - Mathf.Floor(x), y - Mathf.Floor(y));
    }

    public static float AnalyticalNoise(Vector2 p)
    {
        Vector2 i = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
        Vector2 f = new Vector2(p.x - i.x, p.y - i.y);
        Vector2 u = new Vector2(f.x * f.x * (3f - 2f * f.x), f.y * f.y * (3f - 2f * f.y));
        float a = Vector2.Dot(Hash2(i), f);
        float b = Vector2.Dot(Hash2(i + new Vector2(1f, 0f)), f - new Vector2(1f, 0f));
        float c = Vector2.Dot(Hash2(i + new Vector2(0f, 1f)), f - new Vector2(0f, 1f));
        float d = Vector2.Dot(Hash2(i + Vector2.one), f - Vector2.one);
        return Mathf.Lerp(Mathf.Lerp(a, b, u.x), Mathf.Lerp(c, d, u.x), u.y);
    }

    public static void UploadChords(PlantRadialWindCache cache, Vector2 direction, float macroGust, float microFlutter)
    {
        if (cache == null)
            return;
        cache.UploadMacro(PlantRadialWindCache.EncodeChord(macroGust, microFlutter, direction));
    }
}
