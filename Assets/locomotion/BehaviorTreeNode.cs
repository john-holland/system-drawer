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

    [Header("Duration")]
    [Tooltip("Estimated duration in seconds (calculated from cards)")]
    public float estimatedDuration = 0f;

    [Tooltip("Actual measured duration (runtime)")]
    public float actualDuration = 0f;

    [Tooltip("Use card-based duration estimation")]
    public bool useCardEstimation = true;

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

    /// <summary>
    /// Estimate duration from available cards.
    /// </summary>
    public virtual float EstimateDurationFromCards(List<GoodSection> cards)
    {
        if (!useCardEstimation || cards == null || cards.Count == 0)
            return estimatedDuration;

        // Default implementation: sum durations of applicable cards
        float totalDuration = 0f;
        foreach (var card in cards)
        {
            if (card != null)
            {
                // Estimate from card's impulse actions
                float cardDuration = 0f;
                if (card.impulseStack != null)
                {
                    foreach (var action in card.impulseStack)
                    {
                        if (action != null && action.duration > 0f)
                        {
                            cardDuration += action.duration;
                        }
                        else if (action != null)
                        {
                            cardDuration += 0.1f; // Default duration for actions without explicit duration
                        }
                    }
                }
                totalDuration += cardDuration;
            }
        }

        estimatedDuration = totalDuration;
        return estimatedDuration;
    }

    /// <summary>
    /// Calculate total duration including children.
    /// </summary>
    public virtual float CalculateDuration()
    {
        if (children == null || children.Count == 0)
            return estimatedDuration;

        float totalDuration = estimatedDuration;
        foreach (var child in children)
        {
            if (child != null)
            {
                totalDuration += child.CalculateDuration();
            }
        }

        return totalDuration;
    }
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
