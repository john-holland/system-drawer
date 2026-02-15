using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transition settings for blending between animation sets or into a set.
/// </summary>
[System.Serializable]
public class RagdollAnimationTransitionSettings
{
    [Tooltip("Blend duration in seconds")]
    public float blendDuration = 0.25f;

    [Tooltip("Blend curve (default ease-in-out)")]
    public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How to blend procedural vs keyframe animation during transition")]
    public AnimationBlendMode blendMode = AnimationBlendMode.PartialRagdoll;

    public static RagdollAnimationTransitionSettings Default()
    {
        return new RagdollAnimationTransitionSettings
        {
            blendDuration = 0.25f,
            blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
            blendMode = AnimationBlendMode.PartialRagdoll
        };
    }
}

/// <summary>
/// Describes one ragdoll animation set: animation tree, root bones for application, and transition settings.
/// </summary>
[System.Serializable]
public class RagdollAnimationSet
{
    [Tooltip("Display name for inspector and debug")]
    public string displayName = "Animation Set";

    [Tooltip("Animation behavior tree (clip-to-tree) for this set")]
    public AnimationBehaviorTree animationTree;

    [Tooltip("Root bones for animation application (e.g. pelvis, feet). Empty = use RagdollSystem.ragdollRoot")]
    public List<Transform> rootBones = new List<Transform>();

    [Tooltip("Transition settings when blending into or out of this set")]
    public RagdollAnimationTransitionSettings transitionSettings = new RagdollAnimationTransitionSettings();

    [Tooltip("Optional: behavior tree override when this set uses a different generated tree than animationTree.generatedTree")]
    public BehaviorTree behaviorTreeOverride;
}
