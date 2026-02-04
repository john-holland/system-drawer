using UnityEngine;

/// <summary>
/// Executes a place/lift good section to move the goal target object toward placeTargetPosition/placeTargetRotation.
/// Uses cards with isPlaceGoal or placement keywords (lift/place).
/// </summary>
public class PlaceObjectNode : BehaviorTreeNode
{
    [Header("Place")]
    [Tooltip("Place/lift card to execute. If null, solver picks from current goal (GoalType.Place).")]
    public GoodSection placeCard;

    private bool cardExecuted;
    private GoodSection activeCard;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GoodSection card = placeCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Place)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && (c.isPlaceGoal || (!string.IsNullOrEmpty(c.sectionName) && (c.sectionName.ToLowerInvariant().Contains("lift") || c.sectionName.ToLowerInvariant().Contains("place")))))
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card == null)
            return BehaviorTreeStatus.Failure;

        if (!cardExecuted)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
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
