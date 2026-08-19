using UnityEngine;

/// <summary>
/// Stile/rail + pane + muntin alternating cells.
/// N panes → 2N+1 PixelLight cells (3 panes → 7: stile, pane, muntin, pane, muntin, pane, stile).
/// </summary>
public static class MuntinGridLayout
{
    public const int ElbowCount = 4;

    public static int PixelLightMin(int paneCount) => 2 * Mathf.Max(1, paneCount) + 1;

    public static Vector2Int PixelLightSize(
        int paneCountX,
        int paneCountY,
        bool sideTrim,
        bool underSillTrim,
        bool sillOccupiesRow,
        bool autoFit,
        int arbitraryWidth,
        int arbitraryHeight)
    {
        if (!autoFit)
            return new Vector2Int(Mathf.Max(1, arbitraryWidth), Mathf.Max(1, arbitraryHeight));

        int w = PixelLightMin(paneCountX) + (sideTrim ? 2 : 0);
        int h = PixelLightMin(paneCountY);
        if (sillOccupiesRow) h += 1;
        if (underSillTrim) h += 1;
        return new Vector2Int(w, h);
    }

    /// <summary>Edge run length excluding the two corner elbow cells.</summary>
    public static int TrimRunLength(int gridAlong) => Mathf.Max(0, gridAlong - 2);

    public static bool TryParseSillToken(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string t = text.Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        return t == "sill" || t == "cill" || t == "windowsill" || t == "windowcill" || t == "apron";
    }

    /// <summary>Pane/muntin world sizes from the opening, independent of PixelLight cell count.</summary>
    public static void PaneMuntinWorldSizes(
        Vector2 openingSize,
        int paneCountX,
        int paneCountY,
        float muntinWidth,
        out Vector2 paneSize,
        out float barWidth)
    {
        int px = Mathf.Max(1, paneCountX);
        int py = Mathf.Max(1, paneCountY);
        barWidth = Mathf.Max(0.01f, muntinWidth);
        float innerW = Mathf.Max(0.05f, openingSize.x - 2f * barWidth);
        float innerH = Mathf.Max(0.05f, openingSize.y - 2f * barWidth);
        float muntinSpanX = Mathf.Max(0, px - 1) * barWidth;
        float muntinSpanY = Mathf.Max(0, py - 1) * barWidth;
        paneSize = new Vector2(
            Mathf.Max(0.02f, (innerW - muntinSpanX) / px),
            Mathf.Max(0.02f, (innerH - muntinSpanY) / py));
    }
}
