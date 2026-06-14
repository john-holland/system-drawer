using UnityEngine;

/// <summary>
/// Executes one multi-modal plan segment: optional mode-transition activation, then locomotion children.
/// </summary>
public class TravelLegSequenceNode : BehaviorTreeNode
{
    [HideInInspector] public TravelExecutionContextProvider provider;
    [HideInInspector] public CompositeMultiModalPathNode composite;
    [HideInInspector] public MultiModalSegment segment;
    [HideInInspector] public int segmentIndex;
    [HideInInspector] public TravelLegMode legMode;
    [HideInInspector] public TravelLegMode previousLegMode;
    [HideInInspector] public Vector3 transitionWorld;
    [HideInInspector] public TravelAgent travelAgent;

    bool _legEntered;
    bool _childActive;
    int _childIndex;

    void Awake()
    {
        nodeType = NodeType.Sequence;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _legEntered = false;
        _childActive = false;
        _childIndex = 0;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!_legEntered)
        {
            _legEntered = true;
            PublishLegContext(tree);
        }

        if (children == null || children.Count == 0)
            return BehaviorTreeStatus.Success;

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

            if (c is TravelModeTransitionSequenceNode)
                PublishLegContext(tree);
        }

        return BehaviorTreeStatus.Success;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_childActive && children != null && _childIndex >= 0 && _childIndex < children.Count)
        {
            BehaviorTreeNode c = children[_childIndex];
            if (c != null)
                c.OnExit(tree);
        }

        _childActive = false;
        _legEntered = false;
    }

    void PublishLegContext(BehaviorTree tree)
    {
        if (provider == null)
            return;

        bool reverseTail = segment != null && segment.reverseLeg;
        float revRemaining = reverseTail && travelAgent != null ? travelAgent.ReverseBudgetMeters : 0f;
        provider.ReverseBudgetRemainingMeters = revRemaining;
        provider.InReverseTail = reverseTail;

        TravelExecutionContext ctx = TravelExecutionContext.Build(
            tree,
            composite,
            segment,
            segmentIndex,
            previousLegMode,
            isTransition: false,
            from: previousLegMode,
            to: legMode,
            travelAgent,
            revRemaining,
            reverseTail);
        provider.Publish(ctx);

        ReversePlaybackController reverse = travelAgent != null
            ? travelAgent.GetComponentInChildren<ReversePlaybackController>()
            : null;
        reverse?.SyncFromProvider(provider);
    }
}
