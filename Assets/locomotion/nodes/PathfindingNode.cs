using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior tree node for pathfinding using hierarchical pathfinding system.
/// Queries paths and dynamically builds child nodes for each waypoint.
/// </summary>
public class PathfindingNode : BehaviorTreeNode
{
    [Header("Pathfinding Properties")]
    [Tooltip("Origin position for pathfinding")]
    public Vector3 origin;

    [Tooltip("Destination position for pathfinding")]
    public Vector3 destination;

    [Tooltip("Reference to HierarchicalPathingSolver (auto-found if null)")]
    public HierarchicalPathingSolver pathfindingSolver;

    [Tooltip("Distance threshold for waypoint completion")]
    public float waypointReachedDistance = 0.5f;

    [Tooltip("Maximum waypoints to process (0 = unlimited)")]
    public int maxPathLength = 0;

    [Header("Path Settings")]
    [Tooltip("Return best-effort path if no complete path found")]
    public bool returnBestEffortPath = false;

    // Execution state
    private List<Vector3> currentPath = new List<Vector3>();
    private bool pathBuilt = false;

    private void Awake()
    {
        nodeType = NodeType.Sequence;
        
        // Auto-find pathfinding solver if not assigned
        if (pathfindingSolver == null)
        {
            pathfindingSolver = FindObjectOfType<HierarchicalPathingSolver>();
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        // If no pathfinding solver, fail
        if (pathfindingSolver == null)
        {
            Debug.LogWarning("PathfindingNode: No HierarchicalPathingSolver found");
            return BehaviorTreeStatus.Failure;
        }

        // Build path tree on first execution
        if (!pathBuilt)
        {
            if (!BuildPathTree(tree))
            {
                return BehaviorTreeStatus.Failure;
            }
            pathBuilt = true;
        }

        // Execute as Sequence: execute children in order until one fails
        if (children == null || children.Count == 0)
        {
            return BehaviorTreeStatus.Success; // No waypoints = already at destination
        }

        // Execute each child in sequence
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null)
                continue;

            BehaviorTreeStatus childStatus = children[i].Execute(tree);
            
            if (childStatus == BehaviorTreeStatus.Failure)
            {
                return BehaviorTreeStatus.Failure;
            }
            
            if (childStatus == BehaviorTreeStatus.Running)
            {
                return BehaviorTreeStatus.Running;
            }
        }

        // All children succeeded
        return BehaviorTreeStatus.Success;
    }

    public override void OnUpdate(BehaviorTree tree)
    {
        // Update all running children
        if (children != null)
        {
            foreach (var child in children)
            {
                if (child != null)
                {
                    child.OnUpdate(tree);
                }
            }
        }
    }

    /// <summary>
    /// Build path tree by querying pathfinding solver and creating child nodes.
    /// </summary>
    private bool BuildPathTree(BehaviorTree tree)
    {
        if (pathfindingSolver == null)
            return false;

        // Query path from pathfinding solver
        currentPath = pathfindingSolver.FindPath(origin, destination, returnBestEffortPath);
        
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning($"PathfindingNode: No path found from {origin} to {destination}");
            return false;
        }

        // Limit path length if specified
        if (maxPathLength > 0 && currentPath.Count > maxPathLength)
        {
            currentPath = currentPath.GetRange(0, maxPathLength);
        }

        // Clear existing children
        if (children == null)
        {
            children = new List<BehaviorTreeNode>();
        }
        else
        {
            // Destroy existing child GameObjects
            foreach (var child in children)
            {
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            children.Clear();
        }

        // Create MoveToWaypointNode for each waypoint
        for (int i = 0; i < currentPath.Count; i++)
        {
            GameObject waypointNodeObj = new GameObject($"Waypoint_{i}");
            waypointNodeObj.transform.SetParent(transform, worldPositionStays: false);
            
            MoveToWaypointNode waypointNode = waypointNodeObj.AddComponent<MoveToWaypointNode>();
            waypointNode.waypoint = currentPath[i];
            waypointNode.reachedDistance = waypointReachedDistance;
            
            children.Add(waypointNode);
        }

        Debug.Log($"PathfindingNode: Built path with {currentPath.Count} waypoints");
        return true;
    }

    public override void PruneForCards(List<GoodSection> cards)
    {
        if (children == null || cards == null)
            return;

        // Prune waypoints that don't have feasible cards
        List<BehaviorTreeNode> validChildren = new List<BehaviorTreeNode>();
        
        RagdollSystem ragdollSystem = GetComponentInParent<RagdollSystem>();
        if (ragdollSystem == null)
        {
            // Can't prune without ragdoll system, keep all
            base.PruneForCards(cards);
            return;
        }

        RagdollState currentState = ragdollSystem.GetCurrentState();
        PhysicsCardSolver cardSolver = GetComponentInParent<PhysicsCardSolver>();
        
        foreach (var child in children)
        {
            if (child == null)
                continue;

            if (child is MoveToWaypointNode waypointNode)
            {
                // Check if there are feasible cards for this waypoint
                if (cardSolver != null)
                {
                    // Estimate state at waypoint (simplified: use current state)
                    List<GoodSection> applicableCards = cardSolver.FindApplicableCards(currentState);
                    
                    // Filter for walking if enabled
                    if (cardSolver.onlyAllowLegsForWalking)
                    {
                        applicableCards = cardSolver.FilterCardsForWalking(applicableCards);
                    }
                    
                    if (applicableCards.Count > 0)
                    {
                        validChildren.Add(child);
                    }
                    else
                    {
                        // Try auto-generation
                        GoodSection walkingCard = cardSolver.GenerateWalkingCard(
                            currentState.rootPosition, 
                            waypointNode.waypoint, 
                            currentState);
                        if (walkingCard != null)
                        {
                            validChildren.Add(child);
                        }
                    }
                }
                else
                {
                    // No card solver, keep waypoint
                    validChildren.Add(child);
                }
            }
            else
            {
                // Not a waypoint node, keep it
                validChildren.Add(child);
            }
        }

        // Update children list
        children = validChildren;
        
        // Recurse to children
        base.PruneForCards(cards);
    }

    public override void OnEnter(BehaviorTree tree)
    {
        pathBuilt = false;
        currentPath.Clear();
    }

    public override void OnExit(BehaviorTree tree)
    {
        // Cleanup if needed
    }
}
