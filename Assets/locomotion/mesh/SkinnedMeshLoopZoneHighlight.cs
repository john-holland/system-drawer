using System.Collections.Generic;
using UnityEngine;

/// <summary>1-ring / radius zone verts and contrast albedo for the loop picker.</summary>
public static class SkinnedMeshLoopZoneHighlight
{
    public const float BlinkHz = 2f;

    public static void CollectZone(
        int center,
        Vector3[] verts,
        float radius,
        Dictionary<int, List<int>> adjacency,
        List<int> dst)
    {
        dst.Clear();
        if (verts == null || center < 0 || center >= verts.Length)
            return;
        var seen = new HashSet<int> { center };
        dst.Add(center);
        if (adjacency != null && adjacency.TryGetValue(center, out var ring))
        {
            for (int i = 0; i < ring.Count; i++)
            {
                int n = ring[i];
                if (n < 0 || n >= verts.Length || !seen.Add(n))
                    continue;
                dst.Add(n);
            }
        }
        float r2 = radius * radius;
        Vector3 c = verts[center];
        for (int i = 0; i < verts.Length; i++)
        {
            if (!seen.Add(i))
                continue;
            if ((verts[i] - c).sqrMagnitude <= r2)
                dst.Add(i);
        }
    }

    public static Color ZoneAverageAlbedo(
        IList<int> zone,
        Color[] vertexColors,
        Vector2[] uvs,
        Texture2D mainTex,
        Color materialColor)
    {
        if (zone == null || zone.Count == 0)
            return ContrastComplement(materialColor);

        if (vertexColors != null && vertexColors.Length > 0)
        {
            Color sum = Color.black;
            int n = 0;
            for (int i = 0; i < zone.Count; i++)
            {
                int vi = zone[i];
                if (vi < 0 || vi >= vertexColors.Length)
                    continue;
                sum += vertexColors[vi];
                n++;
            }
            if (n > 0)
                return sum / n;
        }

        if (mainTex != null && mainTex.isReadable && uvs != null)
        {
            Color sum = Color.black;
            int n = 0;
            for (int i = 0; i < zone.Count; i++)
            {
                int vi = zone[i];
                if (vi < 0 || vi >= uvs.Length)
                    continue;
                sum += mainTex.GetPixelBilinear(uvs[vi].x, uvs[vi].y);
                n++;
            }
            if (n > 0)
                return sum / n;
        }

        return materialColor.a <= 0f ? Color.gray : materialColor;
    }

    public static Color ContrastComplement(Color albedo)
    {
        return new Color(1f - albedo.r, 1f - albedo.g, 1f - albedo.b, 1f);
    }

    public static float Blink01(double time, float hz = BlinkHz)
    {
        return 0.5f + 0.5f * Mathf.Sin((float)(time * Mathf.PI * 2.0 * hz));
    }

    public static Color Blink(Color albedo, Color contrast, double time)
    {
        return Color.Lerp(albedo, contrast, Blink01(time));
    }
}
