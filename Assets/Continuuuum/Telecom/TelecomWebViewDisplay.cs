using System;
using UnityEngine;

namespace Continuuuum.Telecom
{
    /// <summary>Loads webtop-host.html in SimpleUnity3DWebView or Editor WebView.</summary>
    public class TelecomWebViewDisplay : MonoBehaviour
    {
        [Tooltip("URL or file path to webtop-host.html")]
        public string webtopUrl = "http://127.0.0.1:5175";

        public event Action<string> OnMessageFromJs;

        public virtual void LoadWebtop()
        {
            Debug.Log($"[Telecom] Load webtop: {webtopUrl}");
        }

        public virtual void SendToJs(string json)
        {
            Debug.Log($"[Telecom] → JS: {json}");
        }

        public void ReceiveFromJs(string json) => OnMessageFromJs?.Invoke(json);
    }
}
