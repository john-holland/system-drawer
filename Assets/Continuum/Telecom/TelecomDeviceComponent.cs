using System;
using UnityEngine;

namespace Continuum.Telecom
{
    /// <summary>Phone, IP, and network binding for a CCTV/terminal device.</summary>
    public class TelecomDeviceComponent : MonoBehaviour
    {
        public string deviceId = "terminal-lobby";
        public string networkId = "ubiquitous";
        public string phoneDisplay = "1-1-555-0100";
        public string ipv6Full;
        public TelecomWebViewDisplay webViewDisplay;
        public TelecomUnityBridge bridge;
    }
}
