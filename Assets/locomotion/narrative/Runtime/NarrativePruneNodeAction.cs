using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to prune/remove behavior tree nodes.
    /// </summary>
    [Serializable]
    public class NarrativePruneNodeAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the target GameObject with BehaviorTree")]
        public string targetKey = "target";

        [Tooltip("Behavior tree node to prune (optional, if empty uses targetKey)")]
        public string nodeToPrune = "";

        [Tooltip("If true, drop the whole subtree from this node")]
        public bool dropWholeTree = false;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(targetKey, out var targetGo) || targetGo == null)
            {
                Debug.LogWarning("[NarrativePruneNodeAction] Could not resolve target GameObject");
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
                Debug.LogError("[NarrativePruneNodeAction] Could not find BehaviorTree type");
                return BehaviorTreeStatus.Failure;
            }

            var behaviorTree = targetGo.GetComponent(behaviorTreeType);
            if (behaviorTree == null)
            {
                Debug.LogWarning("[NarrativePruneNodeAction] Target does not have BehaviorTree component");
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
                Debug.LogError("[NarrativePruneNodeAction] Could not find BehaviorTreeNode type");
                return BehaviorTreeStatus.Failure;
            }

            object nodeToRemove = null;

            if (!string.IsNullOrEmpty(nodeToPrune))
            {
                // Find node by key
                var findNodeMethod = behaviorTreeType.GetMethod("FindNode");
                if (findNodeMethod != null)
                {
                    nodeToRemove = findNodeMethod.Invoke(behaviorTree, new object[] { nodeToPrune });
                }
            }
            else
            {
                // Use target as the node (if it's a BehaviorTreeNode)
                var nodeComponent = targetGo.GetComponent(nodeType);
                if (nodeComponent != null)
                {
                    nodeToRemove = nodeComponent;
                }
            }

            if (nodeToRemove == null)
            {
                Debug.LogWarning("[NarrativePruneNodeAction] Could not find node to prune");
                return BehaviorTreeStatus.Failure;
            }

            // Find parent node
            object parentNode = FindParentNode(behaviorTree, nodeToRemove, nodeType);

            if (parentNode != null)
            {
                // Remove from parent's children
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
                        if (dropWholeTree)
                        {
                            // Remove entire subtree
                            RemoveSubtree(children, nodeToRemove);
                        }
                        else
                        {
                            // Remove just this node
                            children.Remove(nodeToRemove);
                        }
                        return BehaviorTreeStatus.Success;
                    }
                }
            }
            else
            {
                // Might be root node - handle specially
                var rootProp = behaviorTreeType.GetProperty("rootNode");
                if (rootProp != null)
                {
                    var root = rootProp.GetValue(behaviorTree);
                    if (root == nodeToRemove)
                    {
                        // Clear root
                        rootProp.SetValue(behaviorTree, null);
                        return BehaviorTreeStatus.Success;
                    }
                }
            }

            Debug.LogWarning("[NarrativePruneNodeAction] Could not prune node");
            return BehaviorTreeStatus.Failure;
        }

        private object FindParentNode(object behaviorTree, object targetNode, System.Type nodeType)
        {
            // This would require traversing the tree
            // For now, try to find a method that does this
            var findParentMethod = behaviorTree.GetType().GetMethod("FindParent");
            if (findParentMethod != null)
            {
                return findParentMethod.Invoke(behaviorTree, new object[] { targetNode });
            }

            // Fallback: traverse tree manually
            var rootProp = behaviorTree.GetType().GetProperty("rootNode");
            if (rootProp != null)
            {
                var root = rootProp.GetValue(behaviorTree);
                return FindParentRecursive(root, targetNode, nodeType);
            }

            return null;
        }

        private object FindParentRecursive(object currentNode, object targetNode, System.Type nodeType)
        {
            if (currentNode == null)
                return null;

            // Get children
            var childrenProp = nodeType.GetProperty("children");
            if (childrenProp == null)
            {
                childrenProp = nodeType.GetProperty("childNodes");
            }

            if (childrenProp != null)
            {
                var children = childrenProp.GetValue(currentNode) as System.Collections.IList;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child == targetNode)
                        {
                            return currentNode;
                        }

                        var found = FindParentRecursive(child, targetNode, nodeType);
                        if (found != null)
                            return found;
                    }
                }
            }

            return null;
        }

        private void RemoveSubtree(System.Collections.IList children, object nodeToRemove)
        {
            children.Remove(nodeToRemove);
        }
    }
}
