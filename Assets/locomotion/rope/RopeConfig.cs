using System;
using UnityEngine;

/// <summary>General-purpose rope operating mode (boundary conditions).</summary>
public enum RopeMode
{
    Grapple,
    Serpent,
    Spool
}

/// <summary>How total break tension is aggregated from per-segment limits.</summary>
public enum RopeTotalStrengthPolicy
{
    WeakestLink,
    SumSegments
}

[Serializable]
public class RopeConfig
{
    [Header("Geometry")]
    public float totalLengthM = 10f;
    public int ringBufferSize = 8;
    public float segmentLengthM = 0.35f;
    public float ropeRadiusM = 0.04f;

    [Header("Mode")]
    public RopeMode mode = RopeMode.Grapple;

    [Header("Material / strength")]
    public float yieldTensionN = 800f;
    public float breakTensionN = 1200f;
    public RopeTotalStrengthPolicy totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink;

    [Header("Wind / unwind")]
    public float maxWindRateMps = 2f;
    public float maxUnwindRateMps = 2f;

    [Header("Cache resolution")]
    public float arcBinSizeM = 0.1f;
    public int radialSlices = 8;

    [Header("Cord solver")]
    public int cordSolverIterations = 2;
    public float cordCorrectionStrength = 0.35f;

    [Header("Physics")]
    public float segmentMassKg = 0.15f;
    public float jointSpring = 8000f;
    public float jointDamper = 120f;
    public LayerMask ropeLayerMask = ~0;

    public int SegmentCount => Mathf.Max(1, Mathf.CeilToInt(totalLengthM / Mathf.Max(0.01f, segmentLengthM)));
    public int ArcBinCount => Mathf.Max(1, Mathf.CeilToInt(totalLengthM / Mathf.Max(0.01f, arcBinSizeM)));
}
