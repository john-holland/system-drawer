using UnityEngine;

/// <summary>
/// Sets system-drawer animation layer weights for the current travel leg mode.
/// </summary>
public sealed class ApplyTravelLegAnimationNode : TravelContextBehaviorTreeNode, ITravelExecutionContextConsumer
{
    [Tooltip("Optional SystemDrawerAnimator component; resolved from travel agent or tree when unset.")]
    public Component systemDrawerAnimator;

    public TravelLegModeLayerMap layerMap = new TravelLegModeLayerMap();

    TravelExecutionContext _injected;

    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public void SetTravelExecutionContext(TravelExecutionContext ctx) => _injected = ctx;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        TravelExecutionContext ctx = Ctx ?? _injected;
        if (ctx == null || ctx.isModeTransition)
            return BehaviorTreeStatus.Success;

        ISystemDrawerLayerControl animator = ResolveAnimator(ctx, tree);
        if (animator == null || layerMap == null)
            return BehaviorTreeStatus.Success;

        int activeLayer = layerMap.ResolveLayerIndex(ctx.legMode);
        float weight = 1f;
        if (ctx.inReverseTail && ctx.reverseBudgetMeters > 1e-4f)
        {
            weight = Mathf.Clamp01(ctx.reverseBudgetRemainingMeters / ctx.reverseBudgetMeters);
            animator.SetLayerPlayDirection(activeLayer, -1);
        }
        else
        {
            animator.SetLayerPlayDirection(activeLayer, 1);
        }
        animator.SetLayerWeight(layerMap.walkLayerIndex, activeLayer == layerMap.walkLayerIndex ? weight : 0f);
        animator.SetLayerWeight(layerMap.driveLayerIndex, activeLayer == layerMap.driveLayerIndex ? weight : 0f);
        animator.SetLayerWeight(layerMap.flyLayerIndex, activeLayer == layerMap.flyLayerIndex ? weight : 0f);
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
}
