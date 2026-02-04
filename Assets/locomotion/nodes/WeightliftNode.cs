using UnityEngine;

/// <summary>
/// Selects a weightlift card, runs pick-up/carry for the tool, then executes the card (muscle group activation).
/// Goal: not fall over (least radial movement). Reuses carry logic for the tool.
/// </summary>
public class WeightliftNode : BehaviorTreeNode
{
    [Header("Weightlift")]
    [Tooltip("Weightlift card to execute. If null, solver picks from current goal (GoalType.Weightlift).")]
    public GoodSection weightliftCard;

    [Tooltip("Tool/weight to pick up. If null, uses goal target or card.weightliftTool.")]
    public GameObject weightliftTool;

    private bool cardExecuted;
    private GoodSection activeCard;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GameObject tool = weightliftTool;
        if (tool == null && tree != null && tree.currentGoal != null && tree.currentGoal.target != null)
            tool = tree.currentGoal.target;

        GoodSection card = weightliftCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Weightlift)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && c.isWeightlift)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }
        if (tool == null && card != null)
            tool = card.weightliftTool;

        if (card == null || !card.isWeightlift)
            return BehaviorTreeStatus.Failure;

        if (!cardExecuted)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;

            if (!string.IsNullOrEmpty(card.weightliftMuscleGroupName))
                ragdoll.ActivateMuscleGroup(card.weightliftMuscleGroupName, 0.7f);

            card.Execute(state);
            activeCard = card;
            cardExecuted = true;
        }

        RagdollState currentState = ragdoll.GetCurrentState();
        bool stillExecuting = activeCard != null && activeCard.Update(currentState, Time.deltaTime);

        if (!stillExecuting)
        {
            activeCard = null;
            cardExecuted = false;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        cardExecuted = false;
        activeCard = null;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (activeCard != null)
        {
            activeCard.Stop();
            activeCard = null;
        }
        cardExecuted = false;
    }
}
