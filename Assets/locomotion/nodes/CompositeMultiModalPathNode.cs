using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior-tree sequence that builds children from <see cref="GenericTraversibilityPlannerSolver"/> output.
/// </summary>
public class CompositeMultiModalPathNode : BehaviorTreeNode
{
    [Header("Planning")]
    public Vector3 origin;
    public Vector3 destination;
    public HierarchicalPathingSolver pathfindingSolver;
    public List<GoodSection> toolSections = new List<GoodSection>();
    public List<GoodSection> acrobaticsSections = new List<GoodSection>();
    [Range(0f, 1f)] public float requireAsset01 = 0.5f;
    [Range(0f, 1f)] public float requireType01 = 0.5f;
    public VehicleActor preferredVehicle;

    [Header("Execution")]
    public float waypointReachedDistance = 0.5f;
    public int maxPathLength = 0;
    public bool tryToolBridgeWhenNoWalk = true;

    [Header("Flying cards (Fly segments)")]
    public bool useFlyingCardsForFlySegments;
    public GameObject goalTarget;

    bool pathBuilt;

    void Awake()
    {
        nodeType = NodeType.Sequence;
        if (pathfindingSolver == null)
            pathfindingSolver = FindAnyObjectByType<HierarchicalPathingSolver>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (pathfindingSolver == null)
            return BehaviorTreeStatus.Failure;

        if (!pathBuilt)
        {
            if (!BuildFromPlanner(tree))
                return BehaviorTreeStatus.Failure;
            pathBuilt = true;
        }

        if (children == null || children.Count == 0)
            return BehaviorTreeStatus.Success;

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null)
                continue;
            BehaviorTreeStatus childStatus = children[i].Execute(tree);
            if (childStatus == BehaviorTreeStatus.Failure)
                return BehaviorTreeStatus.Failure;
            if (childStatus == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
        }

        return BehaviorTreeStatus.Success;
    }

    bool BuildFromPlanner(BehaviorTree tree)
    {
        Vector3 queryPos = origin;
        float queryT = 0f;
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll != null && ragdoll.transform != null)
            queryPos = ragdoll.transform.position;

        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = preferredVehicle
        };

        GenericMultiModalPathPlan plan = GenericTraversibilityPlannerSolver.BuildPlan(
            origin,
            destination,
            pathfindingSolver,
            toolSections,
            acrobaticsSections,
            queryPos,
            queryT,
            hints,
            tryToolBridgeWhenNoWalk,
            goalTarget);

        if (plan == null || plan.IsEmpty)
            return false;

        return BuildChildrenFromPlan(plan, tree);
    }

    bool BuildChildrenFromPlan(GenericMultiModalPathPlan plan, BehaviorTree tree)
    {
        if (children == null)
            children = new List<BehaviorTreeNode>();
        else
        {
            foreach (BehaviorTreeNode child in children)
            {
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }
            children.Clear();
        }

        PhysicsCardSolver cardSolver = tree != null ? tree.GetComponentInParent<PhysicsCardSolver>() : null;

        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg == null)
                continue;

            switch (seg.mode)
            {
                case TravelLegMode.ToolBridge:
                case TravelLegMode.Acrobatics:
                    AddToolSegment(seg);
                    break;

                case TravelLegMode.Fly:
                    if (useFlyingCardsForFlySegments && cardSolver != null && cardSolver.flyingCardConfig != null &&
                        seg.waypoints != null && seg.waypoints.Count >= 2)
                    {
                        if (!AppendFlyingCardChildren(seg.waypoints, tree, cardSolver))
                            AppendWaypointChain(seg.waypoints);
                    }
                    else
                        AppendWaypointChain(seg.waypoints);
                    break;

                default:
                    AppendWaypointChain(seg.waypoints);
                    break;
            }
        }

        if (maxPathLength > 0 && children.Count > maxPathLength)
        {
            while (children.Count > maxPathLength)
            {
                BehaviorTreeNode last = children[children.Count - 1];
                children.RemoveAt(children.Count - 1);
                if (last != null)
                    DestroyImmediate(last.gameObject);
            }
        }

        return children.Count > 0;
    }

    void AppendWaypointChain(List<Vector3> path)
    {
        if (path == null)
            return;
        foreach (Vector3 wp in path)
        {
            GameObject go = new GameObject($"Waypoint_{children.Count}");
            go.transform.SetParent(transform, worldPositionStays: false);
            MoveToWaypointNode node = go.AddComponent<MoveToWaypointNode>();
            node.waypoint = wp;
            node.reachedDistance = waypointReachedDistance;
            children.Add(node);
        }
    }

    bool AppendFlyingCardChildren(List<Vector3> path, BehaviorTree tree, PhysicsCardSolver solver)
    {
        if (path == null || path.Count < 2 || solver == null || solver.flyingCardConfig == null)
            return false;

        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        RagdollState state = ragdoll != null ? ragdoll.GetCurrentState() : null;
        float fuel = solver.flyingCardConfig.fuelCapacity;
        Vector3 prev = path[0];
        int countBefore = children.Count;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 to = path[i];
            GoodSection card = solver.GenerateFlyingCard(prev, to, state, solver.flyingCardConfig, solver.useJetModeForFlyingGoal, ref fuel);
            if (card == null)
                return false;

            GameObject nodeObj = new GameObject($"Flying_{children.Count}");
            nodeObj.transform.SetParent(transform, worldPositionStays: false);
            ExecuteToolTraversabilityNode execNode = nodeObj.AddComponent<ExecuteToolTraversabilityNode>();
            execNode.card = card;
            execNode.toolUseTo = to;
            execNode.reachedDistance = waypointReachedDistance;
            children.Add(execNode);
            prev = to;
            if (state != null)
            {
                state = state.CopyState();
                state.rootPosition = to;
            }
        }

        return children.Count > countBefore;
    }

    void AddToolSegment(MultiModalSegment seg)
    {
        if (seg.card == null)
            return;

        GameObject toolNodeObj = new GameObject($"ToolUse_{children.Count}");
        toolNodeObj.transform.SetParent(transform, worldPositionStays: false);
        ExecuteToolTraversabilityNode toolNode = toolNodeObj.AddComponent<ExecuteToolTraversabilityNode>();
        toolNode.card = seg.card;
        if (seg.tools != null && seg.tools.Count > 0)
        {
            toolNode.tools = new List<GameObject>(seg.tools);
            toolNode.tool = seg.tools[0];
        }
        toolNode.toolUseTo = destination;
        if (seg.waypoints != null && seg.waypoints.Count > 0)
            toolNode.toolUseTo = seg.waypoints[seg.waypoints.Count - 1];
        else if (seg.segmentEnd.sqrMagnitude > 1e-6f)
            toolNode.toolUseTo = seg.segmentEnd;
        toolNode.reachedDistance = waypointReachedDistance;
        children.Add(toolNode);
    }
}
