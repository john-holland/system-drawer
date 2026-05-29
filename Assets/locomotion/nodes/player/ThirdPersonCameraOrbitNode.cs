using UnityEngine;

/// <summary>Orbits a pivot around <see cref="followTarget"/> using mouse; places camera back along view axis.</summary>
public class ThirdPersonCameraOrbitNode : BehaviorTreeNode
{
    public Transform followTarget;
    public Transform pivot;
    public Transform cameraTransform;

    RagdollPlayerInputBuffer buffer;
    float yaw;
    float pitch;
    bool orbitInitialized;

    void Awake()
    {
        nodeType = NodeType.Action;
        buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
    }

    void EnsureOrbitInitialized()
    {
        if (orbitInitialized)
            return;
        if (followTarget != null)
            yaw = followTarget.eulerAngles.y;
        if (pivot != null)
        {
            pitch = pivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
        orbitInitialized = true;
    }

    public override bool Predicate(BehaviorTree tree)
    {
        return buffer != null && buffer.options != null && buffer.options.enableMouseLook;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (buffer == null || buffer.options == null || pivot == null || cameraTransform == null)
            return BehaviorTreeStatus.Failure;

        EnsureOrbitInitialized();

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

        float sens = o.orbitMouseSensitivity > 0f ? o.orbitMouseSensitivity : o.mouseSensitivity;
        yaw += Input.GetAxis("Mouse X") * sens;
        pitch -= Input.GetAxis("Mouse Y") * sens;
        pitch = Mathf.Clamp(pitch, o.minOrbitPitch, o.maxOrbitPitch);

        Vector3 center = followTarget != null ? followTarget.position : pivot.position;
        pivot.position = center;
        pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

        float dist = Mathf.Max(0.5f, o.orbitDistance);
        cameraTransform.position = pivot.position - pivot.forward * dist;
        cameraTransform.LookAt(pivot.position + pivot.forward * 0.1f, Vector3.up);

        return BehaviorTreeStatus.Success;
    }
}
