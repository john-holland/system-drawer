using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StreetNameRun
{
    public string name;
    public int peckingOrder;
    public List<Vector2Int> cells = new List<Vector2Int>();
}

/// <summary>Names contiguous street MST runs. Lower pecking wins on merges.</summary>
public static class StreetNamePeckingOrder
{
    public static StreetNameRun Winner(StreetNameRun a, StreetNameRun b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return a.peckingOrder <= b.peckingOrder ? a : b;
    }

    public static List<StreetNameRun> NameStreetRuns(CityPixelGrid grid, int frameIndex)
    {
        var runs = new List<StreetNameRun>();
        if (grid == null) return runs;
        grid.EnsureHouseLayers();
        var street = HouseLotConnectivity.FindLayer(grid, CityPixelLayerKind.Street);
        if (street == null && (street = HouseLotConnectivity.FindLayer(grid, CityPixelLayerKind.Roads)) == null)
            return runs;
        if (frameIndex < 0 || frameIndex >= street.frames.Count)
            return runs;
        var frame = street.frames[frameIndex];
        var seen = new bool[grid.width * grid.height];
        int peck = 0;
        for (int y = 0; y < grid.height; y++)
        for (int x = 0; x < grid.width; x++)
        {
            int i = x + y * grid.width;
            if (seen[i] || frame.Get(x, y, grid.width) == 0) continue;
            var run = new StreetNameRun
            {
                name = "street-" + peck,
                peckingOrder = peck
            };
            Flood(grid, frame, x, y, seen, run.cells);
            runs.Add(run);
            peck++;
        }
        return runs;
    }

    static void Flood(CityPixelGrid grid, CityPixelFrame frame, int sx, int sy, bool[] seen, List<Vector2Int> cells)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(sx, sy));
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p.x < 0 || p.y < 0 || p.x >= grid.width || p.y >= grid.height) continue;
            int i = p.x + p.y * grid.width;
            if (seen[i] || frame.Get(p.x, p.y, grid.width) == 0) continue;
            seen[i] = true;
            cells.Add(p);
            for (int k = 0; k < 4; k++)
                stack.Push(new Vector2Int(p.x + dx[k], p.y + dy[k]));
        }
    }
}
