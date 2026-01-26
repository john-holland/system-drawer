using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for behavior tree nodes.
/// Supports Sequence, Selector, Condition, Action, and Decorator node types.
/// </summary>
public abstract class BehaviorTreeNode : MonoBehaviour
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
    /// Should execute this node?
    /// Useful for long-running / non-pruned nodes (idle, background behaviors) and for narrative gating.
    /// </summary>
    public virtual bool Predicate(BehaviorTree tree) => true;

    /// <summary>
    /// Prune invalid branches based on available cards.
    /// Default behavior: prune null children and recurse.
    /// </summary>
    public virtual void PruneForCards(List<GoodSection> cards)
    {
        if (children == null)
            return;

        children.RemoveAll(c => c == null);
        for (int i = 0; i < children.Count; i++)
        {
            children[i].PruneForCards(cards);
        }
    }

    public virtual void OnEnter(BehaviorTree tree) { }
    public virtual void OnExit(BehaviorTree tree) { }
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
