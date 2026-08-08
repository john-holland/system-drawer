using UnityEngine;
using Locomotion.Musculature;

/// <summary>
/// Pelvis-lift get-up (ports NarrativeStandAction approach): release FixedJoints, raise pelvis to stand height.
/// </summary>
public class RagdollGetUpActionNode : BehaviorTreeNode
{
    [Tooltip("How high to raise the pelvis from its start position.")]
    public float standHeightOffset = 0.5f;

    [Tooltip("Lerp speed toward stand pose (progress units per second).")]
    [Range(0.1f, 10f)]
    public float standUpSpeed = 2f;

    [Tooltip("Distance to target stand position for Success.")]
    public float standTolerance = 0.08f;

    public RagdollSystem ragdollSystem;

    bool recovering;
    float progress;
    Vector3 targetStandPosition;

    /// <summary>True while a get-up episode is in progress (keeps the OnGround condition latched).</summary>
    public bool IsRecovering => recovering;

    void Awake()
    {
        nodeType = NodeType.Action;
        if (ragdollSystem == null)
            ragdollSystem = GetComponentInParent<RagdollSystem>();
    }

    public override void OnEnter(BehaviorTree tree)
    {
        // Fresh enter does not force restart mid-episode; StartRecovery is called from Execute.
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (ragdollSystem == null)
            ragdollSystem = tree != null
                ? tree.GetComponentInParent<RagdollSystem>()
                : GetComponentInParent<RagdollSystem>();

        if (ragdollSystem == null)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        Transform pelvis = RagdollGroundCheck.ResolvePelvisOrRoot(ragdollSystem);
        if (pelvis == null)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        if (!recovering)
        {
            if (!RagdollGroundCheck.IsFallen(ragdollSystem))
            {
                status = BehaviorTreeStatus.Failure;
                return BehaviorTreeStatus.Failure;
            }

            BeginRecovery(pelvis);
        }

        progress += standUpSpeed * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        Vector3 newPos = Vector3.Lerp(pelvis.position, targetStandPosition, progress);
        Rigidbody rb = pelvis.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            rb.MovePosition(newPos);
        else
            pelvis.position = newPos;

        float distance = Vector3.Distance(pelvis.position, targetStandPosition);
        if (distance <= standTolerance && progress >= 1f)
        {
            recovering = false;
            progress = 0f;
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }

        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }

    void BeginRecovery(Transform pelvis)
    {
        recovering = true;
        progress = 0f;
        targetStandPosition = pelvis.position + Vector3.up * standHeightOffset;

        GameObject actorGo = ragdollSystem.gameObject;
        FixedJoint[] fixedJoints = actorGo.GetComponentsInChildren<FixedJoint>();
        for (int i = 0; i < fixedJoints.Length; i++)
        {
            if (fixedJoints[i] != null)
                Destroy(fixedJoints[i]);
        }

        // Soften muscle drive so the lift is not fighting full activations.
        if (ragdollSystem.muscleGroups != null)
        {
            for (int i = 0; i < ragdollSystem.muscleGroups.Count; i++)
            {
                MuscleGroup group = ragdollSystem.muscleGroups[i];
                if (group == null || string.IsNullOrEmpty(group.groupName))
                    continue;
                ragdollSystem.ActivateMuscleGroup(group.groupName, 0f);
            }
        }
    }

    /// <summary>Reset recovery state (tests / reentry).</summary>
    public void ResetRecovery()
    {
        recovering = false;
        progress = 0f;
    }
}
