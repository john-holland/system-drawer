using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>SDF Max expression built-ins for door rails, stiles, openings, and N-gon moulding.</summary>
public static class GarageDoorSdfBuiltins
{
    public static SdfMaxNode BoxPiece(Vector3 center, Vector3 halfExtents)
    {
        return new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            localPosition = center,
            halfExtents = halfExtents
        };
    }

    public static SdfMaxCompositionAsset BuildDoorShell(DoorAssemblySpec spec)
    {
        spec ??= ScriptableObject.CreateInstance<DoorAssemblySpec>();
        Vector3 half = new Vector3(spec.openingSize.x * 0.5f, spec.openingSize.y * 0.5f, spec.panelThickness * 0.5f);
        var openings = new List<Vector3>();
        if (spec.mullion && spec.sectionCount > 1)
        {
            float span = spec.openingSize.x - spec.stileWidth * 2f;
            float step = span / spec.sectionCount;
            for (int i = 1; i < spec.sectionCount; i++)
                openings.Add(new Vector3(-span * 0.5f + step * i, 0f, 0f));
        }
        return SdfMaxSoftToHardBaker.BakeBoxUnionWithOpenings(half, openings, spec.mullionWidth * 0.45f);
    }

    public static SdfMaxNode MouldingNgon(Vector3 center, float radius, int sides)
    {
        int n = Mathf.Max(3, sides);
        return new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            localPosition = center,
            radius = Mathf.Max(0.02f, radius),
            sphereRadius = Mathf.Max(0.02f, radius),
            halfExtents = Vector3.one * Mathf.Max(0.02f, radius),
            constantValue = n
        };
    }

    public static RadialBuildSpec MouldingRadial(int sides)
    {
        return new RadialBuildSpec
        {
            count = Mathf.Max(3, sides),
            wrapAngleDeg = 360f,
            yawToCenter = true,
            side = RadialSide.Center
        };
    }
}
