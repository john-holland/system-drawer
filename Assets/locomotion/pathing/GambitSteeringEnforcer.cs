using UnityEngine;

/// <summary>
/// Blends player/AI input with planned path tangent. 0 = highlight only, 1 = full control.
/// </summary>
public sealed class GambitSteeringEnforcer : MonoBehaviour
{
    [Range(0f, 1f)] public float enforcement01 = 1f;
    public TravelAgent travelAgent;
    public Transform vehicleRoot;

    /// <summary>Blend desired steer direction with planned path direction.</summary>
    public Vector3 BlendSteerDirection(Vector3 playerOrAiDesired)
    {
        float e = Mathf.Clamp01(enforcement01);
        if (e <= 0f || travelAgent == null)
            return playerOrAiDesired.normalized;

        Vector3 pathDir = EstimatePathTangent();
        if (pathDir.sqrMagnitude < 1e-6f)
            return playerOrAiDesired.normalized;

        var blended = Vector3.Slerp(playerOrAiDesired.normalized, pathDir.normalized, e);
        return blended.normalized;
    }

    /// <summary>When enforcement is 1, returns true if caller should ignore player steer.</summary>
    public bool FullControl => enforcement01 >= 0.999f;

    Vector3 EstimatePathTangent()
    {
        var root = vehicleRoot != null ? vehicleRoot : transform;
        // Prefer next waypoint from cached plan gizmos flatten if available.
        if (travelAgent != null && travelAgent.previewGoalWorld != Vector3.zero)
        {
            var toGoal = travelAgent.previewGoalWorld - root.position;
            toGoal.y = 0f;
            if (toGoal.sqrMagnitude > 1e-4f)
                return toGoal.normalized;
        }
        return root.forward;
    }
}
