using UnityEngine;

/// <summary>
/// Behavior tree node for a single tool-use traversability segment: execute a specific good section
/// (card) with optional tool to bridge a gap (e.g. climb ladder, swing, throw). Downstream behavior:
/// navigate to tool if needed, pick/use, execute card, then continue from toolUseTo.
/// </summary>
public class ExecuteToolTraversabilityNode : BehaviorTreeNode
{
    [Header("Segment (from planner)")]
    [Tooltip("Good section (card) to execute for this traversability segment.")]
    public GoodSection card;

    [Tooltip("Optional tool GameObject (e.g. ladder, batterang).")]
    public GameObject tool;

    [Tooltip("Position after executing the card (bridge end).")]
    public Vector3 toolUseTo;

    [Tooltip("Distance to consider 'arrived' at toolUseTo after execution.")]
    public float reachedDistance = 0.5f;

    [Tooltip("When card is a throw: max launch speed for trajectory feasibility (0 = no cap).")]
    public float maxThrowLaunchSpeed;

    private bool executed;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (card == null)
            return BehaviorTreeStatus.Failure;

        if (card.needsToBeThrown || card.IsThrowGoalOnly())
        {
            Vector3 throwerOrigin = tree != null && tree.transform != null ? tree.transform.position : transform.position;
            RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
            if (ragdoll != null && ragdoll.transform != null)
                throwerOrigin = ragdoll.transform.position;
            if (!ThrowTrajectoryUtility.IsInRangeAndFeasible(card, throwerOrigin, toolUseTo, null, maxThrowLaunchSpeed > 0f ? maxThrowLaunchSpeed : 0f))
                return BehaviorTreeStatus.Failure;
        }

        if (!executed)
        {
            RagdollSystem ragdoll = tree.GetComponent<RagdollSystem>();
            if (ragdoll != null)
            {
                RagdollState state = ragdoll.GetCurrentState();
                if (card.IsFeasible(state))
                {
                    card.Execute(state);
                }
            }
            executed = true;
        }

        return BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        executed = false;
    }

    public override void OnExit(BehaviorTree tree)
    {
        executed = false;
    }
}
