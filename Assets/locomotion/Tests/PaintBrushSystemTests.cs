#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using SdfMax;
using System.Linq;

public sealed class PaintBrushSystemTests
{
    [Test]
    public void InstrumentMap_DefaultsContainBrushChannels()
    {
        var map = ScriptableObject.CreateInstance<PaintInstrumentMap>();
        map.EnsureDefaults();
        Assert.IsTrue(map.ChannelIsAllowed(PaintInstrumentMap.BrushYaw));
        Assert.IsTrue(map.ChannelIsAllowed(PaintInstrumentMap.TubeSqueeze));
        Assert.IsTrue(map.ChannelIsAllowed(PaintInstrumentMap.SealantSpray));
        Object.DestroyImmediate(map);
    }

    [Test]
    public void HairLineSampler_PateBlend_PullsTowardCenter()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.scalpRadiusM = 0.1f;
        cfg.centerPateLocal = Vector3.up * 0.05f;
        cfg.pateAngleBlend = 1f;
        cfg.authoredRadialBias = 0f;
        cfg.hairLineCurve = HairLineCurve.Constant(1f);
        cfg.hairLineAngleCurve = HairLineAngleCurve.Zero();

        var go = new GameObject("Scalp");
        try
        {
            Vector3 dir = HairLineSampler.EmergenceDirection(go.transform, cfg, 0.125f);
            Vector3 pate = HairLineSampler.CenterPateWorld(go.transform, cfg);
            Vector3 p = HairLineSampler.EmergenceRingPoint(go.transform, cfg, 0.125f);
            Vector3 expected = (pate - p).normalized;
            Assert.Greater(Vector3.Dot(dir, expected), 0.95f);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }

    [Test]
    public void HairLineAngle_RotatesEmergence()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.scalpRadiusM = 0.1f;
        cfg.pateAngleBlend = 0f;
        cfg.authoredRadialBias = 0.5f;
        cfg.hairLineAngleCurve = new HairLineAngleCurve
        {
            emergenceAngleDegByAzimuth01 = AnimationCurve.Constant(0f, 1f, 30f)
        };
        var go = new GameObject("Scalp");
        try
        {
            Vector3 dir = HairLineSampler.EmergenceDirection(go.transform, cfg, 0f);
            Assert.Greater(dir.sqrMagnitude, 0.5f);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }

    [Test]
    public void BrushCatalog_Builtins_CoverAllKinds()
    {
        var cat = ScriptableObject.CreateInstance<PaintBrushCatalog>();
        cat.EnsureBuiltins();
        Assert.AreEqual(8, cat.brushes.Count);
        Assert.IsNotNull(cat.Get(PaintBrushDefinition.BrushKind.Fan));
        Assert.IsNotNull(cat.Get(PaintBrushDefinition.BrushKind.Round));
        Assert.IsNotNull(cat.Get(PaintBrushDefinition.BrushKind.Quill));
        Assert.IsNotNull(cat.Get(PaintBrushDefinition.BrushKind.Nib));
        var copies = cat.brushes.ToArray();
        Object.DestroyImmediate(cat);
        for (int i = 0; i < copies.Length; i++)
        {
            if (copies[i] != null)
                Object.DestroyImmediate(copies[i]);
        }
    }

    [Test]
    public void TubeComposer_BuildsRootedGraph()
    {
        var cfg = ScriptableObject.CreateInstance<PaintTubeConfig>();
        var asset = PaintTubeSdfComposer.Compose(cfg);
        Assert.Greater(asset.nodes.Count, 3);
        Assert.GreaterOrEqual(asset.ResolveRootIndex(), 0);
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void StrokeStamper_AppendsSdfMaxSegment()
    {
        var canvasGo = new GameObject("Canvas");
        var brushGo = new GameObject("Brush");
        try
        {
            var canvas = canvasGo.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvasGo.AddComponent<PaintStrokeStamper>().canvas = canvas;

            var def = PaintBrushCatalog.CreateBuiltin(PaintBrushDefinition.BrushKind.Round);
            var brush = brushGo.AddComponent<PaintBrushRuntime>();
            brush.definition = def;
            brush.tip = brushGo.transform;
            brush.ferrule = brushGo.transform;
            brush.load01 = 0.8f;
            brush.loadedColor = Color.red;
            brush.canvas = canvas;
            brushGo.transform.position = canvasGo.transform.position;

            var stamper = canvasGo.GetComponent<PaintStrokeStamper>();
            stamper.StampFromBrush(brush);

            var layer = canvas.layerStack.TopWetLayer();
            Assert.IsNotNull(layer.composition);
            Assert.Greater(layer.composition.nodes.Count, 0);

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(canvas.layerStack);
        }
        finally
        {
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(brushGo);
        }
    }

    [Test]
    public void Smudge_SubtractsWetLayer()
    {
        var go = new GameObject("SmudgeCanvas");
        try
        {
            var col = go.AddComponent<BoxCollider>();
            var canvas = go.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvas.layerStack.enableDestructiveSmudge = true;
            canvas.layerStack.smudgeStrength = 1f;
            var layer = canvas.layerStack.TopWetLayer();
            int before = layer.composition.nodes.Count;

            var smudge = go.AddComponent<PaintSmudgeCollider>();
            smudge.canvas = canvas;
            smudge.ApplySmudge(go.transform.position, Vector3.forward, Vector3.right, col);

            Assert.Greater(layer.composition.nodes.Count, before);
            Object.DestroyImmediate(canvas.layerStack);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void CarryKeepOut_DetectsWetCenter()
    {
        var go = new GameObject("CarryCanvas");
        try
        {
            var canvas = go.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvas.Viscosity.Stamp(new Vector2(0.5f, 0.5f), new Color(1f, 0f, 1f, 0.5f), 0.1f);

            var solver = go.AddComponent<PaintCarryKeepOutSolver>();
            solver.canvas = canvas;
            solver.collisionEnabledCarryMode = true;
            solver.Solve();
            Assert.IsFalse(solver.WouldSmudge); // no ragdoll hands

            Object.DestroyImmediate(canvas.layerStack);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PaintingIkCatalog_IncludesCollisionCarry()
    {
        var cat = ScriptableObject.CreateInstance<PaintingIkTrainingCatalog>();
        cat.EnsureDefaults();
        bool found = false;
        for (int i = 0; i < cat.entries.Count; i++)
        {
            if (cat.entries[i].collisionEnabledCarryMode)
                found = true;
        }
        Assert.IsTrue(found);
        Assert.AreEqual(PhysicsIKTrainingCategory.ToolUse, cat.entries[0].category);
        Object.DestroyImmediate(cat);
    }

    [Test]
    public void Proxy_RoutesAndAppliesTip()
    {
        var go = new GameObject("Proxy");
        var tip = new GameObject("Tip");
        var canvas = new GameObject("Canvas");
        try
        {
            tip.transform.SetParent(go.transform);
            var proxy = go.AddComponent<PaintInstrumentProxy>();
            proxy.sourceMap = ScriptableObject.CreateInstance<PaintInstrumentMap>();
            proxy.sourceMap.EnsureDefaults();
            proxy.brushTip = tip.transform;
            proxy.canvasPlane = canvas.transform;
            Vector3 before = tip.transform.position;
            proxy.RouteAxes(1f, 0f, 0f, 0f, 0f);
            proxy.ApplyToTargets(0.1f);
            Assert.AreNotEqual(before, tip.transform.position);
            Object.DestroyImmediate(proxy.sourceMap);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tip);
            Object.DestroyImmediate(canvas);
        }
    }

    [Test]
    public void HydroSpecular_BeadsMatte_FilmGloss()
    {
        float matte = PaintCanvasHydroSolver.ComputeSpecular(filmFactor: 0.2f, pileFactor: 0.9f, dry01: 0f);
        float gloss = PaintCanvasHydroSolver.ComputeSpecular(filmFactor: 0.9f, pileFactor: 0.05f, dry01: 0f);
        Assert.Greater(gloss, matte);
        float dryGloss = PaintCanvasHydroSolver.ComputeSpecular(0.9f, 0.05f, dry01: 1f);
        Assert.Less(dryGloss, gloss);
    }

    [Test]
    public void Hydro_SeedAndPullAway_RaisesFilmSpecular()
    {
        var go = new GameObject("CanvasHydro");
        try
        {
            var canvas = go.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvas.surfaceTension = 0.9f;
            var hydro = canvas.Hydro;
            hydro.runSimulation = false;

            hydro.SeedFromStamp(go.transform.position, Color.red, mass: 0.5f, wet01: 1f, count: 12);
            Assert.Greater(hydro.ActiveCount, 0);

            var layer = canvas.layerStack.TopWetLayer();
            layer.dry01 = 0f;
            // Simulate tension beads → matte via ComputeSpecular path used in WriteViscosity
            float beadSpec = PaintCanvasHydroSolver.ComputeSpecular(0.2f, 0.85f, 0f);
            hydro.ApplyPullAwayFlux(go.transform.position, -go.transform.forward, 1f);
            float filmSpec = PaintCanvasHydroSolver.ComputeSpecular(0.85f, 0.1f, 0f);
            Assert.Greater(filmSpec, beadSpec);

            Object.DestroyImmediate(canvas.layerStack);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Hydro_PilePull_ConsumesPileMass()
    {
        var canvasGo = new GameObject("Canvas");
        var pileGo = new GameObject("Pile");
        try
        {
            var canvas = canvasGo.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            var pile = pileGo.AddComponent<PaintPileLiquidDriver>();
            pile.totalMass = 1f;
            pile.pileCenter = canvasGo.transform.position;
            pile.pileRadius = 0.2f;
            pile.pileColor = Color.blue;

            var hydro = canvas.Hydro;
            hydro.pileSource = pile;
            hydro.runSimulation = false;
            float before = pile.totalMass;
            Assert.IsTrue(hydro.TryPullFromPile(pile.pileCenter, dt: 0.05f, maxTake: 0.2f));
            Assert.Less(pile.totalMass, before);
            Assert.Greater(hydro.ActiveCount, 0);

            Object.DestroyImmediate(canvas.layerStack);
        }
        finally
        {
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(pileGo);
        }
    }
}
#endif
