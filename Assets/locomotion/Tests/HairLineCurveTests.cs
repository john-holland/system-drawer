#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class HairLineCurveTests
{
    [Test]
    public void GaussianFlux_DensityAndFluxAndIntegral_AreConsistent()
    {
        float sigma = 0.45f;
        Assert.Greater(HairGaussianFlux.Density(0f, sigma), HairGaussianFlux.Density(1f, sigma));
        Assert.AreEqual(0f, HairGaussianFlux.RadialFlux(0f, sigma), 1e-5f);
        Assert.Greater(HairGaussianFlux.RadialFlux(sigma, sigma), 0f);
        Assert.Less(HairGaussianFlux.CumulativeMass01(0f, sigma), HairGaussianFlux.CumulativeMass01(1f, sigma));

        float breakEnergy = HairGaussianFlux.TipBreakFromFlux(0.8f, sigma, tipHold: 0f, fluxGain: 1f);
        float holdEnergy = HairGaussianFlux.TipBreakFromFlux(0.8f, sigma, tipHold: 1f, fluxGain: 1f);
        Assert.Greater(breakEnergy, holdEnergy);
    }

    [Test]
    public void HairPart_BisectsGaussianAlongSpline()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.gaussianSigma = 0.45f;
        cfg.plumeTipHold = 1f;
        cfg.hairPartSpline = new HairPartSpline
        {
            enabled = true,
            partWidthM = 0.015f,
            bisectStrength = 1f,
            localControlPoints = new System.Collections.Generic.List<Vector3>
            {
                new Vector3(0f, 0.02f, 0.08f),
                new Vector3(0f, 0.05f, 0f),
                new Vector3(0f, 0.05f, -0.05f)
            }
        };

        float onPart = HairPlumeSdfComposer.SampleGaussianHeight(0.25f, 0.2f, cfg);
        // Azimuth near +X is away from midline part (part is on Z axis at x=0)
        float offPart = HairPlumeSdfComposer.SampleGaussianHeight(0.0f, 0.2f, cfg);
        // Midline azimuth 0.25 is +Z-ish... azimuth 0 = +X. Part is along +Z. Point at az=0.25 (forward) near part.
        // Use local weight directly:
        float wOn = cfg.hairPartSpline.GaussianBisectWeight(new Vector3(0f, 0f, 0.04f), useLateralFlux: true);
        float wOff = cfg.hairPartSpline.GaussianBisectWeight(new Vector3(0.08f, 0f, 0f), useLateralFlux: true);
        Assert.Less(wOn, wOff);
        Assert.Less(wOn, 0.5f);
        Assert.Greater(wOff, 0.7f);
        Assert.GreaterOrEqual(onPart, 0f);
        Assert.GreaterOrEqual(offPart, 0f);

        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void LatticeBake_UsesHairlineWithoutCrash()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.azimuthBins = 16;
        cfg.lengthBins = 8;
        cfg.hairLineCurve = HairLineCurve.Constant(0.9f);
        cfg.hairLineAngleCurve = HairLineAngleCurve.Zero();
        cfg.pateAngleBlend = 0.35f;
        var bake = HairLatticeWaterfallBaker.Bake(cfg);
        Assert.AreEqual(16 * 8, bake.pixels.Length);
        Object.DestroyImmediate(bake.texture);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void SdfComposer_IncludesHairlineRing()
    {
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        var asset = HairPlumeSdfComposer.ComposeGaussianPlume(cfg);
        // Core + plume + max + 8 ring leaves + 8 maxes (+ optional tip)
        Assert.GreaterOrEqual(asset.nodes.Count, 3 + 8);
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(cfg);
    }
}
#endif
