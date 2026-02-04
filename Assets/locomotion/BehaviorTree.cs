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

    [Header("Current Node")]
    [Tooltip("The current node being executed")]
    public BehaviorTreeNode currentNode;

    [Header("Last Status")]
    [Tooltip("The last status the behavior tree node left")]
    [ReadOnly]
    public BehaviorTreeStatus lastStatus;

    [Header("Decision Time")]
    [Tooltip("Time between decisions (seconds). 0 = evaluate every call.")]
    public float decisionTime = 0f;

    [Tooltip("Time of last decision")]
    public float lastDecisionTime = -999f;

    [Tooltip("Time of next decision")]
    public float nextDecisionTime = 0f;

    [Header("Current Goal")]
    [Tooltip("Current active goal")]
    public BehaviorTreeGoal currentGoal;

    [Header("Reset Behavior")]
    [Tooltip("Reset if currentNode reports anything other than root node")]
    public bool resetIfNotRoot = false;

    [Header("Available Cards")]
    [Tooltip("Cards available from card solver")]
    public List<GoodSection> availableCards = new List<GoodSection>();

    [Header("Scene Object Registry")]
    [Tooltip("Optional. Resolve goal targets by string key or synonym (throw/hit/place/carry).")]
    public SceneObjectRegistry sceneObjectRegistry;

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

        if (decisionTime > 0f && Time.time < nextDecisionTime)
            return BehaviorTreeStatus.Running;

        // Update available cards from solver
        if (cardSolver != null)
        {
            RagdollState state = GetCurrentState();
            availableCards = cardSolver.FindApplicableCards(state, currentGoal?.target);
        }

        if (resetIfNotRoot && currentNode != rootNode) {
            currentNode = rootNode;
        }

        BehaviorTreeNode execNode = rootNode;
        currentNode = execNode;

        if (!execNode.Predicate(this))
            return BehaviorTreeStatus.Running;

        BehaviorTreeStatus status = execNode.Execute(this);
        lastStatus = status;

        lastDecisionTime = Time.time;
        nextDecisionTime = Time.time + Mathf.Max(0f, decisionTime);
        return status;
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

    /// <summary>
    /// Resolve a target GameObject by string key or synonym using the optional scene object registry.
    /// </summary>
    public GameObject ResolveTarget(string keyOrSynonym)
    {
        return sceneObjectRegistry != null ? sceneObjectRegistry.Resolve(keyOrSynonym) : null;
    }

    public bool Contains(BehaviorTreeNode node) {
        if (rootNode == null)
            return false;

        Stack<BehaviorTreeNode> nodesToVisit = new Stack<BehaviorTreeNode>();
        nodesToVisit.Push(rootNode);
        while (nodesToVisit.Count > 0) {
            BehaviorTreeNode currentNode = nodesToVisit.Pop();
            if (currentNode == node)
                return true;
            foreach (var child in currentNode.children)
                nodesToVisit.Push(child);
        }
        return false;
    }

    /// <summary>
    /// Find the first node in the tree that matches the predicate (depth-first).
    /// Used e.g. by CarryObjectNode to find a pathfinding node to reach the object.
    /// </summary>
    public BehaviorTreeNode FindNode(System.Func<BehaviorTreeNode, bool> predicate)
    {
        if (rootNode == null || predicate == null) return null;
        Stack<BehaviorTreeNode> stack = new Stack<BehaviorTreeNode>();
        stack.Push(rootNode);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n != null && predicate(n)) return n;
            if (n?.children == null) continue;
            for (int i = n.children.Count - 1; i >= 0; i--)
            {
                if (n.children[i] != null)
                    stack.Push(n.children[i]);
            }
        }
        return null;
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
