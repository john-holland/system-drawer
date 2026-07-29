#if UNITY_EDITOR
using NUnit.Framework;
using SdfMax;
using UnityEngine;

public sealed class HairdoParamsTests
{
    [Test]
    public void Blend_NormalizesWeights_AndPicksPartByPrecedenceOnTie()
    {
        var blend = HairdoBlend.CreateDefault();
        blend.EnsureSlots();
        for (int i = 0; i < blend.slots.Count; i++)
        {
            blend.slots[i].enabled = false;
            blend.slots[i].weight = 0f;
            blend.slots[i].precedence = 0;
        }

        Set(blend, HairdoCutKind.SidePart, enabled: true, weight: 0.5f, precedence: 2);
        Set(blend, HairdoCutKind.CenterPart, enabled: true, weight: 0.5f, precedence: 0);

        Assert.IsTrue(blend.TryEvaluate(out var p, out float front, out _, out _, out float len));
        Assert.AreEqual(HairdoPartMode.Center, p.partMode);
        Assert.Greater(front, 0f);
        Assert.Greater(len, 0f);

        var norm = blend.NormalizedEnabledWeights();
        Assert.AreEqual(0.5f, norm[HairdoCutKind.SidePart], 1e-4f);
        Assert.AreEqual(0.5f, norm[HairdoCutKind.CenterPart], 1e-4f);
    }

    [Test]
    public void Preset_ApplyTo_WritesHairlineCurveExtrema()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        var mullet = HairdoPresetCatalog.Get(HairdoCutKind.Mullet);
        mullet.ApplyTo(cfg);

        Assert.Greater(cfg.hairLineCurve.Radius01(0.75f), cfg.hairLineCurve.Radius01(0.25f));
        Assert.Greater(cfg.maxStrandLengthM, 0.2f);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void DiamondAxes_LongIsLongerThanBuzz()
    {
        var buzz = HairdoPresetCatalog.Get(HairdoCutKind.Buzz);
        var longCut = HairdoPresetCatalog.Get(HairdoCutKind.Long);
        Assert.Greater(longCut.DiamondLength01, buzz.DiamondLength01);
        Assert.Greater(longCut.DiamondBack01, 0f);
    }

    [Test]
    public void ObsceneSdf_BuildHasManyNodes_AndSexprRoundTrips()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.ApplyLatticeBakeDefaults();
        var blend = HairdoBlend.CreateDefault();
        Set(blend, HairdoCutKind.Crew, true, 0.6f, 0);
        Set(blend, HairdoCutKind.Mullet, true, 0.4f, 1);
        var parms = blend.EvaluateOrDefault();

        var built = HairdoSdfExpressionBuilder.Build(cfg, blend, parms);
        Assert.GreaterOrEqual(built.asset.nodes.Count, 40);
        Assert.IsFalse(string.IsNullOrWhiteSpace(built.sexpr));
        Assert.IsTrue(built.sexpr.Contains(";;"));

        Assert.IsTrue(HairdoSdfSexpr.TryParse(built.sexpr, out var parsed, out string err), err);
        Assert.AreEqual(built.asset.nodes.Count, parsed.nodes.Count);

        // Spot-check root evaluates finite
        var graph = new SdfMaxExpressionGraph(parsed, null, Matrix4x4.identity);
        float sample = graph.SampleWorld(Vector3.up * 0.1f, 0f);
        Assert.IsFalse(float.IsNaN(sample));
        Assert.IsFalse(float.IsInfinity(sample));

        Object.DestroyImmediate(built.asset);
        Object.DestroyImmediate(parsed);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void SortedSlotIndices_OrdersByPrecedence()
    {
        var blend = HairdoBlend.CreateDefault();
        Set(blend, HairdoCutKind.Buzz, false, 0f, 5);
        Set(blend, HairdoCutKind.Crew, true, 1f, 1);
        Set(blend, HairdoCutKind.Bob, false, 0f, 0);
        var order = blend.SortedSlotIndices();
        int bobPos = order.FindIndex(i => blend.slots[i].kind == HairdoCutKind.Bob);
        int crewPos = order.FindIndex(i => blend.slots[i].kind == HairdoCutKind.Crew);
        int buzzPos = order.FindIndex(i => blend.slots[i].kind == HairdoCutKind.Buzz);
        Assert.Less(bobPos, crewPos);
        Assert.Less(crewPos, buzzPos);
    }

    [Test]
    public void CurlsPreset_HasMoreCurlThanWaves_AndWritesConfig()
    {
        var waves = HairdoPresetCatalog.Get(HairdoCutKind.Waves);
        var curls = HairdoPresetCatalog.Get(HairdoCutKind.Curls);
        var ringlets = HairdoPresetCatalog.Get(HairdoCutKind.Ringlets);
        Assert.Greater(curls.curlAmount, waves.curlAmount);
        Assert.Greater(ringlets.curlFrequency, curls.curlFrequency);
        Assert.Greater(ringlets.curlTightness, waves.curlTightness);

        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        curls.ApplyTo(cfg);
        Assert.AreEqual(curls.curlAmount, cfg.curlAmount, 1e-4f);
        Assert.AreEqual(curls.curlFrequency, cfg.curlFrequency, 1e-4f);
        Assert.AreEqual(curls.curlTightness, cfg.curlTightness, 1e-4f);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void CurlRipple_ModulatesBakeHeight_AndSdfGrowsWithCurls()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.ApplyLatticeBakeDefaults();
        cfg.curlAmount = 0f;
        float straight = HairPlumeSdfComposer.SampleGaussianHeight(0.25f, 0.7f, cfg);
        cfg.curlAmount = 1f;
        cfg.curlFrequency = 4f;
        cfg.curlTightness = 0.7f;
        float curled = HairPlumeSdfComposer.SampleGaussianHeight(0.25f, 0.7f, cfg);
        Assert.AreNotEqual(straight, curled);

        var blendStraight = HairdoBlend.CreateDefault();
        Set(blendStraight, HairdoCutKind.Crew, true, 1f, 0);
        var pStraight = blendStraight.EvaluateOrDefault();
        var builtStraight = HairdoSdfExpressionBuilder.Build(cfg, blendStraight, pStraight);

        var blendCurl = HairdoBlend.CreateDefault();
        for (int i = 0; i < blendCurl.slots.Count; i++)
        {
            blendCurl.slots[i].enabled = false;
            blendCurl.slots[i].weight = 0f;
        }
        Set(blendCurl, HairdoCutKind.Curls, true, 1f, 0);
        var pCurl = blendCurl.EvaluateOrDefault();
        var builtCurl = HairdoSdfExpressionBuilder.Build(cfg, blendCurl, pCurl);

        Assert.Greater(builtCurl.asset.nodes.Count, builtStraight.asset.nodes.Count);
        Assert.IsTrue(builtCurl.sexpr.Contains(";; curls"));

        Object.DestroyImmediate(builtStraight.asset);
        Object.DestroyImmediate(builtCurl.asset);
        Object.DestroyImmediate(cfg);
    }

    static void Set(HairdoBlend blend, HairdoCutKind kind, bool enabled, float weight, int precedence)
    {
        blend.EnsureSlots();
        for (int i = 0; i < blend.slots.Count; i++)
        {
            if (blend.slots[i].kind != kind) continue;
            blend.slots[i].enabled = enabled;
            blend.slots[i].weight = weight;
            blend.slots[i].precedence = precedence;
            return;
        }
    }
}
#endif
