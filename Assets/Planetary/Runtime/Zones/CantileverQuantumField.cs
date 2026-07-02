using Planetary.Celestial;
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

        public void ApplyCoupledForce(
            ICelestialBody target,
            Transform shipTransform,
            QuantumTractorBeamPolicy policy,
            float gain)
        {
            if (target?.BodyTransform == null || shipTransform == null)
                return;
            Vector3 toShip = shipTransform.position - target.BodyTransform.position;
            float dist = toShip.magnitude;
            if (dist < 1e-3f)
                return;
            Vector3 dir = toShip / dist;
            float forceMag = gain * target.Mass * 1e-20f / dist;
            if (policy != null && policy.enforceLimits)
                forceMag = Mathf.Min(forceMag, policy.maxCouplingForceN);

            var shipRb = shipTransform.GetComponent<Rigidbody>();
            if (shipRb != null)
                shipRb.AddForce(-dir * forceMag, ForceMode.Force);

            var targetRb = target.BodyTransform.GetComponent<Rigidbody>();
            if (targetRb != null)
                targetRb.AddForce(dir * forceMag, ForceMode.Force);
            else
            {
                var orbit = target.BodyTransform.GetComponent<PlanetOrbitDriver>();
                if (orbit != null)
                    orbit.ApplyDelta(dir * (forceMag * Time.fixedDeltaTime * 1e-6f));
            }
        }
    }
}
