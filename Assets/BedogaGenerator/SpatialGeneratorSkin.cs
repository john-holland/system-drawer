using UnityEngine;

/// <summary>
/// One skin = one SpatialGenerator (3D) + one stylesheet + optional 4D + optional per-skin tree params.
/// Used by SpatialGeneratorSkinController to switch which generator and prefabs are active.
/// </summary>
[CreateAssetMenu(fileName = "SpatialGeneratorSkin", menuName = "BedogaGenerator/Spatial Generator Skin", order = 2)]
public class SpatialGeneratorSkin : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name for this skin.")]
    public string displayName = "Default";

    [Header("Generators")]
    [Tooltip("SpatialGenerator (3D) to use when this skin is active.")]
    public SpatialGenerator spatialGenerator3D;
    [Tooltip("Stylesheet for prefab/reference and tree-param overrides.")]
    public SpatialGeneratorStylesheet stylesheet;
    [Tooltip("Optional: 4D generator to use when this skin is active. If null, orchestrator keeps existing 4D behavior.")]
    public SpatialGenerator4D spatialGenerator4D;

    [Header("Tree params (expand/shrink) - override stylesheet when set")]
    [Tooltip("Multiply generator bounds when active. (1,1,1) = no change. Overrides stylesheet if not (1,1,1).")]
    public Vector3 boundsScale = Vector3.one;
    [Tooltip("Override solver minCellSize when > 0. 0 = use stylesheet or generator default.")]
    public float minCellSize = 0f;
    [Tooltip("Override solver maxDepth when > 0. 0 = use stylesheet or generator default.")]
    public int maxDepth = 0;
    [Tooltip("Override solver maxObjectsPerNode when > 0. 0 = use stylesheet or generator default.")]
    public int maxObjectsPerNode = 0;

    /// <summary>Effective bounds scale: skin value if not default, else stylesheet.</summary>
    public Vector3 EffectiveBoundsScale()
    {
        if (boundsScale != Vector3.one) return boundsScale;
        if (stylesheet != null) return stylesheet.boundsScale;
        return Vector3.one;
    }

    /// <summary>Effective minCellSize: skin > stylesheet > 0.</summary>
    public float EffectiveMinCellSize()
    {
        if (minCellSize > 0f) return minCellSize;
        if (stylesheet != null && stylesheet.minCellSize > 0f) return stylesheet.minCellSize;
        return 0f;
    }

    /// <summary>Effective maxDepth: skin > stylesheet > 0.</summary>
    public int EffectiveMaxDepth()
    {
        if (maxDepth > 0) return maxDepth;
        if (stylesheet != null && stylesheet.maxDepth > 0) return stylesheet.maxDepth;
        return 0;
    }

    /// <summary>Effective maxObjectsPerNode: skin > stylesheet > 0.</summary>
    public int EffectiveMaxObjectsPerNode()
    {
        if (maxObjectsPerNode > 0) return maxObjectsPerNode;
        if (stylesheet != null && stylesheet.maxObjectsPerNode > 0) return stylesheet.maxObjectsPerNode;
        return 0;
    }
}
