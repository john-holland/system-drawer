using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the default get-up BehaviorTree hierarchy (Selector → Sequence OnGround+GetUp + IdleSuccess).
/// Shared by the editor prefab menu and runtime bootstrap fallback.
/// </summary>
public static class RagdollGetUpTreeFactory
{
    public const string DefaultTreeName = "RagdollGetUpBehaviorTree";
    public const string PrefabAssetPath = "Assets/locomotion/Prefabs/ActorRagdolls/RagdollGetUpBehaviorTree.prefab";
    public const string ResourcesAssetPath = "Assets/locomotion/Resources/RagdollGetUpBehaviorTree.prefab";
    public const string ResourcesLoadName = "RagdollGetUpBehaviorTree";

    public static BehaviorTree Build(Transform parent = null)
    {
        var root = new GameObject(DefaultTreeName);
        if (parent != null)
            root.transform.SetParent(parent, false);

        var bt = root.AddComponent<BehaviorTree>();
        bt.decisionTime = 0f;

        var selector = root.AddComponent<RagdollGetUpSelectorNode>();
        bt.rootNode = selector;

        var sequenceGo = new GameObject("GetUpSequence");
        sequenceGo.transform.SetParent(root.transform, false);
        var sequence = sequenceGo.AddComponent<RagdollPlayerSequenceNode>();

        var conditionGo = new GameObject("OnGroundAndFallen");
        conditionGo.transform.SetParent(sequenceGo.transform, false);
        var condition = conditionGo.AddComponent<RagdollOnGroundConditionNode>();

        var actionGo = new GameObject("GetUp");
        actionGo.transform.SetParent(sequenceGo.transform, false);
        var action = actionGo.AddComponent<RagdollGetUpActionNode>();
        condition.getUpAction = action;

        sequence.children = new List<BehaviorTreeNode> { condition, action };

        var idleGo = new GameObject("IdleSuccess");
        idleGo.transform.SetParent(root.transform, false);
        var idle = idleGo.AddComponent<RagdollIdleSuccessNode>();

        selector.children = new List<BehaviorTreeNode> { sequence };
        selector.passthroughChild = idle;

        return bt;
    }
}
