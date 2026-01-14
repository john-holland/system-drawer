using UnityEngine;

// Component for denoting this object as a container for behavior tree nodes
// Used so the root level of the SpatialGenerator can be used for holding the actual generated scenegraph
[RequireComponent(typeof(Transform))]
public class SGTreeNodeContainer : MonoBehaviour
{
    [Header("Behavior Tree Configuration")]
    public SGBehaviorTreeNode rootNode;
    
    void Start()
    {
        // Auto-find root node if not set
        if (rootNode == null)
        {
            rootNode = GetComponentInChildren<SGBehaviorTreeNode>();
        }
    }
    
    public SGBehaviorTreeNode GetRootNode()
    {
        if (rootNode == null)
        {
            rootNode = GetComponentInChildren<SGBehaviorTreeNode>();
        }
        return rootNode;
    }
}
