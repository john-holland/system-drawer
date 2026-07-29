#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class HairPlumeTests
{
    [Test]
    public void CapsuleBuffer_HasTenSlots_BodyAndDynamic()
    {
        Assert.AreEqual(10, HairCapsuleBuffer.SlotCount);
        Assert.AreEqual(6, HairCapsuleBuffer.BodySlots);
        Assert.AreEqual(4, HairCapsuleBuffer.DynamicSlots);

        var buf = new HairCapsuleBuffer();
        buf.SetSlot(HairCapsuleBuffer.BodySlot.Head, Vector3.up, 0.12f);
        buf.SetDynamicSlot(0, Vector3.right, 0.03f);
        Assert.AreEqual(7, buf.Count);
        Assert.AreEqual(0.12f, buf.Slots[0].w, 1e-4f);
        Assert.AreEqual(0.03f, buf.Slots[6].w, 1e-4f);

        buf.ClearDynamicSlots();
        Assert.AreEqual(0f, buf.Slots[6].w, 1e-4f);
        Assert.LessOrEqual(buf.Count, 6);
    }

    [Test]
    public void TipHold_ZeroBreaks_OneHolds()
    {
        var low = ScriptableObject.CreateInstance<HairPlumeConfig>();
        low.plumeTipHold = 0f;
        low.gaussianSigma = 0.45f;
        low.peakHeightM = 1f;

        var high = ScriptableObject.CreateInstance<HairPlumeConfig>();
        high.plumeTipHold = 1f;
        high.gaussianSigma = 0.45f;
        high.peakHeightM = 1f;

        float tip = 0.9f;
        float hBreak = HairPlumeSdfComposer.SampleGaussianHeight(0.25f, tip, low);
        float hHold = HairPlumeSdfComposer.SampleGaussianHeight(0.25f, tip, high);
        Assert.Greater(hHold, hBreak);

        Object.DestroyImmediate(low);
        Object.DestroyImmediate(high);
    }

    [Test]
    public void LatticeBake_WritesCompleteRadialChannels()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.azimuthBins = 16;
        cfg.lengthBins = 8;
        cfg.plumeTipHold = 0.5f;

        var bake = HairLatticeWaterfallBaker.Bake(cfg);
        Assert.AreEqual(16 * 8, bake.pixels.Length);
        Assert.IsNotNull(bake.texture);

        bool anyHeight = false;
        bool anyTip = false;
        for (int i = 0; i < bake.pixels.Length; i++)
        {
            if (bake.pixels[i].r > 0.05f) anyHeight = true;
            if (bake.pixels[i].a > 0.01f) anyTip = true;
        }
        Assert.IsTrue(anyHeight);
        Assert.IsTrue(anyTip);

        var cache = new HairRadialTextureCache(16, 8);
        HairLatticeWaterfallBaker.ApplyToCache(bake, cache);
        Assert.Greater(cache.GetPixel(0, 0).r, 0f);

        cache.Dispose();
        Object.DestroyImmediate(bake.texture);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void HelmetSectionCache_GatesPhysicsAndMask()
    {
        var section = new HairHelmetSectionCache(16, 8);
        section.SetRimUvEdge(0.92f);
        section.ApplyConicTuck(1f, 0f);
        Assert.IsTrue(section.Active);
        Assert.IsFalse(section.IsPhysicsEnabledForAzimuth(0f));

        var cache = new HairRadialTextureCache(16, 8);
        cache.Clear(new Color(0.4f, 0f, 0.2f, 0f));
        float[] helmet = new float[16 * 8];
        for (int i = 0; i < helmet.Length; i++)
            helmet[i] = 0.8f;
        section.CacheMaxHeight(cache, helmet);

        // Covered interior should lock to max(hair, helmet) = 0.8
        Color locked = cache.GetPixel(0, 0);
        Assert.AreEqual(0.8f, locked.r, 0.05f);
        Assert.IsNotNull(section.MaskTexture);
        Assert.Greater(section.MaskTexture.GetPixel(0, 0).r, 0.5f);

        section.ClearCoverage();
        Assert.IsFalse(section.Active);
        Assert.IsTrue(section.IsPhysicsEnabledForAzimuth(0f));

        section.Dispose();
        cache.Dispose();
    }

    [Test]
    public void TuckBehaviorTree_GoldenRatioShrinks()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.tuckFrameCount = 4;
        cfg.tuckStartRadiusM = 0.22f;
        var tree = new HairHelmetTuckBehaviorTree(cfg);
        Assert.AreEqual(4, tree.frames.Length);
        Assert.Greater(tree.frames[0].radiusMeters, tree.frames[1].radiusMeters);
        float ratio = tree.frames[0].radiusMeters / tree.frames[1].radiusMeters;
        Assert.AreEqual(HairPlumeConfig.GoldenRatio, ratio, 0.01f);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void FitColliderCapsule_SphereAndCapsule()
    {
        var go = new GameObject("HairCapsuleFit");
        try
        {
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.1f;
            Assert.IsTrue(HairCapsuleBuffer.TryFitColliderCapsule(sphere, out var c0, out var r0));
            Assert.AreEqual(0.1f, r0, 1e-3f);

            var capGo = new GameObject("Cap");
            capGo.transform.SetParent(go.transform);
            var cap = capGo.AddComponent<CapsuleCollider>();
            cap.height = 0.4f;
            cap.radius = 0.05f;
            Assert.IsTrue(HairCapsuleBuffer.TryFitColliderCapsule(cap, out _, out var r1));
            Assert.AreEqual(0.05f, r1, 1e-3f);
            Object.DestroyImmediate(capGo);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PassthroughAndFiber_BakeNonEmpty()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.azimuthBins = 16;
        cfg.lengthBins = 8;
        var pass = HairPassthroughShapeBaker.Bake(cfg, new[]
        {
            new HairPassthroughShapeBaker.CurveDef
            {
                azimuth01 = 0.5f,
                lengthStart01 = 0.1f,
                lengthEnd01 = 0.9f,
                width01 = 0.1f,
                height01 = 0.8f
            }
        });
        Assert.IsNotNull(pass);
        Assert.Greater(pass.GetPixel(8, 4).r, 0.1f);

        HairFiberMaterialBaker.Bake(cfg, Color.black, Color.white, out var diff, out var spec);
        Assert.IsNotNull(diff);
        Assert.IsNotNull(spec);
        Assert.Greater(spec.GetPixel(0, 4).r, 0f);

        Object.DestroyImmediate(pass);
        Object.DestroyImmediate(diff);
        Object.DestroyImmediate(spec);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void SdfComposer_BuildsRootedGraph()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        var asset = HairPlumeSdfComposer.ComposeGaussianPlume(cfg);
        Assert.IsNotNull(asset);
        Assert.Greater(asset.nodes.Count, 2);
        Assert.GreaterOrEqual(asset.ResolveRootIndex(), 0);
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(cfg);
    }
}
#endif
