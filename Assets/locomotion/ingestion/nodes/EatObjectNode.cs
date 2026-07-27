using UnityEngine;

/// <summary>
/// GoalType.Eat dispatcher: resolves eat card + FoodItem target, then runs EatFoodNode.
/// </summary>
public sealed class EatObjectNode : BehaviorTreeNode
{
    public EatFoodNode eat;
    public GoodSection eatCard;
    bool _entered;

    public override void OnEnter(BehaviorTree tree)
    {
        _entered = false;
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (tree == null) return BehaviorTreeStatus.Failure;

        if (!_entered)
        {
            EnsureCard(tree);
            if (eat == null)
                eat = new EatFoodNode();
            if (eat.food == null && tree.currentGoal?.target != null)
                eat.food = tree.currentGoal.target.GetComponent<FoodItem>();
            if (eat.mouth == null)
                eat.mouth = tree.GetComponent<MouthInteriorRuntime>()
                            ?? tree.GetComponentInChildren<MouthInteriorRuntime>();
            eat.OnEnter(tree);
            _entered = true;
        }

        return eat.Execute(tree);
    }

    void EnsureCard(BehaviorTree tree)
    {
        if (eatCard != null) return;
        var solver = tree.GetComponent<PhysicsCardSolver>();
        if (solver == null || tree.currentGoal == null) return;
        var ragdoll = tree.GetComponent<RagdollSystem>();
        var state = ragdoll != null ? ragdoll.GetCurrentState() : new RagdollState();
        var cards = solver.SolveForGoal(tree.currentGoal, state);
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].isEatGoal)
            {
                eatCard = cards[i];
                return;
            }
        }
        // Autoinject if missing from pool.
        eatCard = ConsiderBodyHygieneCards.MakeEatCard();
        solver.AddCards(new System.Collections.Generic.List<GoodSection> { eatCard });
    }
}
