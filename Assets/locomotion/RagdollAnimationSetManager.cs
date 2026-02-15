using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores lists of ragdoll animation sets (animation tree, root bones, transition settings)
/// and controls playback (play, pause, stop, reverse, reset) for animation and IK behavior trees on RagdollSystem.
/// </summary>
[AddComponentMenu("Locomotion/Ragdoll Animation Set Manager")]
public class RagdollAnimationSetManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ragdoll system (auto-found on this GameObject or children if null)")]
    public RagdollSystem ragdollSystem;

    [Tooltip("Animation sets: each describes an animation tree, root bones, and transition settings")]
    public List<RagdollAnimationSet> animationSets = new List<RagdollAnimationSet>();

    [Header("Playback Trees")]
    [Tooltip("Behavior tree used for animation playback. Defaults to ragdollSystem.animationTree.generatedTree if null")]
    public BehaviorTree animationBehaviorTree;

    [Tooltip("Optional: second behavior tree for IK-focused playback")]
    public BehaviorTree ikAnimationBehaviorTree;

    // Playback state (read by Brain to gate Execute)
    private bool isPaused;
    private bool isStopped = true;
    private int playDirection = 1;
    private RagdollAnimationSet currentSet;

    /// <summary>True when playback is paused.</summary>
    public bool IsPaused => isPaused;

    /// <summary>True when playback is stopped.</summary>
    public bool IsStopped => isStopped;

    /// <summary>Play direction: 1 = forward, -1 = reverse.</summary>
    public int PlayDirection => playDirection;

    /// <summary>Current animation set being played, or null.</summary>
    public RagdollAnimationSet CurrentSet => currentSet;

    private void Awake()
    {
        if (ragdollSystem == null)
            ragdollSystem = GetComponent<RagdollSystem>();
        if (ragdollSystem == null)
            ragdollSystem = GetComponentInChildren<RagdollSystem>();

        if (animationBehaviorTree == null && ragdollSystem != null && ragdollSystem.animationTree != null)
            animationBehaviorTree = ragdollSystem.animationTree.generatedTree;
    }

    /// <summary>Start playback of the set at the given index.</summary>
    public void Play(int setIndex)
    {
        if (animationSets == null || setIndex < 0 || setIndex >= animationSets.Count)
            return;
        Play(animationSets[setIndex]);
    }

    /// <summary>Start playback of the given set.</summary>
    public void Play(RagdollAnimationSet set)
    {
        if (set == null)
            return;

        currentSet = set;
        isStopped = false;
        isPaused = false;
        playDirection = 1;

        // Optionally switch active tree from set
        if (set.behaviorTreeOverride != null)
            animationBehaviorTree = set.behaviorTreeOverride;
        else if (set.animationTree != null && set.animationTree.generatedTree != null)
            animationBehaviorTree = set.animationTree.generatedTree;

        if (ragdollSystem != null && set.animationTree != null && ragdollSystem.animationTree != set.animationTree)
            ragdollSystem.animationTree = set.animationTree;
    }

    /// <summary>Pause playback of animation (and IK) trees.</summary>
    public void Pause()
    {
        isPaused = true;
        isStopped = false;
    }

    /// <summary>Stop playback and leave trees in a stopped state.</summary>
    public void Stop()
    {
        isPaused = false;
        isStopped = true;
        currentSet = null;
    }

    /// <summary>Set reverse playback direction. Frame-backward logic can be added later.</summary>
    public void Reverse()
    {
        playDirection = playDirection == 1 ? -1 : 1;
    }

    /// <summary>Reset playback state and tree to root; optionally clear current node and invoke OnEnter on root.</summary>
    public void ResetState()
    {
        isPaused = false;
        isStopped = true;
        currentSet = null;
        playDirection = 1;

        ResetTreeToRoot(animationBehaviorTree);
        ResetTreeToRoot(ikAnimationBehaviorTree);
    }

    private static void ResetTreeToRoot(BehaviorTree tree)
    {
        if (tree == null)
            return;

        if (tree.rootNode != null)
        {
            tree.currentNode = tree.rootNode;
            tree.rootNode.OnEnter(tree);
        }
    }
}
