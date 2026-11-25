using System.Collections.Generic;
using NUnit.Framework;
using SdfMax;
using UnityEngine;

public sealed class SnowSystemTests
{
    readonly List<Object> _owned = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _owned.Count - 1; i >= 0; i--)
        {
            if (_owned[i] != null)
                Object.DestroyImmediate(_owned[i]);
        }
        _owned.Clear();
    }

    GameObject Own(string name)
    {
        var go = new GameObject(name);
        _owned.Add(go);
        return go;
    }

    T OwnAsset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        _owned.Add(asset);
        return asset;
    }

    DiggableVolume SolidBox(string name)
    {
        GameObject go = Own(name);
        var volume = go.AddComponent<DiggableVolume>();
        volume.localBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        volume.integralGranularity = 1f;
        volume.integralLimit = 1f;
        volume.integralSize = 4f;
        volume.sdf = OwnAsset<SdfMaxCompositionAsset>();
        volume.sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = Vector3.one
        });
        volume.sdf.rootNodeIndex = 0;
        return volume;
    }

    [Test]
    public void GaitStamp_ReusesBin_AndToolRunsFirst()
    {
        var cache = new SnowRadialCache();
        var gait = OwnAsset<SnowPolarGait>();
        gait.gaitId = "run";
        gait.frames = new[] { new SnowPolarFrame { radius = 0.2f, depth = 0.1f } };
        int first = cache.Store(gait);
        int second = cache.Store(gait);
        Assert.AreEqual(first, second);
        Assert.AreEqual("run", cache.bins[first]);

        GameObject host = Own("stamp");
        var volume = host.AddComponent<SnowVolume>();
        volume.integralGranularity = 1f;
        volume.integralLimit = 8f;
        volume.integralSize = 4f;
        volume.sdf = OwnAsset<SdfMaxCompositionAsset>();
        volume.sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = Vector3.one
        });
        volume.sdf.rootNodeIndex = 0;
        var tool = host.AddComponent<SdfSealedClothSkin>();
        tool.fragments.Add(new SdfSealedClothFragment { id = "shovel", centroid = Vector3.zero, curve = SdfSealedClothSkin.CurvedSection(Vector3.zero, Vector3.forward, 0.1f) });
        var log = new List<string>();
        SnowStampSequence.Apply(volume, tool, cache, gait, Vector3.zero, log);
        Assert.AreEqual("tool", log[0]);
        Assert.AreEqual("gait", log[1]);
        Assert.AreEqual(first, cache.lastBin);
    }

    [Test]
    public void PileLip_RisesThenFallsLinearly()
    {
        float a = SnowPile.LipHeight(0f, 1f);
        float b = SnowPile.LipHeight(0.2f, 1f);
        float knee = SnowPile.LipHeight(0.35f, 1f);
        float mid = SnowPile.LipHeight(0.675f, 1f);
        float end = SnowPile.LipHeight(1f, 1f);
        Assert.Less(a, 0.05f);
        Assert.Greater(b, a);
        Assert.AreEqual((knee + end) * 0.5f, mid, 0.02f);
        Assert.Less(end, 0.05f);
    }

    [Test]
    public void Contact_FirmnessShrinks_StickinessGrows_MassConserved()
    {
        float loose = SnowContactBox.ExtentFor(1f, 0f, 0f);
        float firm = SnowContactBox.ExtentFor(1f, 1f, 0f);
        float sticky = SnowContactBox.ExtentFor(1f, 0f, 1f);
        Assert.Less(firm, loose);
        Assert.Greater(sticky, loose);
        SnowContactBox.SplitMass(4f, 0.5f, 0.25f, 0.8f, 0.5f, out float stay, out float moved);
        Assert.AreEqual(4f, stay + moved, 1e-4f);
    }

    [Test]
    public void Snowfall_StaysOpenUntilEverySurfaceIsCleared()
    {
        var fall = SnowFallEvent.Open("storm", new[] { "roof", "path", "bench" }, new Locomotion.Narrative.NarrativeDateTime(2026, 1, 2, 8, 0, 0));
        Assert.IsTrue(fall.IsOpen);
        fall.Resolve("roof", SnowClearKind.Melt);
        fall.Resolve("path", SnowClearKind.Brush);
        Assert.IsTrue(fall.IsOpen);
        fall.Resolve("bench", SnowClearKind.Blow);
        Assert.IsFalse(fall.IsOpen);
    }

    [Test]
    public void IceStress_HeavyBreaks_LightStays()
    {
        var shell = new IceShellState { breakStress = 0.5f, layers = 1 };
        shell.sections.Add(new IcePlaneSection { pixels = new List<SnowQuadPixel> { new SnowQuadPixel() } });
        Assert.IsFalse(shell.ApplyStress(0.2f));
        Assert.AreEqual(1, shell.layers);
        Assert.IsTrue(shell.ApplyStress(0.8f));
        Assert.AreEqual(2, shell.layers);
        Assert.IsTrue(shell.sections[0].pixels[0].shattered);
    }

    [Test]
    public void PollenMix_ZeroLeavesSnowUnmixed()
    {
        Color snow = Color.white;
        Color pollen = Color.yellow;
        Assert.AreEqual(snow, SnowAlbedo.Mix(snow, pollen, 0f));
        Assert.AreNotEqual(snow, SnowAlbedo.Mix(snow, pollen, 1f));
    }

    [Test]
    public void Curvature_PowersAndFlow()
    {
        Assert.AreEqual(0.02f, IceSectionCurvature.Kappa(new[] { 0.02f }, 3f), 1e-4f);
        Assert.AreEqual(0.02f + 0.04f, IceSectionCurvature.Kappa(new[] { 0.02f, 0f, 0.04f }, 1f), 1e-4f);
        Assert.Less(IceSectionCurvature.DegreeCount(1f), IceSectionCurvature.DegreeCount(8f));
        float angular = 0.5f;
        float kappa = IceSectionCurvature.Kappa(new[] { 0.2f }, 1f);
        Assert.AreEqual(angular, IceSectionCurvature.Rendered(angular, 0f, new[] { 0.2f }, 1f), 1e-4f);
        Assert.AreEqual(angular + kappa, IceSectionCurvature.Rendered(angular, 1f, new[] { 0.2f }, 1f), 1e-4f);
    }

    [Test]
    public void TreeCompute_AveragesUntilReset()
    {
        var host = Own("tree");
        var snow = host.AddComponent<TreeSnowManager>();
        float first = snow.Compute(new[] { 1f, 3f });
        Assert.AreEqual(2f, first, 1e-4f);
        Assert.AreEqual(2, snow.features.Count);
        Assert.IsNotNull(snow.features[0].manager);
        float second = snow.Compute(new[] { 3f, 3f });
        Assert.AreEqual(2.5f, second, 1e-4f);
        snow.ResetBake();
        Assert.IsFalse(snow.hasBake);
        Assert.AreEqual(0, snow.features.Count);
    }

    [Test]
    public void Integral_ClearAndFill_StopAtLimit()
    {
        DiggableVolume dug = SolidBox("dig");
        int scooped = dug.ApplyScoop(null, Vector3.zero, 0.6f);
        Assert.AreEqual(1, scooped);
        Assert.AreEqual(1f, dug.clearedIntegral, 1e-3f);
        Assert.LessOrEqual(dug.clearedIntegral, dug.integralLimit + 1e-4f);

        var fillGo = Own("fill");
        var fill = fillGo.AddComponent<DiggableVolume>();
        fill.integralGranularity = 1f;
        fill.integralLimit = 1f;
        fill.integralSize = 4f;
        fill.sdf = OwnAsset<SdfMaxCompositionAsset>();
        fill.sdf.nodes.Add(new SdfMaxNode { op = SdfMaxOp.Constant, constantValue = 1f });
        fill.sdf.rootNodeIndex = 0;
        Assert.AreEqual(1, fill.ApplyFill(Vector3.zero, 0.6f));
        Assert.AreEqual(1f, fill.filledIntegral, 1e-3f);
    }

    [Test]
    public void Integral_OctreeLeafInsideBecomesWalkable()
    {
        var root = new Bounds(Vector3.zero, Vector3.one * 4f);
        HierarchicalPathingOctTree tree = HierarchicalPathingOctTree.Build(root, 2, 1f, _ => true);
        var cave = new Bounds(Vector3.zero, Vector3.one * 1.2f);
        tree.ResampleInside(cave, 1f, 2, center => !cave.Contains(center));
        bool sawInside = false;
        bool sawOutside = false;
        for (int i = 0; i < tree.Leaves.Count; i++)
        {
            HierarchicalPathingOctTree.Leaf leaf = tree.Leaves[i];
            if (cave.Contains(leaf.Center))
            {
                Assert.IsFalse(leaf.blocked);
                sawInside = true;
            }
            else if (!cave.Intersects(leaf.bounds))
            {
                Assert.IsTrue(leaf.blocked);
                sawOutside = true;
            }
        }
        Assert.IsTrue(sawInside);
        Assert.IsTrue(sawOutside);
    }

    [Test]
    public void Integral_TreesDropEntriesInsideTheClear()
    {
        var world = new Bounds(Vector3.zero, Vector3.one * 30f);
        var oct = new SGOctTree(world, 8, 4, 0.1f);
        GameObject inside = Own("inside");
        inside.transform.position = Vector3.zero;
        oct.Insert(new Bounds(Vector3.zero, Vector3.one * 0.2f), inside, null);
        GameObject outside = Own("outside");
        outside.transform.position = new Vector3(8f, 0f, 0f);
        oct.Insert(new Bounds(new Vector3(8f, 0f, 0f), Vector3.one * 0.2f), outside, null);
        int removed = oct.RemoveInside(new Bounds(Vector3.zero, Vector3.one * 2f));
        Assert.AreEqual(1, removed);
        Assert.AreEqual(1, oct.Search(world).Count);

        var quad = new SGQuadTree(world, 8, 4, 0.1f);
        GameObject qIn = Own("qin");
        qIn.transform.position = Vector3.zero;
        quad.Insert(new Bounds(Vector3.zero, Vector3.one), qIn, null);
        GameObject qOut = Own("qout");
        qOut.transform.position = new Vector3(8f, 0f, 0f);
        quad.Insert(new Bounds(new Vector3(8f, 0f, 0f), Vector3.one), qOut, null);
        Assert.AreEqual(1, quad.RemoveInside(new Bounds(Vector3.zero, Vector3.one * 3f)));
        Assert.AreEqual(1, quad.Search(world).Count);
    }

    [Test]
    public void IceBand_StaysSolidAboveTheCave()
    {
        DiggableVolume dug = SolidBox("band");
        dug.iceBandThickness = 0.25f;
        dug.integralLimit = 8f;
        dug.ApplyScoop(null, Vector3.zero, 0.6f);
        Assert.IsTrue(dug.ContainsSolid(new Vector3(0f, 0.9f, 0f)));
        Assert.Greater(dug.clearedIntegral, 0f);
    }
}
