using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prefab resolver that uses the active skin's stylesheet. If stylesheet has an entry for node.name or node.skinKey,
/// returns that entry's prefabOverrides; otherwise returns node.gameObjectPrefabs.
/// </summary>
public class StylesheetPrefabResolver : ISpatialGeneratorPrefabResolver
{
    private SpatialGeneratorStylesheet stylesheet;

    public StylesheetPrefabResolver(SpatialGeneratorStylesheet sheet)
    {
        stylesheet = sheet;
    }

    public void SetStylesheet(SpatialGeneratorStylesheet sheet)
    {
        stylesheet = sheet;
    }

    public List<GameObject> GetPrefabsForNode(SGBehaviorTreeNode node)
    {
        if (node == null)
            return new List<GameObject>();

        if (stylesheet == null)
            return node.gameObjectPrefabs != null ? node.gameObjectPrefabs : new List<GameObject>();

        string key = !string.IsNullOrEmpty(node.skinKey) ? node.skinKey : node.name;
        var overrides = stylesheet.GetPrefabOverrides(key);
        if (overrides != null && overrides.Count > 0)
            return overrides;

        return node.gameObjectPrefabs != null ? node.gameObjectPrefabs : new List<GameObject>();
    }
}
