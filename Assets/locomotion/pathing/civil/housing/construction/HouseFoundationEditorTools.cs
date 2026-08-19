using System.Text;
using UnityEngine;

/// <summary>Paint / inspect brushes for the House Foundation Layers editor.</summary>
public enum HouseFoundationBrushKind
{
    Select = 0,
    Paint = 1,
    Erase = 2
}

/// <summary>MEP / finish modes. Colored with RYB primaries and their complements.</summary>
public enum HouseFoundationEditorMode
{
    Construction = 0,
    Electrical = 1,
    Hvac = 2,
    Lighting = 3,
    Insulation = 4,
    Water = 5,
    Yard = 6
}

/// <summary>RYB primaries (red / yellow / blue) and complements (green / purple / orange).</summary>
public static class HouseFoundationPalette
{
    public static readonly Color Red = new Color(0.90f, 0.22f, 0.21f, 1f);
    public static readonly Color Yellow = new Color(0.99f, 0.85f, 0.21f, 1f);
    public static readonly Color Blue = new Color(0.12f, 0.53f, 0.90f, 1f);
    public static readonly Color Green = new Color(0.26f, 0.63f, 0.28f, 1f);
    public static readonly Color Purple = new Color(0.56f, 0.14f, 0.67f, 1f);
    public static readonly Color Orange = new Color(0.98f, 0.55f, 0.00f, 1f);
    public static readonly Color SelectWhite = new Color(0.94f, 0.94f, 0.96f, 1f);
    public static readonly Color EmptyCell = new Color(0.15f, 0.15f, 0.16f, 1f);
    public static readonly Color Construction = new Color(0.55f, 0.58f, 0.62f, 1f);

    public static Color BrushColor(HouseFoundationBrushKind brush)
    {
        switch (brush)
        {
            case HouseFoundationBrushKind.Select: return SelectWhite;
            case HouseFoundationBrushKind.Paint: return Blue;
            case HouseFoundationBrushKind.Erase: return Orange;
            default: return Color.gray;
        }
    }

    public static Color ModeColor(HouseFoundationEditorMode mode)
    {
        switch (mode)
        {
            case HouseFoundationEditorMode.Electrical: return Yellow;
            case HouseFoundationEditorMode.Hvac: return Blue;
            case HouseFoundationEditorMode.Lighting: return Orange;
            case HouseFoundationEditorMode.Insulation: return Red;
            case HouseFoundationEditorMode.Water: return Purple;
            case HouseFoundationEditorMode.Yard: return Green;
            default: return Construction;
        }
    }

    /// <summary>Store mode in the cell byte so the grid keeps primary/complement colors (0 = empty).</summary>
    public static byte PaintValue(HouseFoundationEditorMode mode) =>
        (byte)(Mathf.Clamp((int)mode, 0, 6) + 1);

    public static Color ColorForCell(byte value) => ColorForCell(value, null);

    public static Color ColorForCell(byte value, WallBrushCatalog catalog)
    {
        if (value == 0) return EmptyCell;
        if (value >= WallBrushSpec.FirstCatalogPaintByte)
        {
            var spec = catalog != null ? catalog.FindByPaintByte(value) : null;
            return spec != null ? spec.color : Construction;
        }
        int mode = value - 1;
        if (mode < 0 || mode > (int)HouseFoundationEditorMode.Yard)
            return Construction;
        return ModeColor((HouseFoundationEditorMode)mode);
    }

    public static Color LayerColor(HouseConstructionLayerKind kind)
    {
        switch (kind)
        {
            case HouseConstructionLayerKind.Insulation: return Red;
            case HouseConstructionLayerKind.YardPatioWalks: return Green;
            case HouseConstructionLayerKind.Openings: return Orange;
            case HouseConstructionLayerKind.RoughMEP: return Yellow;
            case HouseConstructionLayerKind.EavesGutters: return Purple;
            case HouseConstructionLayerKind.Furnishings: return Purple;
            case HouseConstructionLayerKind.DigSite: return Orange;
            case HouseConstructionLayerKind.Foundation: return Yellow;
            default: return Construction;
        }
    }

    public static string ModeLayerId(HouseFoundationEditorMode mode)
    {
        switch (mode)
        {
            case HouseFoundationEditorMode.Electrical:
            case HouseFoundationEditorMode.Hvac:
            case HouseFoundationEditorMode.Water:
                return "rough_mep";
            case HouseFoundationEditorMode.Lighting:
                return "openings";
            case HouseFoundationEditorMode.Insulation:
                return "insulation";
            case HouseFoundationEditorMode.Yard:
                return "yard";
            default:
                return null;
        }
    }
}

/// <summary>Inspect-brush copy for a construction-plan cell.</summary>
public static class HouseFoundationGridInfo
{
    public static string Describe(
        HouseConstructionPlan plan,
        int x,
        int y,
        int layerIndex,
        int frameIndex,
        HouseFoundationBrushKind brush,
        HouseFoundationEditorMode mode)
    {
        if (plan == null)
            return "No plan.";
        if (x < 0 || y < 0 || x >= plan.width || y >= plan.height)
            return "No cell selected. Use the Select brush, then click a grid square.";

        plan.EnsureDefaultLayers();
        var catalog = plan.wallBrushes;
        layerIndex = Mathf.Clamp(layerIndex, 0, Mathf.Max(0, plan.layers.Count - 1));
        var layer = plan.layers[layerIndex];
        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, plan.frameCount - 1));
        if (layer.frames == null || frameIndex >= layer.frames.Count)
            return $"Cell ({x}, {y}) — layer has no frame {frameIndex}.";

        var frame = layer.frames[frameIndex];
        frame.EnsureSize(plan.width, plan.height);
        byte cellValue = frame.Get(x, y, plan.width);
        bool occupied = cellValue != 0;
        Vector3 world = WallBrushCellStamp.CellWorldCenter(plan, x, y);

        var sb = new StringBuilder(256);
        sb.Append("Cell (").Append(x).Append(", ").Append(y).Append(")  world ")
            .Append(world.x.ToString("0.00")).Append(", ")
            .Append(world.y.ToString("0.00")).Append(", ")
            .Append(world.z.ToString("0.00"));
        sb.Append("\nFloor ").Append(HouseFloorIndex.Format(plan.activeFloorIndex))
            .Append(" (").Append(plan.activeFloorIndex).Append(")  frame ").Append(frameIndex);
        sb.Append("\nLayer ").Append(layer.layerId).Append(" [").Append(layer.kind).Append("]  occupied=")
            .Append(occupied ? "yes" : "no");
        if (occupied)
            sb.Append("  fill ").Append(ColorName(HouseFoundationPalette.ColorForCell(cellValue, catalog)));
        if (occupied && cellValue >= WallBrushSpec.FirstCatalogPaintByte)
        {
            var spec = catalog != null ? catalog.FindByPaintByte(cellValue) : null;
            if (spec != null)
            {
                sb.Append("\nWall brush ").Append(spec.displayName).Append(" [").Append(spec.kind).Append("]");
                if (spec.prefab != null)
                    sb.Append("  prefab ").Append(spec.prefab.name);
            }
        }

        string also = OccupiedLayerIds(plan, x, y, frameIndex, layer.layerId);
        if (!string.IsNullOrEmpty(also))
            sb.Append("\nAlso on: ").Append(also);

        sb.Append("\nBrush ").Append(brush).Append("  mode ").Append(mode)
            .Append(" (").Append(ColorName(HouseFoundationPalette.ModeColor(mode))).Append(")");

        var floor = plan.GetOrCreateFloor(plan.activeFloorIndex);
        if (floor.pixelLightGridW > 0 && floor.pixelLightGridH > 0)
        {
            int px = Mathf.Clamp(Mathf.FloorToInt(x * (float)floor.pixelLightGridW / plan.width), 0, floor.pixelLightGridW - 1);
            int py = Mathf.Clamp(Mathf.FloorToInt(y * (float)floor.pixelLightGridH / plan.height), 0, floor.pixelLightGridH - 1);
            sb.Append("\nPixelLight (").Append(px).Append(", ").Append(py).Append(") of ")
                .Append(floor.pixelLightGridW).Append("×").Append(floor.pixelLightGridH)
                .Append("  cell ").Append(floor.pixelLightCellSize.ToString("0.###")).Append(" m");
        }

        return sb.ToString();
    }

    static string OccupiedLayerIds(HouseConstructionPlan plan, int x, int y, int frameIndex, string skipId)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < plan.layers.Count; i++)
        {
            var layer = plan.layers[i];
            if (layer == null || layer.layerId == skipId || layer.frames == null || frameIndex >= layer.frames.Count)
                continue;
            var frame = layer.frames[frameIndex];
            frame.EnsureSize(plan.width, plan.height);
            if (frame.Get(x, y, plan.width) == 0) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(layer.layerId);
        }
        return sb.ToString();
    }

    public static string ColorName(Color c)
    {
        if (Near(c, HouseFoundationPalette.Red)) return "red";
        if (Near(c, HouseFoundationPalette.Yellow)) return "yellow";
        if (Near(c, HouseFoundationPalette.Blue)) return "blue";
        if (Near(c, HouseFoundationPalette.Green)) return "green";
        if (Near(c, HouseFoundationPalette.Purple)) return "purple";
        if (Near(c, HouseFoundationPalette.Orange)) return "orange";
        return "neutral";
    }

    static bool Near(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;
}
