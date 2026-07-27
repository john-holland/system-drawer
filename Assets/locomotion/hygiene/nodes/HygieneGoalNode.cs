using UnityEngine;

/// <summary>
/// GoalType.Hygiene dispatcher: runs brush/floss/tongue/wash/shower from card.hygieneKind or goal name.
/// </summary>
public sealed class HygieneGoalNode : BehaviorTreeNode
{
    public GoodSection hygieneCard;
    public WashHandsNode washHands;
    public ShowerNode shower;
    BehaviorTreeNode _active;
    bool _entered;

    public override void OnEnter(BehaviorTree tree)
    {
        _entered = false;
        _active = null;
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!_entered)
        {
            string kind = ResolveKind(tree);
            _active = Build(kind, tree);
            _active?.OnEnter(tree);
            _entered = true;
        }
        if (_active == null) return BehaviorTreeStatus.Failure;
        return _active.Execute(tree);
    }

    string ResolveKind(BehaviorTree tree)
    {
        if (hygieneCard == null && tree != null)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            var ragdoll = tree.GetComponent<RagdollSystem>();
            if (solver != null && tree.currentGoal != null)
            {
                var state = ragdoll != null ? ragdoll.GetCurrentState() : new RagdollState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] != null && cards[i].isHygieneGoal)
                    {
                        hygieneCard = cards[i];
                        break;
                    }
                }
            }
        }
        if (hygieneCard != null && !string.IsNullOrEmpty(hygieneCard.hygieneKind))
            return hygieneCard.hygieneKind;
        string name = tree?.currentGoal?.goalName ?? "";
        if (name.Contains("shower")) return "shower";
        if (name.Contains("wash")) return "wash_hands";
        if (name.Contains("floss")) return "floss";
        if (name.Contains("tongue")) return "brush_tongue";
        return "brush_teeth";
    }

    BehaviorTreeNode Build(string kind, BehaviorTree tree)
    {
        switch ((kind ?? "").ToLowerInvariant())
        {
            case "brush_tongue":
            case "tongue":
                return new BrushTongueNode();
            case "floss":
                return new FlossTeethNode();
            case "wash_hands":
            case "wash":
                washHands ??= new WashHandsNode();
                if (tree?.currentGoal?.target != null)
                    washHands.sinkCenter = tree.currentGoal.target.transform;
                return washHands;
            case "shower":
                shower ??= new ShowerNode();
                if (tree?.currentGoal?.target != null)
                    shower.showerHead = tree.currentGoal.target.transform;
                return shower;
            default:
                return new BrushTeethNode();
        }
    }
}
