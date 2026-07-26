using System.Collections.Generic;
using UnityEngine;

public enum RagdollSectionStrength
{
    Vulnerable,
    Strong
}

/// <summary>
/// Marks a ragdoll section as vulnerable or strong with a child control-point Transform.
/// </summary>
[AddComponentMenu("Locomotion/Stunt/Ragdoll Section Strength Marker")]
public sealed class RagdollSectionStrengthMarker : MonoBehaviour
{
    public RagdollSectionStrength strength = RagdollSectionStrength.Vulnerable;
    [Tooltip("Child control point used during physics contact sampling.")]
    public Transform controlPoint;
    [Range(0.05f, 1.5f)] public float influenceRadius = 0.35f;
    [Range(0f, 2f)] public float capsuleWeight = 1f;

    void Reset()
    {
        EnsureControlPoint();
    }

    void OnValidate()
    {
        EnsureControlPoint();
    }

    public void EnsureControlPoint()
    {
        if (controlPoint != null) return;
        Transform child = transform.Find("ControlPoint");
        if (child == null)
        {
            var go = new GameObject("ControlPoint");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }
        controlPoint = child;
    }

    public Vector3 SamplePoint => controlPoint != null ? controlPoint.position : transform.position;

    public static RagdollSectionStrengthMarker[] Collect(GameObject actor)
    {
        if (actor == null) return System.Array.Empty<RagdollSectionStrengthMarker>();
        return actor.GetComponentsInChildren<RagdollSectionStrengthMarker>(true);
    }

    public static bool HasStrongLead(GameObject actor)
    {
        var markers = Collect(actor);
        for (int i = 0; i < markers.Length; i++)
            if (markers[i] != null && markers[i].strength == RagdollSectionStrength.Strong)
                return true;
        return false;
    }

    /// <summary>0–1 damage bias: high when contact near vulnerable, low near strong.</summary>
    public static float EstimateDamageBias(GameObject actor, Vector3 contactWorld)
    {
        var markers = Collect(actor);
        if (markers.Length == 0) return 0.15f;
        float vuln = 0f;
        float strong = 0f;
        float wSum = 0f;
        for (int i = 0; i < markers.Length; i++)
        {
            var m = markers[i];
            if (m == null) continue;
            float d = Vector3.Distance(contactWorld, m.SamplePoint);
            float w = m.capsuleWeight * Mathf.Clamp01(1f - d / Mathf.Max(0.05f, m.influenceRadius));
            if (w <= 0f) continue;
            wSum += w;
            if (m.strength == RagdollSectionStrength.Vulnerable) vuln += w;
            else strong += w;
        }
        if (wSum <= 1e-5f) return 0.15f;
        return Mathf.Clamp01((vuln - strong * 0.5f) / wSum);
    }

    /// <summary>Capsule CoG weight bias for tip/fall (vulnerable pulls risk up).</summary>
    public static float CapsuleWeightBias(GameObject actor)
    {
        var markers = Collect(actor);
        float bias = 1f;
        for (int i = 0; i < markers.Length; i++)
        {
            var m = markers[i];
            if (m == null) continue;
            bias += m.strength == RagdollSectionStrength.Vulnerable
                ? 0.08f * m.capsuleWeight
                : -0.05f * m.capsuleWeight;
        }
        return Mathf.Clamp(bias, 0.5f, 1.5f);
    }
}
