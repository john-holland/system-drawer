using UnityEngine;

namespace Planetary
{
    public sealed class QuantumEntanglementZone : MonoBehaviour
    {
        public QuantumEntanglementZone linkedZone;
        public float radiationOffset;
        public bool requiresNarrativeGate = true;
        public string narrativeEventId;

        [Header("Tractor coupling")]
        public string entangledBodyId;
        public Transform coupledShipTransform;
        public float forceGain = 1f;

        public bool CanActivate(bool narrativeEventFired) =>
            !requiresNarrativeGate || narrativeEventFired;
    }
}
