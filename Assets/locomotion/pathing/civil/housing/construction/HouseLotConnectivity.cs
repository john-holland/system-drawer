using System.Collections.Generic;
using UnityEngine;

/// <summary>Driveway/garage lots must 4-touch street, sidewalk, or driveway cells.</summary>
public static class HouseLotConnectivity
{
    public static bool CellTouchesKind(CityPixelGrid grid, int frameIndex, int x, int y, CityPixelLayerKind kind)
    {
        if (grid == null) return false;
        grid.EnsureLayersAndFrames();
        var layer = FindLayer(grid, kind);
        if (layer == null || frameIndex < 0 || frameIndex >= layer.frames.Count) return false;
        var frame = layer.frames[frameIndex];
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx < 0 || ny < 0 || nx >= grid.width || ny >= grid.height) continue;
            if (frame.Get(nx, ny, grid.width) != 0) return true;
        }
        return false;
    }

    public static bool DrivewayHasValidOutlet(CityPixelGrid grid, int frameIndex, int x, int y)
    {
        return CellTouchesKind(grid, frameIndex, x, y, CityPixelLayerKind.Roads)
               || CellTouchesKind(grid, frameIndex, x, y, CityPixelLayerKind.Street)
               || CellTouchesKind(grid, frameIndex, x, y, CityPixelLayerKind.Sidewalk);
    }

    public static bool GarageHasValidOutlet(CityPixelGrid grid, int frameIndex, int x, int y)
    {
        return DrivewayHasValidOutlet(grid, frameIndex, x, y)
               || CellTouchesKind(grid, frameIndex, x, y, CityPixelLayerKind.Driveway);
    }

    public static List<string> ValidateHouseLotAdjacency(CityPixelGrid grid, int frameIndex)
    {
        var errors = new List<string>();
        if (grid == null) return errors;
        grid.EnsureLayersAndFrames();
        ValidateLayer(grid, frameIndex, CityPixelLayerKind.Driveway, DrivewayHasValidOutlet, "driveway", errors);
        ValidateLayer(grid, frameIndex, CityPixelLayerKind.Garage, GarageHasValidOutlet, "garage", errors);
        return errors;
    }

    static void ValidateLayer(
        CityPixelGrid grid,
        int frameIndex,
        CityPixelLayerKind kind,
        System.Func<CityPixelGrid, int, int, int, bool> ok,
        string label,
        List<string> errors)
    {
        var layer = FindLayer(grid, kind);
        if (layer == null || frameIndex >= layer.frames.Count) return;
        var frame = layer.frames[frameIndex];
        for (int y = 0; y < grid.height; y++)
        for (int x = 0; x < grid.width; x++)
        {
            if (frame.Get(x, y, grid.width) == 0) continue;
            if (!ok(grid, frameIndex, x, y))
                errors.Add($"{label} at {x},{y} has no valid outlet");
        }
    }

    static CityPixelLayer FindLayer(CityPixelGrid grid, CityPixelLayerKind kind)
    {
        for (int i = 0; i < grid.layers.Count; i++)
            if (grid.layers[i] != null && grid.layers[i].kind == kind)
                return grid.layers[i];
        return null;
    }
}
