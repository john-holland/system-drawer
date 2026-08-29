#if UNITY_INCLUDE_TESTS

using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class MainMenuNetworkRequirementsSyncTests
{
    GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void Sync_CreatesCanonicalLobbyTree()
    {
        _root = BuildMenuRoot(out _, out var gen);
        gen.syncNetworkRequirements = true;
        gen.UpdateMainMenuForNetworkRequirements();

        var nodes = _root.GetComponentsInChildren<MenuRagdollNode>(true);
        Assert.IsTrue(nodes.Any(n => n.eventName == "menu.root"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "lobby.join.group"));
        Assert.AreEqual(1, nodes.Count(n => n.eventName == "lobby.join"));
    }

    [Test]
    public void Sync_SpectateNode_HasSpectatorOnlyMask()
    {
        _root = BuildMenuRoot(out _, out var gen);
        gen.UpdateMainMenuForNetworkRequirements();

        var spectate = _root.GetComponentsInChildren<MenuRagdollNode>(true)
            .First(n => n.eventName == "lobby.spectate.join");
        Assert.AreEqual(MenuClientRoleMask.SpectatorOnly, spectate.clientRoleMask);
    }

    [Test]
    public void Sync_JoinContainer_UsesGroupEventName()
    {
        _root = BuildMenuRoot(out _, out var gen);
        gen.UpdateMainMenuForNetworkRequirements();

        var joinContainers = _root.GetComponentsInChildren<MenuRagdollNode>(true)
            .Where(n => n.gameObject.name == "Join").ToList();
        Assert.AreEqual(1, joinContainers.Count);
        Assert.AreEqual("lobby.join.group", joinContainers[0].eventName);
    }

    [Test]
    public void Sync_WiresLobbyPasswordField()
    {
        _root = BuildMenuRoot(out var menu, out var gen);
        gen.UpdateMainMenuForNetworkRequirements();

        var field = _root.GetComponentInChildren<LobbyMenuPasswordField>(true);
        Assert.IsNotNull(field);
        Assert.AreEqual(menu, field.menuRagdoll);
    }

    [Test]
    public void Sync_CreatesGameSessionNodes()
    {
        _root = BuildMenuRoot(out _, out var gen);
        gen.syncNetworkRequirements = true;
        gen.UpdateMainMenuForNetworkRequirements();

        var nodes = _root.GetComponentsInChildren<MenuRagdollNode>(true);
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.new"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.join"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.spectate"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.close"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.close.umbrella"));
        Assert.IsTrue(nodes.Any(n => n.eventName == "game.session.save"));
    }

    static GameObject BuildMenuRoot(out MenuRagdoll menu, out MainMenuSpatialGenerator gen)
    {
        var root = new GameObject("MenuRagdollRoot");
        menu = root.AddComponent<MenuRagdoll>();
        gen = root.AddComponent<MainMenuSpatialGenerator>();
        gen.menuRoot = root.transform;
        gen.generateLayoutAfterUpdate = false;
        return root;
    }
}

#endif
