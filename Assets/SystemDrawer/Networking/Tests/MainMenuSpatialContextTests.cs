#if UNITY_INCLUDE_TESTS

using NUnit.Framework;
using UnityEngine;

public class MainMenuSpatialContextTests
{
    GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void EnsureSpatialContext_AddsTreeNodeContainer()
    {
        _root = new GameObject("MenuRagdollRoot");
        _root.AddComponent<MenuRagdoll>();
        var gen = _root.AddComponent<MainMenuSpatialGenerator>();
        gen.menuRoot = _root.transform;
        gen.syncNetworkRequirements = false;
        gen.EnsureSpatialContext();

        Assert.IsNotNull(_root.GetComponent<SGTreeNodeContainer>());
        Assert.AreEqual(SpatialGenerator.GenerationMode.TwoDimensional, gen.generator.mode);
    }

    [Test]
    public void UpdateMainMenu_SetsContainerRootNode()
    {
        _root = new GameObject("MenuRagdollRoot");
        _root.AddComponent<MenuRagdoll>();
        var gen = _root.AddComponent<MainMenuSpatialGenerator>();
        gen.menuRoot = _root.transform;
        gen.generateLayoutAfterUpdate = false;
        gen.UpdateMainMenuForNetworkRequirements();

        var container = _root.GetComponent<SGTreeNodeContainer>();
        Assert.IsNotNull(container);
        Assert.IsNotNull(container.rootNode);
        Assert.AreEqual("menu.root", ((MenuRagdollNode)container.rootNode).eventName);
    }
}

#endif
