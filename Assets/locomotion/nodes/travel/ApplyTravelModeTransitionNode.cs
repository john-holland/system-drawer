using UnityEngine;

/// <summary>
/// Cross-fades animation layers at a travel mode boundary; optionally plays a set when SDA deferral is off.
/// </summary>
public sealed class ApplyTravelModeTransitionNode : TravelContextBehaviorTreeNode, ITravelExecutionContextConsumer
{
    [Tooltip("Optional SystemDrawerAnimator component; resolved from travel agent or tree when unset.")]
    public Component systemDrawerAnimator;

    public TravelLegModeLayerMap layerMap = new TravelLegModeLayerMap();

    [Tooltip("When true and animator does not defer set manager, Play the first matching animation set for toMode.")]
    public bool playAnimationSetWhenNotDeferred = true;

    TravelExecutionContext _injected;
    bool _applied;

    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public void SetTravelExecutionContext(TravelExecutionContext ctx) => _injected = ctx;

    public override void OnEnter(BehaviorTree tree) => _applied = false;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (_applied)
            return BehaviorTreeStatus.Success;

        TravelExecutionContext ctx = Ctx ?? _injected;
        if (ctx == null || !ctx.isModeTransition)
            return BehaviorTreeStatus.Success;

        ISystemDrawerLayerControl animator = ResolveAnimator(ctx, tree);

        if (animator != null && layerMap != null)
        {
            int fromLayer = layerMap.ResolveLayerIndex(ctx.fromMode);
            int toLayer = layerMap.ResolveLayerIndex(ctx.toMode);
            float blend = layerMap.ResolveBlendDuration(ctx.estimatedLegTimeSec);
            float t = blend > 0.01f ? Mathf.Clamp01(Time.deltaTime / blend) : 1f;

            float fromW = animator.GetLayerWeight(fromLayer);
            if (fromW < 0f) fromW = 1f;
            float toW = animator.GetLayerWeight(toLayer);
            if (toW < 0f) toW = 0f;

            animator.SetLayerWeight(fromLayer, Mathf.Lerp(fromW, 0f, t));
            animator.SetLayerWeight(toLayer, Mathf.Lerp(toW, 1f, t));
        }

        if (playAnimationSetWhenNotDeferred &&
            ctx.animationSetManager != null &&
            (animator == null || !animator.ShouldDeferSetManagerPlayback()))
        {
            int setIndex = ResolveSetIndexForMode(ctx.animationSetManager, ctx.toMode);
            if (setIndex >= 0)
            {
                RagdollAnimationSet set = ctx.animationSetManager.animationSets[setIndex];
                ABTClipConfig cfg = set?.animationTree?.GetActiveConfiguration();
                bool preferNonIk = AnimationPlaybackPolicyApplicator.ResolveForTravelContext(ctx, set, cfg);
                AnimationPlaybackPolicyApplicator.ApplyToAnimatorLayers(animator, set, preferNonIk, layerMap, ctx.toMode);
                ctx.animationSetManager.Play(setIndex);
            }
        }

        _applied = true;
        return BehaviorTreeStatus.Success;
    }

    ISystemDrawerLayerControl ResolveAnimator(TravelExecutionContext ctx, BehaviorTree tree)
    {
        ISystemDrawerLayerControl fromField = SystemDrawerLayerControlLookup.FromComponent(systemDrawerAnimator);
        if (fromField != null)
            return fromField;

        if (ctx.travelAgent != null)
        {
            ISystemDrawerLayerControl fromAgent = SystemDrawerLayerControlLookup.FindInChildren(ctx.travelAgent);
            if (fromAgent != null)
                return fromAgent;
        }

        if (tree != null)
            return SystemDrawerLayerControlLookup.FindInChildren(tree);
        return null;
    }

    static int ResolveSetIndexForMode(RagdollAnimationSetManager mgr, TravelLegMode mode)
    {
        if (mgr?.animationSets == null)
            return -1;

        string token = mode.ToString();
        for (int i = 0; i < mgr.animationSets.Count; i++)
        {
            RagdollAnimationSet set = mgr.animationSets[i];
            if (set?.displayName != null &&
                set.displayName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }

        return mode == TravelLegMode.Walk ? 0 : -1;
    }
}
