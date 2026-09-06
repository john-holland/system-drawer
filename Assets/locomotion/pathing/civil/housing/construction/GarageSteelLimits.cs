using System;
using UnityEngine;

/// <summary>Standard physics-manifold steel limits for garage roller chain (not structural I-beam).</summary>
[Serializable]
public sealed class GarageSteelLimits
{
    public float densityKgPerM3 = 7850f;
    public float chainYieldN = 8000f;
    public float chainBreakN = 12000f;
    public float masterYieldN = 12000f;
    public float masterBreakN = 18000f;
    public float brokenYieldN;
    public float brokenBreakN = 1f;
    [Range(0f, 1f)] public float tensionScale = 0.25f;
    [Range(0f, 1f)] public float porosityWeakening = 0.35f;

    public static GarageSteelLimits DefaultSteel() => new GarageSteelLimits();

    public float YieldTensionN(GarageChainLinkKind kind)
    {
        switch (kind)
        {
            case GarageChainLinkKind.Master: return masterYieldN;
            case GarageChainLinkKind.Broken: return brokenYieldN;
            default: return chainYieldN;
        }
    }

    public float BreakTensionN(GarageChainLinkKind kind)
    {
        switch (kind)
        {
            case GarageChainLinkKind.Master: return masterBreakN;
            case GarageChainLinkKind.Broken: return brokenBreakN;
            default: return chainBreakN;
        }
    }

    public float ManifoldScale(float surfaceTension01, float porosity01)
    {
        float t = 1f + Mathf.Clamp01(surfaceTension01) * tensionScale;
        float p = 1f - Mathf.Clamp01(porosity01) * porosityWeakening;
        return Mathf.Max(0.05f, t * p);
    }

    public void ApplyTo(RopeConfig config, GarageChainLinkKind weakest)
    {
        if (config == null) return;
        config.yieldTensionN = YieldTensionN(weakest);
        config.breakTensionN = BreakTensionN(weakest);
        config.totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink;
        config.segmentMassKg = Mathf.Max(0.01f, densityKgPerM3 * 0.000015f);
    }
}
