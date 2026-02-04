using UnityEngine;

/// <summary>
/// Runs a carry good section, sets carried object on CarriedObjectAttachment, and if pleaseHold is true
/// and the object is detected as not held (distance from attach point > threshold), re-executes the carry/pick-up section.
/// </summary>
public class CarryObjectNode : BehaviorTreeNode
{
    [Header("Carry")]
    [Tooltip("Good section (card) for carry. If null, solver picks from current goal (GoalType.Carry).")]
    public GoodSection carryCard;

    [Tooltip("Distance from attach point above which object is considered 'put down' for pleaseHold.")]
    public float notHeldThreshold = 0.15f;

    [Tooltip("If > 0, use a pathfinding node (name contains 'pathfind' or type PathfindingNode) to approach the object when farther than this distance. 0 = skip pathfinding.")]
    public float approachDistance = 1.5f;

    [Tooltip("Pathfinding node name substring to search for (e.g. 'pathfind'). Ignored if approachDistance <= 0.")]
    public string pathfindNodeName = "pathfind";

    private bool cardExecuted;
    private GoodSection activeCard;
    private bool pathfindingComplete;
    private bool pathfindingStarted;
    private PathfindingNode pathfindingNode;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        CarriedObjectAttachment attachment = tree != null ? tree.GetComponent<CarriedObjectAttachment>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        GoodSection card = carryCard;
        if (card == null && tree != null && tree.currentGoal != null && tree.currentGoal.type == GoalType.Carry)
        {
            var solver = tree.GetComponent<PhysicsCardSolver>();
            if (solver != null)
            {
                var state = ragdoll.GetCurrentState();
                var cards = solver.SolveForGoal(tree.currentGoal, state);
                foreach (var c in cards)
                {
                    if (c != null && c.isCarry)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card == null || !card.isCarry)
            return BehaviorTreeStatus.Failure;

        GameObject toCarry = tree != null && tree.currentGoal != null && tree.currentGoal.target != null
            ? tree.currentGoal.target
            : (card.carriedObject != null ? (card.carriedObject as GameObject) : null);
        if (toCarry == null)
            toCarry = card.carriedObject as GameObject;
        if (toCarry == null)
            return BehaviorTreeStatus.Failure;

        bool pleaseHold = (tree != null && tree.currentGoal != null && tree.currentGoal.pleaseHold) || card.pleaseHold;

        // If not in range and approach pathfinding is enabled, use a pathfinding node from the tree to reach the object first.
        if (!pathfindingComplete && approachDistance > 0f && tree != null && ragdoll != null)
        {
            float distToObject = Vector3.Distance(ragdoll.transform.position, toCarry.transform.position);
            if (distToObject > approachDistance)
            {
                if (pathfindingNode == null)
                {
                    pathfindingNode = tree.FindNode(n =>
                        n is PathfindingNode ||
                        (n != null && !string.IsNullOrEmpty(n.name) && n.name.ToLowerInvariant().Contains(pathfindNodeName.ToLowerInvariant()))) as PathfindingNode;
                }
                if (pathfindingNode != null)
                {
                    pathfindingNode.origin = ragdoll.transform.position;
                    pathfindingNode.destination = toCarry.transform.position;
                    if (!pathfindingStarted)
                    {
                        pathfindingStarted = true;
                        pathfindingNode.OnEnter(tree);
                    }
                    BehaviorTreeStatus pathStatus = pathfindingNode.Execute(tree);
                    if (pathStatus == BehaviorTreeStatus.Running)
                        return BehaviorTreeStatus.Running;
                    if (pathStatus == BehaviorTreeStatus.Success || pathStatus == BehaviorTreeStatus.Failure)
                        pathfindingComplete = true; // Success: we arrived; Failure: give up pathfinding and try carry anyway
                }
                else
                    pathfindingComplete = true; // No pathfind node found, skip pathfinding and use default behavior
            }
            else
                pathfindingComplete = true;
        }

        if (attachment != null)
        {
            if (!attachment.IsHeld(notHeldThreshold) && attachment.carriedTransform != null && pleaseHold)
            {
                attachment.ClearCarried();
                cardExecuted = false;
                activeCard = null;
            }
        }

        if (!cardExecuted)
        {
            RagdollState state = ragdoll.GetCurrentState();
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
            card.Execute(state);
            activeCard = card;
            cardExecuted = true;
            if (attachment != null)
            {
                string boneName = string.IsNullOrEmpty(card.carryAttachBoneName) ? null : card.carryAttachBoneName;
                attachment.SetCarried(toCarry.transform, boneName);
            }
        }

        RagdollState currentState = ragdoll.GetCurrentState();
        bool stillExecuting = activeCard != null && activeCard.Update(currentState, Time.deltaTime);

        if (!stillExecuting)
        {
            if (pleaseHold && attachment != null && !attachment.IsHeld(notHeldThreshold))
            {
                activeCard.Stop();
                cardExecuted = false;
                activeCard = null;
                return BehaviorTreeStatus.Running;
            }
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        cardExecuted = false;
        activeCard = null;
        pathfindingComplete = false;
        pathfindingStarted = false;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (activeCard != null)
        {
            activeCard.Stop();
            activeCard = null;
        }
        cardExecuted = false;
        var attachment = tree != null ? tree.GetComponent<CarriedObjectAttachment>() : null;
        if (attachment != null)
            attachment.ClearCarried();
    }
}
