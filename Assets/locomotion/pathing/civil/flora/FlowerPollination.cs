using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FlowerPollenGrain
{
    public Vector3 position;
    public Vector3 velocity;
    public string sourceTreeId;
    public bool foreign;
}

public static class FlowerPollination
{
    public static bool InsideIntakeCone(Vector3 stigmaPos, Vector3 stigmaForward, Vector3 grainPos, float angleDeg, float radius)
    {
        Vector3 d = grainPos - stigmaPos;
        if (d.magnitude > Mathf.Max(0f, radius)) return false;
        if (d.sqrMagnitude < 1e-8f) return true;
        if (stigmaForward.sqrMagnitude < 1e-8f) return true;
        return Vector3.Angle(stigmaForward, d) <= Mathf.Max(0f, angleDeg) * 0.5f;
    }

    public static float Alignment01(Vector3 stigmaForward, Vector3 grainPos, Vector3 stigmaPos)
    {
        Vector3 d = grainPos - stigmaPos;
        if (d.sqrMagnitude < 1e-8f || stigmaForward.sqrMagnitude < 1e-8f) return 1f;
        return Mathf.Clamp01(Vector3.Dot(stigmaForward.normalized, d.normalized));
    }

    public static float Economy01(float affinity01, float empowerMult)
    {
        float empower = Mathf.Clamp01((empowerMult - 1f) / 3f);
        return Mathf.Clamp01(Mathf.Clamp01(affinity01) * 0.7f + empower * 0.3f);
    }

    public static float Chance(float foreignMass, float economy01, bool insideCone, float alignment01)
    {
        if (!insideCone) return 0f;
        return Mathf.Clamp01(Mathf.Max(0f, foreignMass) * Mathf.Clamp01(economy01) * Mathf.Clamp01(alignment01));
    }

    public static float Affinity(TradeDemographics trade, string fromRetinue, string toRetinue, string commodity)
    {
        if (trade?.edges == null) return 0.5f;
        for (int i = 0; i < trade.edges.Count; i++)
        {
            var edge = trade.edges[i];
            if (edge == null) continue;
            if (!string.IsNullOrEmpty(fromRetinue) &&
                !string.Equals(edge.fromRetinueId, fromRetinue, System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(edge.toRetinueId, fromRetinue, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(toRetinue) &&
                !string.Equals(edge.toRetinueId, toRetinue, System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(edge.fromRetinueId, toRetinue, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrEmpty(commodity) &&
                !string.Equals(edge.commodityKey, commodity, System.StringComparison.OrdinalIgnoreCase))
                continue;
            return edge.affinity01;
        }
        return 0.5f;
    }
}

/// <summary>Polar bins for interior flower light. One writer owns each sector.</summary>
public sealed class FlowerRadialCache
{
    public readonly int sectors;
    readonly Color[] _bins;
    Texture2D _tex;

    public FlowerRadialCache(int sectorCount = 16)
    {
        sectors = Mathf.Max(4, sectorCount);
        _bins = new Color[sectors];
    }

    public int Sector(float azimuth01)
    {
        int s = Mathf.FloorToInt(Mathf.Repeat(azimuth01, 1f) * sectors);
        return Mathf.Clamp(s, 0, sectors - 1);
    }

    public bool TryClaim(int sector, Color interiorLight)
    {
        if (sector < 0 || sector >= sectors) return false;
        if (_bins[sector].a > 0.5f) return false;
        var c = interiorLight;
        c.a = 1f;
        _bins[sector] = c;
        return true;
    }

    public Color Read(int sector)
    {
        if (sector < 0 || sector >= sectors) return Color.clear;
        return _bins[sector];
    }

    public Texture2D Upload()
    {
        if (_tex == null)
        {
            _tex = new Texture2D(sectors, 1, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                name = "FlowerRadialCache"
            };
        }
        _tex.SetPixels(_bins);
        _tex.Apply(false, false);
        return _tex;
    }
}

public static class FlowerWindBendLut
{
    public static AnimationCurve Cherry()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.25f, 0.85f),
            new Keyframe(0.5f, 0.2f),
            new Keyframe(0.75f, 0.9f),
            new Keyframe(1f, 0.1f));
    }

    public static AnimationCurve Pear()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(0.5f, 0.35f),
            new Keyframe(1f, 0.05f));
    }

    public static float Sample(AnimationCurve curve, float azimuth01)
    {
        if (curve == null || curve.length == 0) return 0f;
        return curve.Evaluate(Mathf.Repeat(azimuth01, 1f));
    }

    public static Texture2D Bake(AnimationCurve curve, int width = 32)
    {
        int w = Mathf.Max(4, width);
        var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "FlowerWindLut"
        };
        for (int x = 0; x < w; x++)
        {
            float b = Mathf.Clamp01(Sample(curve, x / (float)(w - 1)));
            tex.SetPixel(x, 0, new Color(b, b, b, 1f));
        }
        tex.Apply(false, false);
        return tex;
    }
}
