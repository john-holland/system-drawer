using UnityEngine;

/// <summary>
/// Selector: try the get-up branch first; on Failure run <see cref="passthroughChild"/>
/// (merged prior BT root, or IdleSuccess in the default prefab).
/// </summary>
public class RagdollGetUpSelectorNode : BehaviorTreeNode
{
    [Tooltip("Prior BehaviorTree root (or IdleSuccess). Used when get-up is not applicable.")]
    public BehaviorTreeNode passthroughChild;

    void Awake()
    {
        nodeType = NodeType.Selector;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        BehaviorTreeNode getUpBranch = ResolveGetUpBranch();
        if (getUpBranch != null)
        {
            BehaviorTreeStatus s = getUpBranch.Execute(tree);
            if (s == BehaviorTreeStatus.Running || s == BehaviorTreeStatus.Success)
            {
                status = s;
                return s;
            }
        }

        if (passthroughChild != null)
        {
            status = passthroughChild.Execute(tree);
            return status;
        }

        status = BehaviorTreeStatus.Success;
        return BehaviorTreeStatus.Success;
    }

    BehaviorTreeNode ResolveGetUpBranch()
    {
        if (children == null)
            return null;

        for (int i = 0; i < children.Count; i++)
        {
            BehaviorTreeNode c = children[i];
            if (c == null || c == passthroughChild)
                continue;
            return c;
        }

        return null;
    }

    /// <summary>Used by bootstrap when wrapping an existing tree root.</summary>
    public void SetPassthrough(BehaviorTreeNode node)
    {
        passthroughChild = node;
    }
}
