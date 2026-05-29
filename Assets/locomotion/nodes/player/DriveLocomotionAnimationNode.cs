using UnityEngine;

/// <summary>Optional Animator driver: sets a speed float and moving bool from input + ragdoll root velocity.</summary>
public class DriveLocomotionAnimationNode : BehaviorTreeNode
{
    public Animator animator;
    public string speedParameter = "Speed";
    public string movingParameter = "IsMoving";
    [Min(0.001f)] public float speedScale = 1f / 5f;
    [Min(0.01f)] public float movingSpeedThreshold = 0.15f;

    RagdollPlayerInputBuffer buffer;
    RagdollSystem ragdollSystem;

    void Awake()
    {
        nodeType = NodeType.Action;
        buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
        if (ragdollSystem == null) ragdollSystem = GetComponentInParent<RagdollSystem>();
        if (animator == null && ragdollSystem != null && ragdollSystem.ragdollRoot != null)
            animator = ragdollSystem.ragdollRoot.GetComponentInChildren<Animator>();
    }

    public override bool Predicate(BehaviorTree tree)
    {
        return buffer != null && buffer.options != null && buffer.options.enableAnimations;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (buffer == null || buffer.options == null)
            return BehaviorTreeStatus.Success;

        if (animator == null)
            return BehaviorTreeStatus.Success;

        var st = buffer != null ? buffer.State : default;
        float inputMag = Mathf.Abs(st.horizontal) + Mathf.Abs(st.vertical);
        float velMag = ragdollSystem != null ? ragdollSystem.GetCurrentState().rootVelocity.magnitude : 0f;
        float blend = Mathf.Max(inputMag, velMag * speedScale);

        if (!string.IsNullOrEmpty(speedParameter))
            animator.SetFloat(speedParameter, blend);

        if (!string.IsNullOrEmpty(movingParameter))
            animator.SetBool(movingParameter, blend > movingSpeedThreshold || st.sprint);

        return BehaviorTreeStatus.Success;
    }
}
