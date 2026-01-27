using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to append a behavior tree node to a target.
    /// </summary>
    [Serializable]
    public class NarrativeAppendNodeAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the target GameObject with BehaviorTree")]
        public string targetKey = "target";

        [Tooltip("Behavior tree node to append (reference)")]
        public UnityEngine.Object nodeToAppend;

        [Tooltip("Parent node name/ID to append to (empty = root)")]
        public string parentNodeKey = "";

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (nodeToAppend == null)
            {
                Debug.LogWarning("[NarrativeAppendNodeAction] No node to append specified");
                return BehaviorTreeStatus.Failure;
            }

            if (!ctx.TryResolveGameObject(targetKey, out var targetGo) || targetGo == null)
            {
                Debug.LogWarning("[NarrativeAppendNodeAction] Could not resolve target GameObject");
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
                Debug.LogError("[NarrativeAppendNodeAction] Could not find BehaviorTree type");
                return BehaviorTreeStatus.Failure;
            }

            var behaviorTree = targetGo.GetComponent(behaviorTreeType);
            if (behaviorTree == null)
            {
                Debug.LogWarning("[NarrativeAppendNodeAction] Target does not have BehaviorTree component");
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
                Debug.LogError("[NarrativeAppendNodeAction] Could not find BehaviorTreeNode type");
                return BehaviorTreeStatus.Failure;
            }

            // Check if nodeToAppend is a BehaviorTreeNode
            if (!nodeType.IsAssignableFrom(nodeToAppend.GetType()))
            {
                Debug.LogWarning("[NarrativeAppendNodeAction] nodeToAppend is not a BehaviorTreeNode");
                return BehaviorTreeStatus.Failure;
            }

            // Find parent node if specified
            object parentNode = null;
            if (!string.IsNullOrEmpty(parentNodeKey))
            {
                var findNodeMethod = behaviorTreeType.GetMethod("FindNode");
                if (findNodeMethod != null)
                {
                    parentNode = findNodeMethod.Invoke(behaviorTree, new object[] { parentNodeKey });
                }
            }

            // Get children property or method
            if (parentNode == null)
            {
                // Append to root
                var rootProp = behaviorTreeType.GetProperty("rootNode");
                if (rootProp != null)
                {
                    parentNode = rootProp.GetValue(behaviorTree);
                }
            }

            if (parentNode != null)
            {
                // Get children list
                var childrenProp = nodeType.GetProperty("children");
                if (childrenProp == null)
                {
                    childrenProp = nodeType.GetProperty("childNodes");
                }

                if (childrenProp != null)
                {
                    var children = childrenProp.GetValue(parentNode) as System.Collections.IList;
                    if (children != null)
                    {
                        children.Add(nodeToAppend);
                        return BehaviorTreeStatus.Success;
                    }
                }
            }

            Debug.LogWarning("[NarrativeAppendNodeAction] Could not append node");
            return BehaviorTreeStatus.Failure;
        }
    }
}
