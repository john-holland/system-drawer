using UnityEngine;

/// <summary>
/// Procedural gum height maps: tongue space + bezel from 50% tooth height shrink-wrapping roof/floor.
/// </summary>
public static class GumHeightMapGenerator
{
    public static Texture2D Generate(
        int resolution,
        ToothSlot[] teeth,
        ToothArch arch,
        float bezelStart01 = 0.5f,
        float tongueChannelWidth01 = 0.35f)
    {
        int res = Mathf.Clamp(resolution, 16, 512);
        var tex = new Texture2D(res, res, TextureFormat.RFloat, false, true)
        {
            name = arch == ToothArch.Upper ? "UpperGumHeight" : "LowerGumHeight",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1);
                float v = y / (float)(res - 1);
                float h = SampleHeight(u, v, teeth, arch, bezelStart01, tongueChannelWidth01);
                pixels[y * res + x] = new Color(h, h, h, 1f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    static float SampleHeight(
        float u, float v,
        ToothSlot[] teeth,
        ToothArch arch,
        float bezelStart01,
        float tongueChannelWidth01)
    {
        // Tongue channel depression down the midline.
        float mid = Mathf.Abs(u - 0.5f);
        float tongue = mid < tongueChannelWidth01 * 0.5f
            ? Mathf.Lerp(0.15f, 0.55f, mid / Mathf.Max(1e-3f, tongueChannelWidth01 * 0.5f))
            : 0.55f;

        float bezel = 0.55f;
        if (teeth != null)
        {
            float best = 1f;
            for (int i = 0; i < teeth.Length; i++)
            {
                var t = teeth[i];
                if (t == null || !t.present || t.arch != arch) continue;
                float tu = t.stop01;
                float tv = t.side == ToothSide.Left ? 0.25f : (t.side == ToothSide.Right ? 0.75f : 0.5f);
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(tu, tv));
                // Bezel starts at 50% tooth and shrink-wraps.
                float ring = Mathf.Abs(d - bezelStart01 * 0.08f);
                best = Mathf.Min(best, ring);
            }
            bezel = Mathf.Lerp(0.85f, 0.4f, Mathf.Clamp01(best * 12f));
        }

        float roofFloor = arch == ToothArch.Upper
            ? Mathf.Lerp(0.7f, tongue, v)
            : Mathf.Lerp(tongue, 0.7f, v);
        return Mathf.Clamp01(Mathf.Max(roofFloor, bezel) * 0.5f + tongue * 0.5f);
    }
}
