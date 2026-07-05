using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Arrival gate math for ambulation vs open overlap.</summary>
    public static class OpenCloseArrivalGate
    {
        const float StopVelocityThreshold = 0.15f;
        const float DefaultReachDistance = 0.5f;

        public static float ComputeStopProgress(Vector3 actorPos, Vector3 anchor, float reachedDistance, Rigidbody body)
        {
            float dist = Vector3.Distance(actorPos, anchor);
            float distProgress = 1f - Mathf.Clamp01(dist / Mathf.Max(reachedDistance, 0.01f));
            if (body == null)
                return distProgress >= 1f ? 1f : distProgress;

            float vel = body.linearVelocity.magnitude;
            float velProgress = 1f - Mathf.Clamp01(vel / StopVelocityThreshold);
            return Mathf.Min(1f, distProgress * 0.7f + velProgress * 0.3f);
        }

        public static float ComputeReachProgress(Vector3 actorPos, Vector3 handle, float reachRadius)
        {
            float d = Vector3.Distance(actorPos, handle);
            return 1f - Mathf.Clamp01(d / Mathf.Max(reachRadius, DefaultReachDistance));
        }

        public static bool IsFacingTarget(Transform actor, Vector3 target, float maxAngleDeg = 35f)
        {
            if (actor == null)
                return true;
            Vector3 to = target - actor.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f)
                return true;
            float angle = Vector3.Angle(actor.forward, to.normalized);
            return angle <= maxAngleDeg;
        }

        public static float ComputeGate(
            float arrivalBlendCoefficient,
            float stopProgress,
            float reachProgress,
            bool requireFacing,
            bool facingOk)
        {
            if (requireFacing && !facingOk && arrivalBlendCoefficient < 1f)
                stopProgress *= 0.5f;
            return Mathf.Lerp(stopProgress, reachProgress, Mathf.Clamp01(arrivalBlendCoefficient));
        }

        public static bool ShouldAttemptOpen(float gate, float arrivalBlendCoefficient, bool inReach)
        {
            if (gate >= 1f)
                return true;
            return arrivalBlendCoefficient >= 1f && inReach;
        }
    }
}
