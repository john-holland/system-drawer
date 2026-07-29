using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>
/// Builds an SDF Max composition approximating a gaussian hair plume (displaced sphere + tip hold).
/// Used for lattice waterfall baking; runtime rendering prefers the radial texture cache.
/// </summary>
public static class HairPlumeSdfComposer
{
    public static SdfMaxCompositionAsset ComposeGaussianPlume(
        HairPlumeConfig config,
        string assetName = "HairPlumeSdf")
    {
        config ??= ScriptableObject.CreateInstance<HairPlumeConfig>();
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.name = assetName;
        asset.nodes = new List<SdfMaxNode>();

        float peak = Mathf.Max(0.01f, config.peakHeightM);
        float scalp = Mathf.Max(0.01f, config.scalpRadiusM);
        float tipHold = Mathf.Clamp01(config.plumeTipHold);

        // 0: core scalp / crown sphere
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            sphereRadius = scalp * 0.55f,
            radius = scalp * 0.55f,
            localPosition = Vector3.up * (scalp * 0.2f)
        });

        // 1: displaced plume shell (noise-driven stand-in for gaussian height field)
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.DisplacedSphere,
            sphereRadius = scalp + peak * Mathf.Lerp(0.35f, 1f, tipHold),
            radius = scalp + peak,
            noiseFrequency = Mathf.Lerp(2.5f, 0.8f, tipHold),
            noiseOctaves = tipHold > 0.7f ? 2 : 4,
            noisePersistence = Mathf.Lerp(0.55f, 0.25f, tipHold),
            localPosition = Vector3.up * (peak * 0.35f * (1f - tipHold * 0.3f))
        });

        // 2: Max union core + plume
        asset.nodes.Add(new SdfMaxNode
        {
            op = tipHold > 0.5f ? SdfMaxOp.SmoothMax : SdfMaxOp.Max,
            smoothRadius = 0.04f + tipHold * 0.06f,
            childIndexA = 0,
            childIndexB = 1
        });

        // Hairline ring capsules (sample 8 azimuths) Max'd onto plume
        int ringRoot = 2;
        const int ringSamples = 8;
        for (int i = 0; i < ringSamples; i++)
        {
            float u = i / (float)ringSamples;
            float ang = u * Mathf.PI * 2f;
            float r = config.hairLineCurve != null
                ? config.hairLineCurve.Radius01(u) * scalp
                : scalp;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, peak * 0.05f, Mathf.Sin(ang) * r);
            // Aim capsule slightly toward pate
            Vector3 pate = config.centerPateLocal;
            Vector3 axis = (pate - pos).normalized;
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.up;
            int leaf = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Capsule,
                radius = scalp * 0.08f,
                sphereRadius = scalp * 0.08f,
                localPosition = pos + axis * (peak * 0.15f),
                localRotationEuler = Quaternion.LookRotation(axis).eulerAngles
            });
            int maxNode = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Max,
                childIndexA = ringRoot,
                childIndexB = leaf
            });
            ringRoot = maxNode;
        }

        // Tip gravity bias (pulls volume downward when tipHold is low)
        float tipDrop = peak * config.gravityTipGain * (1f - tipHold);
        if (tipDrop > 0.01f)
        {
            int tipLeaf = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Capsule,
                sphereRadius = scalp * 0.4f,
                radius = scalp * 0.4f,
                halfExtents = new Vector3(scalp * 0.5f, tipDrop * 0.5f, scalp * 0.5f),
                localPosition = Vector3.down * tipDrop * 0.5f + Vector3.up * (scalp * 0.1f)
            });
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Max,
                childIndexA = ringRoot,
                childIndexB = tipLeaf
            });
            asset.rootNodeIndex = asset.nodes.Count - 1;
        }
        else
        {
            asset.rootNodeIndex = ringRoot;
        }

        return asset;
    }

    /// <summary>
    /// Analytic gaussian height for lattice bake: density + radial flux tip shaping + part.
    /// </summary>
    public static float SampleGaussianHeight(
        float azimuth01,
        float length01,
        HairPlumeConfig config)
    {
        float sigma = Mathf.Max(0.01f, config.gaussianSigma);
        float tipHold = Mathf.Clamp01(config.plumeTipHold);
        float fluxGain = config.gaussianFluxGain;
        float h = HairGaussianFlux.Height01(length01, sigma, tipHold, fluxGain);
        float clump = 0.85f + 0.15f * Mathf.Sin(azimuth01 * Mathf.PI * 4f);
        h = Mathf.Clamp01(h * clump);
        h = ApplyCurlRipple(h, azimuth01, length01, config);
        h = HairPartSampler.ApplyPartToGaussian(h, azimuth01, length01, config);
        return h;
    }

    /// <summary>
    /// Curl phase shared by bake ripple and (conceptually) shader helix:
    /// length * freq * 2π + azimuth * 2π.
    /// </summary>
    public static float CurlPhase(float azimuth01, float length01, float frequency)
    {
        float freq = Mathf.Clamp(frequency, 0.5f, 8f);
        return length01 * freq * Mathf.PI * 2f + azimuth01 * Mathf.PI * 2f;
    }

    static float ApplyCurlRipple(float height01, float azimuth01, float length01, HairPlumeConfig config)
    {
        float amount = config != null ? Mathf.Clamp01(config.curlAmount) : 0f;
        if (amount < 1e-4f) return height01;
        float tight = config != null ? Mathf.Clamp01(config.curlTightness) : 0.5f;
        float phase = CurlPhase(azimuth01, length01, config.curlFrequency);
        // R-channel ripple grows along the strand; tightness sharpens the lobes
        float ripple = Mathf.Sin(phase) * amount * Mathf.Lerp(0.06f, 0.18f, tight) * length01;
        return Mathf.Clamp01(height01 * (1f + ripple));
    }

    /// <summary>Tip-break channel (A) from radial flux vs tipHold.</summary>
    public static float SampleTipBreakEnergy(float length01, HairPlumeConfig config)
    {
        return HairGaussianFlux.TipBreakFromFlux(
            length01,
            Mathf.Max(0.01f, config.gaussianSigma),
            config.plumeTipHold,
            config.gaussianFluxGain);
    }
}