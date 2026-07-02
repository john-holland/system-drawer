using Planetary.AsteroidBelt;
using UnityEngine;

namespace Locomotion.Spaceship
{
    /// <summary>Bridges IFastMoverTarget to Planetary AsteroidBody without asmdef cycle.</summary>
    [DisallowMultipleComponent]
    public sealed class AsteroidFastMoverAdapter : MonoBehaviour, IFastMoverTarget
    {
        AsteroidBody _body;

        public Transform TargetTransform => _body != null ? _body.transform : transform;

        public void Bind(AsteroidBody body) => _body = body;

        void OnEnable()
        {
            if (_body == null)
                _body = GetComponent<AsteroidBody>();
            var registry = FindAnyObjectByType<FastMoverRegistry>();
            registry?.RegisterTarget(this);
        }

        void OnDisable()
        {
            var registry = FindAnyObjectByType<FastMoverRegistry>();
            registry?.UnregisterTarget(this);
        }

        public Vector3 PredictedPosition(float timeAhead) =>
            _body != null ? _body.PredictedPosition(timeAhead) : transform.position;

        public void ReceiveFastMoverHit(FastMoverHitEvent hit)
        {
            if (_body == null)
                return;
            _body.ReceiveHit(new AsteroidHitInfo
            {
                worldPoint = hit.worldPoint,
                incomingDirection = hit.incomingDirection,
                speed = hit.speed
            });
        }
    }
}
