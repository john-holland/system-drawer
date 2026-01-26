using UnityEngine;

/// <summary>
/// ScriptableObject asset for storing AnimationCurves.
/// Used for prefab saving and Blender export workflows.
/// </summary>
[CreateAssetMenu(fileName = "New Animation Curve", menuName = "Locomotion/Animation Curve Asset")]
public class AnimationCurveAsset : ScriptableObject
{
    [Header("Curve Data")]
    [Tooltip("The stored curve")]
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Metadata")]
    [Tooltip("Description of the curve's purpose")]
    [TextArea(3, 5)]
    public string description = "";

    [Tooltip("Frame range this curve applies to")]
    public Vector2Int frameRange = new Vector2Int(0, 0);

    private void OnEnable()
    {
        // Initialize curve if null
        if (curve == null || curve.keys.Length == 0)
        {
            curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
