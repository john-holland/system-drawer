using UnityEngine;

namespace Locomotion.Spaceship
{
    public struct FastMoverHitEvent
    {
        public Vector3 worldPoint;
        public Vector3 incomingDirection;
        public float speed;
        public Transform source;
    }

    public interface IFastMoverTarget
    {
        Transform TargetTransform { get; }
        Vector3 PredictedPosition(float timeAhead);
        void ReceiveFastMoverHit(FastMoverHitEvent hit);
    }
}
