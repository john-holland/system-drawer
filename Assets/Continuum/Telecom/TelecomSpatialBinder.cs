using System.Threading.Tasks;
using UnityEngine;

namespace Continuum.Telecom
{
    /// <summary>Auto-address device via SG4D leaf / geohash (calls API when configured).</summary>
    public class TelecomSpatialBinder : MonoBehaviour
    {
        public TelecomDeviceComponent device;
        public string causalityLeafId;
        public string spatialGeohash;
        public string apiBaseUrl = "http://127.0.0.1:5050/api/telecom";

        public async void BindOnStart()
        {
            if (device == null) return;
            device.ipv6Full = $"0100:0001:{spatialGeohash ?? "auto"}:{causalityLeafId ?? "leaf"}";
            await Task.Yield();
            var bridge = device.bridge ?? GetComponent<TelecomUnityBridge>();
            bridge?.PushDeviceContext();
        }
    }
}
