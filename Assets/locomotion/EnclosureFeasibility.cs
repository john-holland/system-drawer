using UnityEngine;

/// <summary>
/// Enclosure feasibility evaluation structure for hemispherical grasping.
/// Contains enclosure ratio, grasp point, finger spread, and grip strength requirements.
/// </summary>
[System.Serializable]
public class EnclosureFeasibility
{
    [Header("Feasibility")]
    [Tooltip("Can hand enclose object?")]
    public bool canEnclose;

    [Tooltip("Enclosure ratio (0-1, 0.55+ = good)")]
    [Range(0f, 1f)]
    public float enclosureRatio = 0.5f;

    [Header("Grasp Parameters")]
    [Tooltip("Optimal point to center grasp")]
    public Vector3 optimalGraspPoint;

    [Tooltip("Direction hand should approach from")]
    public Vector3 optimalGraspDirection;

    [Tooltip("Required finger spread angle (degrees)")]
    public float requiredFingerSpread;

    [Tooltip("Minimum grip strength required (Newtons)")]
    public float gripStrengthRequired;

    [Header("Reason")]
    [Tooltip("Reason for feasibility/infeasibility")]
    public string feasibilityReason = "";
}
