using UnityEngine;

/// <summary>
/// Optional callback surface for animation behavior trees (or nested procedural drivers)
/// to report playback state to a host implementing <see cref="ISystemDrawerAnimationRegistration"/>.
/// </summary>
public interface IAnimationLayerReporter
{
    /// <summary>Associate this reporter with the registration host (usually called from OnEnable).</summary>
    void RegisterWithHost(ISystemDrawerAnimationRegistration host);

    /// <summary>Report the active node after an evaluation step (normalized time 0..1 if known).</summary>
    void ReportPlaying(BehaviorTreeNode activeNode, float normalizedTime, int layerId);
}
