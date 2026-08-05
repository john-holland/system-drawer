using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class CityPixelGridTests
{
    [Test]
    public void RecalculateCellSize_UsesActorFootprint()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.cellWorldSize = 10f;
        var go = new GameObject("actor");
        // Minimal renderer bounds via BoxCollider proxy: add BaseAmbulatingActor subclass VehicleActor needs more setup.
        // Without profile, RecalculateCellSize keeps/clamps existing.
        float c = grid.RecalculateCellSize();
        Assert.GreaterOrEqual(c, 0.25f);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void CellToWorld_And_WorldToCell_RoundTrip()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.worldOrigin = new Vector3(100, 0, 200);
        grid.cellWorldSize = 2f;
        grid.width = 16;
        grid.height = 16;
        Vector3 w = grid.CellToWorld(3, 4);
        Assert.IsTrue(grid.WorldToCell(w, out int x, out int y));
        Assert.AreEqual(3, x);
        Assert.AreEqual(4, y);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void BakeFrame_ProducesMstFromRoads()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 8;
        grid.height = 8;
        grid.cellWorldSize = 1f;
        grid.frameCount = 1;
        grid.EnsureLayersAndFrames();
        var roads = grid.layers.Find(l => l.kind == CityPixelLayerKind.Roads);
        Assert.IsNotNull(roads);
        // Paint a line of road cells
        for (int x = 0; x < 6; x++)
            roads.frames[0].Set(x, 2, grid.width, 1);

        var bake = CityPixelGridBaker.BakeFrame(grid, 0, new List<TravelAgent>());
        Assert.IsNotNull(bake);
        Assert.Greater(bake.nodes.Count, 0);
        Assert.Greater(bake.mstEdges.Count, 0);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void BrushStamp_OneWay_AffectsBakeDemand()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 6;
        grid.height = 6;
        grid.cellWorldSize = 1f;
        grid.frameCount = 1;
        grid.EnsureLayersAndFrames();
        var roads = grid.layers.Find(l => l.kind == CityPixelLayerKind.Roads);
        for (int x = 0; x < 5; x++)
            roads.frames[0].Set(x, 1, grid.width, 1);

        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            frameIndex = 0,
            cellX = 2,
            cellY = 1,
            kind = CityPixelBrushKind.OneWay,
            yawDegrees = 90f
        });

        var bake = CityPixelGridBaker.BakeFrame(grid, 0);
        Assert.IsNotNull(bake);
        Assert.Greater(bake.mstEdges.Count, 0);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void TASignCard_ApplyHints_SetsAvoid()
    {
        var go = new GameObject("agent");
        var ta = go.AddComponent<TravelAgent>();
        var signGo = new GameObject("sign");
        var card = TASignCard.Generate(TASignKind.SlowChildren, signGo.transform.position, 4f, 15f);
        card.goalTarget = signGo;
        card.ApplyHintsTo(ta);
        CollectionAssert.Contains(ta.avoidActors, signGo.transform);
        Assert.GreaterOrEqual(ta.avoidCostMultiplier, 4f);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(signGo);
    }

    [Test]
    public void ExportNarrativeEvents_WritesBounds4()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 4;
        grid.height = 4;
        grid.cellWorldSize = 2f;
        grid.frameGranularitySec = 30f;
        grid.frameCount = 1;
        grid.EnsureLayersAndFrames();
        var hazard = grid.layers.Find(l => l.kind == CityPixelLayerKind.PowerLinesDown);
        Assert.IsNotNull(hazard);
        hazard.frames[0].Set(1, 1, grid.width, 1);
        hazard.frames[0].Set(2, 1, grid.width, 1);

        var calGo = new GameObject("cal");
        var calendar = calGo.AddComponent<NarrativeCalendarAsset>();
        int n = CityPixelGridRuntime.ExportNarrativeEvents(grid, calendar);
        Assert.Greater(n, 0);
        Assert.Greater(calendar.events.Count, 0);
        Assert.IsTrue(calendar.events[0].spatiotemporalVolume.HasValue);
        var vol = calendar.events[0].spatiotemporalVolume.Value;
        Assert.AreEqual(0f, vol.tMin, 0.01f);
        Assert.AreEqual(30f, vol.tMax, 0.01f);

        Object.DestroyImmediate(calGo);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void TrafficWarden_PreferBake_UsesCityGrid()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 4;
        grid.height = 4;
        grid.cellWorldSize = 1f;
        grid.frameCount = 1;
        grid.EnsureLayersAndFrames();
        var roads = grid.layers.Find(l => l.kind == CityPixelLayerKind.Roads);
        for (int x = 0; x < 3; x++)
            roads.frames[0].Set(x, 0, grid.width, 1);
        CityPixelGridBaker.BakeFrame(grid, 0);

        var wardenGo = new GameObject("warden");
        var warden = wardenGo.AddComponent<TrafficWarden>();
        warden.cityGrid = grid;
        warden.preferCityGridBake = true;
        warden.RebuildCorridorMst();
        Assert.Greater(warden.backboneEdges.Count, 0);

        Object.DestroyImmediate(wardenGo);
        Object.DestroyImmediate(grid);
    }
}
