using UnityEngine;

/// <summary>
/// Tipping viability evaluation structure.
/// Contains direction, viability score, torque ratio, and stability information.
/// </summary>
[System.Serializable]
public class TippingViability
{
    [Tooltip("Direction to tip")]
    public Vector3 direction;

    [Tooltip("Viability score (0-1)")]
    [Range(0f, 1f)]
    public float viability = 0.5f;

    [Tooltip("Torque ratio (applied torque / weight)")]
    public float torqueRatio;

    [Tooltip("Contact point where force should be applied")]
    public Vector3 contactPoint;

    [Tooltip("Will object stabilize in new position?")]
    public bool isStable;

    [Tooltip("Reason for viability/infeasibility")]
    public string reason = "";
}
