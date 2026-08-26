#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

public sealed class PenInkSystemTests
{
    [Test]
    public void InkMix_SingleLayer_UsesDilution_PaintlikeKeepsStackLerp()
    {
        var ink = InkMaterialProfile.CreateInkDefaults();
        var stack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
        stack.EnsureBaseLayer();
        var layer = stack.TopWetLayer();
        layer.albedo = Color.white;
        stack.MixDeposit(Color.black, 1f, ink);
        Assert.AreEqual(ink.dilution, 1f - layer.albedo.r, 0.02f);

        var paintlike = InkMaterialProfile.CreateInkDefaults();
        paintlike.paintlikeInk = true;
        var stack2 = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
        stack2.EnsureBaseLayer();
        stack2.TopWetLayer().albedo = Color.white;
        stack2.MixDeposit(Color.black, 1f, paintlike);
        Assert.AreEqual(0.35f, 1f - stack2.TopWetLayer().albedo.r, 0.02f);

        Object.DestroyImmediate(ink);
        Object.DestroyImmediate(paintlike);
        Object.DestroyImmediate(stack);
        Object.DestroyImmediate(stack2);
    }

    [Test]
    public void Quill_GaussianSpread_PeaksOnAxis()
    {
        var nib = QuillNibDefinition.CreateDefaults();
        Assert.Greater(nib.GaussianSpread01(0f), nib.GaussianSpread01(nib.maxSpreadAngleDeg));
        Assert.Greater(nib.GaussianSpread01(0f), 0.9f);
        Object.DestroyImmediate(nib);
    }

    [Test]
    public void Quill_ClampBend_DefaultTenDegrees_OverLimitFeedsStress()
    {
        var nib = QuillNibDefinition.CreateDefaults();
        Assert.AreEqual(10f, nib.maxBendDeg, 0.01f);
        Assert.AreEqual(10f, nib.ClampBendDeg(25f), 0.01f);
        Assert.GreaterOrEqual(nib.Stress01(25f, 0f, 12f), 1f);
        Object.DestroyImmediate(nib);
    }

    [Test]
    public void SeeThrough_Timer_ThirtySecondsThenOpaque()
    {
        var go = new GameObject("InkDry");
        try
        {
            var calGo = new GameObject("Cal");
            var cal = calGo.AddComponent<NarrativeCalendarAsset>();
            var bridge = go.AddComponent<InkDryingNarrativeBridge>();
            bridge.calendar = cal;
            var driver = go.AddComponent<InkDryingLayerDriver>();
            driver.ink = InkMaterialProfile.CreateInkDefaults();
            driver.narrative = bridge;
            driver.BeginDry();
            Assert.IsTrue(driver.seeThrough);
            Assert.IsTrue(cal.events.Exists(e => e.id == InkDryingNarrativeBridge.DryStartId));
            driver.ApplyElapsed(29f);
            Assert.IsTrue(driver.seeThrough);
            driver.ApplyElapsed(30f);
            Assert.IsFalse(driver.seeThrough);
            Assert.IsTrue(cal.events.Exists(e => e.id == InkDryingNarrativeBridge.DryOpaqueId));
            Object.DestroyImmediate(driver.ink);
            Object.DestroyImmediate(calGo);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void DrawingTarget_UnknownCodePoints_BoxAndReplacement()
    {
        Assert.AreEqual(PenInkDrawingTarget.ReplacementCodePoint, PenInkDrawingTarget.VerifyCodePoint(1));
        Assert.AreEqual(PenInkDrawingTarget.BoxCodePoint, PenInkDrawingTarget.VerifyCodePoint(0x4E00));
        Assert.AreEqual((int)'A', PenInkDrawingTarget.VerifyCodePoint('A'));
        var go = new GameObject("Draw");
        try
        {
            var t = go.AddComponent<PenInkDrawingTarget>();
            t.CompileText("A\u0001");
            Assert.AreEqual((int)'A', t.codePoints[0]);
            Assert.AreEqual(PenInkDrawingTarget.ReplacementCodePoint, t.codePoints[1]);
            Assert.IsFalse(t.CanTrain);
            t.understandingConfirmed = true;
            Assert.IsTrue(t.CanTrain);
            t.sourceKind = PenInkDrawingTarget.SourceKind.Image;
            t.enableOcrImage = false;
            t.Compile();
            Assert.AreEqual(PenInkDrawingTarget.BoxCodePoint, t.codePoints[0]);
            if (t.strokeSdf != null)
                Object.DestroyImmediate(t.strokeSdf);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void NibBreak_ExpandsAperture_AndSeedsHydro()
    {
        var canvasGo = new GameObject("Canvas");
        var penGo = new GameObject("Pen");
        GameObject debris = null;
        try
        {
            var canvas = canvasGo.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvas.inkProfile = InkMaterialProfile.CreateInkDefaults();
            canvas.EnsureHydro();
            var col = canvasGo.AddComponent<BoxCollider>();

            var pen = penGo.AddComponent<PenInkInstrument>();
            pen.tip = penGo.transform;
            pen.nib = QuillNibDefinition.CreateDefaults();
            pen.ink = canvas.inkProfile;
            pen.breakForceN = 1f;
            float before = pen.EffectiveApertureRadiusM();
            var result = pen.ContactCanvas(canvas, 25f, 20f, col, Vector3.forward);
            debris = result.debris;
            Assert.IsTrue(result.broke);
            Assert.Greater(pen.EffectiveApertureRadiusM(), before);
            Assert.Greater(result.hydroSeeded, 0);
            Assert.Greater(canvas.Hydro.ActiveCount, 0);
        }
        finally
        {
            if (debris != null) Object.DestroyImmediate(debris);
            if (canvasGo.GetComponent<PaintCanvas>()?.layerStack != null)
                Object.DestroyImmediate(canvasGo.GetComponent<PaintCanvas>().layerStack);
            if (canvasGo.GetComponent<PaintCanvas>()?.inkProfile != null)
                Object.DestroyImmediate(canvasGo.GetComponent<PaintCanvas>().inkProfile);
            if (penGo.GetComponent<PenInkInstrument>()?.nib != null)
                Object.DestroyImmediate(penGo.GetComponent<PenInkInstrument>().nib);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(penGo);
        }
    }

    [Test]
    public void HydroRidgeForce_OptionFeedsNib_WithoutReseeding()
    {
        var canvasGo = new GameObject("CanvasHydro");
        var penGo = new GameObject("PenHydro");
        try
        {
            var canvas = canvasGo.AddComponent<PaintCanvas>();
            canvas.layerStack = ScriptableObject.CreateInstance<PaintCanvasLayerStack>();
            canvas.layerStack.EnsureBaseLayer();
            canvas.inkProfile = InkMaterialProfile.CreateInkDefaults();
            canvas.EnsureHydro();
            var hydro = canvas.Hydro;
            var pen = penGo.AddComponent<PenInkInstrument>();
            pen.tip = penGo.transform;
            pen.nib = QuillNibDefinition.CreateDefaults();
            pen.ink = canvas.inkProfile;
            penGo.transform.position = canvasGo.transform.position;
            hydro.nibFeedbackTarget = pen;

            hydro.SeedFromStamp(pen.TipWorld, Color.black, 0.2f, 1f, count: 10);
            hydro.Simulate(0.02f);
            int seeded = hydro.ActiveCount;
            Assert.Greater(seeded, 0);

            hydro.feedRidgeForceToNib = false;
            pen.lastContactForceN = 0f;
            Assert.IsFalse(hydro.TryFeedRidgeForceToNib(pen));
            Assert.AreEqual(0f, pen.lastContactForceN, 1e-5f);
            Assert.AreEqual(seeded, hydro.ActiveCount);

            hydro.feedRidgeForceToNib = true;
            Assert.IsTrue(hydro.TryFeedRidgeForceToNib(pen));
            Assert.Greater(pen.lastContactForceN, 0f);
            Assert.AreEqual(seeded, hydro.ActiveCount);
        }
        finally
        {
            if (canvasGo.GetComponent<PaintCanvas>()?.layerStack != null)
                Object.DestroyImmediate(canvasGo.GetComponent<PaintCanvas>().layerStack);
            if (canvasGo.GetComponent<PaintCanvas>()?.inkProfile != null)
                Object.DestroyImmediate(canvasGo.GetComponent<PaintCanvas>().inkProfile);
            if (penGo.GetComponent<PenInkInstrument>()?.nib != null)
                Object.DestroyImmediate(penGo.GetComponent<PenInkInstrument>().nib);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(penGo);
        }
    }

    [Test]
    public void CurvedDecal_WorldToUv_OnShell()
    {
        var go = new GameObject("Curved");
        try
        {
            var canvas = go.AddComponent<PaintCanvas>();
            var curved = go.AddComponent<PaintCanvasCurvedDecal>();
            curved.radiusM = 0.25f;
            curved.arcDeg = 90f;
            curved.heightM = 0.2f;
            curved.RebuildMesh();
            Vector3 onShell = go.transform.TransformPoint(new Vector3(0f, 0f, curved.radiusM));
            Assert.IsTrue(curved.WorldToUv(onShell, out Vector2 uv));
            Assert.AreEqual(0.5f, uv.x, 0.05f);
            Assert.AreEqual(0.5f, uv.y, 0.05f);
            Assert.IsTrue(canvas.WorldToCanvasUv(onShell, out _));
            Vector3 off = go.transform.TransformPoint(new Vector3(0f, 0f, curved.radiusM * 3f));
            Assert.IsFalse(curved.WorldToUv(off, out _));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Hydro_InkProfile_RaisesDryRate()
    {
        var go = new GameObject("HydroInk");
        try
        {
            var canvas = go.AddComponent<PaintCanvas>();
            canvas.inkProfile = InkMaterialProfile.CreateInkDefaults();
            canvas.EnsureHydro();
            Assert.AreEqual(0.45f, canvas.Hydro.EffectiveSphDryRate, 0.001f);
            Object.DestroyImmediate(canvas.inkProfile);
            canvas.inkProfile = null;
            Assert.AreEqual(0.02f, canvas.Hydro.EffectiveSphDryRate, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PaintingIkCatalog_IncludesPenInkEntries()
    {
        var cat = ScriptableObject.CreateInstance<PaintingIkTrainingCatalog>();
        cat.EnsureDefaults();
        Assert.IsTrue(cat.entries.Exists(e => e.id == "pen_dip" && e.category == PhysicsIKTrainingCategory.Drink));
        Assert.IsTrue(cat.entries.Exists(e => e.id == "ink_stroke"));
        Assert.IsTrue(cat.entries.Exists(e => e.id == "cap_open" && e.category == PhysicsIKTrainingCategory.Open));
        Assert.IsTrue(cat.entries.Exists(e => e.id == "cap_close" && e.category == PhysicsIKTrainingCategory.Close));
        Assert.IsTrue(cat.entries.Exists(e => e.id == "blot_dry"));
        Object.DestroyImmediate(cat);
    }

    [Test]
    public void FeatureBudget_HasPaintInk()
    {
        Assert.AreEqual("paint_ink", FeatureBudgetIds.PaintInk);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.PaintInk));
        var ink = entries.Find(e => e.featureId == FeatureBudgetIds.PaintInk);
        Assert.IsTrue(System.Array.Exists(ink.perfScopePrefixes, p => p == "PenInk"));
        Assert.IsTrue(System.Array.Exists(ink.perfScopePrefixes, p => p == "InkDrying"));
        Assert.IsTrue(System.Array.Exists(ink.perfScopePrefixes, p => p == "QuillNib"));
    }

    [Test]
    public void LemmaResolver_CapOpen_SendMessage()
    {
        var go = new GameObject("PenLemma");
        try
        {
            var pen = go.AddComponent<PenInkInstrument>();
            pen.capOpen = true;
            var resolver = go.AddComponent<PenInkLemmaResolver>();
            resolver.instrument = pen;
            Assert.IsTrue(resolver.Apply(PenInkLemmaPropertyKeys.Close, "true"));
            Assert.IsFalse(pen.capOpen);
            Assert.IsTrue(resolver.Apply(PenInkLemmaPropertyKeys.CapOpen, "true"));
            Assert.IsTrue(pen.capOpen);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
