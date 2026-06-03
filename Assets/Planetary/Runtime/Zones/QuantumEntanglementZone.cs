using UnityEngine;

namespace Planetary
{
    public sealed class QuantumEntanglementZone : MonoBehaviour
    {
        public QuantumEntanglementZone linkedZone;
        public float radiationOffset;
        public bool requiresNarrativeGate = true;
        public string narrativeEventId;

        public bool CanActivate(bool narrativeEventFired) =>
            !requiresNarrativeGate || narrativeEventFired;
    }
}
