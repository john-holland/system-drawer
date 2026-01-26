using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manual frame mapping override system with curve asset support.
/// Allows manual adjustment of specific frame ranges in animation-to-behavior-tree conversion.
/// </summary>
[System.Serializable]
public class BreakoutCurve
{
    [Header("Frame Range")]
    [Tooltip("Start and end frame indices")]
    public Vector2Int frameRange = new Vector2Int(0, 0);

    [Header("Mapping")]
    [Tooltip("Custom mapping curve")]
    public AnimationCurve mappingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Reference to curve asset (for prefab saving/Blender export)")]
    public AnimationCurveAsset curveAsset;

    [Tooltip("Use this instead of auto-interpolation")]
    public bool overrideInterpolation = false;

    [Header("Target Frames")]
    [Tooltip("Specific target frame indices")]
    public List<int> targetFrames = new List<int>();

    [Header("Visualization")]
    [Tooltip("Color for timeline/graph visualization")]
    public Color visualizationColor = new Color(1f, 0.5f, 0f, 1f); // Orange

    /// <summary>
    /// Apply manual mapping to frames.
    /// </summary>
    public void ApplyToFrames(List<AnimationFrame> frames)
    {
        if (frames == null || frames.Count == 0)
            return;

        if (overrideInterpolation && mappingCurve != null && mappingCurve.keys.Length > 0)
        {
            // Apply curve-based mapping
            int startFrame = frameRange.x;
            int endFrame = frameRange.y;
            int rangeLength = endFrame - startFrame;

            if (rangeLength <= 0)
                return;

            for (int i = 0; i < frames.Count; i++)
            {
                AnimationFrame frame = frames[i];
                if (frame.frameIndex >= startFrame && frame.frameIndex <= endFrame)
                {
                    // Normalize frame index to 0-1 range
                    float normalizedIndex = (float)(frame.frameIndex - startFrame) / rangeLength;
                    float curveValue = mappingCurve.Evaluate(normalizedIndex);

                    // Apply curve value to frame (example: adjust time or other properties)
                    // This is a placeholder - actual implementation depends on what needs to be adjusted
                }
            }
        }
        else if (targetFrames != null && targetFrames.Count > 0)
        {
            // Apply specific target frame mapping
            // This would map frames to specific target indices
            // Implementation depends on specific use case
        }
    }

    /// <summary>
    /// Create AnimationCurveAsset for this curve.
    /// </summary>
    public AnimationCurveAsset CreateCurveAsset()
    {
        if (mappingCurve == null || mappingCurve.keys.Length == 0)
            return null;

        AnimationCurveAsset asset = ScriptableObject.CreateInstance<AnimationCurveAsset>();
        asset.curve = new AnimationCurve(mappingCurve.keys);
        asset.frameRange = frameRange;
        asset.description = $"Breakout curve for frames {frameRange.x}-{frameRange.y}";

        curveAsset = asset;
        return asset;
    }

    /// <summary>
    /// Load curve from asset.
    /// </summary>
    public void LoadFromCurveAsset(AnimationCurveAsset asset)
    {
        if (asset == null)
            return;

        curveAsset = asset;
        if (asset.curve != null && asset.curve.keys.Length > 0)
        {
            mappingCurve = new AnimationCurve(asset.curve.keys);
        }
        if (asset.frameRange != Vector2Int.zero)
        {
            frameRange = asset.frameRange;
        }
    }

    /// <summary>
    /// Check if this breakout curve applies to a specific frame.
    /// </summary>
    public bool AppliesToFrame(int frameIndex)
    {
        return frameIndex >= frameRange.x && frameIndex <= frameRange.y;
    }
}
