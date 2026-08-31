using UnityEngine;

/// <summary>
/// Default house service tap: driveway or HouseFront that 4-touches Street / Sidewalk / Roads.
/// Fallback: front-walk endpoint, then egressMain.
/// </summary>
public static class HouseServiceTapResolver
{
    public static bool TryResolveCell(CityPixelGrid grid, int frameIndex, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (grid == null) return false;
        grid.EnsureHouseLayers();
        if (FindTouching(grid, frameIndex, CityPixelLayerKind.Driveway, out x, out y))
            return true;
        if (FindTouching(grid, frameIndex, CityPixelLayerKind.HouseFront, out x, out y))
            return true;
        if (FindTouching(grid, frameIndex, CityPixelLayerKind.Doors, out x, out y))
            return true;
        return false;
    }

    public static Vector3 ResolveWorld(CityPixelGrid grid, int frameIndex, HouseReferenceSlots slots)
    {
        if (TryResolveCell(grid, frameIndex, out int x, out int y))
            return grid.CellToWorld(x, y);
        if (slots != null && slots.frontWalk != null)
            return slots.frontWalk.position;
        if (slots != null && slots.egressMain != null)
            return slots.egressMain.position;
        return grid != null ? grid.worldOrigin : Vector3.zero;
    }

    static bool FindTouching(CityPixelGrid grid, int frameIndex, CityPixelLayerKind kind, out int x, out int y)
    {
        x = 0;
        y = 0;
        var layer = HouseLotConnectivity.FindLayer(grid, kind);
        if (layer == null || frameIndex < 0 || frameIndex >= layer.frames.Count)
            return false;
        var frame = layer.frames[frameIndex];
        for (int yy = 0; yy < grid.height; yy++)
        for (int xx = 0; xx < grid.width; xx++)
        {
            if (frame.Get(xx, yy, grid.width) == 0) continue;
            if (HouseLotConnectivity.CellTouchesKind(grid, frameIndex, xx, yy, CityPixelLayerKind.Street)
                || HouseLotConnectivity.CellTouchesKind(grid, frameIndex, xx, yy, CityPixelLayerKind.Sidewalk)
                || HouseLotConnectivity.CellTouchesKind(grid, frameIndex, xx, yy, CityPixelLayerKind.Roads))
            {
                x = xx;
                y = yy;
                return true;
            }
        }
        return false;
    }
}
