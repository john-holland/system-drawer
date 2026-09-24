using System;
using UnityEngine;

/// <summary>Shared radial place spec for SpatialGenerator and PixelLight.</summary>
[Serializable]
public sealed class RadialBuildSpec
{
    public string centerPostId = "";
    public Vector3 centerPostPosition;
    public Vector3 axis = Vector3.up;
    public int count = 4;
    public float startAngleDeg;
    public float wrapAngleDeg;
    public float radius;
    public RadialSide side = RadialSide.Center;
    public bool useCustomSide;
    public CustomRadialSidePose customSide;
    public bool yawToCenter = true;
    public RadialJoinKind joinKind = RadialJoinKind.Natural;
    public float joinOffset;
    public string jointId = "";
    public int solvedConfigIndex = -1;
    public int minigridW = 1;
    public int minigridH = 1;
    public int centroidCellX;
    public int centroidCellY;

    public void ApplySolved(RadialSolvedConfig cfg)
    {
        if (cfg == null) return;
        count = Mathf.Max(1, cfg.count);
        radius = cfg.radius;
        startAngleDeg = cfg.startAngleDeg;
        wrapAngleDeg = cfg.wrapAngleDeg;
        joinKind = cfg.joinKind;
    }

    public float ResolvedWrapDeg()
    {
        if (useCustomSide && customSide.hasCustomAngleObject)
            return 0f;
        if (useCustomSide && customSide.customAngle > 0f)
            return customSide.customAngle;
        return wrapAngleDeg > 0f ? wrapAngleDeg : 360f;
    }

    public float EffectiveJoinOffset() =>
        joinKind == RadialJoinKind.Natural ? 0f : Mathf.Max(0f, joinOffset);
}
