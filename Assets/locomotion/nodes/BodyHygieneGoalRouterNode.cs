using UnityEngine;

/// <summary>
/// Selector-style router for Eat / Toilet / Hygiene goals. Place under BT root (or as root action)
/// so GoalType.Eat|Toilet|Hygiene dispatch without bespoke tree authoring per encounter.
/// </summary>
public sealed class BodyHygieneGoalRouterNode : BehaviorTreeNode
{
    EatObjectNode _eat;
    ToiletVisitNode _toilet;
    HygieneGoalNode _hygiene;
    FreeExcreteNode _free;
    BehaviorTreeNode _active;
    GoalType _boundType;

    public override void OnEnter(BehaviorTree tree)
    {
        _active = null;
        _boundType = GoalType.Movement;
        status = BehaviorTreeStatus.Running;
    }

    public override bool Predicate(BehaviorTree tree)
    {
        if (tree?.currentGoal == null) return false;
        var t = tree.currentGoal.type;
        return t == GoalType.Eat || t == GoalType.Toilet || t == GoalType.Hygiene ||
               (t == GoalType.Interaction && IsFreeExcreteGoal(tree.currentGoal));
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (tree?.currentGoal == null) return BehaviorTreeStatus.Failure;
        var type = tree.currentGoal.type;
        if (_active == null || _boundType != type)
        {
            _active = Bind(tree, type);
            _boundType = type;
            _active?.OnEnter(tree);
        }
        if (_active == null) return BehaviorTreeStatus.Failure;
        return _active.Execute(tree);
    }

    BehaviorTreeNode Bind(BehaviorTree tree, GoalType type)
    {
        // Ensure default cards exist for matching.
        var consider = tree.GetComponent<ConsiderBodyHygieneCards>();
        if (consider == null)
            consider = tree.gameObject.AddComponent<ConsiderBodyHygieneCards>();
        consider.GenerateCards();

        switch (type)
        {
            case GoalType.Eat:
                _eat ??= new EatObjectNode();
                return _eat;
            case GoalType.Toilet:
                _toilet ??= new ToiletVisitNode();
                return _toilet;
            case GoalType.Hygiene:
                _hygiene ??= new HygieneGoalNode();
                return _hygiene;
            case GoalType.Interaction when IsFreeExcreteGoal(tree.currentGoal):
                _free ??= new FreeExcreteNode();
                return _free;
            default:
                return null;
        }
    }

    static bool IsFreeExcreteGoal(BehaviorTreeGoal g) =>
        g != null && !string.IsNullOrEmpty(g.goalName) &&
        g.goalName.IndexOf("free_excrete", System.StringComparison.OrdinalIgnoreCase) >= 0;
}
