using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Segment of a tool-aware path plan: either a walk (waypoints) or a tool-use (card + tool bridging from A to B).
/// </summary>
[Serializable]
public struct ToolTraversabilityPathSegment
{
    public bool isWalk;
    /// <summary>When isWalk: waypoints. When !isWalk: single "from" then "to" is in toolUseTo.</summary>
    public List<Vector3> waypoints;
    /// <summary>When !isWalk: the good section (card) to execute.</summary>
    public GoodSection toolUseCard;
    /// <summary>When !isWalk: optional tool GameObject (e.g. ladder, batterang).</summary>
    public GameObject toolUseTool;
    /// <summary>When !isWalk: position after executing the card (bridge end).</summary>
    public Vector3 toolUseTo;

    public static ToolTraversabilityPathSegment Walk(List<Vector3> waypoints)
    {
        return new ToolTraversabilityPathSegment
        {
            isWalk = true,
            waypoints = waypoints != null ? new List<Vector3>(waypoints) : new List<Vector3>(),
            toolUseCard = null,
            toolUseTool = null,
            toolUseTo = default
        };
    }

    public static ToolTraversabilityPathSegment ToolUse(GoodSection card, GameObject tool, Vector3 from, Vector3 to)
    {
        return new ToolTraversabilityPathSegment
        {
            isWalk = false,
            waypoints = null,
            toolUseCard = card,
            toolUseTool = tool,
            toolUseTo = to
        };
    }
}

/// <summary>
/// Result of tool-aware path planning: ordered list of walk and tool-use segments.
/// </summary>
[Serializable]
public class ToolTraversabilityPathPlan
{
    public List<ToolTraversabilityPathSegment> segments = new List<ToolTraversabilityPathSegment>();

    public bool IsEmpty => segments == null || segments.Count == 0;
}

/// <summary>
/// Option B (MVP): Detects gaps in walk pathfinding and injects tool-use segments using good sections
/// tagged with traversability mode + optional causality. Does not modify the core grid.
/// </summary>
public static class ToolTraversabilityPlanner
{
    /// <summary>
    /// Build a path plan from start to goal: walk path when available, or bridge with tool-use sections
    /// when no walk path exists (or optionally when a gap is detected). Uses causality (position, t) to
    /// filter sections that are valid in the current narrative/time context.
    /// </summary>
    /// <param name="start">Start position (world).</param>
    /// <param name="goal">Goal position (world).</param>
    /// <param name="solver">Hierarchical pathing solver (walk path).</param>
    /// <param name="availableSections">Good sections that may enable traversability (e.g. climb, swing, pick, roll, throw).</param>
    /// <param name="queryPosition">Position for causality check (e.g. agent position).</param>
    /// <param name="queryT">Time for causality check (e.g. narrative time in seconds).</param>
    /// <param name="tryToolBridgeWhenNoPath">If true, when no walk path is found, try to bridge start→goal with one tool-use section.</param>
    /// <param name="desiredThrowDistanceFromTarget">When using a throw section: if &gt; 0, insert a walk segment to a stand position at this distance from goal, then throw.</param>
    /// <param name="maxThrowLaunchSpeed">When using a throw section: max launch speed for trajectory feasibility (0 = no cap).</param>
    /// <returns>Path plan with walk and/or tool-use segments.</returns>
    public static ToolTraversabilityPathPlan FindPlan(
        Vector3 start,
        Vector3 goal,
        HierarchicalPathingSolver solver,
        List<GoodSection> availableSections,
        Vector3 queryPosition,
        float queryT,
        bool tryToolBridgeWhenNoPath = true,
        float desiredThrowDistanceFromTarget = 0f,
        float maxThrowLaunchSpeed = 0f)
    {
        var plan = new ToolTraversabilityPathPlan();

        if (solver == null)
            return plan;

        // 1) Get walk path
        List<Vector3> walkPath = solver.FindPath(start, goal, returnBestEffortPathWhenNoPath: false);

        if (walkPath != null && walkPath.Count > 0)
        {
            plan.segments.Add(ToolTraversabilityPathSegment.Walk(walkPath));
            return plan;
        }

        // 2) No walk path: optionally try tool-use bridge
        if (!tryToolBridgeWhenNoPath || availableSections == null || availableSections.Count == 0)
            return plan;

        // 3) Find sections that enable traversability and are valid at (queryPosition, queryT)
        foreach (GoodSection section in availableSections)
        {
            if (section == null || section.IsThrowGoalOnly())
                continue;
            if (!section.EnablesTraversabilityAt(queryPosition, queryT))
                continue;

            bool isThrow = section.needsToBeThrown || section.traversabilityMode == TraversabilityMode.Throw;
            Vector3 throwOrigin = start;
            if (isThrow)
            {
                if (!ThrowTrajectoryUtility.IsInRangeAndFeasible(section, start, goal, null, maxThrowLaunchSpeed > 0f ? maxThrowLaunchSpeed : 0f))
                    continue;
                if (desiredThrowDistanceFromTarget > 0f)
                {
                    Vector3 standPos = solver.FindPositionAtDistanceFromGoal(start, goal, desiredThrowDistanceFromTarget, returnBestEffortPathWhenNoPath: false);
                    if (Vector3.Distance(standPos, start) > 0.5f)
                    {
                        List<Vector3> walkToStand = solver.FindPath(start, standPos, returnBestEffortPathWhenNoPath: false);
                        if (walkToStand != null && walkToStand.Count > 0)
                        {
                            plan.segments.Add(ToolTraversabilityPathSegment.Walk(walkToStand));
                            throwOrigin = standPos;
                        }
                    }
                }
            }
            plan.segments.Add(ToolTraversabilityPathSegment.ToolUse(section, section.requiredTool, throwOrigin, goal));
            return plan;
        }

        return plan;
    }

    /// <summary>
    /// Find plan using NarrativeVolumeQuery for causality when a clock is available.
    /// Convenience overload that uses queryPosition and queryT from context.
    /// </summary>
    public static ToolTraversabilityPathPlan FindPlanWithCausality(
        Vector3 start,
        Vector3 goal,
        HierarchicalPathingSolver solver,
        List<GoodSection> availableSections,
        Vector3 queryPosition,
        float queryT,
        bool tryToolBridgeWhenNoPath = true,
        float desiredThrowDistanceFromTarget = 0f,
        float maxThrowLaunchSpeed = 0f)
    {
        return FindPlan(start, goal, solver, availableSections, queryPosition, queryT, tryToolBridgeWhenNoPath, desiredThrowDistanceFromTarget, maxThrowLaunchSpeed);
    }
}
