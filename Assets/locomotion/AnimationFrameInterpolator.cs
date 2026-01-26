using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interpolates animation frames to behavior tree nodes.
/// Handles frame interpolation and breakout curve application.
/// </summary>
public static class AnimationFrameInterpolator
{
    /// <summary>
    /// Interpolate frames to target node count.
    /// </summary>
    public static List<AnimationFrame> InterpolateFrames(List<AnimationFrame> frames, int targetNodeCount)
    {
        if (frames == null || frames.Count == 0)
            return new List<AnimationFrame>();

        if (targetNodeCount <= 0)
            return new List<AnimationFrame>(frames);

        // If we already have the right number, return as-is
        if (frames.Count == targetNodeCount)
            return new List<AnimationFrame>(frames);

        List<AnimationFrame> interpolatedFrames = new List<AnimationFrame>();

        // Calculate step size
        float step = (float)(frames.Count - 1) / (targetNodeCount - 1);

        for (int i = 0; i < targetNodeCount; i++)
        {
            float index = i * step;
            int lowerIndex = Mathf.FloorToInt(index);
            int upperIndex = Mathf.CeilToInt(index);
            float t = index - lowerIndex;

            // Clamp indices
            lowerIndex = Mathf.Clamp(lowerIndex, 0, frames.Count - 1);
            upperIndex = Mathf.Clamp(upperIndex, 0, frames.Count - 1);

            AnimationFrame interpolatedFrame;
            if (lowerIndex == upperIndex || t < 0.001f)
            {
                // Use exact frame
                interpolatedFrame = frames[lowerIndex].Copy();
                interpolatedFrame.frameIndex = i;
            }
            else
            {
                // Interpolate between frames
                interpolatedFrame = InterpolateFrame(frames[lowerIndex], frames[upperIndex], t);
                interpolatedFrame.frameIndex = i;
            }

            interpolatedFrames.Add(interpolatedFrame);
        }

        return interpolatedFrames;
    }

    /// <summary>
    /// Interpolate between two frames.
    /// </summary>
    public static AnimationFrame InterpolateFrame(AnimationFrame frame1, AnimationFrame frame2, float t)
    {
        if (frame1 == null || frame2 == null)
            return null;

        t = Mathf.Clamp01(t);

        AnimationFrame interpolated = new AnimationFrame
        {
            frameIndex = Mathf.RoundToInt(Mathf.Lerp(frame1.frameIndex, frame2.frameIndex, t)),
            time = Mathf.Lerp(frame1.time, frame2.time, t),
            rootMotion = Vector3.Lerp(frame1.rootMotion, frame2.rootMotion, t),
            rootRotation = Quaternion.Lerp(frame1.rootRotation, frame2.rootRotation, t),
            requiresTool = frame1.requiresTool || frame2.requiresTool, // If either requires tool, interpolated does too
            detectedTool = frame1.detectedTool ?? frame2.detectedTool,
            isDropped = false // Interpolated frames are never dropped
        };

        // Interpolate bone transforms
        HashSet<string> allBoneNames = new HashSet<string>();
        if (frame1.boneTransforms != null)
        {
            foreach (var boneName in frame1.boneTransforms.Keys)
            {
                allBoneNames.Add(boneName);
            }
        }
        if (frame2.boneTransforms != null)
        {
            foreach (var boneName in frame2.boneTransforms.Keys)
            {
                allBoneNames.Add(boneName);
            }
        }

        interpolated.boneTransforms = new Dictionary<string, TransformData>();
        foreach (string boneName in allBoneNames)
        {
            bool hasFrame1 = frame1.boneTransforms != null && frame1.boneTransforms.TryGetValue(boneName, out TransformData transform1);
            bool hasFrame2 = frame2.boneTransforms != null && frame2.boneTransforms.TryGetValue(boneName, out TransformData transform2);

            TransformData interpolatedTransform;
            if (hasFrame1 && hasFrame2)
            {
                // Interpolate between both
                interpolatedTransform = new TransformData(
                    Vector3.Lerp(transform1.position, transform2.position, t),
                    Quaternion.Lerp(transform1.rotation, transform2.rotation, t),
                    Vector3.Lerp(transform1.scale, transform2.scale, t)
                );
            }
            else if (hasFrame1)
            {
                // Use frame1 only
                interpolatedTransform = transform1;
            }
            else if (hasFrame2)
            {
                // Use frame2 only
                interpolatedTransform = transform2;
            }
            else
            {
                continue; // Skip if neither has this bone
            }

            interpolated.boneTransforms[boneName] = interpolatedTransform;
        }

        return interpolated;
    }

    /// <summary>
    /// Apply breakout curves to frames.
    /// </summary>
    public static void ApplyBreakoutCurves(List<AnimationFrame> frames, List<BreakoutCurve> curves)
    {
        if (frames == null || frames.Count == 0 || curves == null || curves.Count == 0)
            return;

        foreach (var curve in curves)
        {
            if (curve != null && curve.overrideInterpolation)
            {
                curve.ApplyToFrames(frames);
            }
        }
    }
}
