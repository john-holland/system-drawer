using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Utility class for estimating behavior tree duration from physics cards.
/// </summary>
public static class BehaviorTreeDurationEstimator
{
    /// <summary>
    /// Estimate duration for a single node.
    /// </summary>
    public static float EstimateNodeDuration(BehaviorTreeNode node, List<GoodSection> cards)
    {
        if (node == null)
            return 0f;

        // If node has card estimation enabled, use it
        if (node.useCardEstimation)
        {
            return node.EstimateDurationFromCards(cards);
        }

        return node.estimatedDuration;
    }

    /// <summary>
    /// Estimate total tree duration.
    /// </summary>
    public static float EstimateTreeDuration(BehaviorTree tree, List<GoodSection> cards)
    {
        if (tree == null || tree.rootNode == null)
            return 0f;

        return EstimateNodeDuration(tree.rootNode, cards);
    }

    /// <summary>
    /// Estimate duration from card properties.
    /// </summary>
    public static float EstimateCardDuration(GoodSection card)
    {
        if (card == null)
            return 0f;

        float duration = 0f;

        // Sum durations from impulse actions
        if (card.impulseStack != null)
        {
            foreach (var action in card.impulseStack)
            {
                if (action != null)
                {
                    if (action.duration > 0f)
                    {
                        duration += action.duration;
                    }
                    else
                    {
                        duration += 0.1f; // Default duration for actions without explicit duration
                    }
                }
            }
        }

        // If no actions have duration, use a default
        if (duration <= 0f)
        {
            duration = 0.5f; // Default card duration
        }

        return duration;
    }

    /// <summary>
    /// Calculate duration for branching nodes (Selector, Condition).
    /// </summary>
    public static float CalculateBranchingDuration(BehaviorTreeNode node, List<GoodSection> cards)
    {
        if (node == null || node.children == null || node.children.Count == 0)
            return EstimateNodeDuration(node, cards);

        // For Selector: use maximum duration (worst case - try all children)
        if (node.nodeType == NodeType.Selector)
        {
            float maxDuration = 0f;
            foreach (var child in node.children)
            {
                if (child != null)
                {
                    float childDuration = EstimateNodeDuration(child, cards);
                    maxDuration = Mathf.Max(maxDuration, childDuration);
                }
            }
            return maxDuration;
        }

        // For Condition: use average of true/false branches
        if (node.nodeType == NodeType.Condition)
        {
            float totalDuration = 0f;
            int count = 0;
            foreach (var child in node.children)
            {
                if (child != null)
                {
                    totalDuration += EstimateNodeDuration(child, cards);
                    count++;
                }
            }
            return count > 0 ? totalDuration / count : 0f;
        }

        // Default: sum of children
        return CalculateSequenceDuration(node, cards);
    }

    /// <summary>
    /// Calculate duration for sequence nodes (sum of children).
    /// </summary>
    public static float CalculateSequenceDuration(BehaviorTreeNode node, List<GoodSection> cards)
    {
        if (node == null || node.children == null || node.children.Count == 0)
            return EstimateNodeDuration(node, cards);

        float totalDuration = EstimateNodeDuration(node, cards);
        foreach (var child in node.children)
        {
            if (child != null)
            {
                totalDuration += EstimateNodeDuration(child, cards);
            }
        }

        return totalDuration;
    }
}
