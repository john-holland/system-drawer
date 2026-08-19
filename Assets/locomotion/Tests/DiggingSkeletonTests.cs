using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class DiggingSkeletonTests
{
    [Test]
    public void DiggingCard_Generate_StopAmbulationFlag()
    {
        var card = DiggingCard.Generate(null, null, stopAmbulation: false);
        Assert.IsFalse(card.stopAmbulation);
        Assert.AreEqual("digging_ambulate", card.sectionName);
        var stop = DiggingCard.Generate(null, null, stopAmbulation: true);
        Assert.IsTrue(stop.stopAmbulation);
    }

    [Test]
    public void TopologicalDigSolver_StepIds()
    {
        var asset = ScriptableObject.CreateInstance<DiggingTopologyAsset>();
        asset.nodes = new List<DiggingTopologyNode>
        {
            new DiggingTopologyNode { nodeId = "a", stopAmbulation = true },
            new DiggingTopologyNode { nodeId = "b", stopAmbulation = false }
        };
        var compiled = TopologicalDigSolver.Compile(asset);
        Assert.AreEqual(2, compiled.stepIds.Count);
        Assert.AreEqual("a_stop", compiled.stepIds[0]);
        Assert.AreEqual("b_ambulate", compiled.stepIds[1]);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void ConsiderDiggingCards_FindsWallVolume()
    {
        var wallGo = new GameObject("wall");
        wallGo.transform.position = Vector3.zero;
        var vol = wallGo.AddComponent<PrisonWallVolume>();
        vol.diggable = true;
        var scannerGo = new GameObject("scan");
        var scan = scannerGo.AddComponent<ConsiderDiggingCards>();
        scan.scanRadius = 8f;
        var cards = scan.Scan(Vector3.zero);
        Assert.Greater(cards.Count, 0);
        Object.DestroyImmediate(wallGo);
        Object.DestroyImmediate(scannerGo);
    }

    [Test]
    public void ScoopCapacity_And_TipMinimum()
    {
        float cap = ScoopCapacityEstimator.Estimate(10f, 2f, 1f);
        Assert.AreEqual(5f, cap, 0.01f);
        Assert.AreEqual(0f, TipMinimumSimulator.RemainingAfterFullTurn(1f), 0.01f);
        var sph = new DigScoopSph();
        sph.SeedFill(8);
        float c = sph.Scoop(null, 1f, 0.2f, 360f);
        Assert.GreaterOrEqual(c, 0f);
        var sub = sph.BuildSubtractNode(Vector3.zero, 0.4f);
        Assert.AreEqual(SdfMax.SdfMaxOp.Subtract, sub.op);
    }

    [Test]
    public void TunnelSupport_Collapse_AddsDoNotPath()
    {
        var go = new GameObject("tunnel");
        var sim = go.AddComponent<TunnelSupportSimulation>();
        sim.availableVolume = 1f;
        sim.FillFromTop(1f);
        Assert.GreaterOrEqual(sim.Dip01, 0.99f);
        sim.Collapse();
        Assert.IsTrue(sim.collapsed);
        Assert.IsNotNull(go.GetComponent<DoNotPathRegion>());
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CityPixelGrid_PrisonLayers_ExportBounds4()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 8;
        grid.height = 8;
        grid.EnsurePrisonLayers();
        Assert.IsTrue(grid.layers.Exists(l => l.kind == CityPixelLayerKind.Cells));
        var cells = grid.layers.Find(l => l.kind == CityPixelLayerKind.Cells);
        cells.frames[0].Set(2, 2, grid.width, 1);
        cells.frames[0].Set(3, 2, grid.width, 1);
        var vols = grid.ExportPrisonClustersToBounds4(0);
        Assert.Greater(vols.Count, 0);
        Object.DestroyImmediate(grid);
    }
}
