using System.Collections.Generic;
using UnityEngine;

/// <summary>Bakes MST/A* corridor caches into CityPixelGrid using TrafficCorridorGraph + TrafficMstBuilder.</summary>
public static class CityPixelGridBaker
{
    public static CityPixelBakedCacheLayer BakeFrame(
        CityPixelGrid grid,
        int frameIndex,
        IEnumerable<TravelAgent> agents = null)
    {
        if (grid == null) return null;
        grid.EnsureLayersAndFrames();
        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, grid.frameCount - 1));

        var graph = new TrafficCorridorGraph { cellSize = Mathf.Max(0.25f, grid.cellWorldSize) };

        // Seed roads as walkable corridor demand.
        SeedRoads(grid, frameIndex, graph);
        // Block / detour / one-way reverse from stamps + TrafficBlock layer.
        ApplyBlocksAndStamps(grid, frameIndex, graph);

        if (agents != null)
            graph.IngestTravelAgentPlans(agents, driveLegsPreferred: true);
        else
            graph.IngestTravelAgentPlans(TravelAgentRegistry.All, driveLegsPreferred: true);

        var mst = TrafficMstBuilder.Build(graph);
        var bake = new CityPixelBakedCacheLayer
        {
            frameIndex = frameIndex,
            corridorCellMarks = new byte[grid.CellCount]
        };

        foreach (var kv in graph.nodes)
        {
            var n = kv.Value;
            if (!grid.WorldToCell(n.world, out int cx, out int cy))
            {
                // Snap node world may still map via origin.
                cx = Mathf.Clamp(Mathf.RoundToInt((n.world.x - grid.worldOrigin.x) / graph.cellSize), 0, grid.width - 1);
                cy = Mathf.Clamp(Mathf.RoundToInt((n.world.z - grid.worldOrigin.z) / graph.cellSize), 0, grid.height - 1);
            }
            bake.nodes.Add(new CityPixelBakedNode
            {
                cellX = cx,
                cellY = cy,
                world = n.world,
                corridorId = n.id
            });
            int idx = cx + cy * grid.width;
            if (idx >= 0 && idx < bake.corridorCellMarks.Length)
                bake.corridorCellMarks[idx] = 1;
        }

        for (int i = 0; i < mst.Count; i++)
        {
            var e = mst[i];
            bake.mstEdges.Add(new CityPixelBakedMstEdge
            {
                a = e.a,
                b = e.b,
                length = e.length,
                demand = e.demand
            });
        }

        grid.UpsertBake(bake);
        return bake;
    }

    public static void BakeAllFrames(CityPixelGrid grid, IEnumerable<TravelAgent> agents = null)
    {
        if (grid == null) return;
        grid.EnsureLayersAndFrames();
        for (int f = 0; f < grid.frameCount; f++)
            BakeFrame(grid, f, agents);
    }

    static void SeedRoads(CityPixelGrid grid, int frameIndex, TrafficCorridorGraph graph)
    {
        for (int li = 0; li < grid.layers.Count; li++)
        {
            var layer = grid.layers[li];
            if (layer == null || layer.kind != CityPixelLayerKind.Roads) continue;
            if (frameIndex >= layer.frames.Count) continue;
            var frame = layer.frames[frameIndex];
            for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                if (frame.Get(x, y, grid.width) == 0) continue;
                Vector3 w = grid.CellToWorld(x, y);
                graph.EnsureNode(w);
                // Neighbor connectivity for road cells.
                TryLink(grid, graph, x, y, x + 1, y, frame);
                TryLink(grid, graph, x, y, x, y + 1, frame);
            }
        }
    }

    static void TryLink(CityPixelGrid grid, TrafficCorridorGraph graph, int x0, int y0, int x1, int y1, CityPixelFrame frame)
    {
        if (x1 < 0 || y1 < 0 || x1 >= grid.width || y1 >= grid.height) return;
        if (frame.Get(x1, y1, grid.width) == 0) return;
        graph.AddPathDemand(new List<Vector3> { grid.CellToWorld(x0, y0), grid.CellToWorld(x1, y1) }, 1f);
    }

    static void ApplyBlocksAndStamps(CityPixelGrid grid, int frameIndex, TrafficCorridorGraph graph)
    {
        // Remove edges that touch TrafficBlock / PowerLinesDown / Flood / Protest / Construction cells.
        var blocked = new HashSet<long>();
        for (int li = 0; li < grid.layers.Count; li++)
        {
            var layer = grid.layers[li];
            if (layer == null) continue;
            if (layer.kind == CityPixelLayerKind.Roads || layer.kind == CityPixelLayerKind.Custom) continue;
            if (frameIndex >= layer.frames.Count) continue;
            var frame = layer.frames[frameIndex];
            for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                if (frame.Get(x, y, grid.width) == 0) continue;
                blocked.Add(graph.SnapId(grid.CellToWorld(x, y)));
            }
        }

        if (grid.brushStamps != null)
        {
            for (int i = 0; i < grid.brushStamps.Count; i++)
            {
                var s = grid.brushStamps[i];
                if (s.frameIndex != frameIndex) continue;
                if (s.kind == CityPixelBrushKind.Detour || s.kind == CityPixelBrushKind.StopSign)
                    blocked.Add(graph.SnapId(grid.CellToWorld(s.cellX, s.cellY)));
                if (s.kind == CityPixelBrushKind.OneWay)
                {
                    // Add directed preference as extra demand along heading; reverse remains weaker via no reverse edge add.
                    Vector3 from = grid.CellToWorld(s.cellX, s.cellY);
                    Vector3 dir = Quaternion.Euler(0f, s.yawDegrees, 0f) * Vector3.forward * grid.cellWorldSize;
                    graph.AddPathDemand(new List<Vector3> { from, from + dir }, 2f);
                }
            }
        }

        if (blocked.Count == 0) return;
        var kept = new List<TrafficCorridorEdge>();
        for (int i = 0; i < graph.edges.Count; i++)
        {
            var e = graph.edges[i];
            if (!blocked.Contains(e.a) && !blocked.Contains(e.b))
                kept.Add(e);
        }
        graph.edges.Clear();
        graph.edges.AddRange(kept);
    }

    /// <summary>Apply baked MST edges onto a live TrafficCorridorGraph / backbone list.</summary>
    public static void ApplyBakeToWarden(CityPixelGrid grid, int frameIndex, TrafficWarden warden)
    {
        if (grid == null || warden == null) return;
        var bake = grid.FindBake(frameIndex);
        if (bake == null || bake.mstEdges == null || bake.mstEdges.Count == 0) return;

        warden.corridorGraph.Clear();
        warden.corridorGraph.cellSize = Mathf.Max(0.25f, grid.cellWorldSize);
        for (int i = 0; i < bake.nodes.Count; i++)
        {
            var n = bake.nodes[i];
            warden.corridorGraph.EnsureNode(n.world);
        }

        warden.backboneEdges = new List<TrafficCorridorEdge>();
        for (int i = 0; i < bake.mstEdges.Count; i++)
        {
            var e = bake.mstEdges[i];
            warden.backboneEdges.Add(new TrafficCorridorEdge
            {
                a = e.a,
                b = e.b,
                length = e.length,
                demand = e.demand
            });
            // Re-seed graph edges for demand tracking.
            if (warden.corridorGraph.nodes.TryGetValue(e.a, out var na) &&
                warden.corridorGraph.nodes.TryGetValue(e.b, out var nb))
            {
                warden.corridorGraph.AddPathDemand(new List<Vector3> { na.world, nb.world }, e.demand);
            }
        }
    }
}
