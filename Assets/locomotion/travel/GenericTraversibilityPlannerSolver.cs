using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Composes walk, fly (medium registry), drive (XZ grid stub), tool traversability, and acrobatics-stub bridges.
/// Hint weights bias section picking when multiple tool/acrobatics sections qualify.
/// </summary>
public static class GenericTraversibilityPlannerSolver
{
    public struct PlannerHints
    {
        public float requireAsset01;
        public float requireType01;
        public VehicleActor preferredVehicle;
    }

    /// <summary>
    /// Build a multi-modal plan from start to goal (greedy priority: walk → fly → drive → tool bridge → acrobatics stub).
    /// </summary>
    public static GenericMultiModalPathPlan BuildPlan(
        Vector3 start,
        Vector3 goal,
        HierarchicalPathingSolver solver,
        List<GoodSection> toolTraversabilitySections,
        List<GoodSection> acrobaticsSections,
        Vector3 queryPosition,
        float queryT,
        PlannerHints hints,
        bool tryToolBridgeWhenNoWalk = true,
        GameObject goalTarget = null,
        PhysicalPathingMedium tryFlyMedium = PhysicalPathingMedium.Air)
    {
        var plan = new GenericMultiModalPathPlan();
        if (solver == null)
            return plan;

        PathingMode savedMode = solver.pathingMode;

        try
        {
            solver.pathingMode = PathingMode.Walk;
            List<Vector3> walkPath = solver.FindPath(start, goal, returnBestEffortPathWhenNoPath: false);
            if (walkPath != null && walkPath.Count > 0)
            {
                plan.segments.Add(MultiModalSegment.FromWalk(walkPath));
                return plan;
            }

            List<Vector3> airPath = PhysicalPathingSolverRegistry.FindPathForMedium(
                tryFlyMedium,
                solver,
                start,
                goal,
                returnBestEffortPathWhenNoPath: false);
            if (airPath != null && airPath.Count > 0)
            {
                plan.segments.Add(MultiModalSegment.FromFly(airPath, tryFlyMedium));
                return plan;
            }

            solver.pathingMode = PathingMode.Drive;
            List<Vector3> drivePath = solver.FindPath(start, goal, returnBestEffortPathWhenNoPath: false);
            if (drivePath != null && drivePath.Count > 0)
            {
                plan.segments.Add(MultiModalSegment.FromDrive(drivePath, hints.preferredVehicle));
                return plan;
            }

            if (tryToolBridgeWhenNoWalk)
            {
                ToolTraversabilityPathPlan toolPlan = ToolTraversabilityPlanner.FindPlan(
                    start,
                    goal,
                    solver,
                    toolTraversabilitySections ?? new List<GoodSection>(),
                    queryPosition,
                    queryT,
                    tryToolBridgeWhenNoPath: true,
                    goalTarget: goalTarget);

                if (!toolPlan.IsEmpty)
                {
                    AppendToolPlan(plan, toolPlan, start);
                    return plan;
                }
            }

            GoodSection acrobaticsPick = PickBestAcrobaticsSection(
                acrobaticsSections,
                queryPosition,
                queryT,
                hints);
            if (acrobaticsPick != null && acrobaticsPick.EnablesTraversabilityAt(queryPosition, queryT))
            {
                List<GameObject> tls = acrobaticsPick.GetRequiredToolsList();
                if (tls == null || tls.Count == 0)
                    tls = acrobaticsPick.requiredTool != null
                        ? new List<GameObject> { acrobaticsPick.requiredTool }
                        : new List<GameObject>();
                plan.segments.Add(MultiModalSegment.FromAcrobatics(acrobaticsPick, tls, start, goal));
                return plan;
            }
        }
        finally
        {
            solver.pathingMode = savedMode;
        }

        return plan;
    }

    static void AppendToolPlan(GenericMultiModalPathPlan plan, ToolTraversabilityPathPlan toolPlan, Vector3 defaultStart)
    {
        foreach (ToolTraversabilityPathSegment seg in toolPlan.segments)
        {
            if (seg.isWalk && seg.waypoints != null && seg.waypoints.Count > 0)
                plan.segments.Add(MultiModalSegment.FromWalk(seg.waypoints));
            else if (!seg.isWalk && seg.toolUseCard != null)
            {
                List<GameObject> tls = seg.toolUseTools != null && seg.toolUseTools.Count > 0
                    ? seg.toolUseTools
                    : (seg.toolUseTool != null ? new List<GameObject> { seg.toolUseTool } : new List<GameObject>());
                Vector3 from = defaultStart;
                if (plan.segments.Count > 0 && plan.segments[plan.segments.Count - 1].waypoints != null &&
                    plan.segments[plan.segments.Count - 1].waypoints.Count > 0)
                {
                    List<Vector3> wp = plan.segments[plan.segments.Count - 1].waypoints;
                    from = wp[wp.Count - 1];
                }

                plan.segments.Add(MultiModalSegment.FromToolBridge(seg.toolUseCard, tls, from, seg.toolUseTo));
            }
        }
    }

    static GoodSection PickBestAcrobaticsSection(
        List<GoodSection> acrobaticsSections,
        Vector3 queryPosition,
        float queryT,
        PlannerHints hints)
    {
        if (acrobaticsSections == null || acrobaticsSections.Count == 0)
            return null;

        List<GoodSection> candidates = acrobaticsSections
            .Where(s => s != null && s.enablesTraversability && s.EnablesTraversabilityAt(queryPosition, queryT))
            .ToList();
        if (candidates.Count == 0)
            return null;

        float Score(GoodSection s)
        {
            int toolCount = s.GetRequiredToolsList()?.Count ?? 0;
            if (s.requiredTool != null) toolCount = Mathf.Max(toolCount, 1);
            float assetBias = toolCount * (1f - Mathf.Clamp01(hints.requireAsset01));
            float typeBias = string.IsNullOrEmpty(s.traversabilityTag)
                ? 0f
                : (1f - Mathf.Clamp01(hints.requireType01)) * 0.5f;
            return assetBias + typeBias;
        }

        GoodSection best = candidates[0];
        float bestScore = Score(best);
        for (int i = 1; i < candidates.Count; i++)
        {
            float sc = Score(candidates[i]);
            if (sc < bestScore)
            {
                bestScore = sc;
                best = candidates[i];
            }
        }

        return best;
    }
}
