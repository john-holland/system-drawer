using System;
using System.Collections.Generic;
using Locomotion.Narrative;

/// <summary>Applies resolved Non-IK policy to system-drawer animation layers.</summary>
public static class AnimationPlaybackPolicyApplicator
{
    public static bool ResolveForTravelContext(
        TravelExecutionContext ctx,
        RagdollAnimationSet set,
        ABTClipConfig clipConfig)
    {
        if (ctx == null)
            return false;

        AnimationPlaybackPolicyContext policy = ctx.policyContext;
        if (policy != null && !string.IsNullOrEmpty(ctx.activePhrase))
            policy.activePhrase = ctx.activePhrase;
        if (policy != null && ctx.segmentIndex >= 0)
            policy.activeEventIndex = ctx.segmentIndex;

        if (policy != null)
        {
            ScriptPlaybackCursor.Update(
                policy.GetActiveScriptText(),
                ctx.activePhrase,
                ctx.segmentIndex);
        }

        bool travelPrefer = ctx.travelAgent != null && ctx.travelAgent.preferNonIkPlayback;

        if (policy != null &&
            policy.TryGetLemmaBoolForActivePhrase(AnimationPlaybackPolicyResolver.NonIkAnimationKey, out bool fromLemma))
            return fromLemma;

        IReadOnlyList<PromptSegment> segments = policy != null
            ? policy.GetSegmentsForActivePhrase()
            : Array.Empty<PromptSegment>();
        IReadOnlyList<LocalizationClauseBindingRecord> bindings = policy != null
            ? policy.GetBindingsForActivePhrase()
            : null;

        return AnimationPlaybackPolicyResolver.ResolveNonIkForActivePhrase(
            ctx.activePhrase,
            segments,
            bindings,
            clipConfig,
            set,
            travelPrefer);
    }

    public static void ApplyToAnimatorLayers(
        ISystemDrawerLayerControl layerCtrl,
        RagdollAnimationSet set,
        bool preferNonIk,
        TravelLegModeLayerMap layerMap,
        TravelLegMode legMode)
    {
        if (layerCtrl == null || set?.animationTree == null)
            return;

        var targetMode = preferNonIk
            ? AnimationLayerPlaybackMode.NonIkKinematic
            : AnimationLayerPlaybackMode.PhysicsCards;

        layerCtrl.SetPlaybackModeForBehaviorTree(set.animationTree, targetMode);

        int layerIndex = layerMap != null ? layerMap.ResolveLayerIndex(legMode) : -1;
        if (layerIndex >= 0)
            layerCtrl.SetLayerPlaybackMode(layerIndex, targetMode);
    }
}
