using UnityEngine;

/// <summary>
/// Samples legacy <see cref="Input"/> axes and sprint/jump; writes <see cref="RagdollPlayerInputBuffer"/>.
/// </summary>
public class ReadRagdollPlayerMovementInputNode : BehaviorTreeNode
{
    RagdollPlayerInputBuffer buffer;

    void Awake()
    {
        nodeType = NodeType.Action;
        buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
    }

    public override bool Predicate(BehaviorTree tree)
    {
        return buffer != null && buffer.options != null;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (buffer == null || buffer.options == null)
            return BehaviorTreeStatus.Failure;

        var o = buffer.options;
        bool uiMode = o.altHeldEnablesUIMode &&
                      (Input.GetKey(o.uiModeHoldKey) || Input.GetKey(KeyCode.RightAlt));

        bool blockMove = uiMode || !o.enableMovement;
        float h = blockMove ? 0f : Input.GetAxis("Horizontal");
        float v = blockMove ? 0f : Input.GetAxis("Vertical");
        bool sprint = !blockMove && Input.GetKey(KeyCode.LeftShift);
        bool jump = !blockMove && Input.GetButtonDown("Jump");
        float brake01 = blockMove ? 0f : (Input.GetKey(o.brakeKey) ? 1f : 0f);
        bool selfDriving = buffer.State.selfDriving;
        if (o.selfDrivingEnabled && !blockMove && Input.GetKeyDown(o.selfDrivingToggleKey))
            selfDriving = !selfDriving;

        buffer.WriteState(new RagdollPlayerInputState
        {
            horizontal = h,
            vertical = v,
            sprint = sprint,
            jumpPressedThisFrame = jump,
            uiMode = uiMode,
            brake01 = brake01,
            selfDriving = selfDriving
        });

        return BehaviorTreeStatus.Success;
    }
}
