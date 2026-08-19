using UnityEngine;

public enum WindowGlazingKind
{
    Single = 0,
    DoubleVacuum = 1
}

public enum WindowSashSlideAxis
{
    Horizontal = 0,
    Vertical = 1
}

public enum WindowShutterKind
{
    None = 0,
    Left = 1,
    Right = 2,
    Both = 3
}

public enum WindowShadeKind
{
    None = 0,
    Slats = 1,
    Cloth = 2,
    Reeds = 3
}

/// <summary>Composable house window: panes, IGU, sliding sashes, muntin grid, sill, trim, shutters, shades.</summary>
[CreateAssetMenu(fileName = "WindowAssemblySpec", menuName = "Locomotion/Civil/Window Assembly")]
public sealed class WindowAssemblySpec : ScriptableObject
{
    [Header("Panes")]
    public int paneCountX = 3;
    public int paneCountY = 3;
    public Vector2 openingSize = new Vector2(1.2f, 1.4f);
    public float paneThickness = 0.006f;
    public WindowGlazingKind glazing = WindowGlazingKind.Single;
    public float vacuumGap = 0.012f;

    [Header("Sliding")]
    public int slidingSashCount;
    public WindowSashSlideAxis slideAxis = WindowSashSlideAxis.Horizontal;
    public float slideMeters = 0.55f;

    [Header("Muntin grid")]
    public float muntinWidth = 0.03f;
    public float muntinDepth = 0.04f;
    public bool pixelLightOnMuntins;

    [Header("Sill / cill")]
    public float sillDepth = 0.12f;
    public float sillHeight = 0.04f;
    public bool sillOccupiesRow = true;
    public bool underSillTrim;

    [Header("Trim")]
    public bool sideTrim = true;
    public int trimRunSegments = 3;

    [Header("Shutters / shades")]
    public WindowShutterKind shutters = WindowShutterKind.None;
    public WindowShadeKind shade = WindowShadeKind.None;

    [Header("PixelLight")]
    public bool autoFitPixelLightGrid = true;
    public int pixelLightGridW = 7;
    public int pixelLightGridH = 7;
    public float pixelLightCellSize = 0.08f;

    [Range(0f, 1f)] public float smellPassThrough01 = 0.15f;
    [Range(0f, 1f)] public float hearingLeak01 = 0.2f;

    public Vector2Int FittedGridSize =>
        MuntinGridLayout.PixelLightSize(
            paneCountX, paneCountY, sideTrim, underSillTrim, sillOccupiesRow,
            autoFitPixelLightGrid, pixelLightGridW, pixelLightGridH);

    public void ApplyAutoFit()
    {
        if (!autoFitPixelLightGrid) return;
        var size = FittedGridSize;
        pixelLightGridW = size.x;
        pixelLightGridH = size.y;
    }

    public void PaneMuntinWorldSizes(out Vector2 paneSize, out float barWidth) =>
        MuntinGridLayout.PaneMuntinWorldSizes(openingSize, paneCountX, paneCountY, muntinWidth, out paneSize, out barWidth);

    public int TrimRunLengthAlongX() => MuntinGridLayout.TrimRunLength(FittedGridSize.x);
    public int TrimRunLengthAlongY() => MuntinGridLayout.TrimRunLength(FittedGridSize.y);
}
