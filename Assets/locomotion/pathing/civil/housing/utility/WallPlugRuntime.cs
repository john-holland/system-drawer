using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>Wall receptacle: slot cavities and plug tines as SDF Max subtract solids.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Wall Plug")]
public sealed class WallPlugRuntime : MonoBehaviour
{
    public bool occupied;
    public CircuitBreakerPanel panel;
    public float branchAmps = 15f;
    public SdfMaxCompositionAsset slotsComposition;

    public SdfMaxCompositionAsset ComposeTineCavities(string assetName = "WallPlugSlotsSdf")
    {
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.name = assetName;
        asset.nodes = new List<SdfMaxNode>();
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = new Vector3(0.04f, 0.03f, 0.012f),
            localPosition = Vector3.zero
        });
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = new Vector3(0.004f, 0.01f, 0.008f),
            localPosition = new Vector3(-0.01f, 0f, 0.01f)
        });
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            halfExtents = new Vector3(0.004f, 0.01f, 0.008f),
            localPosition = new Vector3(0.01f, 0f, 0.01f)
        });
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Max,
            childIndexA = 1,
            childIndexB = 2
        });
        asset.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = 0,
            childIndexB = 3
        });
        asset.rootNodeIndex = 4;
        slotsComposition = asset;
        return asset;
    }

    public void PlugIn()
    {
        occupied = true;
        if (panel != null)
            panel.branchAmps.Add(branchAmps);
    }

    public void Unplug()
    {
        occupied = false;
        if (panel != null && panel.branchAmps.Count > 0)
            panel.branchAmps.RemoveAt(panel.branchAmps.Count - 1);
    }
}
