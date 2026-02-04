using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ORM stylesheet for spatial generator skins. Maps node keys to prefab overrides and optional tree params
/// (bounds scale, minCellSize, maxDepth) so one logical behavior tree can render with different prefabs per skin.
/// </summary>
[CreateAssetMenu(fileName = "SpatialGeneratorStylesheet", menuName = "BedogaGenerator/Spatial Generator Stylesheet", order = 1)]
public class SpatialGeneratorStylesheet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Node key (e.g. node name or SGBehaviorTreeNode.skinKey). Used to match during placement.")]
        public string nodeKey = "";
        [Tooltip("When non-empty, overrides node.gameObjectPrefabs during placement.")]
        public List<GameObject> prefabOverrides = new List<GameObject>();
        [Tooltip("Optional key-object reference overrides for this node (e.g. other refs in generator).")]
        public List<ReferenceOverride> referenceOverrides = new List<ReferenceOverride>();
    }

    [Serializable]
    public class ReferenceOverride
    {
        public string key = "";
        public UnityEngine.Object value;
    }

    [Header("Node overrides")]
    [Tooltip("Maps nodeKey to prefab list and optional reference overrides.")]
    public List<Entry> entries = new List<Entry>();

    [Header("Tree params (expand/shrink)")]
    [Tooltip("Multiply generator bounds when this stylesheet is active. (1,1,1) = no change; smaller = shrink inclusion.")]
    public Vector3 boundsScale = Vector3.one;
    [Tooltip("Override solver minCellSize when > 0. 0 = use generator default.")]
    public float minCellSize = 0f;
    [Tooltip("Override solver maxDepth when > 0. 0 = use generator default.")]
    public int maxDepth = 0;
    [Tooltip("Override solver maxObjectsPerNode when > 0. 0 = use generator default.")]
    public int maxObjectsPerNode = 0;

    /// <summary>Get prefab overrides for a node key. Returns null if no entry or empty overrides (caller should use node's list).</summary>
    public List<GameObject> GetPrefabOverrides(string nodeKey)
    {
        if (entries == null || string.IsNullOrEmpty(nodeKey)) return null;
        foreach (var e in entries)
        {
            if (e != null && string.Equals(e.nodeKey, nodeKey, StringComparison.OrdinalIgnoreCase)
                && e.prefabOverrides != null && e.prefabOverrides.Count > 0)
                return e.prefabOverrides;
        }
        return null;
    }

    /// <summary>Get reference override by key for a node. Returns null if not found.</summary>
    public UnityEngine.Object GetReferenceOverride(string nodeKey, string referenceKey)
    {
        if (entries == null || string.IsNullOrEmpty(nodeKey)) return null;
        foreach (var e in entries)
        {
            if (e == null || !string.Equals(e.nodeKey, nodeKey, StringComparison.OrdinalIgnoreCase)
                || e.referenceOverrides == null) continue;
            foreach (var ro in e.referenceOverrides)
            {
                if (ro != null && string.Equals(ro.key, referenceKey, StringComparison.OrdinalIgnoreCase))
                    return ro.value;
            }
        }
        return null;
    }

    /// <summary>True if this stylesheet defines tree param overrides (boundsScale != 1 or solver overrides).</summary>
    public bool HasTreeParamOverrides()
    {
        if (boundsScale != Vector3.one) return true;
        if (minCellSize > 0f || maxDepth > 0 || maxObjectsPerNode > 0) return true;
        return false;
    }
}
