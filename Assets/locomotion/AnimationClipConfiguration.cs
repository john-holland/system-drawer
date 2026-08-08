using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-clip configuration for animation-to-behavior-tree conversion.
/// Holds one AnimationClip plus its frame sampling, breakout curves, attenuation, and tool usage settings.
/// Named ABTClipConfig to avoid conflict with Locomotion.Audio.AnimationClipConfiguration.
/// </summary>
[System.Serializable]
public class ABTClipConfig
{
    [Tooltip("Source animation clip")]
    public AnimationClip clip;

    [Tooltip("Display name (e.g. clip name, user-editable)")]
    public string displayName;

    [Header("Training")]
    [Tooltip("Initial pose mode for IK training (e.g. FirstFrame = use this clip's first frame).")]
    public IKTrainingInitialPoseMode initialPoseMode = IKTrainingInitialPoseMode.FirstFrame;

    [Tooltip("Test category for IK training runs (Locomotion, ToolUse, Throw, etc.).")]
    public PhysicsIKTrainingCategory testCategory = PhysicsIKTrainingCategory.Locomotion;

    [Header("Frame Sampling")]
    [Tooltip("Sample every Nth frame (default: 1 = every frame)")]
    public int frameSamplingRate = 1;

    [Tooltip("Use only keyframes if true")]
    public bool useKeyframesOnly = false;

    [Header("Interpolation")]
    [Tooltip("Interpolation mode")]
    public InterpolationMode interpolationMode = InterpolationMode.Linear;

    [Header("Breakout Curves")]
    [Tooltip("Manual frame mapping overrides")]
    public List<BreakoutCurve> breakoutCurves = new List<BreakoutCurve>();

    [Header("Attenuation")]
    [Tooltip("Animation attenuation settings")]
    public AttenuationProperties attenuationProperties = new AttenuationProperties();

    [Header("Tool Usage")]
    [Tooltip("Goals for tool usage (shortcuts for animations requiring tools)")]
    public List<BehaviorTreeGoal> toolUsageGoals = new List<BehaviorTreeGoal>();

    [Header("Dropped Frames")]
    [Tooltip("Frames that were dropped/trimmed (for recovery)")]
    public List<AnimationFrame> droppedFrames = new List<AnimationFrame>();

    [Header("Playback policy")]
    [Tooltip("When true, prefer Non-IK kinematic sampling for this clip.")]
    public bool nonIkAnimation;

    [Header("Land Animation Prep")]
    [Tooltip("Landing goal + impact curve for parkour land BT IK (used when testCategory is a landing category).")]
    public LandAnimationPrep landPrep = new LandAnimationPrep();

    /// <summary>
    /// Create a configuration with default values from the given clip.
    /// </summary>
    public static ABTClipConfig FromClip(AnimationClip clip)
    {
        var config = new ABTClipConfig
        {
            clip = clip,
            displayName = clip != null ? clip.name : "New Clip",
            frameSamplingRate = 1,
            useKeyframesOnly = false,
            interpolationMode = InterpolationMode.Linear,
            landPrep = new LandAnimationPrep()
        };
        return config;
    }
}
