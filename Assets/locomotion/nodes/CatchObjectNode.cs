using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Executes a catch good section (card) to intercept the goal target. Uses CatchTrajectoryUtility to get
/// predicted intercept position; the card's impulse stack drives the hand(s) toward the catch.
/// </summary>
public class CatchObjectNode : BehaviorTreeNode
{
    [Header("Catch")]
    [Tooltip("Catch card to execute. If null, solver picks from current goal (GoalType.Catch).")]
    public GoodSection catchCard;

    [Tooltip("Approximate hand speed (m/s) for intercept time estimate. Used for moving objects.")]
    public float handSpeed = 5f;

    private bool cardExecuted;
    private GoodSection activeCard;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GameObject toCatch = tree != null && tree.currentGoal != null && tree.currentGoal.target != null
            ? tree.currentGoal.target
            : null;
        if (toCatch == null)
            return BehaviorTreeStatus.Failure;

        GoodSection card = catchCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Catch)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && c.isCatchGoal)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card == null || !card.isCatchGoal)
            return BehaviorTreeStatus.Failure;

        if (!cardExecuted)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;

            string boneName = !string.IsNullOrEmpty(card.catchLimbBoneName) ? card.catchLimbBoneName : "RightHand";
            Vector3 handPos = ragdoll.transform.position;
            Transform limbT = ragdoll.GetBoneTransform(boneName);
            if (limbT != null)
                handPos = limbT.position;
            CatchTrajectoryUtility.GetInterceptPosition(handPos, toCatch.transform, handSpeed, out _, out _);

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
