using UnityEngine;

/// <summary>
/// Implemented by <c>SystemDrawerAnimator</c> (SystemDrawer assembly) so <see cref="Brain"/> and
/// <see cref="RagdollAnimationSetManager"/> do not reference that assembly (breaks asmdef cycles).
/// </summary>
public interface IBehaviorTreePlaybackGate
{
    bool ManagesBehaviorTree(BehaviorTree tree);
}

/// <summary>
/// Optional deferral hook for <see cref="RagdollAnimationSetManager.Play"/> when a drawer animator owns playback.
/// </summary>
public interface IAnimationSetManagerDeferral
{
    bool ShouldDeferSetManagerPlayback();
}

/// <summary>
/// Registration surface for <see cref="AnimationBehaviorTree"/> / <see cref="IAnimationLayerReporter"/>.
/// </summary>
public interface ISystemDrawerAnimationRegistration
{
    void RegisterAnimationBehaviorTree(AnimationBehaviorTree abt);

    void NotifyReporterPlayback(AnimationBehaviorTree tree, BehaviorTreeNode activeNode, float normalizedTime, int layerId);
}
