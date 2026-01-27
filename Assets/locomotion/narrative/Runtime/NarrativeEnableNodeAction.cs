using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to enable or disable a behavior tree node.
    /// </summary>
    [Serializable]
    public class NarrativeEnableNodeAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the target GameObject with BehaviorTree")]
        public string targetKey = "target";

        [Tooltip("Name or ID of the node to enable/disable")]
        public string nodeKey = "";

        [Tooltip("True to enable, false to disable")]
        public bool enable = true;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(targetKey, out var targetGo) || targetGo == null)
            {
                Debug.LogWarning("[NarrativeEnableNodeAction] Could not resolve target GameObject");
                return BehaviorTreeStatus.Failure;
            }

            // Get BehaviorTree component using reflection
            var behaviorTreeType = System.Type.GetType("BehaviorTree, Locomotion.Runtime");
            if (behaviorTreeType == null)
            {
                behaviorTreeType = System.Type.GetType("BehaviorTree, Assembly-CSharp");
            }

            if (behaviorTreeType == null)
            {
                Debug.LogError("[NarrativeEnableNodeAction] Could not find BehaviorTree type");
                return BehaviorTreeStatus.Failure;
            }

            var behaviorTree = targetGo.GetComponent(behaviorTreeType);
            if (behaviorTree == null)
            {
                Debug.LogWarning("[NarrativeEnableNodeAction] Target does not have BehaviorTree component");
                return BehaviorTreeStatus.Failure;
            }

            // Get BehaviorTreeNode type
            var nodeType = System.Type.GetType("BehaviorTreeNode, Locomotion.Runtime");
            if (nodeType == null)
            {
                nodeType = System.Type.GetType("BehaviorTreeNode, Assembly-CSharp");
            }

            if (nodeType == null)
            {
                Debug.LogError("[NarrativeEnableNodeAction] Could not find BehaviorTreeNode type");
                return BehaviorTreeStatus.Failure;
            }

            // Find node by name or ID
            // This would require access to the tree structure
            // For now, use reflection to find methods
            var findNodeMethod = behaviorTreeType.GetMethod("FindNode");
            if (findNodeMethod == null)
            {
                // Try alternative method names
                var getNodeMethod = behaviorTreeType.GetMethod("GetNode");
                if (getNodeMethod != null)
                {
                    object node = getNodeMethod.Invoke(behaviorTree, new object[] { nodeKey });
                    if (node != null)
                    {
                        // Set enabled property
                        var enabledProp = nodeType.GetProperty("enabled");
                        if (enabledProp != null && enabledProp.CanWrite)
                        {
                            enabledProp.SetValue(node, enable);
                            return BehaviorTreeStatus.Success;
                        }
                    }
                }
            }
            else
            {
                object node = findNodeMethod.Invoke(behaviorTree, new object[] { nodeKey });
                if (node != null)
                {
                    var enabledProp = nodeType.GetProperty("enabled");
                    if (enabledProp != null && enabledProp.CanWrite)
                    {
                        enabledProp.SetValue(node, enable);
                        return BehaviorTreeStatus.Success;
                    }
                }
            }

            Debug.LogWarning($"[NarrativeEnableNodeAction] Could not find or enable/disable node: {nodeKey}");
            return BehaviorTreeStatus.Failure;
        }
    }
}
