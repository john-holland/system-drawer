using UnityEngine;

/// <summary>
/// Runs activation children once at a travel mode boundary, then succeeds without re-firing.
/// </summary>
public class TravelModeTransitionSequenceNode : BehaviorTreeNode
{
    [HideInInspector] public TravelExecutionContextProvider provider;
    [HideInInspector] public CompositeMultiModalPathNode composite;
    [HideInInspector] public MultiModalSegment segment;
    [HideInInspector] public int segmentIndex;
    [HideInInspector] public TravelLegMode fromMode;
    [HideInInspector] public TravelLegMode toMode;
    [HideInInspector] public TravelLegMode previousLegMode;
    [HideInInspector] public TravelAgent travelAgent;

    bool _executed;
    bool _childActive;
    int _childIndex;

    void Awake()
    {
        nodeType = NodeType.Sequence;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _executed = false;
        _childActive = false;
        _childIndex = 0;
        PublishTransitionContext(tree);
        InjectContextToConsumers();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (_executed)
            return BehaviorTreeStatus.Success;

        if (children == null || children.Count == 0)
        {
            _executed = true;
            return BehaviorTreeStatus.Success;
        }

        for (int i = _childIndex; i < children.Count; i++)
        {
            BehaviorTreeNode c = children[i];
            if (c == null)
                continue;

            if (!_childActive)
            {
                c.OnEnter(tree);
                _childActive = true;
            }

            BehaviorTreeStatus s = c.Execute(tree);
            if (s == BehaviorTreeStatus.Running)
            {
                _childIndex = i;
                return BehaviorTreeStatus.Running;
            }

            if (s == BehaviorTreeStatus.Failure)
            {
                c.OnExit(tree);
                _childActive = false;
                return BehaviorTreeStatus.Failure;
            }

            c.OnExit(tree);
            _childActive = false;
        }

        _executed = true;
        return BehaviorTreeStatus.Success;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_childActive && _childIndex >= 0 && _childIndex < (children?.Count ?? 0))
        {
            BehaviorTreeNode c = children[_childIndex];
            if (c != null)
                c.OnExit(tree);
        }

        _childActive = false;
        PublishLegContext(tree);
    }

    void PublishTransitionContext(BehaviorTree tree)
    {
        if (provider == null)
            return;

        TravelExecutionContext ctx = TravelExecutionContext.Build(
            tree,
            composite,
            segment,
            segmentIndex,
            previousLegMode,
            isTransition: true,
            from: fromMode,
            to: toMode,
            travelAgent);
        provider.Publish(ctx);
    }

    void PublishLegContext(BehaviorTree tree)
    {
        if (provider == null)
            return;

        TravelExecutionContext ctx = TravelExecutionContext.Build(
            tree,
            composite,
            segment,
            segmentIndex,
            previousLegMode,
            isTransition: false,
            from: fromMode,
            to: toMode,
            travelAgent);
        provider.Publish(ctx);
    }

    void InjectContextToConsumers()
    {
        if (provider == null || children == null)
            return;

        TravelExecutionContext ctx = provider.Current;
        if (ctx == null)
            return;

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is ITravelExecutionContextConsumer consumer)
                consumer.SetTravelExecutionContext(ctx);
        }
    }
}
