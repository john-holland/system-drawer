using System;
using System.Collections.Generic;
using SdfMax;
using UnityEngine;

public enum Bounds4SdfInclusionKind
{
    Shell = 0,
    Frame = 1
}

/// <summary>Maps Bounds4 Frame/Shell inclusion to Continuuuum builtin lemmas.</summary>
public static class Bounds4SdfInclusionLemmas
{
    public static Bounds4SdfInclusionKind FromLemma(string lemma) =>
        FrameShellInclusionLemmaPropertyKeys.IsFrameInclusion(lemma)
            ? Bounds4SdfInclusionKind.Frame
            : Bounds4SdfInclusionKind.Shell;

    public static string ToLemma(Bounds4SdfInclusionKind kind) =>
        kind == Bounds4SdfInclusionKind.Frame
            ? FrameShellInclusionLemmaPropertyKeys.Frame
            : FrameShellInclusionLemmaPropertyKeys.Shell;

    public static string ToInclusionLemma(Bounds4SdfInclusionKind kind) =>
        kind == Bounds4SdfInclusionKind.Frame
            ? FrameShellInclusionLemmaPropertyKeys.FrameInclusion
            : FrameShellInclusionLemmaPropertyKeys.ShellInclusion;
}

/// <summary>
/// PixelLight mount whose 4-bounds stamps are included by Shell (filled cavity) or Frame (member tube),
/// then hardware axles / hollow slots are Subtracted. Optional composition / convert-from-mesh volume sources.
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
        => BakeHardwareSubtract(null, dest, true);

    public SdfMaxCompositionAsset BakeHardwareSubtract(
        PixelLightMultiSlotCatalog catalog, SdfMaxCompositionAsset dest = null)
        => BakeHardwareSubtract(catalog, dest, true);

    public SdfMaxCompositionAsset BakeHardwareSubtract(
        PixelLightMultiSlotCatalog catalog, SdfMaxCompositionAsset dest, bool rebuildSolid)
    {
        dest ??= ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        dest.nodes = dest.nodes ?? new List<SdfMaxNode>();
        int root;
        if (rebuildSolid || dest.nodes.Count == 0)
        {
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
            root = 0;
        }
        else
            root = dest.ResolveRootIndex();
        if (catalog != null)
        {
            var slots = catalog.SubtractSlotsFor(inclusionKind);
            for (int i = 0; i < slots.Count; i++)
                root = SubtractHollow(dest.nodes, root, slots[i]);
        }
        if (hardwareSubtract != null)
        {
            for (int i = 0; i < hardwareSubtract.Count; i++)
            {
                var hw = hardwareSubtract[i];
                if (hw == null || hw.nodes == null || hw.nodes.Count == 0)
                    continue;
                int bore = CopyGraph(dest.nodes, hw);
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
        composition = dest;
        return dest;
    }

    int SubtractHollow(List<SdfMaxNode> nodes, int root, PixelLightGridSlotEntry slot)
    {
        Vector3 p = CellLocalPosition(slot.cellX, slot.cellY) + slot.fineOffset;
        float radius = slot.hollowRadius > 1e-5f ? slot.hollowRadius : Mathf.Max(0.005f, cellSize * 0.45f);
        int bore = nodes.Count;
        nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            localPosition = p,
            radius = radius,
            sphereRadius = radius
        });
        nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = root,
            childIndexB = bore
        });
        return nodes.Count - 1;
    }

    static int CopyGraph(List<SdfMaxNode> dest, SdfMaxCompositionAsset src)
    {
        int offset = dest.Count;
        for (int i = 0; i < src.nodes.Count; i++)
        {
            var n = src.nodes[i];
            dest.Add(CloneNode(n, offset));
        }
        int srcRoot = src.ResolveRootIndex();
        return srcRoot >= 0 ? srcRoot + offset : offset;
    }

    static SdfMaxNode CloneNode(SdfMaxNode n, int childOffset)
    {
        var c = new SdfMaxNode();
        if (n == null) return c;
        c.op = n.op;
        c.primitiveType = n.primitiveType;
        c.localPosition = n.localPosition;
        c.localRotationEuler = n.localRotationEuler;
        c.localScale = n.localScale;
        c.radius = n.radius;
        c.halfExtents = n.halfExtents;
        c.constantValue = n.constantValue;
        c.blendK = n.blendK;
        c.smoothRadius = n.smoothRadius;
        c.sphereRadius = n.sphereRadius;
        c.torusMajorRadius = n.torusMajorRadius;
        c.torusMinorRadius = n.torusMinorRadius;
        c.extrusionRadius = n.extrusionRadius;
        c.extrusionEnd = n.extrusionEnd;
        c.extrusionPath = n.extrusionPath != null ? new List<Vector3>(n.extrusionPath) : new List<Vector3>();
        c.childIndexA = n.childIndexA >= 0 ? n.childIndexA + childOffset : -1;
        c.childIndexB = n.childIndexB >= 0 ? n.childIndexB + childOffset : -1;
        return c;
    }

    static bool SampleComposition(SdfMaxCompositionAsset asset, Vector3 local)
    {
        var eval = new SdfMaxEvaluator(new SdfMaxExpressionGraph(asset, null, Matrix4x4.identity));
        return eval.Sample(local, 0f) < 0f;
    }
}
