using UnityEngine;

/// <summary>
/// Selector: run primary unfold/close child; on Failure run defaulted failure branch and emit train_fold_failed.
/// children[0] = primary, children[1] = failure branch (optional).
/// </summary>
public sealed class TrainCarFoldFailureBranchNode : BehaviorTreeNode
{
    public TrainCarVehicleRagdoll car;
    public BehaviorTree failureSubtree;
    public string failureLimbOrBayId;
    int _phase; // 0 primary, 1 failure
    bool _enteredPrimary;
    bool _enteredFailure;

    void Awake() => nodeType = NodeType.Selector;

    public override void OnEnter(BehaviorTree tree)
    {
        _phase = 0;
        _enteredPrimary = false;
        _enteredFailure = false;
        if (car == null && tree != null)
            car = tree.GetComponentInParent<TrainCarVehicleRagdoll>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        var primary = children != null && children.Count > 0 ? children[0] : null;
        var failBranch = children != null && children.Count > 1 ? children[1] : null;

        if (_phase == 0)
        {
            if (primary == null)
            {
                RunDefaultFailure(tree);
                status = BehaviorTreeStatus.Failure;
                return BehaviorTreeStatus.Failure;
            }
            if (!_enteredPrimary)
            {
                primary.OnEnter(tree);
                _enteredPrimary = true;
            }
            var st = primary.Execute(tree);
            if (st == BehaviorTreeStatus.Running)
            {
                status = BehaviorTreeStatus.Running;
                return BehaviorTreeStatus.Running;
            }
            if (st == BehaviorTreeStatus.Success)
            {
                primary.OnExit(tree);
                status = BehaviorTreeStatus.Success;
                return BehaviorTreeStatus.Success;
            }
            primary.OnExit(tree);
            car?.MarkFoldFailed(failureLimbOrBayId);
            _phase = 1;
        }

        // Failure branch
        if (failBranch != null)
        {
            if (!_enteredFailure)
            {
                failBranch.OnEnter(tree);
                _enteredFailure = true;
            }
            var fst = failBranch.Execute(tree);
            if (fst == BehaviorTreeStatus.Running)
            {
                status = BehaviorTreeStatus.Running;
                return BehaviorTreeStatus.Running;
            }
            failBranch.OnExit(tree);
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        if (failureSubtree != null)
        {
            // Host tree pointer only — emit narrative and fail.
            tree?.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.FoldFailed,
                SendMessageOptions.DontRequireReceiver);
        }
        RunDefaultFailure(tree);
        status = BehaviorTreeStatus.Failure;
        return BehaviorTreeStatus.Failure;
    }

    void RunDefaultFailure(BehaviorTree tree)
    {
        car?.MarkFoldFailed(failureLimbOrBayId);
        tree?.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.FoldFailed,
            SendMessageOptions.DontRequireReceiver);
    }
}
