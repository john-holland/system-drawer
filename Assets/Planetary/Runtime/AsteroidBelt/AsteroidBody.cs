using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public struct AsteroidHitInfo
    {
        public Vector3 worldPoint;
        public Vector3 incomingDirection;
        public float speed;
    }

    [AddComponentMenu("Planetary/Asteroid Belt/Asteroid Body")]
    public sealed class AsteroidBody : MonoBehaviour
    {
        public int beltSectorIndex;
        public int beltSlotIndex;
        public float mass = 1000f;
        public float radius = 10f;
        public int compositionSeed;
        public AsteroidBeltMutationLog mutationLog;
        public ProceduralAsteroidDestruction destruction;

        Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (destruction == null)
                destruction = GetComponent<ProceduralAsteroidDestruction>();
            if (destruction == null)
                destruction = gameObject.AddComponent<ProceduralAsteroidDestruction>();
            destruction.asteroidBody = this;
        }

        public Vector3 PredictedPosition(float timeAhead)
        {
            if (_rb != null)
                return transform.position + _rb.linearVelocity * timeAhead;
            return transform.position;
        }

        public void ReceiveHit(AsteroidHitInfo hit)
        {
            if (destruction != null)
                destruction.TriggerDestruction(hit);
        }
    }
}
