using UnityEngine;

/// <summary>
/// Parabolic throw trajectory (no air resistance). Computes feasibility, initial velocity, and time of flight
/// for a throw from origin to target. Used to gate throw segments and drive animation choice.
/// </summary>
public static class ThrowTrajectoryUtility
{
    /// <summary>
    /// Result of a trajectory calculation.
    /// </summary>
    public struct TrajectoryResult
    {
        public bool feasible;
        public Vector3 initialVelocity;
        public float timeOfFlight;
        public float distance;
    }

    /// <summary>
    /// Compute whether a throw from origin to target is feasible and get initial velocity and time of flight.
    /// Uses parabolic model: v0 = displacement/time - 0.5*gravity*time.
    /// </summary>
    /// <param name="origin">Release point (e.g. hand position).</param>
    /// <param name="target">World position to hit.</param>
    /// <param name="gravity">Defaults to Physics.gravity.</param>
    /// <param name="maxLaunchSpeed">If positive, feasibility is false when required speed exceeds this.</param>
    /// <returns>Feasibility, initial velocity, time of flight, and horizontal distance.</returns>
    public static TrajectoryResult Compute(Vector3 origin, Vector3 target, Vector3? gravity = null, float maxLaunchSpeed = 0f)
    {
        Vector3 g = gravity ?? Physics.gravity;
        Vector3 displacement = target - origin;
        float horizontalDist = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(target.x, 0f, target.z));
        float heightDiff = target.y - origin.y;

        float time = EstimateTimeToTarget(origin, target, g);
        if (time < 0.001f)
            time = 0.5f;

        Vector3 initialVelocity = displacement / time - 0.5f * g * time;
        float speed = initialVelocity.magnitude;

        bool feasible = true;
        if (maxLaunchSpeed > 0f && speed > maxLaunchSpeed)
            feasible = false;

        return new TrajectoryResult
        {
            feasible = feasible,
            initialVelocity = initialVelocity,
            timeOfFlight = time,
            distance = horizontalDist
        };
    }

    /// <summary>
    /// Estimate time to reach target (parabolic). Uses vertical and horizontal motion heuristic.
    /// </summary>
    public static float EstimateTimeToTarget(Vector3 origin, Vector3 target, Vector3 gravity)
    {
        float heightDiff = target.y - origin.y;
        float horizontalDist = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(target.x, 0f, target.z));

        float gy = Mathf.Abs(gravity.y);
        if (gy < 0.001f)
            gy = 9.81f;

        float timeFromVertical = Mathf.Sqrt(2f * Mathf.Abs(heightDiff) / gy);
        if (timeFromVertical < 0.1f)
            timeFromVertical = 0.1f;

        if (horizontalDist < 0.001f)
            return timeFromVertical;

        float minTime = Mathf.Max(timeFromVertical, horizontalDist / 20f);
        return minTime;
    }

    /// <summary>
    /// Check if target is in range for the given card (throwMinRange / throwMaxRange) and trajectory is feasible.
    /// </summary>
    public static bool IsInRangeAndFeasible(GoodSection card, Vector3 origin, Vector3 target, Vector3? gravity = null, float maxLaunchSpeed = 0f)
    {
        if (card == null)
            return false;
        TrajectoryResult r = Compute(origin, target, gravity, maxLaunchSpeed);
        if (!r.feasible)
            return false;
        if (card.throwMaxRange > 0f && r.distance > card.throwMaxRange)
            return false;
        if (card.throwMinRange > 0f && r.distance < card.throwMinRange)
            return false;
        return true;
    }

    /// <summary>
    /// Optional: max range at target height for a given launch speed (in direction of forward).
    /// For UI/debug. Uses flat ground assumption (origin.y, target at same height).
    /// </summary>
    public static float MaxRangeAtSpeed(float launchSpeed, Vector3 gravity, float targetHeight = 0f)
    {
        float g = Mathf.Abs(gravity.y);
        if (g < 0.001f)
            g = 9.81f;
        return (launchSpeed * launchSpeed) / g;
    }
}
