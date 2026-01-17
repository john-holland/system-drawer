using UnityEngine;

/// <summary>
/// Tool information structure for tracking tools in the nervous system.
/// </summary>
[System.Serializable]
public class ToolInfo
{
    [Tooltip("Tool GameObject")]
    public GameObject gameObject;

    [Tooltip("Tool name/identifier")]
    public string toolName;

    [Tooltip("Original position where tool came from (for cleanup)")]
    public Vector3 originalPosition;

    [Tooltip("Usefulness score (0-1)")]
    [Range(0f, 1f)]
    public float usefulness = 0.5f;

    [Tooltip("Accessibility score (0-1)")]
    [Range(0f, 1f)]
    public float accessibility = 0.5f;
}
