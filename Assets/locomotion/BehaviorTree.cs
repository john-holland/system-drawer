using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior tree system for goal-oriented planning.
/// Integrates with physics card solvers and can be pruned based on available cards.
/// </summary>
public class BehaviorTree : MonoBehaviour
{
    [Header("Tree Structure")]
    [Tooltip("Root node of the behavior tree")]
    public BehaviorTreeNode rootNode;

    [Header("Current Goal")]
    [Tooltip("Current active goal")]
    public BehaviorTreeGoal currentGoal;

    [Header("Available Cards")]
    [Tooltip("Cards available from card solver")]
    public List<GoodSection> availableCards = new List<GoodSection>();

    // References
    private PhysicsCardSolver cardSolver;
    private NervousSystem nervousSystem;

    private void Awake()
    {
        cardSolver = GetComponent<PhysicsCardSolver>();
        nervousSystem = GetComponent<NervousSystem>();
    }

    /// <summary>
    /// Execute behavior tree.
    /// </summary>
    public BehaviorTreeStatus Execute()
    {
        if (rootNode == null)
            return BehaviorTreeStatus.Failure;

        // Update available cards from solver
        if (cardSolver != null)
        {
            RagdollState state = GetCurrentState();
            availableCards = cardSolver.FindApplicableCards(state, currentGoal?.target);
        }

        // Execute root node
        return rootNode.Execute(this);
    }

    /// <summary>
    /// Set new goal.
    /// </summary>
    public void SetGoal(BehaviorTreeGoal goal)
    {
        currentGoal = goal;
        
        // Notify nervous system
        if (nervousSystem != null)
        {
            nervousSystem.SetCurrentGoal(goal);
        }
    }

    /// <summary>
    /// Prune tree based on available cards.
    /// </summary>
    public void PruneForCards(List<GoodSection> cards)
    {
        availableCards = cards;
        
        // Prune invalid branches from root
        if (rootNode != null)
        {
            rootNode.PruneForCards(cards);
        }
    }

    /// <summary>
    /// Get cards required for current goal.
    /// </summary>
    public List<GoodSection> GetRequiredCards()
    {
        if (currentGoal == null || cardSolver == null)
            return new List<GoodSection>();

        RagdollState state = GetCurrentState();
        return cardSolver.SolveForGoal(currentGoal, state);
    }

    private RagdollState GetCurrentState()
    {
        RagdollSystem ragdollSystem = GetComponent<RagdollSystem>();
        if (ragdollSystem != null)
        {
            return ragdollSystem.GetCurrentState();
        }
        return new RagdollState();
    }
}

/// <summary>
/// Behavior tree execution status.
/// </summary>
public enum BehaviorTreeStatus
{
    Success,
    Failure,
    Running
}
