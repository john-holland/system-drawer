#if UNITY_INCLUDE_TESTS

using NUnit.Framework;
using UnityEngine;

public class MenuRagdollNodeInheritanceTests
{
    [Test]
    public void MenuRagdollNode_IsAssignableToSgBehaviorTreeNode2D()
    {
        var go = new GameObject("node");
        var node = go.AddComponent<MenuRagdollNode>();
        Assert.IsTrue(node is SGBehaviorTreeNode2D);
        Assert.IsTrue(node is SGBehaviorTreeNode);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void RefreshChildNodesFromHierarchy_PopulatesChildNodes()
    {
        var rootGo = new GameObject("root");
        var root = rootGo.AddComponent<MenuRagdollNode>();
        var childGo = new GameObject("child");
        childGo.transform.SetParent(rootGo.transform, false);
        childGo.AddComponent<MenuRagdollNode>();

        root.RefreshChildNodesFromHierarchy();
        Assert.AreEqual(1, root.childNodes.Count);
        Assert.IsInstanceOf<MenuRagdollNode>(root.childNodes[0]);

        Object.DestroyImmediate(rootGo);
    }
}

#endif
