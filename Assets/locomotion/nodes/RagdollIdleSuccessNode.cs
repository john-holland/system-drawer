using UnityEngine;

/// <summary>Leaf that always succeeds — default passthrough when get-up is not needed.</summary>
public class RagdollIdleSuccessNode : BehaviorTreeNode
{
    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        status = BehaviorTreeStatus.Success;
        return BehaviorTreeStatus.Success;
    }
}
