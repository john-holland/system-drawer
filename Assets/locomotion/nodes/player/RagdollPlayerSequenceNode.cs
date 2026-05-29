using UnityEngine;

/// <summary>
/// Runs child nodes in order each <see cref="BehaviorTree.Execute"/> call; skips children whose <see cref="BehaviorTreeNode.Predicate"/> is false.
/// </summary>
public class RagdollPlayerSequenceNode : BehaviorTreeNode
{
    void Awake()
    {
        nodeType = NodeType.Sequence;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (children == null || children.Count == 0)
            return BehaviorTreeStatus.Success;

        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c == null)
                continue;
            if (!c.Predicate(tree))
                continue;

            BehaviorTreeStatus s = c.Execute(tree);
            if (s == BehaviorTreeStatus.Failure)
                return BehaviorTreeStatus.Failure;
            if (s == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
        }

        return BehaviorTreeStatus.Success;
    }
}
