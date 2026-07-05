using UnityEngine;

namespace Continuuuum.Telecom
{
    /// <summary>Runtime gateway connecting two telecom networks.</summary>
    public class NetworkConnectionComponent : MonoBehaviour
    {
        public string fromNetworkId = "ubiquitous";
        public string toNetworkId;
        public string gatewayDeviceId;
        public bool gatewayEnabled = true;
    }
}
