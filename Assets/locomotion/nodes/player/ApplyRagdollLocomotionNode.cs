using UnityEngine;

/// <summary>
/// Builds horizontal velocity from input + camera yaw; sends <see cref="ImpulseType.Motor"/> impulses via <see cref="NervousSystem"/> (same pattern as <see cref="MoveToWaypointNode"/> fallback).
/// </summary>
public class ApplyRagdollLocomotionNode : BehaviorTreeNode
{
    [Tooltip("Forward reference for movement (FP: yaw root; TP: follow target or ragdoll forward).")]
    public Transform facingReference;

    public PhysicsCardSolver cardSolver;
    public NervousSystem nervousSystem;
    public RagdollSystem ragdollSystem;

    RagdollPlayerInputBuffer buffer;

    void Awake()
    {
        nodeType = NodeType.Action;
        buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
        if (cardSolver == null) cardSolver = GetComponentInParent<PhysicsCardSolver>();
        if (nervousSystem == null) nervousSystem = GetComponentInParent<NervousSystem>();
        if (ragdollSystem == null) ragdollSystem = GetComponentInParent<RagdollSystem>();
    }

    public override bool Predicate(BehaviorTree tree)
    {
        return buffer != null && buffer.options != null && buffer.options.enableMovement;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (buffer == null || buffer.options == null || nervousSystem == null || ragdollSystem == null)
            return BehaviorTreeStatus.Failure;

        var o = buffer.options;
        var st = buffer.State;
        if (st.uiMode)
            return BehaviorTreeStatus.Success;

        Transform face = facingReference;
        if (face == null && tree != null)
            face = tree.transform;

        if (face == null)
            return BehaviorTreeStatus.Failure;

        Vector3 flatForward = face.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 1e-6f)
            flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 flatRight = face.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 wish = flatRight * st.horizontal + flatForward * st.vertical;
        if (wish.sqrMagnitude > 1f)
            wish.Normalize();

        float speed = o.moveSpeed * (st.sprint ? o.sprintMultiplier : 1f);
        wish *= speed;

        if (wish.sqrMagnitude > 1e-6f)
        {
            Vector3 dir = wish.normalized;
            float mag = wish.magnitude;
            if (cardSolver != null && cardSolver.IsWorldPositionAmbulationDoNotPath(ragdollSystem.GetCurrentState().rootPosition + dir))
                return BehaviorTreeStatus.Success;

            var motorData = new MotorData("ambulation", Mathf.Clamp01(mag / 10f), 0.35f, null)
            {
                forceDirection = dir
            };
            var impulse = new ImpulseData(ImpulseType.Motor, nameof(ApplyRagdollLocomotionNode), "Limb", motorData, 0);
            nervousSystem.SendImpulseDown("Limb", impulse);
        }

        if (buffer.ConsumeJumpPressed() && IsGrounded())
        {
            var jumpMotor = new MotorData("ambulation", Mathf.Clamp01(o.jumpImpulseStrength / 10f), 0.2f, null)
            {
                forceDirection = Vector3.up
            };
            nervousSystem.SendImpulseDown("Limb", new ImpulseData(ImpulseType.Motor, nameof(ApplyRagdollLocomotionNode), "Limb", jumpMotor, 0));
        }

        return BehaviorTreeStatus.Success;
    }

    bool IsGrounded()
    {
        if (ragdollSystem == null || buffer == null || buffer.options == null)
            return false;
        Vector3 p = ragdollSystem.GetCurrentState().rootPosition + Vector3.up * 0.05f;
        float d = buffer.options.groundProbeDistance;
        return Physics.Raycast(p, Vector3.down, d, buffer.options.groundLayers, QueryTriggerInteraction.Ignore);
    }
}
