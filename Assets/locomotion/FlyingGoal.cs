using UnityEngine;

/// <summary>
/// 3D flying trajectory defined by three AnimationCurves (X, Y, Z) over normalized time t in [0, 1].
/// Use the custom property drawer to edit the curve in the inspector.
/// </summary>
[System.Serializable]
public struct FlyingGoal
{
    [Tooltip("Duration of the goal in seconds (used by PositionAtTime).")]
    public float duration;

    [Tooltip("World position X over normalized time [0,1]. If null, uses 0.")]
    public AnimationCurve curveX;

    [Tooltip("World position Y over normalized time [0,1]. If null, uses linear 0→5.")]
    public AnimationCurve curveY;

    [Tooltip("World position Z over normalized time [0,1]. If null, uses linear 0→10.")]
    public AnimationCurve curveZ;

    /// <summary>
    /// World position at normalized time t in [0, 1].
    /// </summary>
    public Vector3 PositionAt(float t)
    {
        t = Mathf.Clamp01(t);
        float x = curveX != null ? curveX.Evaluate(t) : 0f;
        float y = curveY != null ? curveY.Evaluate(t) : (t * 5f);
        float z = curveZ != null ? curveZ.Evaluate(t) : (t * 10f);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// World position at time t (seconds). Normalizes using duration.
    /// </summary>
    public Vector3 PositionAtTime(float t)
    {
        float nt = duration > 0f ? Mathf.Clamp01(t / duration) : 0f;
        return PositionAt(nt);
    }

    /// <summary>
    /// Create default curves for a simple arc: start at origin, end at (0, 5, 10).
    /// </summary>
    public static AnimationCurve DefaultCurveX()
    {
        return AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    public static AnimationCurve DefaultCurveY()
    {
        return AnimationCurve.Linear(0f, 0f, 1f, 5f);
    }

    public static AnimationCurve DefaultCurveZ()
    {
        return AnimationCurve.Linear(0f, 0f, 1f, 10f);
    }
}
