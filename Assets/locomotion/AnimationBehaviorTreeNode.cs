using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized BehaviorTreeNode for animation frames.
/// Represents a single frame of animation as a behavior tree node with physics card data.
/// </summary>
public class AnimationBehaviorTreeNode : BehaviorTreeNode
{
    [Header("Animation Frame Data")]
    [Tooltip("Frame number in animation")]
    public int frameIndex = 0;

    [Tooltip("Time position in animation")]
    public float frameTime = 0f;

    [Tooltip("Reference to source animation")]
    public AnimationClip animationClip;

    [Tooltip("Reference to root AnimationBehaviorTree component")]
    public AnimationBehaviorTree rootBehaviorTree;

    [Header("Physics Card")]
    [Tooltip("Generated physics card for this frame")]
    public GoodSection physicsCard;

    [Header("Bone Transforms")]
    [Tooltip("Bone transforms at this frame")]
    public Dictionary<string, TransformData> boneTransforms = new Dictionary<string, TransformData>();

    [Header("Attenuation")]
    [Tooltip("Attenuation curve for this frame")]
    public AnimationCurve attenuationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private bool isExecuting = false;
    private float executionStartTime = 0f;

    private void Awake()
    {
        if (nodeType != NodeType.Sequence)
            nodeType = NodeType.Action;
    }

    int _sequenceChildIndex;
    bool _sequenceChildActive;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (nodeType == NodeType.Sequence)
            return ExecuteSequence(tree);

        if (physicsCard == null)
        {
            Debug.LogWarning($"AnimationBehaviorTreeNode {gameObject.name}: No physics card assigned.");
            return BehaviorTreeStatus.Failure;
        }

        // Start execution if not already executing
        if (!isExecuting)
        {
            isExecuting = true;
            executionStartTime = Time.time;
            physicsCard.Execute(tree.GetComponent<RagdollSystem>()?.GetCurrentState() ?? new RagdollState());
        }

        // Update physics card
        RagdollState currentState = tree.GetComponent<RagdollSystem>()?.GetCurrentState() ?? new RagdollState();
        float direction = rootBehaviorTree != null ? rootBehaviorTree.playbackDirection : 1;
        float delta = direction < 0 ? -Time.deltaTime : Time.deltaTime;
        bool stillExecuting = physicsCard.Update(currentState, delta);

        if (!stillExecuting)
        {
            isExecuting = false;
            actualDuration = Time.time - executionStartTime;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Running;
    }

    BehaviorTreeStatus ExecuteSequence(BehaviorTree tree)
    {
        if (children == null || children.Count == 0)
            return BehaviorTreeStatus.Success;

        int direction = rootBehaviorTree != null ? rootBehaviorTree.playbackDirection : 1;
        if (direction >= 0)
        {
            for (int i = _sequenceChildIndex; i < children.Count; i++)
            {
                BehaviorTreeNode child = children[i];
                if (child == null)
                    continue;

                if (!_sequenceChildActive)
                {
                    child.OnEnter(tree);
                    _sequenceChildActive = true;
                }

                BehaviorTreeStatus status = child.Execute(tree);
                if (status == BehaviorTreeStatus.Running)
                {
                    _sequenceChildIndex = i;
                    return BehaviorTreeStatus.Running;
                }

                if (status == BehaviorTreeStatus.Failure)
                {
                    child.OnExit(tree);
                    _sequenceChildActive = false;
                    return BehaviorTreeStatus.Failure;
                }

                child.OnExit(tree);
                _sequenceChildActive = false;
            }
        }
        else
        {
            for (int i = _sequenceChildIndex; i >= 0; i--)
            {
                BehaviorTreeNode child = children[i];
                if (child == null)
                    continue;

                if (!_sequenceChildActive)
                {
                    child.OnEnter(tree);
                    _sequenceChildActive = true;
                }

                BehaviorTreeStatus status = child.Execute(tree);
                if (status == BehaviorTreeStatus.Running)
                {
                    _sequenceChildIndex = i;
                    return BehaviorTreeStatus.Running;
                }

                if (status == BehaviorTreeStatus.Failure)
                {
                    child.OnExit(tree);
                    _sequenceChildActive = false;
                    return BehaviorTreeStatus.Failure;
                }

                child.OnExit(tree);
                _sequenceChildActive = false;
            }
        }

        return BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _sequenceChildIndex = nodeType == NodeType.Sequence && rootBehaviorTree != null &&
            rootBehaviorTree.playbackDirection < 0 && children != null && children.Count > 0
            ? children.Count - 1
            : 0;
        _sequenceChildActive = false;
        isExecuting = false;
        executionStartTime = 0f;
        if (physicsCard != null)
        {
            physicsCard.Stop();
        }
    }

    public override void OnExit(BehaviorTree tree)
    {
        isExecuting = false;
        if (physicsCard != null)
        {
            physicsCard.Stop();
        }
    }

    /// <summary>
    /// Get frame data (bone transforms).
    /// </summary>
    public Dictionary<string, TransformData> GetFrameData()
    {
        return new Dictionary<string, TransformData>(boneTransforms);
    }

    /// <summary>
    /// Update physics card from frame data.
    /// </summary>
    public void UpdatePhysicsCard()
    {
        if (rootBehaviorTree == null)
            return;

        RagdollSystem ragdoll = GetComponentInParent<RagdollSystem>();
        if (ragdoll == null)
            ragdoll = FindAnyObjectByType<RagdollSystem>();

        if (ragdoll == null)
            return;

        // Create animation frame from this node's data
        AnimationFrame frame = new AnimationFrame
        {
            frameIndex = this.frameIndex,
            time = this.frameTime,
            boneTransforms = new Dictionary<string, TransformData>(this.boneTransforms)
        };

        // Generate physics card
        physicsCard = AnimationPhysicsCardGenerator.GenerateCardFromFrame(frame, ragdoll);
    }

    /// <summary>
    /// Override to estimate duration from frame physics card.
    /// </summary>
    public override float EstimateDurationFromCards(List<GoodSection> cards)
    {
        if (physicsCard != null)
        {
            return BehaviorTreeDurationEstimator.EstimateCardDuration(physicsCard);
        }

        return base.EstimateDurationFromCards(cards);
    }
}
