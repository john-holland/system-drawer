using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Optional scene marker for scanned topology hints.</summary>
    public sealed class OpenCloseTopologyMarker : MonoBehaviour
    {
        public string nodeId;
        public Vector3 approachAnchor;
        public Vector3 openingNormal;
        public bool enabledInGameplay = true;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(approachAnchor, 0.15f);
            Gizmos.DrawRay(approachAnchor, openingNormal * 0.5f);
        }
    }
}
