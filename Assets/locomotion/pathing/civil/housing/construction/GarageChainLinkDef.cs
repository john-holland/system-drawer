using System;
using UnityEngine;

/// <summary>Per-kind garage chain link: prefab, mass, join socket, piece curve.</summary>
[Serializable]
public sealed class GarageChainLinkDef
{
    public GarageChainLinkKind kind = GarageChainLinkKind.Chain;
    public GameObject prefab;
    public float massKg = 0.12f;
    public string jointId = "chain_link";
    public CustomRadialSideAsset pieceCurve;
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
