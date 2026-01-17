using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Physics card solver that finds applicable good sections from current state,
/// orders them by feasibility, performs topological searches, and matches cards to behavior tree goals.
/// </summary>
public class PhysicsCardSolver : MonoBehaviour
{
    [Header("Available Cards")]
    [Tooltip("Current pool of available good sections")]
    public List<GoodSection> availableCards = new List<GoodSection>();

    [Header("Feasibility Weights")]
    [Tooltip("Weight for degrees difference (default 0.3)")]
    [Range(0f, 1f)]
    public float degreesWeight = 0.3f;

    [Tooltip("Weight for torque feasibility (default 0.3)")]
    [Range(0f, 1f)]
    public float torqueWeight = 0.3f;

    [Tooltip("Weight for force feasibility (default 0.2)")]
    [Range(0f, 1f)]
    public float forceWeight = 0.2f;

    [Tooltip("Weight for velocity change likelihood (default 0.2)")]
    [Range(0f, 1f)]
    public float velocityWeight = 0.2f;

    // References
    private NervousSystem nervousSystem;
    private RagdollSystem ragdollSystem;
    private TemporalGraph temporalGraph;

    private void Awake()
    {
        // Get references
        nervousSystem = GetComponent<NervousSystem>();
        ragdollSystem = GetComponent<RagdollSystem>();

        if (nervousSystem != null)
        {
            temporalGraph = nervousSystem.GetTemporalGraph();
        }
    }

    /// <summary>
    /// Find applicable cards from current state for a target object.
    /// </summary>
    public List<GoodSection> FindApplicableCards(RagdollState state, GameObject target = null)
    {
        List<GoodSection> applicable = new List<GoodSection>();

        // Check all available cards
        foreach (var card in availableCards)
        {
            if (card != null && card.IsFeasible(state))
            {
                applicable.Add(card);
            }
        }

        // Query nervous system for additional cards
        if (nervousSystem != null)
        {
            var nervousCards = nervousSystem.GetAvailableGoodSections(target);
            foreach (var card in nervousCards)
            {
                if (card != null && !applicable.Contains(card) && card.IsFeasible(state))
                {
                    applicable.Add(card);
                }
            }
        }

        return applicable;
    }

    /// <summary>
    /// Order cards by feasibility score (highest first).
    /// </summary>
    public List<GoodSection> OrderCardsByFeasibility(List<GoodSection> cards, RagdollState state)
    {
        if (cards == null || state == null)
            return new List<GoodSection>();

        // Calculate feasibility scores
        var scoredCards = cards.Select(card => new
        {
            card = card,
            score = CalculateFeasibilityScore(card, state)
        }).OrderByDescending(x => x.score).ToList();

        // Return ordered list
        return scoredCards.Select(x => x.card).ToList();
    }

    /// <summary>
    /// Calculate feasibility score for a card given current state.
    /// </summary>
    public float CalculateFeasibilityScore(GoodSection card, RagdollState state)
    {
        if (card == null || state == null)
            return 0f;

        float score = 0f;

        // Degrees difference (30% weight, or custom weight)
        float degreesDiff = CalculateDegreesDifference(card, state);
        float degreesScore = 1f - Mathf.Clamp01(degreesDiff / 180f);
        score += degreesScore * degreesWeight;

        // Torque feasibility (30% weight)
        float torqueFeasibility = CheckTorqueFeasibility(card, state);
        score += torqueFeasibility * torqueWeight;

        // Force feasibility (20% weight)
        float forceFeasibility = CheckForceFeasibility(card, state);
        score += forceFeasibility * forceWeight;

        // Velocity change likelihood (20% weight)
        float velocityLikelihood = EstimateVelocityChangeLikelihood(card, state);
        score += velocityLikelihood * velocityWeight;

        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Solve for a goal (find card sequence that achieves the goal).
    /// </summary>
    public List<GoodSection> SolveForGoal(BehaviorTreeGoal goal, RagdollState state)
    {
        if (goal == null || state == null)
            return new List<GoodSection>();

        // Find applicable cards
        List<GoodSection> applicable = FindApplicableCards(state, goal.target);

        // Order by feasibility
        List<GoodSection> ordered = OrderCardsByFeasibility(applicable, state);

        // Try to find direct path to goal
        if (temporalGraph != null && ordered.Count > 0)
        {
            // Find card that matches goal type
            GoodSection goalCard = FindCardMatchingGoal(ordered, goal);
            if (goalCard != null)
            {
                // Find path to goal card
                return temporalGraph.FindPath(state, goalCard);
            }
        }

        // Fallback: return most feasible card
        if (ordered.Count > 0)
        {
            return new List<GoodSection> { ordered[0] };
        }

        return new List<GoodSection>();
    }

    /// <summary>
    /// Perform topological search (find path through graph from start to goal).
    /// </summary>
    public List<GoodSection> TopologicalSearch(GoodSection start, GoodSection goal)
    {
        if (start == null || goal == null || temporalGraph == null)
            return new List<GoodSection>();

        // Use temporal graph's pathfinding
        RagdollState currentState = ragdollSystem != null ? ragdollSystem.GetCurrentState() : new RagdollState();
        return temporalGraph.FindPath(currentState, goal);
    }

    /// <summary>
    /// Add cards to available pool.
    /// </summary>
    public void AddCards(List<GoodSection> cards)
    {
        if (cards == null)
            return;

        foreach (var card in cards)
        {
            if (card != null && !availableCards.Contains(card))
            {
                availableCards.Add(card);
            }
        }
    }

    /// <summary>
    /// Remove cards from available pool.
    /// </summary>
    public void RemoveCards(List<GoodSection> cards)
    {
        if (cards == null)
            return;

        foreach (var card in cards)
        {
            availableCards.Remove(card);
        }
    }

    /// <summary>
    /// Clear all available cards.
    /// </summary>
    public void ClearCards()
    {
        availableCards.Clear();
    }

    // Helper methods for feasibility scoring

    private float CalculateDegreesDifference(GoodSection card, RagdollState state)
    {
        if (card.requiredState == null)
            return 0f;

        return card.requiredState.CalculateDistance(state) * 180f;
    }

    private float CheckTorqueFeasibility(GoodSection card, RagdollState state)
    {
        if (card.limits == null || card.requiredState == null)
            return 1f;

        return card.limits.GetLimitScore(state, card.requiredState);
    }

    private float CheckForceFeasibility(GoodSection card, RagdollState state)
    {
        if (card.limits == null || card.requiredState == null)
            return 1f;

        // Similar to torque, but focused on force
        return card.limits.GetLimitScore(state, card.requiredState);
    }

    private float EstimateVelocityChangeLikelihood(GoodSection card, RagdollState state)
    {
        if (card.requiredState == null)
            return 1f;

        float velChange = (card.requiredState.rootVelocity - state.rootVelocity).magnitude;
        float maxVelChange = card.limits != null ? card.limits.maxVelocityChange : 10f;

        return 1f - Mathf.Clamp01(velChange / maxVelChange);
    }

    private GoodSection FindCardMatchingGoal(List<GoodSection> cards, BehaviorTreeGoal goal)
    {
        // Find card that matches goal type or name
        foreach (var card in cards)
        {
            if (card == null)
                continue;

            // Check if card name matches goal name
            if (!string.IsNullOrEmpty(card.sectionName) && 
                !string.IsNullOrEmpty(goal.goalName) &&
                card.sectionName.Contains(goal.goalName))
            {
                return card;
            }

            // Check if card's behavior tree matches goal type
            if (card.behaviorTree != null && goal.type != GoalType.Composite)
            {
                // This would require behavior tree matching logic
                // For now, return first feasible card
                return card;
            }
        }

        return cards.Count > 0 ? cards[0] : null;
    }
}
