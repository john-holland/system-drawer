using UnityEngine;

/// <summary>
/// Composed toilet visit: BeforeToiletSit → ExcreteOnToilet → AfterToiletSit.
/// </summary>
public sealed class ToiletVisitNode : BehaviorTreeNode
{
    public ToiletStation toilet;
    public BeforeToiletSitNode before;
    public ExcreteOnToiletNode excrete;
    public AfterToiletSitNode after;
    public bool doPee = true;
    public bool doPoop = true;

    enum Phase { Before, Excrete, After, Done }
    Phase _phase;

    public override void OnEnter(BehaviorTree tree)
    {
        _phase = Phase.Before;
        if (toilet == null && tree?.currentGoal?.target != null)
            toilet = tree.currentGoal.target.GetComponent<ToiletStation>();
        if (toilet == null)
            toilet = Object.FindFirstObjectByType<ToiletStation>();

        before = new BeforeToiletSitNode { toilet = toilet };
        before.OnEnter(tree);
        excrete = new ExcreteOnToiletNode
        {
            toilet = toilet,
            doPee = doPee,
            doPoop = doPoop
        };
        after = new AfterToiletSitNode { toilet = toilet };
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (toilet == null) return BehaviorTreeStatus.Failure;

        switch (_phase)
        {
            case Phase.Before:
            {
                var st = before.Execute(tree);
                if (st == BehaviorTreeStatus.Running) return BehaviorTreeStatus.Running;
                _phase = Phase.Excrete;
                excrete.OnEnter(tree);
                return BehaviorTreeStatus.Running;
            }
            case Phase.Excrete:
            {
                var st = excrete.Execute(tree);
                if (st == BehaviorTreeStatus.Running) return BehaviorTreeStatus.Running;
                _phase = Phase.After;
                after.OnEnter(tree);
                return BehaviorTreeStatus.Running;
            }
            case Phase.After:
            {
                var st = after.Execute(tree);
                if (st == BehaviorTreeStatus.Running) return BehaviorTreeStatus.Running;
                _phase = Phase.Done;
                return BehaviorTreeStatus.Success;
            }
            default:
                return BehaviorTreeStatus.Success;
        }
    }
}
