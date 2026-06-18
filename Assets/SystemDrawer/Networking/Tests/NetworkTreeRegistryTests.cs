#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public class NetworkTreeRegistryTests
{
    [Test]
    public void TransferOwnership_OnlyPeerTransferable()
    {
        var registry = new NetworkTreeRegistry();
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "elevator",
            TransmitPolicy = TreeTransmitPolicy.PeerTransferable,
            OwnerClientId = "a"
        });
        Assert.IsTrue(registry.TransferOwnership("elevator", "b"));
        registry.TryGet("elevator", out var d);
        Assert.AreEqual("b", d.OwnerClientId);
    }

    [Test]
    public void TransferOwnership_RejectsLocalOnly()
    {
        var registry = new NetworkTreeRegistry();
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "player",
            TransmitPolicy = TreeTransmitPolicy.LocalOnly
        });
        Assert.IsFalse(registry.TransferOwnership("player", "b"));
    }
}
#endif
