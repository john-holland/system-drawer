using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves prefabs for a behavior tree node during placement. Used by SpatialGenerator when a skin/stylesheet is active.
/// Default behavior: return node.gameObjectPrefabs. Stylesheet resolver returns overrides when nodeKey matches.
/// </summary>
public interface ISpatialGeneratorPrefabResolver
{
    /// <summary>Return the prefab list to use for this node. Never null; return node.gameObjectPrefabs when no override.</summary>
    List<GameObject> GetPrefabsForNode(SGBehaviorTreeNode node);
}
