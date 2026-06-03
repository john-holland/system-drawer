using UnityEngine;

namespace Planetary
{
    public sealed class CantileverQuantumField : MonoBehaviour
    {
        public QuantumEntanglementZone shipZone;
        public QuantumEntanglementZone emissionZone;
        public float velocityBoost = 2f;
        public float narrativeCooldownSeconds = 60f;
        float _lastActivateTime = -999f;

        public bool TryActivate(bool narrativeOk, out float boost)
        {
            boost = 1f;
            if (!narrativeOk || Time.time - _lastActivateTime < narrativeCooldownSeconds)
                return false;
            if (shipZone == null || emissionZone == null || !shipZone.CanActivate(narrativeOk))
                return false;
            _lastActivateTime = Time.time;
            boost = velocityBoost;
            emissionZone.radiationOffset -= shipZone.radiationOffset;
            return true;
        }
    }
}
