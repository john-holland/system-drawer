#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>WebView / JS bridge — API calls always routed through C# (no credentials in browser).</summary>
public static class ContinuumEditorBridge
{
    [Serializable]
    public class BridgeRequest
    {
        public string action;
        public string requestId;
        public string method;
        public string path;
        public string body;
        public string reviewId;
        public string draftId;
        public string notificationId;
    }

    [Serializable]
    public class BridgeResponse
    {
        public string requestId;
        public bool ok;
        public string data;
        public string error;
    }

    public static async Task<BridgeResponse> HandleAsync(string json)
    {
        BridgeRequest req;
        try
        {
            req = JsonUtility.FromJson<BridgeRequest>(json);
        }
        catch (Exception ex)
        {
            return new BridgeResponse { ok = false, error = ex.Message };
        }

        if (req == null || string.IsNullOrEmpty(req.action))
            return new BridgeResponse { requestId = req?.requestId, ok = false, error = "missing action" };

        switch (req.action)
        {
            case "api":
                var api = await ContinuumEditorApiClient.RequestAsync(req.method ?? "GET", req.path, req.body);
                return new BridgeResponse
                {
                    requestId = req.requestId,
                    ok = api.success,
                    data = api.json,
                    error = api.error
                };
            case "openReview":
                ContinuumScriptEditorWindow.Open(req.draftId, req.reviewId);
                return new BridgeResponse { requestId = req.requestId, ok = true };
            case "notificationRead":
                await ContinuumEditorLocalizationClient.Instance.MarkReadAsync(req.notificationId);
                return new BridgeResponse { requestId = req.requestId, ok = true };
            default:
                return new BridgeResponse { requestId = req.requestId, ok = false, error = "unknown action" };
        }
    }

    public static string ToJson(BridgeResponse resp) => JsonUtility.ToJson(resp);
}

#endif
