using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Shared ground / fallen probes for ragdoll get-up BehaviorTree nodes.
/// </summary>
public static class RagdollGroundCheck
{
    public const float DefaultGroundProbeDistance = 0.35f;
    public const float DefaultProbeLift = 0.05f;
    public const float DefaultUprightDotThreshold = 0.5f;

    public static Transform ResolvePelvisOrRoot(RagdollSystem system)
    {
        if (system == null)
            return null;

        RagdollPelvis pelvis = system.pelvisComponent;
        if (pelvis != null)
        {
            Transform t = pelvis.PrimaryBoneTransform;
            if (t != null)
                return t;
        }

        if (system.ragdollRoot != null)
            return system.ragdollRoot;

        return system.transform;
    }

    public static Vector3 ResolveProbeOrigin(RagdollSystem system)
    {
        Transform t = ResolvePelvisOrRoot(system);
        return t != null ? t.position : Vector3.zero;
    }

    /// <summary>Raycast down from pelvis/root for nearby support.</summary>
    public static bool IsOnGround(
        RagdollSystem system,
        LayerMask layers,
        float distance = DefaultGroundProbeDistance,
        float probeLift = DefaultProbeLift)
    {
        if (system == null)
            return false;

        Vector3 origin = ResolveProbeOrigin(system) + Vector3.up * Mathf.Max(0f, probeLift);
        float d = Mathf.Max(0.01f, distance);
        return Physics.Raycast(origin, Vector3.down, d, layers, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// True when pelvis/root up-dot vs world up is below <paramref name="uprightDotThreshold"/>
    /// (torso nearly horizontal / fallen).
    /// </summary>
    public static bool IsFallen(RagdollSystem system, float uprightDotThreshold = DefaultUprightDotThreshold)
    {
        Transform t = ResolvePelvisOrRoot(system);
        if (t == null)
            return false;

        float dot = Vector3.Dot(t.up, Vector3.up);
        return dot < uprightDotThreshold;
    }

    public static bool IsOnGroundAndFallen(
        RagdollSystem system,
        LayerMask layers,
        float groundDistance = DefaultGroundProbeDistance,
        float uprightDotThreshold = DefaultUprightDotThreshold,
        float probeLift = DefaultProbeLift)
    {
        return IsOnGround(system, layers, groundDistance, probeLift)
               && IsFallen(system, uprightDotThreshold);
    }
}
