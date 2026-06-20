using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How an animation layer drives the ragdoll.</summary>
public enum AnimationLayerPlaybackMode
{
    PhysicsCards,
    NonIkKinematic
}

/// <summary>
/// One logical animation layer: an <see cref="AnimationBehaviorTree"/>, sort key, weight,
/// and optional additive muscle groups (Option A: base layer full body; others scale muscle activation).
/// </summary>
[Serializable]
public class AnimationLayerSlot
{
    [Tooltip("Source animation behavior tree for this layer.")]
    public AnimationBehaviorTree animationBehaviorTree;

    [Tooltip("Stable layer index for ordering, weights, and API (0 = typically base).")]
    public int layerIndex;

    [Range(0f, 1f)]
    [Tooltip("Blend weight for this layer. Layers with weight ~0 are skipped by TickLayers.")]
    public float weight = 1f;

    [Tooltip("Optional: muscle groups to drive at scaled activation when this layer is additive (Option A). Empty = full-body influence via behavior tree only.")]
    public List<string> additiveMuscleGroups = new List<string>();

    [Tooltip("Optional display override in inspector / overlay.")]
    public string displayName;

    [Tooltip("1 = forward, -1 = reverse playback for this layer.")]
    public int playDirection = 1;

    [Tooltip("PhysicsCards = behavior-tree muscle cards; NonIkKinematic = clip sampling via NonIkRagdollAnimator.")]
    public AnimationLayerPlaybackMode playbackMode = AnimationLayerPlaybackMode.PhysicsCards;
}
