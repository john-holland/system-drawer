using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior tree node for a single tool-use traversability segment: execute a specific good section
/// (card) with optional tool(s) to bridge a gap (e.g. climb ladder, swing, throw, screwdriver+hammer).
/// When tools list is non-empty, multiple tools are used; else single tool is used.
/// </summary>
public class ExecuteToolTraversabilityNode : BehaviorTreeNode
{
    [Header("Segment (from planner)")]
    [Tooltip("Good section (card) to execute for this traversability segment.")]
    public GoodSection card;

    [Tooltip("Optional single tool (backward compat). Used when tools list is empty.")]
    public GameObject tool;

    [Tooltip("Multiple tools for this segment. When non-empty, used instead of tool.")]
    public List<GameObject> tools = new List<GameObject>();

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
            Vector3 targetPos = toolUseTo;
            Rigidbody targetRb = null;
            if (tree != null && tree.currentGoal != null && tree.currentGoal.target != null)
            {
                targetPos = tree.currentGoal.target.transform.position;
                targetRb = tree.currentGoal.target.GetComponent<Rigidbody>();
            }
            bool feasible = targetRb != null
                ? ThrowTrajectoryUtility.IsInRangeAndFeasibleMovingTarget(card, throwerOrigin, targetPos, targetRb.linearVelocity, null, maxThrowLaunchSpeed > 0f ? maxThrowLaunchSpeed : 0f)
                : ThrowTrajectoryUtility.IsInRangeAndFeasible(card, throwerOrigin, targetPos, null, maxThrowLaunchSpeed > 0f ? maxThrowLaunchSpeed : 0f);
            if (!feasible)
                return BehaviorTreeStatus.Failure;
        }

        if (!executed)
        {
            if (tools != null && tools.Count > 0)
            {
                foreach (GameObject t in tools)
                {
                    if (t == null || !t.activeInHierarchy)
                        return BehaviorTreeStatus.Failure;
                }
            }
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
