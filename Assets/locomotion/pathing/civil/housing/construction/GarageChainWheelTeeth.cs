using System.Collections.Generic;
using UnityEngine;

/// <summary>Sprocket tooth poses from RadialSlotMath (no new polar solver).</summary>
public static class GarageChainWheelTeeth
{
    public static Vector3 ToothPose(Vector3 center, Vector3 axis, float pitchRadius, int index, int toothCount)
    {
        int n = Mathf.Max(3, toothCount);
        return RadialSlotMath.PolarSlot(center, axis, Mathf.Max(0.02f, pitchRadius), index, n, 0f, 360f);
    }

    public static List<Vector3> AllToothPoses(Vector3 center, Vector3 axis, float pitchRadius, int toothCount)
    {
        int n = Mathf.Max(3, toothCount);
        var list = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
            list.Add(ToothPose(center, axis, pitchRadius, i, n));
        return list;
    }

    public static List<PixelLightRadialStampCell> StampTeeth(
        PixelLightGridMountGameObject mount,
        GarageChainSpec spec)
    {
        if (mount == null || spec == null)
            return new List<PixelLightRadialStampCell>();
        spec.SyncRadialFromTeeth();
        var radial = spec.radialBuild ?? new RadialBuildSpec();
        radial.count = Mathf.Max(3, spec.toothCount);
        radial.radius = Mathf.Max(0.02f, spec.pitchRadiusM);
        return PixelLightRadialStamp.Enumerate(
            mount.gridWidth, mount.gridHeight, mount.cellSize, mount.localPlaneNormal, mount.fineOffset,
            mount.centroidCellX, mount.centroidCellY,
            Mathf.Max(1, spec.toothCount), 1,
            mount.radialSide, radial, default, false, false, 1, 1);
    }
}
