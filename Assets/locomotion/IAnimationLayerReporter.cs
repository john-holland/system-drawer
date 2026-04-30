using UnityEngine;

/// <summary>
/// Optional callback surface for animation behavior trees (or nested procedural drivers)
/// to report playback state to <see cref="SystemDrawerAnimator"/> for snapshots and play-order checks.
/// </summary>
public interface IAnimationLayerReporter
{
    /// <summary>Associate this reporter with the animator (usually called from OnEnable).</summary>
    void RegisterWithAnimator(SystemDrawerAnimator animator);

    /// <summary>Report the active node after an evaluation step (normalized time 0..1 if known).</summary>
    void ReportPlaying(BehaviorTreeNode activeNode, float normalizedTime, int layerId);
}
