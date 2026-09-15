using System;
using System.Collections.Generic;
using SdfMax;
using UnityEngine;

public enum Bounds4SdfInclusionKind
{
    Shell = 0,
    Frame = 1
}

/// <summary>
/// PixelLight mount whose 4-bounds stamps are included by Shell (filled cavity) or Frame (member tube),
/// then hardware axles are Subtracted. Optional composition / convert-from-mesh volume sources.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Lights/Bounds4 SDF Inclusion PixelLight Mount")]
public class Bounds4SdfInclusionPixelLightMount : PixelLightGridMountGameObject
{
    public Bounds4SdfInclusionKind inclusionKind = Bounds4SdfInclusionKind.Shell;
    public CustomRadialSideAsset shellCurve;
    public float frameThickness = 0.04f;
    public SdfMaxCompositionAsset composition;
    public Mesh convertFromMesh;
    public List<SdfMaxCompositionAsset> hardwareSubtract = new List<SdfMaxCompositionAsset>();

    public bool CellIncluded(int x, int y)
    {
        Vector3 p = CellLocalPosition(x, y);
        if (composition != null)
            return SampleComposition(composition, p);
        if (inclusionKind == Bounds4SdfInclusionKind.Frame)
            return FrameMemberContains(p);
        return ShellContains(p);
    }

    public bool ShellContains(Vector3 local)
    {
        float r = ShellRadius();
        return local.sqrMagnitude <= r * r;
    }

    public bool FrameMemberContains(Vector3 local)
    {
        float r = ShellRadius();
        float thick = Mathf.Max(0.005f, frameThickness);
        float d = local.magnitude;
        return d >= r - thick && d <= r + thick * 0.25f;
    }

    public float ShellRadius()
    {
        if (shellCurve != null)
            return Mathf.Max(0.05f, shellCurve.jointMiddle.extents.magnitude);
        return Mathf.Max(gridWidth, gridHeight) * cellSize * 0.45f;
    }

    public void ConvertFromMeshNow()
    {
        if (convertFromMesh == null)
            return;
        if (composition == null)
            composition = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        var go = new GameObject("Bounds4ConvertProxy");
        try
        {
            go.AddComponent<MeshFilter>().sharedMesh = convertFromMesh;
            go.AddComponent<MeshRenderer>();
            SdfMaxMeshAutoSetup.ApplyToComposition(composition, go.transform, null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    public SdfMaxCompositionAsset BakeHardwareSubtract(SdfMaxCompositionAsset dest = null)
    {
        dest ??= ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        dest.nodes = dest.nodes ?? new List<SdfMaxNode>();
        dest.nodes.Clear();
        float r = ShellRadius();
        dest.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = inclusionKind == Bounds4SdfInclusionKind.Frame
                ? SdfPrimitiveType.SplineExtrusion
                : SdfPrimitiveType.Sphere,
            radius = r,
            sphereRadius = r,
            extrusionRadius = Mathf.Max(0.005f, frameThickness),
            extrusionPath = inclusionKind == Bounds4SdfInclusionKind.Frame
                ? new List<Vector3> { Vector3.left * r, Vector3.right * r }
                : new List<Vector3>()
        });
        int root = 0;
        if (hardwareSubtract != null)
        {
            for (int i = 0; i < hardwareSubtract.Count; i++)
            {
                var hw = hardwareSubtract[i];
                if (hw == null || hw.nodes == null || hw.nodes.Count == 0)
                    continue;
                int bore = dest.nodes.Count;
                dest.nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.Sphere,
                    radius = 0.02f,
                    sphereRadius = 0.02f
                });
                dest.nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.Subtract,
                    childIndexA = root,
                    childIndexB = bore
                });
                root = dest.nodes.Count - 1;
            }
        }
        dest.rootNodeIndex = root;
        dest.name = inclusionKind == Bounds4SdfInclusionKind.Shell
            ? "Bounds4ShellSdf"
            : "Bounds4FrameSdf";
        return dest;
    }

    static bool SampleComposition(SdfMaxCompositionAsset asset, Vector3 local)
    {
        var eval = new SdfMaxEvaluator(new SdfMaxExpressionGraph(asset, null, Matrix4x4.identity));
        return eval.Sample(local, 0f) < 0f;
    }
}
