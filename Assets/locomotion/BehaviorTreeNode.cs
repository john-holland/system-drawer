using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for behavior tree nodes.
/// Supports Sequence, Selector, Condition, Action, and Decorator node types.
/// </summary>
public abstract class BehaviorTreeNode
{
    [Header("Node Properties")]
    [Tooltip("Type of this node")]
    public NodeType nodeType = NodeType.Action;

    [Tooltip("Child nodes (for Sequence, Selector, Decorator)")]
    public List<BehaviorTreeNode> children = new List<BehaviorTreeNode>();

    [Tooltip("Current execution status")]
    public BehaviorTreeStatus status = BehaviorTreeStatus.Running;

    /// <summary>
    /// Execute this node.
    /// </summary>
    public abstract BehaviorTreeStatus Execute(BehaviorTree tree);

    /// <summary>
    /// Prune invalid branches based on available cards.
    /// </summary>
    public virtual void PruneForCards(List<GoodSection> cards)
    {
        // Prune children
        foreach (var child in children)
        {
            if (child != null)
            {
                child.PruneForCards(cards);
            }
        }
    }

    /// <summary>
    /// OnEnter callback (called when node starts executing).
    /// </summary>
    public virtual void OnEnter(BehaviorTree tree) { }

    /// <summary>
    /// OnExit callback (called when node finishes executing).
    /// </summary>
    public virtual void OnExit(BehaviorTree tree) { }

    /// <summary>
    /// OnUpdate callback (called every frame while node is running).
    /// </summary>
    public virtual void OnUpdate(BehaviorTree tree) { }
}

/// <summary>
/// Types of behavior tree nodes.
/// </summary>
public enum NodeType
{
    Sequence,    // Execute children in order until one fails
    Selector,    // Execute children until one succeeds
    Condition,   // Check a condition
    Action,      // Execute an action
    Decorator    // Modify child behavior
}
