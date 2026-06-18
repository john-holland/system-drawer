#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public class MenuRagdollNodeTests
{
    [Test]
    public void CanShow_SinglePlayerNode_VisibleInSinglePlayerMode()
    {
        var go = new UnityEngine.GameObject("node");
        var node = go.AddComponent<MenuRagdollNode>();
        node.serverModeMask = MenuServerModeMask.SinglePlayer;
        Assert.IsTrue(node.CanShow());
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CanShow_MultiplayerOnly_HiddenWithoutClientInSinglePlayer()
    {
        var go = new UnityEngine.GameObject("node");
        var node = go.AddComponent<MenuRagdollNode>();
        node.serverModeMask = MenuServerModeMask.Multiplayer;
        Assert.IsFalse(node.CanShow());
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
