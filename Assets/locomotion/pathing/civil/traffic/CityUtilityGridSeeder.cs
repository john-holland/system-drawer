using System.Collections.Generic;
using UnityEngine;

/// <summary>Bakes street water/sewer mains from a painted city grid and attaches house taps.</summary>
public static class CityUtilityGridSeeder
{
    public static void SeedWaterAndSewerFromGrid(
        CityPixelGrid grid,
        WaterGraph water,
        SewerGraph sewer,
        IList<HousingBuildingRagdoll> houses,
        int frameIndex = 0)
    {
        if (grid == null) return;
        grid.EnsureHouseLayers();
        var runs = StreetNamePeckingOrder.NameStreetRuns(grid, frameIndex);
        runs.Sort((a, b) => a.peckingOrder.CompareTo(b.peckingOrder));
        for (int r = 0; r < runs.Count; r++)
        {
            var run = runs[r];
            if (run?.cells == null || run.cells.Count == 0) continue;
            string prev = null;
            for (int i = 0; i < run.cells.Count; i++)
            {
                var c = run.cells[i];
                Vector3 world = grid.CellToWorld(c.x, c.y);
                string id = "st_" + run.peckingOrder + "_" + c.x + "_" + c.y;
                if (water != null)
                {
                    water.AddOrGet(id, world, streetMain: true);
                    if (prev != null)
                        water.Connect(prev, id);
                }
                if (sewer != null)
                {
                    sewer.nodes.Add(new SewerNode
                    {
                        nodeId = "sw_" + id,
                        worldPosition = world,
                        isStreetDrain = true
                    });
                }
                prev = id;
            }
        }

        if (houses == null) return;
        for (int h = 0; h < houses.Count; h++)
        {
            var house = houses[h];
            if (house == null) continue;
            Vector3 tapWorld = HouseServiceTapResolver.ResolveWorld(grid, frameIndex, house.slots);
            var tap = house.GetComponent<HouseUtilityTap>() ?? house.gameObject.AddComponent<HouseUtilityTap>();
            tap.tapWorld = tapWorld;
            if (water != null)
                tap.waterNode = water.AddOrGetBuildingTap(house.gameObject, tapWorld);
            var sewerTap = house.GetComponent<SewerBuildingTap>() ?? house.gameObject.AddComponent<SewerBuildingTap>();
            if (sewer != null)
            {
                sewerTap.graph = sewer;
                tap.sewerNode = sewer.AddOrGetBuildingNode(house.gameObject);
                if (tap.sewerNode != null)
                    tap.sewerNode.worldPosition = tapWorld;
                sewer.EnsureFullyConnectedToPlant();
            }
            if (water != null && tap.waterNode != null && water.nodes.Count > 0)
            {
                string nearest = NearestStreet(water, tapWorld);
                if (nearest != null)
                    water.Connect(nearest, tap.waterNode.nodeId);
            }
        }
    }

    static string NearestStreet(WaterGraph water, Vector3 world)
    {
        string best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < water.nodes.Count; i++)
        {
            var n = water.nodes[i];
            if (n == null || !n.isStreetMain) continue;
            float d = (n.worldPosition - world).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = n.nodeId;
            }
        }
        return best;
    }
}
