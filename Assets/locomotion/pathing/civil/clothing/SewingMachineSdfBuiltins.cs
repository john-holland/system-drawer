using System.Collections.Generic;
using SdfMax;
using UnityEngine;

/// <summary>SDF Max graphs for sewing-machine and serger Frame/Shell solids.</summary>
public static class SewingMachineSdfBuiltins
{
    public static SdfMaxCompositionAsset BuildSewing(SdfMaxCompositionAsset dest = null)
    {
        dest = Prepare(dest, "SewingMachineSdf");
        int bed = AddBox(dest.nodes, Vector3.zero, new Vector3(0.18f, 0.035f, 0.12f));
        int arm = AddBox(dest.nodes, new Vector3(0f, 0.11f, -0.04f), new Vector3(0.04f, 0.09f, 0.1f));
        int body = Union(dest.nodes, bed, arm);
        int needle = AddCapsule(dest.nodes, new Vector3(0.02f, 0.04f, 0.08f), 0.004f, 0.06f);
        body = Union(dest.nodes, body, needle);
        int presser = AddBox(dest.nodes, new Vector3(0.02f, 0.02f, 0.085f), new Vector3(0.018f, 0.006f, 0.012f));
        body = Union(dest.nodes, body, presser);
        int bobbin = AddTorus(dest.nodes, new Vector3(0.02f, -0.01f, 0.08f), 0.018f, 0.004f);
        dest.rootNodeIndex = Union(dest.nodes, body, bobbin);
        return dest;
    }

    public static SdfMaxCompositionAsset BuildSerger(SdfMaxCompositionAsset dest = null)
    {
        dest = Prepare(dest, "SergerSdf");
        int chassis = AddBox(dest.nodes, Vector3.zero, new Vector3(0.16f, 0.05f, 0.11f));
        int needle = AddCapsule(dest.nodes, new Vector3(0.04f, 0.05f, 0.07f), 0.004f, 0.05f);
        int body = Union(dest.nodes, chassis, needle);
        int upper = AddCapsule(dest.nodes, new Vector3(0.06f, 0.03f, 0.05f), 0.005f, 0.04f);
        int lower = AddCapsule(dest.nodes, new Vector3(0.06f, -0.01f, 0.05f), 0.005f, 0.04f);
        body = Union(dest.nodes, body, Union(dest.nodes, upper, lower));
        int dogs = AddBox(dest.nodes, new Vector3(0.03f, 0.01f, 0.08f), new Vector3(0.03f, 0.008f, 0.02f));
        dest.rootNodeIndex = Union(dest.nodes, body, dogs);
        return dest;
    }

    static SdfMaxCompositionAsset Prepare(SdfMaxCompositionAsset dest, string name)
    {
        if (dest == null)
            dest = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        dest.nodes = dest.nodes ?? new List<SdfMaxNode>();
        dest.nodes.Clear();
        dest.name = name;
        return dest;
    }

    static int AddBox(List<SdfMaxNode> nodes, Vector3 center, Vector3 halfExtents)
    {
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            localPosition = center,
            halfExtents = halfExtents
        });
    }

    static int AddCapsule(List<SdfMaxNode> nodes, Vector3 center, float radius, float length)
    {
        float h = Mathf.Max(0.001f, length) * 0.5f;
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            localPosition = center,
            extrusionRadius = Mathf.Max(0.0004f, radius),
            extrusionPath = new List<Vector3> { Vector3.down * h, Vector3.up * h }
        });
    }

    static int AddTorus(List<SdfMaxNode> nodes, Vector3 center, float major, float minor)
    {
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Torus,
            localPosition = center,
            torusMajorRadius = Mathf.Max(0.002f, major),
            torusMinorRadius = Mathf.Max(0.0004f, minor),
            radius = major
        });
    }

    static int Add(List<SdfMaxNode> nodes, SdfMaxNode node)
    {
        nodes.Add(node);
        return nodes.Count - 1;
    }

    static int Union(List<SdfMaxNode> nodes, int a, int b)
    {
        return Add(nodes, new SdfMaxNode
        {
            op = SdfMaxOp.Min,
            childIndexA = a,
            childIndexB = b
        });
    }
}
