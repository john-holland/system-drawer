using System.Threading.Tasks;
using NUnit.Framework;
using Continuum.Telecom;

public class TelecomBridgeTests
{
    [Test]
    public async Task UnknownAction_ReturnsError()
    {
        var go = new UnityEngine.GameObject("bridge");
        var bridge = go.AddComponent<TelecomUnityBridge>();
        var resp = await bridge.HandleAsync("{\"action\":\"nope\",\"requestId\":\"t1\"}");
        Assert.IsFalse(string.IsNullOrEmpty(resp.error));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public async Task RingAction_Succeeds()
    {
        var go = new UnityEngine.GameObject("bridge");
        var bridge = go.AddComponent<TelecomUnityBridge>();
        bridge.callHandler = go.AddComponent<TelecomCallHandler>();
        var resp = await bridge.HandleAsync("{\"action\":\"ring\",\"requestId\":\"t2\",\"payload\":\"{}\"}");
        Assert.IsNull(resp.error);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
