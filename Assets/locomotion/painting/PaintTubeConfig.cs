using UnityEngine;
using SdfMax;
using System.Collections.Generic;

/// <summary>
/// Paint tube: circular nozzle + flared base, finger-sphere dispense, SDF deform memory.
/// </summary>
[CreateAssetMenu(fileName = "PaintTubeConfig", menuName = "Locomotion/Painting/Tube Config")]
public sealed class PaintTubeConfig : ScriptableObject
{
    [Min(0.005f)] public float nozzleRadiusM = 0.008f;
    [Min(0.02f)] public float baseRadiusM = 0.04f;
    [Min(0.05f)] public float heightM = 0.12f;
    [Min(0.001f)] public float volumePerMeter = 0.0025f;
    [Min(0.01f)] public float hangTime = 0.45f;
    public Color paintColor = new Color(0.8f, 0.15f, 0.1f, 1f);
    [Min(1f)] public float viscosityPaS = 50f;
    [Range(0f, 1f)] public float surfaceTension = 0.85f;
    public bool useFakeGravity = true;
    public Vector3 fakeGravity = new Vector3(0f, -2.5f, 0f);
    [Range(0f, 1f)] public float resealRate = 0.15f;
}

/// <summary>Builds SDF Max for conical flat tube (circular top, flared base).</summary>
public static class PaintTubeSdfComposer
{
    public static SdfMaxCompositionAsset Compose(PaintTubeConfig config, string name = "PaintTubeSdf")
    {
        config ??= ScriptableObject.CreateInstance<PaintTubeConfig>();
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.name = name;
        asset.nodes = new List<SdfMaxNode>();

        float h = config.heightM;
        float topR = config.nozzleRadiusM;
        float baseR = config.baseRadiusM;

        // 0: nozzle sphere/capsule at top
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            radius = topR,
            sphereRadius = topR,
            localPosition = Vector3.up * (h * 0.5f)
        });

        // 1: flared base box
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = new Vector3(baseR, h * 0.15f, baseR),
            localPosition = Vector3.down * (h * 0.35f)
        });

        // 2: body capsule
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Capsule,
            radius = Mathf.Lerp(topR, baseR, 0.5f),
            sphereRadius = Mathf.Lerp(topR, baseR, 0.5f),
            localPosition = Vector3.zero
        });

        asset.nodes.Add(new SdfMaxNode { op = SdfMaxOp.Max, childIndexA = 0, childIndexB = 2 });
        asset.nodes.Add(new SdfMaxNode { op = SdfMaxOp.Max, childIndexA = 3, childIndexB = 1 });
        asset.rootNodeIndex = 4;
        return asset;
    }
}
