using UnityEngine;

/// <summary>
/// Utility for catch goals: predict where an incoming object will be so the hand(s) can intercept it.
/// Uses Rigidbody on the object when present for prediction; otherwise treats target as static.
/// </summary>
public static class CatchTrajectoryUtility
{
    /// <summary>
    /// Get position where the hand can intercept the incoming object, and time to that intercept.
    /// If the object has a Rigidbody, uses its velocity for linear prediction; otherwise treats it as static.
    /// </summary>
    /// <param name="handPos">Current hand (or catch limb) position.</param>
    /// <param name="incomingObject">Transform of the object to catch (may have Rigidbody).</param>
    /// <param name="handSpeed">Approximate hand reach speed (m/s).</param>
    /// <param name="interceptPos">Output: position to move the hand to for intercept.</param>
    /// <param name="timeToIntercept">Output: estimated time until intercept.</param>
    public static void GetInterceptPosition(
        Vector3 handPos,
        Transform incomingObject,
        float handSpeed,
        out Vector3 interceptPos,
        out float timeToIntercept)
    {
        interceptPos = incomingObject != null ? incomingObject.position : handPos;
        timeToIntercept = 0f;
        if (incomingObject == null || handSpeed <= 0f) return;

        HitTrajectoryUtility.ComputeIntercept(handPos, incomingObject, handSpeed, out interceptPos, out timeToIntercept);
    }

    /// <summary>
    /// Get intercept position using speed from a limb component (e.g. RagdollBodyPart).
    /// </summary>
    public static void GetInterceptPosition(
        Vector3 handPos,
        Transform incomingObject,
        Component limb,
        float fallbackHandSpeed,
        out Vector3 interceptPos,
        out float timeToIntercept)
    {
        float speed = HitTrajectoryUtility.GetLimbSpeed(limb, fallbackHandSpeed);
        GetInterceptPosition(handPos, incomingObject, speed, out interceptPos, out timeToIntercept);
    }
}
