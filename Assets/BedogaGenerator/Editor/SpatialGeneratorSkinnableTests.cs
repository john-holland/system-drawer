#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tests for the SpatialGenerator4D skinnable system: stylesheet, skin, prefab resolver.
/// </summary>
public class SpatialGeneratorSkinnableTests
{
    [Test]
    public void SpatialGeneratorStylesheet_GetPrefabOverrides_NullOrEmptyKey_ReturnsNull()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>();
        Assert.IsNull(sheet.GetPrefabOverrides(null));
        Assert.IsNull(sheet.GetPrefabOverrides(""));
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_GetPrefabOverrides_NoMatchingEntry_ReturnsNull()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "OtherNode", prefabOverrides = new List<GameObject>() }
        };
        Assert.IsNull(sheet.GetPrefabOverrides("Room"));
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_GetPrefabOverrides_MatchingEntry_ReturnsOverrides()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var prefab = new GameObject("TestPrefab");
        var overrides = new List<GameObject> { prefab };
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "Room", prefabOverrides = overrides }
        };
        var result = sheet.GetPrefabOverrides("Room");
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(prefab, result[0]);
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_GetPrefabOverrides_KeyCaseInsensitive()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var prefab = new GameObject("TestPrefab");
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "room", prefabOverrides = new List<GameObject> { prefab } }
        };
        Assert.IsNotNull(sheet.GetPrefabOverrides("ROOM"));
        Assert.IsNotNull(sheet.GetPrefabOverrides("Room"));
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_GetReferenceOverride_NoMatch_ReturnsNull()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>();
        Assert.IsNull(sheet.GetReferenceOverride("Node", "refKey"));
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_GetReferenceOverride_Match_ReturnsValue()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var obj = new GameObject("RefObject");
        var entry = new SpatialGeneratorStylesheet.Entry
        {
            nodeKey = "Node",
            referenceOverrides = new List<SpatialGeneratorStylesheet.ReferenceOverride>
            {
                new SpatialGeneratorStylesheet.ReferenceOverride { key = "target", value = obj }
            }
        };
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry> { entry };
        var result = sheet.GetReferenceOverride("Node", "target");
        Assert.IsNotNull(result);
        Assert.AreEqual(obj, result);
        Object.DestroyImmediate(obj);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_HasTreeParamOverrides_Default_False()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        Assert.IsFalse(sheet.HasTreeParamOverrides());
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_HasTreeParamOverrides_WhenBoundsScaleNotOne_True()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.boundsScale = new Vector3(0.5f, 0.5f, 0.5f);
        Assert.IsTrue(sheet.HasTreeParamOverrides());
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void SpatialGeneratorStylesheet_HasTreeParamOverrides_WhenSolverOverrides_True()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.minCellSize = 1f;
        Assert.IsTrue(sheet.HasTreeParamOverrides());
        sheet.minCellSize = 0f;
        sheet.maxDepth = 3;
        Assert.IsTrue(sheet.HasTreeParamOverrides());
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void StylesheetPrefabResolver_NullNode_ReturnsEmptyList()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var resolver = new StylesheetPrefabResolver(sheet);
        var result = resolver.GetPrefabsForNode(null);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void StylesheetPrefabResolver_NullStylesheet_ReturnsNodePrefabs()
    {
        var resolver = new StylesheetPrefabResolver(null);
        var nodeGo = new GameObject("Node");
        var node = nodeGo.AddComponent<SGBehaviorTreeNode>();
        var nodePrefab = new GameObject("NodePrefab");
        node.gameObjectPrefabs = new List<GameObject> { nodePrefab };
        var result = resolver.GetPrefabsForNode(node);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(nodePrefab, result[0]);
        Object.DestroyImmediate(nodePrefab);
        Object.DestroyImmediate(nodeGo);
    }

    [Test]
    public void StylesheetPrefabResolver_NoOverride_ReturnsNodePrefabs()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>();
        var resolver = new StylesheetPrefabResolver(sheet);
        var nodeGo = new GameObject("Node");
        var node = nodeGo.AddComponent<SGBehaviorTreeNode>();
        node.name = "Wall";
        var nodePrefab = new GameObject("WallPrefab");
        node.gameObjectPrefabs = new List<GameObject> { nodePrefab };
        var result = resolver.GetPrefabsForNode(node);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(nodePrefab, result[0]);
        Object.DestroyImmediate(nodePrefab);
        Object.DestroyImmediate(nodeGo);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void StylesheetPrefabResolver_WithOverride_ReturnsStylesheetOverrides()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var overridePrefab = new GameObject("OverridePrefab");
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "Wall", prefabOverrides = new List<GameObject> { overridePrefab } }
        };
        var resolver = new StylesheetPrefabResolver(sheet);
        var nodeGo = new GameObject("Node");
        var node = nodeGo.AddComponent<SGBehaviorTreeNode>();
        node.name = "Wall";
        node.gameObjectPrefabs = new List<GameObject>(); // node has no prefabs; stylesheet overrides
        var result = resolver.GetPrefabsForNode(node);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(overridePrefab, result[0]);
        Object.DestroyImmediate(overridePrefab);
        Object.DestroyImmediate(nodeGo);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void StylesheetPrefabResolver_UsesSkinKeyWhenSet()
    {
        var sheet = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var overridePrefab = new GameObject("SkinKeyPrefab");
        sheet.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "custom_key", prefabOverrides = new List<GameObject> { overridePrefab } }
        };
        var resolver = new StylesheetPrefabResolver(sheet);
        var nodeGo = new GameObject("Node");
        var node = nodeGo.AddComponent<SGBehaviorTreeNode>();
        node.name = "DifferentName";
        node.skinKey = "custom_key";
        node.gameObjectPrefabs = new List<GameObject>();
        var result = resolver.GetPrefabsForNode(node);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(overridePrefab, result[0]);
        Object.DestroyImmediate(overridePrefab);
        Object.DestroyImmediate(nodeGo);
        Object.DestroyImmediate(sheet);
    }

    [Test]
    public void StylesheetPrefabResolver_SetStylesheet_UpdatesResolver()
    {
        var sheet1 = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var sheet2 = ScriptableObject.CreateInstance<SpatialGeneratorStylesheet>();
        var prefab2 = new GameObject("Prefab2");
        sheet2.entries = new List<SpatialGeneratorStylesheet.Entry>
        {
            new SpatialGeneratorStylesheet.Entry { nodeKey = "A", prefabOverrides = new List<GameObject> { prefab2 } }
        };
        var resolver = new StylesheetPrefabResolver(sheet1);
        resolver.SetStylesheet(sheet2);
        var nodeGo = new GameObject("Node");
        var node = nodeGo.AddComponent<SGBehaviorTreeNode>();
        node.name = "A";
        node.gameObjectPrefabs = new List<GameObject>();
        var result = resolver.GetPrefabsForNode(node);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(prefab2, result[0]);
        Object.DestroyImmediate(prefab2);
        Object.DestroyImmediate(nodeGo);
        Object.DestroyImmediate(sheet1);
        Object.DestroyImmediate(sheet2);
    }
}
#endif
