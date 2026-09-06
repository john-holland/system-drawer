using System.Collections.Generic;
using UnityEngine;

/// <summary>Garage roller chain: length, link kinds, axle wrap, steel limits, SPH pull bake inputs.</summary>
[CreateAssetMenu(fileName = "GarageChain", menuName = "Locomotion/Civil/Garage Chain")]
public sealed class GarageChainSpec : ScriptableObject
{
    [Header("Length")]
    public float totalLengthM = 4.8f;
    public float linkPitchM = 0.08f;

    [Header("Links")]
    public GarageChainLinkKind selectedKind = GarageChainLinkKind.Chain;
    public GarageChainLinkDef master = new GarageChainLinkDef
    {
        kind = GarageChainLinkKind.Master,
        jointId = "chain_master",
        massKg = 0.18f
    };
    public GarageChainLinkDef chain = new GarageChainLinkDef { kind = GarageChainLinkKind.Chain };
    public GarageChainLinkDef broken = new GarageChainLinkDef
    {
        kind = GarageChainLinkKind.Broken,
        jointId = "chain_broken",
        massKg = 0.1f
    };

    [Header("Axle / teeth")]
    public float axleDiameterM = 0.04f;
    public Vector3 axleLocalPosition;
    public Vector3 axleLocalEuler;
    public int toothCount = 12;
    public float pitchRadiusM = 0.12f;
    public float toothDepthM = 0.018f;
    public RadialBuildSpec radialBuild = new RadialBuildSpec
    {
        count = 12,
        radius = 0.12f,
        yawToCenter = true,
        wrapAngleDeg = 360f
    };

    [Header("Steel / rope")]
    public GarageSteelLimits steel = new GarageSteelLimits();

    [Header("SPH pull bake")]
    public float sphArcBinM = 0.1f;

    [Header("PixelLight")]
    public int pixelLightGridW = 8;
    public int pixelLightGridH = 8;
    public float pixelLightCellSize = 0.06f;

    public int LinkCount =>
        Mathf.Max(1, Mathf.CeilToInt(totalLengthM / Mathf.Max(0.02f, linkPitchM)));

    public GarageChainLinkDef DefFor(GarageChainLinkKind kind)
    {
        switch (kind)
        {
            case GarageChainLinkKind.Master: return master;
            case GarageChainLinkKind.Broken: return broken;
            default: return chain;
        }
    }

    public GarageChainLinkDef SelectedDef() => DefFor(selectedKind);

    public IEnumerable<GarageChainLinkKind> AllKinds()
    {
        yield return GarageChainLinkKind.Master;
        yield return GarageChainLinkKind.Chain;
        yield return GarageChainLinkKind.Broken;
    }

    public void SyncRadialFromTeeth()
    {
        if (radialBuild == null)
            radialBuild = new RadialBuildSpec();
        radialBuild.count = Mathf.Max(3, toothCount);
        radialBuild.radius = Mathf.Max(0.02f, pitchRadiusM);
        radialBuild.wrapAngleDeg = 360f;
        radialBuild.yawToCenter = true;
    }

    public RopeConfig ToRopeConfig()
    {
        var cfg = new RopeConfig
        {
            totalLengthM = Mathf.Max(0.2f, totalLengthM),
            segmentLengthM = Mathf.Max(0.02f, linkPitchM),
            mode = RopeMode.Spool,
            arcBinSizeM = Mathf.Max(0.02f, sphArcBinM),
            totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink
        };
        (steel ?? GarageSteelLimits.DefaultSteel()).ApplyTo(cfg, selectedKind);
        return cfg;
    }

    public string BakeHash() =>
        totalLengthM.ToString("0.###") + "|" + linkPitchM.ToString("0.###") + "|"
        + pitchRadiusM.ToString("0.###") + "|" + toothCount + "|" + axleDiameterM.ToString("0.###");
}
