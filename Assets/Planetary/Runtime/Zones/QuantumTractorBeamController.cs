using Planetary.Celestial;
using UnityEngine;

namespace Planetary
{
    /// <summary>Corvette-scale tractor beam: entangles ship to planetoid via quantum zones.</summary>
    public sealed class QuantumTractorBeamController : MonoBehaviour
    {
        public QuantumTractorBeamPolicy policy;
        public float maxRangeM = 5000f;
        public float forceGain = 1f;
        public LayerMask targetMask = ~0;
        public QuantumEntanglementZone shipZone;
        public Transform shipRigidbodyRoot;

        ICelestialBody _target;
        QuantumEntanglementZone _targetZone;
        CantileverQuantumField _field;

        void Awake()
        {
            if (shipZone == null)
                shipZone = GetComponentInChildren<QuantumEntanglementZone>();
            if (shipRigidbodyRoot == null)
                shipRigidbodyRoot = transform;
            _field = GetComponent<CantileverQuantumField>();
        }

        void FixedUpdate()
        {
            if (_target == null || _targetZone == null)
                return;
            _field?.ApplyCoupledForce(_target, shipRigidbodyRoot, policy, forceGain);
        }

        public bool TryEngage(out string failReason)
        {
            failReason = null;
            if (policy == null)
            {
                failReason = "no policy";
                return false;
            }
            if (!TryRaycastTarget(out ICelestialBody body, out QuantumEntanglementZone zone))
            {
                failReason = "no target";
                return false;
            }
            if (!policy.CanTarget(body, out failReason))
                return false;
            _target = body;
            _targetZone = zone;
            if (shipZone != null)
            {
                shipZone.linkedZone = zone;
                shipZone.entangledBodyId = body.BodyId;
                shipZone.coupledShipTransform = shipRigidbodyRoot;
                shipZone.forceGain = forceGain;
            }
            if (zone != null)
            {
                zone.linkedZone = shipZone;
                zone.entangledBodyId = body.BodyId;
                zone.coupledShipTransform = shipRigidbodyRoot;
                zone.forceGain = forceGain;
            }
            return true;
        }

        public void Disengage()
        {
            if (shipZone != null)
            {
                shipZone.linkedZone = null;
                shipZone.entangledBodyId = null;
                shipZone.coupledShipTransform = null;
            }
            if (_targetZone != null)
            {
                _targetZone.linkedZone = null;
                _targetZone.entangledBodyId = null;
                _targetZone.coupledShipTransform = null;
            }
            _target = null;
            _targetZone = null;
        }

        bool TryRaycastTarget(out ICelestialBody body, out QuantumEntanglementZone zone)
        {
            body = null;
            zone = null;
            if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxRangeM, targetMask))
                return false;
            body = hit.collider.GetComponentInParent<ICelestialBody>();
            if (body == null)
                body = hit.collider.GetComponentInParent<PlanetCelestialBridge>();
            zone = hit.collider.GetComponentInParent<QuantumEntanglementZone>();
            if (zone == null && body?.BodyTransform != null)
                zone = body.BodyTransform.GetComponentInChildren<QuantumEntanglementZone>();
            return body != null;
        }
    }
}
