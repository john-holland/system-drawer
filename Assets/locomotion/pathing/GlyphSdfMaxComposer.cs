using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>
/// Converts glyph mesh / convex-tree bounds into an SDF Max composition used as a Subtract
/// cavity from a chiclet solid (legend carve + optional light volume).
/// </summary>
public static class GlyphSdfMaxComposer
{
    public static SdfMaxCompositionAsset ComposeLegendSubtract(
        Bounds localGlyphBounds,
        Vector3 chicletHalfExtents,
        string assetName = "GlyphLegendSdf")
    {
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.name = assetName;
        asset.nodes = new List<SdfMaxNode>();

        // 0: chiclet box solid
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = chicletHalfExtents,
            localPosition = Vector3.zero
        });

        // 1: glyph cavity (MeshBounds / box approx of glyph)
        Vector3 half = localGlyphBounds.extents;
        if (half.sqrMagnitude < 1e-8f)
            half = new Vector3(0.03f, 0.01f, 0.03f);
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.MeshBounds,
            halfExtents = half,
            localPosition = localGlyphBounds.center + Vector3.up * (chicletHalfExtents.y * 0.85f)
        });

        // 2: Subtract glyph from chiclet
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = 0,
            childIndexB = 1
        });
        asset.rootNodeIndex = 2;
        return asset;
    }

    public static SdfMaxCompositionAsset ComposeFromMesh(Mesh glyphMesh, Vector3 chicletHalfExtents)
    {
        Bounds b = glyphMesh != null ? glyphMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.05f);
        return ComposeLegendSubtract(b, chicletHalfExtents);
    }
}
