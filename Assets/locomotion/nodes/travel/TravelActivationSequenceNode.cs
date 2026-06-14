using UnityEngine;

/// <summary>Concrete sequence root for mode-transition activation prefab templates.</summary>
public sealed class TravelActivationSequenceNode : BehaviorTreeNode
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
            if (children[i] == null)
                continue;
            BehaviorTreeStatus s = children[i].Execute(tree);
            if (s != BehaviorTreeStatus.Success)
                return s;
        }

        return BehaviorTreeStatus.Success;
    }
}
