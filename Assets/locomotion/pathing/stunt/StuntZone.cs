using System.Collections.Generic;
using UnityEngine;

public enum StuntZoneKind
{
    Runway,
    Terminus,
    Both
}

/// <summary>
/// At-speed stunt volume: runway approach and/or terminus landing requiring parkour IK/anim.
/// </summary>
[AddComponentMenu("Locomotion/Stunt/Stunt Zone")]
public sealed class StuntZone : MonoBehaviour
{
    public StuntZoneKind kind = StuntZoneKind.Both;
    [Min(0.5f)] public float lengthMeters = 6f;
    [Min(0.5f)] public float widthMeters = 2f;
    [Range(0f, 1f)] public float requiredEntrySpeed01 = 0.55f;
    [Tooltip("Animation group tags allowed when entering at speed (see ParkourAnimationGroup).")]
    public List<string> allowAnimations = new List<string>();
    [Tooltip("Optional link to a PathingAperture for crash/pass-through.")]
    public PathingAperture linkedAperture;

    public Vector3 Center => transform.position;
    public Vector3 Forward => transform.forward;
    public Vector3 RunwayStart => transform.position - transform.forward * (lengthMeters * 0.5f);
    public Vector3 RunwayEnd => transform.position + transform.forward * (lengthMeters * 0.5f);

    public bool IsRunway => kind == StuntZoneKind.Runway || kind == StuntZoneKind.Both;
    public bool IsTerminus => kind == StuntZoneKind.Terminus || kind == StuntZoneKind.Both;

    public bool AllowsAnimation(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return true;
        if (allowAnimations == null || allowAnimations.Count == 0) return true;
        for (int i = 0; i < allowAnimations.Count; i++)
            if (string.Equals(allowAnimations[i], tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>True when approach length can reach required entry speed from rest (heuristic).</summary>
    public bool HasAdequateRunwayForSpeed(float approachArcMeters)
    {
        if (!IsRunway) return true;
        float need = Mathf.Lerp(2f, lengthMeters, requiredEntrySpeed01);
        return approachArcMeters + 1e-3f >= need * 0.5f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = kind == StuntZoneKind.Terminus
            ? new Color(1f, 0.4f, 0.2f, 0.65f)
            : new Color(0.2f, 1f, 0.45f, 0.65f);
        Matrix4x4 m = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = m;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(widthMeters, 0.2f, lengthMeters));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(RunwayStart, RunwayEnd);
    }
}
