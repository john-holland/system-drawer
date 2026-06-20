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
/// Layer weight and playback direction control implemented by <c>SystemDrawerAnimator</c> (SystemDrawer assembly).
/// Keeps travel nodes in Locomotion.Runtime without an asmdef cycle.
/// </summary>
public interface ISystemDrawerLayerControl : IAnimationSetManagerDeferral
{
    void SetLayerWeight(int layerIndex, float weight);
    float GetLayerWeight(int layerIndex);
    void SetLayerPlayDirection(int layerIndex, int direction);
    void SetGlobalPlayDirection(int direction);
    void SetLayerPlaybackMode(int layerIndex, AnimationLayerPlaybackMode mode);
    AnimationLayerPlaybackMode GetLayerPlaybackMode(int layerIndex);
    void SetPlaybackModeForBehaviorTree(AnimationBehaviorTree tree, AnimationLayerPlaybackMode mode);
}

/// <summary>Find <see cref="ISystemDrawerLayerControl"/> without referencing the SystemDrawer assembly.</summary>
public static class SystemDrawerLayerControlLookup
{
    public static ISystemDrawerLayerControl FindInChildren(Component root, bool includeInactive = true)
    {
        if (root == null)
            return null;

        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISystemDrawerLayerControl control)
                return control;
        }

        return null;
    }

    public static ISystemDrawerLayerControl FromComponent(Component component) =>
        component as ISystemDrawerLayerControl;
}

/// <summary>
/// Registration surface for <see cref="AnimationBehaviorTree"/> / <see cref="IAnimationLayerReporter"/>.
/// </summary>
public interface ISystemDrawerAnimationRegistration
{
    void RegisterAnimationBehaviorTree(AnimationBehaviorTree abt);

    void NotifyReporterPlayback(AnimationBehaviorTree tree, BehaviorTreeNode activeNode, float normalizedTime, int layerId);
}
