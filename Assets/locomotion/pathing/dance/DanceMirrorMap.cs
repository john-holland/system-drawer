using System;
using UnityEngine;

/// <summary>Lead → answer pairing on the rainbow mirror map.</summary>
[Serializable]
public sealed class DancePairing
{
    public int callAnimationIndex;
    public int responseAnimationIndex;
    [Range(0f, 1f)] public float callSlot01;
    [Range(0f, 1f)] public float responseSlot01;
    public string danceModeId = "bar_sway";
}

/// <summary>
/// Interstellar-5555 style polar / square map: hue from blue by angular offset;
/// diagonal intersect veto when allowIntersect is false.
/// </summary>
public static class DanceMirrorMap
{
    public static Color HueFromOffset(float offsetDegFromPerpendicular)
    {
        float t = Mathf.Clamp01(Mathf.Abs(offsetDegFromPerpendicular) / 90f);
        float hue = Mathf.Lerp(0.66f, 0.0f, t);
        return Color.HSVToRGB(hue, 0.85f, 1f);
    }

    public static float OffsetFromPerpendicular(DancePairing pairing)
    {
        if (pairing == null)
            return 0f;
        float dy = pairing.responseSlot01 - pairing.callSlot01;
        float angle = Mathf.Atan2(dy, 1f) * Mathf.Rad2Deg;
        return angle - 90f;
    }

    /// <summary>
    /// Pairings as segments (0, call) → (1, response) in unit square. True if they cross.
    /// </summary>
    public static bool PairingsIntersect(DancePairing a, DancePairing b)
    {
        if (a == null || b == null)
            return false;
        return SegmentsCross(
            0f, a.callSlot01, 1f, a.responseSlot01,
            0f, b.callSlot01, 1f, b.responseSlot01);
    }

    public static bool SegmentsCross(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
    {
        float d1 = Cross(bx - ax, by - ay, cx - ax, cy - ay);
        float d2 = Cross(bx - ax, by - ay, dx - ax, dy - ay);
        float d3 = Cross(dx - cx, dy - cy, ax - cx, ay - cy);
        float d4 = Cross(dx - cx, dy - cy, bx - cx, by - cy);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;
        return false;
    }

    static float Cross(float ax, float ay, float bx, float by) => ax * by - ay * bx;

    /// <summary>True if adding candidate would cross an existing pairing while veto is on.</summary>
    public static bool IsBlockedByIntersect(DancePairing[] existing, DancePairing candidate, bool allowIntersect)
    {
        if (allowIntersect || candidate == null || existing == null)
            return false;
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null)
                continue;
            if (ReferenceEquals(existing[i], candidate))
                continue;
            if (PairingsIntersect(existing[i], candidate))
                return true;
        }
        return false;
    }
}
