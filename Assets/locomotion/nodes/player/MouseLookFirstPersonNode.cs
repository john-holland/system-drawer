using UnityEngine;

/// <summary>Yaw on <see cref="yawRoot"/> and pitch on <see cref="pitchTransform"/> (typically a camera child).</summary>
public class MouseLookFirstPersonNode : BehaviorTreeNode
{
    [Tooltip("Receives horizontal rotation (Y). Defaults to behavior tree actor root.")]
    public Transform yawRoot;

    [Tooltip("Receives pitch (local X). Usually the camera transform.")]
    public Transform pitchTransform;

    RagdollPlayerInputBuffer buffer;
    float horizontalRotation;
    float verticalRotation;
    bool rotationsInitialized;

    void Awake()
    {
        nodeType = NodeType.Action;
        buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
    }

    void EnsureRotationsInitialized()
    {
        if (rotationsInitialized)
            return;
        if (yawRoot == null)
        {
            var bt = GetComponentInParent<BehaviorTree>();
            if (bt != null)
                yawRoot = bt.transform;
        }
        if (yawRoot != null)
            horizontalRotation = yawRoot.eulerAngles.y;
        if (pitchTransform != null)
        {
            verticalRotation = -pitchTransform.localEulerAngles.x;
            if (verticalRotation > 180f)
                verticalRotation -= 360f;
        }
        rotationsInitialized = true;
    }

    public override bool Predicate(BehaviorTree tree)
    {
        return buffer != null && buffer.options != null && buffer.options.enableMouseLook;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (buffer == null || buffer.options == null)
            return BehaviorTreeStatus.Failure;

        EnsureRotationsInitialized();

        var o = buffer.options;
        if (buffer.State.uiMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return BehaviorTreeStatus.Success;
        }

        if (Cursor.lockState != CursorLockMode.Locked && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mx = Input.GetAxis("Mouse X") * o.mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * o.mouseSensitivity;

        horizontalRotation += mx;
        if (horizontalRotation > 360f) horizontalRotation -= 360f;
        if (horizontalRotation < 0f) horizontalRotation += 360f;

        verticalRotation += (o.invertY ? 1f : -1f) * my;
        verticalRotation = Mathf.Clamp(verticalRotation, -o.verticalLookLimit, o.verticalLookLimit);

        if (yawRoot != null)
            yawRoot.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);

        if (pitchTransform != null && yawRoot != null)
        {
            Quaternion verticalRot = Quaternion.Euler(verticalRotation, 0f, 0f);
            pitchTransform.rotation = yawRoot.rotation * verticalRot;
        }

        return BehaviorTreeStatus.Success;
    }
}
