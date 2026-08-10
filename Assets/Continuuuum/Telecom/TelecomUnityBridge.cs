using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Continuuuum.Telecom
{
    [Serializable]
    public class TelecomBridgeMessage
    {
        public string action;
        public string requestId;
        public string method;
        public string path;
        public string body;
        public string payload;
    }

    [Serializable]
    public class TelecomBridgeResponse
    {
        public string action = "bridgeResponse";
        public string requestId;
        public string payload;
        public string error;
    }

    /// <summary>C# message pump mirroring telecom-message-pump.js contract.</summary>
    public class TelecomUnityBridge : MonoBehaviour
    {
        public TelecomDeviceComponent device;
        public TelecomCallHandler callHandler;
        public TelecomVisualNotifier visualNotifier;
        public string apiBaseUrl = "http://127.0.0.1:5050/api/telecom";

        void Start()
        {
            if (device?.webViewDisplay != null)
                device.webViewDisplay.OnMessageFromJs += HandleMessageAsync;
            PushDeviceContext();
        }

        public void PushDeviceContext()
        {
            if (device?.webViewDisplay == null) return;
            var json = $"{{\"action\":\"deviceContext\",\"payload\":{{\"deviceId\":\"{device.deviceId}\",\"networkId\":\"{device.networkId}\",\"phone\":\"{device.phoneDisplay}\",\"ipv6Full\":\"{device.ipv6Full}\"}}}}";
            device.webViewDisplay.SendToJs(json);
        }

        public void PushSocietySnapshot(string cityId, string snapshotJson)
        {
            if (device?.webViewDisplay == null) return;
            var escaped = snapshotJson?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "{}";
            var json = $"{{\"action\":\"societySnapshot\",\"payload\":{{\"cityId\":\"{cityId}\",\"snapshot\":\"{escaped}\"}}}}";
            device.webViewDisplay.SendToJs(json);
        }

        async void HandleMessageAsync(string json)
        {
            var resp = await HandleAsync(json);
            device?.webViewDisplay?.SendToJs(JsonUtility.ToJson(resp));
        }

        /// <summary>Fired when webtop publishes window frame centroids JSON payload.</summary>
        public static event Action<string> WindowCentroidsReceived;

        /// <summary>Fired when webtop publishes UnityRenderPortal bounds2 anchors (CSS px + optional normalized UV).</summary>
        public static event Action<string> PortalBounds2Received;

        public async Task<TelecomBridgeResponse> HandleAsync(string json)
        {
            TelecomBridgeMessage msg;
            try { msg = JsonUtility.FromJson<TelecomBridgeMessage>(json); }
            catch (Exception ex) { return Error(null, ex.Message); }

            switch (msg.action)
            {
                case "api":
                    return await HandleApiAsync(msg);
                case "ring":
                    callHandler?.HandleRing(msg.payload);
                    return Ok(msg.requestId, "{}");
                case "notifyVisual":
                    visualNotifier?.Notify(msg.payload);
                    return Ok(msg.requestId, "{}");
                case "cctvFrame":
                    return Ok(msg.requestId, "{}");
                case "windowCentroids":
                    WindowCentroidsReceived?.Invoke(msg.payload ?? "{}");
                    return Ok(msg.requestId, "{}");
                case "portalBounds2":
                    PortalBounds2Received?.Invoke(msg.payload ?? "{}");
                    return Ok(msg.requestId, "{}");
                default:
                    return Error(msg.requestId, "unknown action");
            }
        }

        async Task<TelecomBridgeResponse> HandleApiAsync(TelecomBridgeMessage msg)
        {
            var url = apiBaseUrl.TrimEnd('/') + (msg.path.StartsWith("/") ? msg.path : "/" + msg.path);
            // Runtime: use UnityWebRequest in production; stub returns empty for tests
            await Task.Yield();
            return Ok(msg.requestId, "{\"ok\":true,\"url\":\"" + url + "\"}");
        }

        static TelecomBridgeResponse Ok(string requestId, string payload) =>
            new TelecomBridgeResponse { action = "bridgeResponse", requestId = requestId, payload = payload };

        static TelecomBridgeResponse Error(string requestId, string error) =>
            new TelecomBridgeResponse { action = "bridgeResponse", requestId = requestId, error = error };

    }
}
