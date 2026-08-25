using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>Curb as SplineExtrusion SDF along the shoulder, with optional dapple bevel noise.</summary>
public static class SidewalkCurbBaker
{
    public static SdfMaxCompositionAsset Build(RoadLaneConfigAsset config, IList<Vector3> shoulderPolyline)
    {
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.nodes = new List<SdfMaxNode>();
        float height = config != null ? config.curbHeightM : 0.15f;
        float width = config != null ? config.curbWidthM : 0.2f;
        float dapple = config != null ? config.dappleBevel01 : 0f;
        var path = new List<Vector3>();
        if (shoulderPolyline != null)
            path.AddRange(shoulderPolyline);
        if (path.Count < 2)
        {
            path.Clear();
            path.Add(Vector3.zero);
            path.Add(new Vector3(4f, 0f, 0f));
        }

        var extrusion = new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            extrusionRadius = Mathf.Max(0.02f, Mathf.Max(height, width) * 0.5f),
            extrusionEnd = path[path.Count - 1] - path[0],
            extrusionPath = path,
            noiseFrequency = 0f,
            noiseOctaves = 1
        };
        asset.nodes.Add(extrusion);
        asset.rootNodeIndex = 0;

        if (dapple > 1e-4f)
        {
            var bevel = new SdfMaxNode
            {
                op = SdfMaxOp.SmoothMax,
                primitiveType = SdfPrimitiveType.SplineExtrusion,
                extrusionRadius = extrusion.extrusionRadius * 0.55f,
                extrusionEnd = extrusion.extrusionEnd,
                extrusionPath = path,
                smoothRadius = Mathf.Lerp(0.02f, 0.12f, dapple),
                noiseFrequency = Mathf.Lerp(0.5f, 4f, dapple),
                noiseOctaves = 3,
                childIndexA = 0,
                childIndexB = 2
            };
            var noiseLeaf = new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.SplineExtrusion,
                extrusionRadius = extrusion.extrusionRadius * 0.55f,
                extrusionEnd = extrusion.extrusionEnd,
                extrusionPath = path,
                noiseFrequency = bevel.noiseFrequency,
                noiseOctaves = 3
            };
            asset.nodes.Add(bevel);
            asset.nodes.Add(noiseLeaf);
            asset.rootNodeIndex = 1;
        }
        return asset;
    }

    public static bool ContainsShoulder(SdfMaxCompositionAsset asset, Vector3 world)
    {
        if (asset == null) return false;
        var graph = new SdfMaxExpressionGraph(asset, null, Matrix4x4.identity);
        return graph.SampleWorld(world, 0f) < 0f;
    }
}
