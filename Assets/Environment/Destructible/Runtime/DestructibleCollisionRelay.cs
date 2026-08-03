using UnityEngine;

namespace DestructibleEnvironment
{
    [DisallowMultipleComponent]
    public class DestructibleCollisionRelay : MonoBehaviour
    {
        DestructibleEnvironmentMeshRenderer _target;
        float _minImpulseN;

        public void Initialize(DestructibleEnvironmentMeshRenderer target, float minImpulseN)
        {
            _target = target;
            _minImpulseN = minImpulseN;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (_target == null || _target.IsActivated)
                return;
            if (collision.impulse.magnitude < _minImpulseN)
                return;
            Vector3 pt = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            ImpulseMaterialMemory.NotifyImpulse(gameObject, collision.impulse.magnitude, pt);
            _target.Activate(DestructibleImpactContext.FromCollision(collision, _target.gravityDir));
        }
    }
}
