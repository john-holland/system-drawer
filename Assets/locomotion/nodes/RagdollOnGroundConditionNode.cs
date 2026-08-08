using UnityEngine;

/// <summary>
/// Condition: Success when the ragdoll is on the ground and fallen (or mid get-up recovery).
/// Failure otherwise so a Selector can run the passthrough branch.
/// </summary>
public class RagdollOnGroundConditionNode : BehaviorTreeNode
{
    [Tooltip("Ground layers for the downward probe.")]
    public LayerMask groundLayers = ~0;

    [Tooltip("Max raycast distance from pelvis/root.")]
    public float groundProbeDistance = RagdollGroundCheck.DefaultGroundProbeDistance;

    [Tooltip("Pelvis up · world up below this counts as fallen.")]
    public float uprightDotThreshold = RagdollGroundCheck.DefaultUprightDotThreshold;

    [Tooltip("Optional. While recovering, keep Success so the Sequence does not abort mid stand.")]
    public RagdollGetUpActionNode getUpAction;

    public RagdollSystem ragdollSystem;

    void Awake()
    {
        nodeType = NodeType.Condition;
        if (ragdollSystem == null)
            ragdollSystem = GetComponentInParent<RagdollSystem>();
        if (getUpAction == null)
            getUpAction = GetComponentInParent<BehaviorTree>()?.GetComponentInChildren<RagdollGetUpActionNode>(true);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (ragdollSystem == null)
            ragdollSystem = tree != null
                ? tree.GetComponentInParent<RagdollSystem>()
                : GetComponentInParent<RagdollSystem>();

        if (getUpAction != null && getUpAction.IsRecovering)
        {
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }

        if (ragdollSystem == null)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        bool ok = RagdollGroundCheck.IsOnGroundAndFallen(
            ragdollSystem,
            groundLayers,
            groundProbeDistance,
            uprightDotThreshold);

        status = ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}
