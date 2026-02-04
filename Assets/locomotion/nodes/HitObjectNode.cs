using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Executes a hit good section (card) toward the goal target. Uses HitTrajectoryUtility to get predicted
/// target position at impact time; the card's impulse stack drives the limb toward the strike.
/// </summary>
public class HitObjectNode : BehaviorTreeNode
{
    [Header("Hit")]
    [Tooltip("Hit card to execute. If null, solver picks from current goal (GoalType.Hit).")]
    public GoodSection hitCard;

    [Tooltip("Approximate limb speed (m/s) for intercept time estimate. Used for moving targets.")]
    public float limbSpeed = 5f;

    private bool cardExecuted;
    private GoodSection activeCard;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GameObject targetObj = tree != null && tree.currentGoal != null && tree.currentGoal.target != null
            ? tree.currentGoal.target
            : null;
        if (targetObj == null)
            return BehaviorTreeStatus.Failure;

        GoodSection card = hitCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Hit)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && c.isHitGoal)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card == null || !card.isHitGoal)
            return BehaviorTreeStatus.Failure;

        if (!cardExecuted)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;

            Vector3 limbPos = ragdoll.transform.position;
            Transform limbT = ragdoll.GetBoneTransform(!string.IsNullOrEmpty(card.hitLimbBoneName) ? card.hitLimbBoneName : "RightHand");
            if (limbT != null)
                limbPos = limbT.position;
            HitTrajectoryUtility.ComputeIntercept(limbPos, targetObj.transform, limbSpeed, out _, out _);

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

    private static RagdollBodyPart GetLimbBodyPart(RagdollSystem ragdoll, string limbName)
    {
        if (ragdoll == null || string.IsNullOrEmpty(limbName)) return null;
        if (limbName.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase) && ragdoll.rightHandComponent != null)
            return ragdoll.rightHandComponent;
        if (limbName.Equals("LeftHand", System.StringComparison.OrdinalIgnoreCase) && ragdoll.leftHandComponent != null)
            return ragdoll.leftHandComponent;
        Transform t = ragdoll.GetBoneTransform(limbName);
        return t != null ? t.GetComponent<RagdollBodyPart>() : null;
    }
}
