using System;
using SdfMax;
using UnityEngine;

/// <summary>Per-kind garage chain link: prefab, mass, join socket, piece curve, baked SDF.</summary>
[Serializable]
public sealed class GarageChainLinkDef
{
    public GarageChainLinkKind kind = GarageChainLinkKind.Chain;
    public GameObject prefab;
    public float massKg = 0.12f;
    public string jointId = "chain_link";
    public CustomRadialSideAsset pieceCurve;
    public SdfMaxCompositionAsset linkSdf;
    public RadialJoinKind joinKind = RadialJoinKind.Natural;
    public float joinOffset;

    public float BreakTensionN(GarageSteelLimits steel)
    {
        steel ??= GarageSteelLimits.DefaultSteel();
        return steel.BreakTensionN(kind);
    }

    public float YieldTensionN(GarageSteelLimits steel)
    {
        steel ??= GarageSteelLimits.DefaultSteel();
        return steel.YieldTensionN(kind);
    }
}
