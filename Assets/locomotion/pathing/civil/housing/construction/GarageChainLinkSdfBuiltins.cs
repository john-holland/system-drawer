using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>SDF Max graph for a bicycle roller link: raccoon plates, pins, hollow sheaths, master clip.</summary>
public static class GarageChainLinkSdfBuiltins
{
    public static SdfMaxCompositionAsset BuildLink(
        GarageChainLinkCurve curve,
        GarageChainLinkKind kind,
        SdfMaxCompositionAsset dest = null)
    {
        curve ??= new GarageChainLinkCurve();
        if (dest == null)
            dest = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        dest.nodes = new List<SdfMaxNode>();
        dest.name = kind == GarageChainLinkKind.Master ? "GarageChainLinkMasterSdf" : "GarageChainLinkSdf";

        float zPlate = curve.PlateGap * 0.5f;
        int plates = Union(
            dest.nodes,
            BuildPlate(dest.nodes, curve, zPlate),
            BuildPlate(dest.nodes, curve, -zPlate));

        int leftPin = AddPin(dest.nodes, curve, curve.LeftPin);
        int rightPin = AddPin(dest.nodes, curve, curve.RightPin);
        int body;
        if (kind == GarageChainLinkKind.Master)
        {
            int welded = Weld(dest.nodes, plates, rightPin, curve.WeldK);
            body = Union(dest.nodes, welded, leftPin);
        }
        else
        {
            int weldedLeft = Weld(dest.nodes, plates, leftPin, curve.WeldK);
            body = Weld(dest.nodes, weldedLeft, rightPin, curve.WeldK);
        }

        int rollers = Union(
            dest.nodes,
            AddHollowSheath(dest.nodes, curve, curve.LeftPin),
            AddHollowSheath(dest.nodes, curve, curve.RightPin));
        int root = Union(dest.nodes, body, rollers);

        if (kind == GarageChainLinkKind.Master)
            root = Union(dest.nodes, root, AddShearClip(dest.nodes, curve));

        dest.rootNodeIndex = root;
        return dest;
    }

    public static SdfMaxCompositionAsset BuildHollowSheath(
        GarageChainLinkCurve curve,
        Vector3 center,
        SdfMaxCompositionAsset dest = null)
    {
        curve ??= new GarageChainLinkCurve();
        if (dest == null)
            dest = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        dest.nodes = new List<SdfMaxNode>();
        dest.rootNodeIndex = AddHollowSheath(dest.nodes, curve, center);
        dest.name = "GarageChainLinkSheathSdf";
        return dest;
    }

    static int BuildPlate(List<SdfMaxNode> nodes, GarageChainLinkCurve curve, float z)
    {
        Vector3 left = curve.LeftPin + Vector3.forward * z;
        Vector3 right = curve.RightPin + Vector3.forward * z;
        int ears = Union(
            nodes,
            AddCylinder(nodes, left, curve.PlateOuterR, curve.PlateThickness),
            AddCylinder(nodes, right, curve.PlateOuterR, curve.PlateThickness));
        var waist = new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            localPosition = new Vector3(0f, 0f, z),
            halfExtents = new Vector3(curve.Pitch * 0.5f, curve.WaistHalfWidth, curve.PlateThickness * 0.5f)
        };
        int plate = Union(nodes, ears, Add(nodes, waist));
        plate = Subtract(nodes, plate, AddCylinder(nodes, left, curve.PinRadius, curve.PlateThickness + 0.002f));
        return Subtract(nodes, plate, AddCylinder(nodes, right, curve.PinRadius, curve.PlateThickness + 0.002f));
    }

    static int AddPin(List<SdfMaxNode> nodes, GarageChainLinkCurve curve, Vector3 center)
    {
        return AddCylinder(nodes, center, curve.PinRadius, curve.PinLength);
    }

    static int AddHollowSheath(List<SdfMaxNode> nodes, GarageChainLinkCurve curve, Vector3 center)
    {
        int outer = AddCylinder(nodes, center, curve.RollerRadius, curve.RollerLength);
        int inner = AddCylinder(nodes, center, curve.RollerInnerRadius, curve.RollerLength + 0.002f);
        return Subtract(nodes, outer, inner);
    }

    static int AddShearClip(List<SdfMaxNode> nodes, GarageChainLinkCurve curve)
    {
        int u = AddPathTube(nodes, GarageChainLinkCurves.ShearClipPath(curve), curve.ClipThickness);
        int a = AddPathTube(nodes, GarageChainLinkCurves.ShearBladePath(curve, true), curve.ClipThickness);
        int b = AddPathTube(nodes, GarageChainLinkCurves.ShearBladePath(curve, false), curve.ClipThickness);
        return Union(nodes, u, Union(nodes, a, b));
    }

    static int AddCylinder(List<SdfMaxNode> nodes, Vector3 center, float radius, float length)
    {
        float h = Mathf.Max(0.0004f, length) * 0.5f;
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            localPosition = center,
            extrusionRadius = Mathf.Max(0.0004f, radius),
            extrusionPath = new List<Vector3> { Vector3.back * h, Vector3.forward * h }
        });
    }

    static int AddPathTube(List<SdfMaxNode> nodes, List<Vector3> path, float radius)
    {
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            extrusionRadius = Mathf.Max(0.0004f, radius),
            extrusionPath = path ?? new List<Vector3>()
        });
    }

    static int Add(List<SdfMaxNode> nodes, SdfMaxNode node)
    {
        nodes.Add(node);
        return nodes.Count - 1;
    }

    static int Union(List<SdfMaxNode> nodes, int a, int b) =>
        Combine(nodes, SdfMaxOp.Min, a, b, 0f);

    static int Weld(List<SdfMaxNode> nodes, int a, int b, float k) =>
        Combine(nodes, SdfMaxOp.Add, a, b, k);

    static int Subtract(List<SdfMaxNode> nodes, int a, int b) =>
        Combine(nodes, SdfMaxOp.Subtract, a, b, 0f);

    static int Combine(List<SdfMaxNode> nodes, SdfMaxOp op, int a, int b, float k)
    {
        return Add(nodes, new SdfMaxNode
        {
            op = op,
            childIndexA = a,
            childIndexB = b,
            blendK = k,
            smoothRadius = k
        });
    }
}
