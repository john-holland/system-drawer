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

    [Tooltip("Use flying pathfinding (no slope blocking, Y interpolated between start and goal)")]
    public bool useFlyingPathfinding = false;

    [Tooltip("When true and useFlyingPathfinding, build path segments as flying cards (wing/jet) via PhysicsCardSolver.GenerateFlyingCard instead of walk waypoints. Uses solver.flyingCardConfig.")]
    public bool useFlyingCardsForFlyingPath = false;

    [Tooltip("When no walk path exists, use good sections that enable traversability (climb, swing, pick, roll, throw) to bridge gaps. Uses causality (position, t) to filter sections.")]
    public bool useToolTraversability = false;

    [Tooltip("Optional: sections to consider for tool traversability. If empty, uses PhysicsCardSolver.availableCards or BehaviorTree.availableCards when building path.")]
    public List<GoodSection> availableSectionsForTraversability;

    // Execution state
    private List<Vector3> currentPath = new List<Vector3>();
    private bool pathBuilt = false;

    private void Awake()
    {
        nodeType = NodeType.Sequence;
        
        // Auto-find pathfinding solver if not assigned
        if (pathfindingSolver == null)
        {
            pathfindingSolver = FindAnyObjectByType<HierarchicalPathingSolver>();
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
    /// Build path tree by querying pathfinding solver (and optionally tool traversability planner) and creating child nodes.
    /// </summary>
    private bool BuildPathTree(BehaviorTree tree)
    {
        if (pathfindingSolver == null)
            return false;

        // Option: use tool traversability planner (gap → tool-use segments)
        if (useToolTraversability)
        {
            List<GoodSection> sections = GetAvailableSectionsForTraversability(tree);
            Vector3 queryPos = origin;
            float queryT = 0f;
            RagdollSystem ragdoll = tree.GetComponent<RagdollSystem>();
            if (ragdoll != null && ragdoll.transform != null)
                queryPos = ragdoll.transform.position;
            ToolTraversabilityPathPlan plan = ToolTraversabilityPlanner.FindPlan(origin, destination, pathfindingSolver, sections, queryPos, queryT, tryToolBridgeWhenNoPath: true);
            if (!plan.IsEmpty)
            {
                return BuildChildrenFromPlan(plan, tree);
            }
        }

        PathingMode savedMode = pathfindingSolver.pathingMode;
        if (useFlyingPathfinding)
            pathfindingSolver.pathingMode = PathingMode.Fly;

        try
        {
            currentPath = pathfindingSolver.FindPath(origin, destination, returnBestEffortPath);
        }
        finally
        {
            if (useFlyingPathfinding)
                pathfindingSolver.pathingMode = savedMode;
        }
        
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

        // When flying pathfinding and use flying cards, build segments as flying cards (procedural) instead of walk waypoints
        if (useFlyingPathfinding && useFlyingCardsForFlyingPath && tree != null)
        {
            PhysicsCardSolver cardSolver = tree.GetComponentInParent<PhysicsCardSolver>();
            if (cardSolver != null && cardSolver.flyingCardConfig != null)
            {
                RagdollState state = null;
                RagdollSystem ragdoll = tree.GetComponent<RagdollSystem>();
                if (ragdoll != null)
                    state = ragdoll.GetCurrentState();
                if (BuildFlyingPathChildren(currentPath, tree, cardSolver, state))
                    return true;
            }
        }

        return BuildWalkChildrenOnly(currentPath);
    }

    /// <summary>
    /// Build child nodes as flying card segments (GenerateFlyingCard per segment). Uses fuel from config.
    /// </summary>
    private bool BuildFlyingPathChildren(List<Vector3> path, BehaviorTree tree, PhysicsCardSolver solver, RagdollState state)
    {
        if (path == null || path.Count < 2 || solver == null || solver.flyingCardConfig == null)
            return false;

        if (children == null)
            children = new List<BehaviorTreeNode>();
        else
        {
            foreach (var child in children)
            {
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }
            children.Clear();
        }

        float fuel = solver.flyingCardConfig.fuelCapacity;
        Vector3 prev = path[0];
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 to = path[i];
            GoodSection card = solver.GenerateFlyingCard(prev, to, state, solver.flyingCardConfig, solver.useJetModeForFlyingGoal, ref fuel);
            if (card == null)
                break;
            GameObject nodeObj = new GameObject($"Flying_{children.Count}");
            nodeObj.transform.SetParent(transform, worldPositionStays: false);
            ExecuteToolTraversabilityNode execNode = nodeObj.AddComponent<ExecuteToolTraversabilityNode>();
            execNode.card = card;
            execNode.tool = null;
            execNode.toolUseTo = to;
            execNode.reachedDistance = waypointReachedDistance;
            children.Add(execNode);
            prev = to;
            if (state != null)
                state = state.CopyState();
            if (state != null)
                state.rootPosition = to;
        }

        if (children.Count == 0)
            return false;
        return true;
    }

    private List<GoodSection> GetAvailableSectionsForTraversability(BehaviorTree tree)
    {
        if (availableSectionsForTraversability != null && availableSectionsForTraversability.Count > 0)
            return availableSectionsForTraversability;
        PhysicsCardSolver solver = tree.GetComponentInParent<PhysicsCardSolver>();
        if (solver != null && solver.availableCards != null)
            return solver.availableCards;
        if (tree != null && tree.availableCards != null)
            return tree.availableCards;
        return new List<GoodSection>();
    }

    private bool BuildChildrenFromPlan(ToolTraversabilityPathPlan plan, BehaviorTree tree)
    {
        currentPath = new List<Vector3>();
        if (children == null)
            children = new List<BehaviorTreeNode>();
        else
        {
            foreach (var child in children)
            {
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }
            children.Clear();
        }

        foreach (ToolTraversabilityPathSegment seg in plan.segments)
        {
            if (seg.isWalk && seg.waypoints != null)
            {
                for (int i = 0; i < seg.waypoints.Count; i++)
                {
                    currentPath.Add(seg.waypoints[i]);
                    GameObject waypointNodeObj = new GameObject($"Waypoint_{children.Count}");
                    waypointNodeObj.transform.SetParent(transform, worldPositionStays: false);
                    MoveToWaypointNode waypointNode = waypointNodeObj.AddComponent<MoveToWaypointNode>();
                    waypointNode.waypoint = seg.waypoints[i];
                    waypointNode.reachedDistance = waypointReachedDistance;
                    children.Add(waypointNode);
                }
            }
            else if (!seg.isWalk && seg.toolUseCard != null)
            {
                currentPath.Add(seg.toolUseTo);
                GameObject toolNodeObj = new GameObject($"ToolUse_{children.Count}");
                toolNodeObj.transform.SetParent(transform, worldPositionStays: false);
                ExecuteToolTraversabilityNode toolNode = toolNodeObj.AddComponent<ExecuteToolTraversabilityNode>();
                toolNode.card = seg.toolUseCard;
                if (seg.toolUseTools != null && seg.toolUseTools.Count > 0)
                {
                    toolNode.tools = new List<GameObject>(seg.toolUseTools);
                    toolNode.tool = seg.toolUseTools[0];
                }
                else
                {
                    toolNode.tool = seg.toolUseTool;
                }
                toolNode.toolUseTo = seg.toolUseTo;
                toolNode.reachedDistance = waypointReachedDistance;
                children.Add(toolNode);
            }
        }

        if (maxPathLength > 0 && currentPath.Count > maxPathLength)
        {
            currentPath = currentPath.GetRange(0, maxPathLength);
            while (children.Count > maxPathLength)
            {
                var last = children[children.Count - 1];
                children.RemoveAt(children.Count - 1);
                if (last != null)
                    DestroyImmediate(last.gameObject);
            }
        }

        Debug.Log($"PathfindingNode: Built path from plan with {children.Count} segments");
        return children.Count > 0;
    }

    private bool BuildWalkChildrenOnly(List<Vector3> path)
    {
        if (children == null)
            children = new List<BehaviorTreeNode>();
        else
        {
            foreach (var child in children)
            {
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }
            children.Clear();
        }

        for (int i = 0; i < path.Count; i++)
        {
            GameObject waypointNodeObj = new GameObject($"Waypoint_{i}");
            waypointNodeObj.transform.SetParent(transform, worldPositionStays: false);
            MoveToWaypointNode waypointNode = waypointNodeObj.AddComponent<MoveToWaypointNode>();
            waypointNode.waypoint = path[i];
            waypointNode.reachedDistance = waypointReachedDistance;
            children.Add(waypointNode);
        }

        Debug.Log($"PathfindingNode: Built path with {path.Count} waypoints");
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
            else if (child is ExecuteToolTraversabilityNode)
            {
                validChildren.Add(child);
            }
            else
            {
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
