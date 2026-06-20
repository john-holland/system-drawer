using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class ContinuumEditorBridgeTests
{
    [Test]
    public async Task Bridge_OpenReview_ReturnsOk()
    {
        var json = "{\"action\":\"openReview\",\"requestId\":\"t1\",\"draftId\":\"d1\",\"reviewId\":\"r1\"}";
        var resp = await ContinuumEditorBridge.HandleAsync(json);
        Assert.IsTrue(resp.ok);
        Assert.AreEqual("t1", resp.requestId);
    }

    [Test]
    public async Task Bridge_UnknownAction_Fails()
    {
        var resp = await ContinuumEditorBridge.HandleAsync("{\"action\":\"nope\",\"requestId\":\"t2\"}");
        Assert.IsFalse(resp.ok);
    }

    [Test]
    public void BridgeResponse_RoundTripsJson()
    {
        var resp = new ContinuumEditorBridge.BridgeResponse { requestId = "x", ok = true, data = "{\"a\":1}" };
        var json = ContinuumEditorBridge.ToJson(resp);
        var back = JsonUtility.FromJson<ContinuumEditorBridge.BridgeResponse>(json);
        Assert.AreEqual("x", back.requestId);
        Assert.IsTrue(back.ok);
    }
}

public class ContinuumNotificationClientTests
{
    [Test]
    public async Task StubNotificationClient_ReturnsEmptyList()
    {
        var client = new StubContinuumLocalizationClient();
        var resp = await client.GetNotificationsAsync();
        Assert.IsNotNull(resp.items);
        Assert.AreEqual(0, resp.items.Length);
    }
}
