using UnityEngine;

/// <summary>
/// Utility for hit goals: predict target position/velocity at a future time so the weapon limb (hand + optional tool)
/// can be driven to meet the target. Used with baked IK tuning to compute limb end-effector trajectory for intercept.
/// </summary>
public static class HitTrajectoryUtility
{
    /// <summary>
    /// Get target position and velocity at time t (linear prediction from current state).
    /// If target has a Rigidbody, uses its position and velocity; otherwise position only, velocity = 0.
    /// </summary>
    public static void GetTargetStateAt(Transform target, float t, out Vector3 position, out Vector3 velocity)
    {
        if (target == null)
        {
            position = Vector3.zero;
            velocity = Vector3.zero;
            return;
        }
        position = target.position;
        velocity = Vector3.zero;
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            velocity = rb.linearVelocity;
            position = position + velocity * t;
        }
    }

    /// <summary>
    /// Get target position at a future time t (current position + velocity * t when Rigidbody present).
    /// </summary>
    public static Vector3 GetPredictedPosition(Transform target, float timeFromNow)
    {
        GetTargetStateAt(target, timeFromNow, out Vector3 pos, out _);
        return pos;
    }

    /// <summary>
    /// Get current target velocity (from Rigidbody if present, else zero).
    /// </summary>
    public static Vector3 GetTargetVelocity(Transform target)
    {
        if (target == null) return Vector3.zero;
        Rigidbody rb = target.GetComponent<Rigidbody>();
        return rb != null ? rb.linearVelocity : Vector3.zero;
    }

    /// <summary>
    /// Get limb speed in m/s from a component that has a Rigidbody (e.g. RagdollBodyPart, or the limb's Transform).
    /// Body parts can expose Speed (e.g. RagdollBodyPart.Speed) for use here. Returns fallback if limb is null or has no Rigidbody.
    /// </summary>
    public static float GetLimbSpeed(Component limb, float fallback = 5f)
    {
        if (limb == null) return fallback;
        var rb = limb.GetComponent<Rigidbody>();
        if (rb == null) return fallback;
        float v = rb.linearVelocity.magnitude;
        return v > 0.01f ? v : fallback;
    }

    /// <summary>
    /// Estimate time for limb to reach a target position from current limb position (distance / approximate speed).
    /// Used to choose impact time T for intercept prediction. approximateLimbSpeed in m/s (e.g. 3–8 for arm strike).
    /// When a limb component is available (e.g. RagdollBodyPart with Speed), use GetLimbSpeed(limb, fallback) and pass the result.
    /// </summary>
    public static float EstimateTimeToReach(Vector3 fromPosition, Vector3 toPosition, float approximateLimbSpeed = 5f)
    {
        if (approximateLimbSpeed <= 0f) approximateLimbSpeed = 5f;
        float d = Vector3.Distance(fromPosition, toPosition);
        return d / approximateLimbSpeed;
    }

    /// <summary>
    /// Estimate time to reach target using speed from a limb component (e.g. RagdollBodyPart). Uses GetLimbSpeed when limb is provided.
    /// </summary>
    public static float EstimateTimeToReach(Vector3 fromPosition, Vector3 toPosition, Component limb, float fallbackSpeed = 5f)
    {
        float speed = GetLimbSpeed(limb, fallbackSpeed);
        return EstimateTimeToReach(fromPosition, toPosition, speed);
    }

    /// <summary>
    /// Compute predicted target position at estimated impact time so limb can be driven to intercept.
    /// Returns the position the target will be at when the limb would reach it (linear prediction).
    /// </summary>
    /// <param name="limbPosition">Current weapon limb (or hand + tool) position.</param>
    /// <param name="target">Target transform (may have Rigidbody for moving target).</param>
    /// <param name="limbSpeed">Approximate limb reach speed (m/s).</param>
    /// <param name="predictedImpactPosition">Output: position to aim the limb at for intercept.</param>
    /// <param name="estimatedTimeToImpact">Output: estimated time until impact.</param>
    public static void ComputeIntercept(
        Vector3 limbPosition,
        Transform target,
        float limbSpeed,
        out Vector3 predictedImpactPosition,
        out float estimatedTimeToImpact)
    {
        predictedImpactPosition = target != null ? target.position : limbPosition;
        estimatedTimeToImpact = 0f;
        if (target == null || limbSpeed <= 0f) return;

        Vector3 vel = GetTargetVelocity(target);
        float t = EstimateTimeToReach(limbPosition, target.position, limbSpeed);
        estimatedTimeToImpact = t;
        predictedImpactPosition = target.position + vel * t;
    }

    /// <summary>
    /// Compute intercept using speed from a limb component (e.g. RagdollBodyPart). Uses GetLimbSpeed when limb is provided.
    /// </summary>
    public static void ComputeIntercept(
        Vector3 limbPosition,
        Transform target,
        Component limb,
        float fallbackLimbSpeed,
        out Vector3 predictedImpactPosition,
        out float estimatedTimeToImpact)
    {
        float speed = GetLimbSpeed(limb, fallbackLimbSpeed);
        ComputeIntercept(limbPosition, target, speed, out predictedImpactPosition, out estimatedTimeToImpact);
    }
}
