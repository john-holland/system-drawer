using System.Collections.Generic;
using UnityEngine;

/// <summary>One cell of a PixelLight N×N or nested radial stamp.</summary>
public sealed class PixelLightRadialStampCell
{
    public int cellX;
    public int cellY;
    public Vector3 localPosition;
    public int ringIndex;
    public bool nested;
}

/// <summary>N×N minigrid and one-level recursive block around a centroid / 9-way side.</summary>
public static class PixelLightRadialStamp
{
    public static List<PixelLightRadialStampCell> Enumerate(
        int gridW,
        int gridH,
        float cellSize,
        Vector3 planeNormal,
        Vector3 fineOffset,
        int centroidX,
        int centroidY,
        int miniW,
        int miniH,
        RadialSide side,
        RadialBuildSpec spec,
        CustomRadialSidePose custom,
        bool useCustom,
        bool recursive,
        int nestedW,
        int nestedH)
    {
        var list = new List<PixelLightRadialStampCell>();
        Vector3 centroidLocal = PixelLightGridMountGameObject.CellLocalPosition(
            gridW, gridH, cellSize, planeNormal, fineOffset, centroidX, centroidY);
        var cellBounds = new Bounds(centroidLocal, Vector3.one * Mathf.Max(0.01f, cellSize));
        Vector3 origin = RadialSlotMath.SideOrigin(cellBounds, side);
        if (useCustom && custom.origin.sqrMagnitude > 1e-8f)
            origin = custom.origin;

        int count = Mathf.Max(1, miniW * miniH);
        if (spec != null && spec.count > 1)
            count = spec.count;
        Vector3 axis = spec != null && spec.axis.sqrMagnitude > 1e-8f ? spec.axis : Vector3.up;
        float wrap = spec != null ? spec.ResolvedWrapDeg() : 360f;
        if (useCustom)
            wrap = RadialSlotMath.ResolveWrapDeg(custom, origin, axis, spec != null ? spec.centerPostPosition : origin, false);
        if (wrap <= 0f)
            wrap = 360f;
        float start = spec != null ? spec.startAngleDeg : 0f;
        float joinOff = spec != null ? spec.EffectiveJoinOffset() : 0f;
        Vector3 piece = Vector3.one * Mathf.Max(0.01f, cellSize);
        float radius = spec != null && spec.radius > 0f
            ? spec.radius
            : RadialSlotMath.NaturalRadius(piece, count, wrap, joinOff);

        int mw = Mathf.Max(1, miniW);
        for (int i = 0; i < count; i++)
        {
            int lx = i % mw;
            int ly = i / mw;
            Vector3 pos = RadialSlotMath.PolarSlot(origin, axis, radius, i, count, start, wrap);
            list.Add(new PixelLightRadialStampCell
            {
                cellX = centroidX + lx,
                cellY = centroidY + ly,
                localPosition = pos,
                ringIndex = i,
                nested = false
            });
            int nw = Mathf.Max(1, nestedW);
            int nh = Mathf.Max(1, nestedH);
            int nestedCount = nw * nh;
            if (recursive && nestedCount > 1)
            {
                var nestBounds = new Bounds(pos, Vector3.one * Mathf.Max(0.01f, cellSize));
                Vector3 nestOrigin = RadialSlotMath.SideOrigin(nestBounds, side);
                float nestRadius = RadialSlotMath.NaturalRadius(piece, nestedCount, wrap, joinOff);
                for (int j = 0; j < nestedCount; j++)
                {
                    list.Add(new PixelLightRadialStampCell
                    {
                        cellX = centroidX + lx,
                        cellY = centroidY + ly,
                        localPosition = RadialSlotMath.PolarSlot(nestOrigin, axis, nestRadius, j, nestedCount, start, wrap),
                        ringIndex = j,
                        nested = true
                    });
                }
            }
        }
        return list;
    }
}
