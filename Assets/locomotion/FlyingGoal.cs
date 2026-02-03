using UnityEngine;

/// <summary>
/// Sin-wave flying goal (Flappy Bird style): target trajectory y(t) = baseY + amplitude * sin(frequency * t).
/// Optional XZ path for horizontal movement.
/// </summary>
[System.Serializable]
public struct FlyingGoal
{
    [Tooltip("Base Y (world) for the sin wave.")]
    public float baseY;

    [Tooltip("Amplitude of vertical oscillation (meters).")]
    public float amplitude;

    [Tooltip("Angular frequency (rad/s) for sin(omega * t).")]
    public float frequency;

    [Tooltip("Duration of the goal (seconds).")]
    public float duration;

    [Tooltip("Start position (XZ); if zero, use agent position.")]
    public Vector3 startXZ;

    [Tooltip("End position (XZ); if zero, no horizontal movement.")]
    public Vector3 endXZ;

    /// <summary>
    /// World position at normalized time t in [0, 1].
    /// </summary>
    public Vector3 PositionAt(float t)
    {
        float y = baseY + amplitude * Mathf.Sin(frequency * t * duration * Mathf.PI * 2f);
        float x = Mathf.Lerp(startXZ.x, endXZ.x, t);
        float z = Mathf.Lerp(startXZ.z, endXZ.z, t);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// World position at time t (seconds).
    /// </summary>
    public Vector3 PositionAtTime(float t)
    {
        float nt = duration > 0f ? Mathf.Clamp01(t / duration) : 0f;
        return PositionAt(nt);
    }
}
