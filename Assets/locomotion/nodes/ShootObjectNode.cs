using UnityEngine;

/// <summary>
/// Executes a shoot good section (card) toward the goal target. Uses ThrowTrajectoryUtility for feasibility
/// and range; the card's impulse stack drives the shoot. Reuses throw trajectory math with shoot-specific card fields.
/// </summary>
public class ShootObjectNode : BehaviorTreeNode
{
    [Header("Shoot")]
    [Tooltip("Shoot card to execute. If null, solver picks from current goal (GoalType.Shoot).")]
    public GoodSection shootCard;

    [Tooltip("Max launch speed for trajectory feasibility (0 = no cap).")]
    public float maxShootLaunchSpeed;

    private bool cardExecuted;
    private GoodSection activeCard;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        Vector3 targetPos = tree != null && tree.currentGoal != null && tree.currentGoal.target != null
            ? tree.currentGoal.target.transform.position
            : (tree != null && tree.currentGoal != null ? tree.currentGoal.targetPosition : Vector3.zero);

        GoodSection card = shootCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Shoot)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && c.isShootGoal)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card == null || !card.isShootGoal)
            return BehaviorTreeStatus.Failure;

        Vector3 origin = ragdoll.transform != null ? ragdoll.transform.position : transform.position;
        var r = ThrowTrajectoryUtility.Compute(origin, targetPos, null, maxShootLaunchSpeed > 0f ? maxShootLaunchSpeed : 0f);
        if (!r.feasible)
            return BehaviorTreeStatus.Failure;
        if (card.shootMaxRange > 0f && r.distance > card.shootMaxRange)
            return BehaviorTreeStatus.Failure;
        if (card.shootMinRange > 0f && r.distance < card.shootMinRange)
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
