using UnityEngine;

/// <summary>Layout math for exploded PixelLight slot stacks (padding + slight skew).</summary>
public static class PixelLightStackPreview
{
    public const float DefaultPadPx = 6f;
    public const float DefaultSkew = 0.2f;

    public static Rect CellRect(
        int cellX, int cellY, int zIndex, float cellPx, float padPx, float skew, bool explode)
    {
        float cell = Mathf.Max(1f, cellPx);
        if (!explode)
            return new Rect(cellX * cell, cellY * cell, cell - 1f, cell - 1f);
        float pad = Mathf.Max(0f, padPx);
        float sk = Mathf.Max(0f, skew);
        float z = Mathf.Max(0, zIndex);
        float x = cellX * cell + cellY * sk * cell + z * pad;
        float y = cellY * cell + z * pad;
        return new Rect(x, y, cell - 1f, cell - 1f);
    }

    public static Vector2 GridSize(
        int width, int height, int maxZ, float cellPx, float padPx, float skew, bool explode)
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        float cell = Mathf.Max(1f, cellPx);
        if (!explode)
            return new Vector2(w * cell, h * cell);
        float extra = Mathf.Max(0, maxZ) * Mathf.Max(0f, padPx);
        return new Vector2(
            w * cell + (h - 1) * Mathf.Max(0f, skew) * cell + extra,
            h * cell + extra);
    }
}
