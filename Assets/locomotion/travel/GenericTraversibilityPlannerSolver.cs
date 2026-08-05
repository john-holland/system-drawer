using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Composes walk, fly (medium registry), drive (XZ grid stub), tool traversability, and acrobatics-stub bridges.
/// Hint weights bias section picking when multiple tool/acrobatics sections qualify.
/// Optional PlannerTimelineOptions enables multi-leg shortest-path search over landmarks before greedy fallback.
/// </summary>
public static class GenericTraversibilityPlannerSolver
{
    public struct PlannerHints
    {
        public float requireAsset01;
        public float requireType01;
        public VehicleActor preferredVehicle;

        /// <summary>Unset = NaN. risk &lt;= maxRisk01.</summary>
        public float maxRisk01;
        /// <summary>Unset = NaN. risk &gt;= minRisk01.</summary>
        public float minRisk01;
        /// <summary>Unset = NaN. safety &gt;= minSafety01 ⇒ risk &lt;= 1 - minSafety01.</summary>
        public float minSafety01;
        /// <summary>Unset = NaN. safety &lt;= maxSafety01 ⇒ risk &gt;= 1 - maxSafety01.</summary>
        public float maxSafety01;

        /// <summary>Soft-avoid world points (e.g. police cruisers).</summary>
        public Vector3[] avoidPoints;
        public float avoidRadius;
        public float avoidCostMultiplier;
        public bool ignoreAvoidance;
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
        PlannerTimelineOptions timeline = PlannerTimelineOptions.DefaultLegacy();
        return BuildPlan(
            start,
            goal,
            solver,
            toolTraversabilitySections,
            acrobaticsSections,
            queryPosition,
            queryT,
            hints,
            tryToolBridgeWhenNoWalk,
            goalTarget,
            tryFlyMedium,
            in timeline);
    }

    /// <summary>
    /// Build plan with optional timeline multi-leg search. When <paramref name="timeline"/>.enableMultiLegTimelineSearch is false, behavior matches the legacy overload.
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
        bool tryToolBridgeWhenNoWalk,
        GameObject goalTarget,
        PhysicalPathingMedium tryFlyMedium,
        in PlannerTimelineOptions timeline)
    {
        if (solver == null)
            return new GenericMultiModalPathPlan();

        float hintEff = timeline.GetEffectiveHintEffectiveness();

        if (timeline.enableMultiLegTimelineSearch)
        {
            GenericMultiModalPathPlan timelinePlan = TimelineMultiModalPlanner.TryBuildPlan(
                start,
                goal,
                solver,
                in timeline,
                in hints,
                tryFlyMedium);
            if (!timelinePlan.IsEmpty)
                return timelinePlan;
        }

        return BuildPlanGreedyLegacy(
            start,
            goal,
            solver,
            toolTraversabilitySections,
            acrobaticsSections,
            queryPosition,
            queryT,
            hints,
            tryToolBridgeWhenNoWalk,
            goalTarget,
            tryFlyMedium,
            hintEff);
    }

    static GenericMultiModalPathPlan BuildPlanGreedyLegacy(
        Vector3 start,
        Vector3 goal,
        HierarchicalPathingSolver solver,
        List<GoodSection> toolTraversabilitySections,
        List<GoodSection> acrobaticsSections,
        Vector3 queryPosition,
        float queryT,
        PlannerHints hints,
        bool tryToolBridgeWhenNoWalk,
        GameObject goalTarget,
        PhysicalPathingMedium tryFlyMedium,
        float acrobaticsHintEffectiveness)
    {
        var plan = new GenericMultiModalPathPlan();
        PathingMode savedMode = solver.pathingMode;

        try
        {
            solver.pathingMode = PathingMode.Walk;
            List<Vector3> walkPath = solver.FindPath(start, goal, returnBestEffortPathWhenNoPath: false);
            if (walkPath != null && walkPath.Count > 0)
            {
                var seg = MultiModalSegment.FromWalk(walkPath);
                seg.medium = PhysicalMediumVolumeIndex.ResolveSegmentMedium(walkPath);
                plan.segments.Add(seg);
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
                var seg = MultiModalSegment.FromDrive(drivePath, hints.preferredVehicle);
                seg.medium = PhysicalMediumVolumeIndex.ResolveSegmentMedium(drivePath);
                plan.segments.Add(seg);
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
                hints,
                acrobaticsHintEffectiveness);
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
        PlannerHints hints,
        float hintEffectiveness01)
    {
        if (acrobaticsSections == null || acrobaticsSections.Count == 0)
            return null;

        List<GoodSection> candidates = acrobaticsSections
            .Where(s => s != null && s.enablesTraversability && s.EnablesTraversabilityAt(queryPosition, queryT))
            .ToList();
        if (candidates.Count == 0)
            return null;

        float eff = Mathf.Clamp01(hintEffectiveness01);

        float Score(GoodSection s)
        {
            int toolCount = s.GetRequiredToolsList()?.Count ?? 0;
            if (s.requiredTool != null) toolCount = Mathf.Max(toolCount, 1);
            float assetBias = toolCount * (1f - Mathf.Clamp01(hints.requireAsset01));
            float typeBias = string.IsNullOrEmpty(s.traversabilityTag)
                ? 0f
                : (1f - Mathf.Clamp01(hints.requireType01)) * 0.5f;
            return (assetBias + typeBias) * eff;
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

    /// <summary>Append terminal leg when enabled; returns input plan when disabled or resolution fails.</summary>
    public static GenericMultiModalPathPlan AppendTerminalLegIfEnabled(
        GenericMultiModalPathPlan plan,
        Vector3 approachStart,
        Vector3 goalHint,
        HierarchicalPathingSolver solver,
        ActorPhysicalProfile profile,
        in PlannerTerminalOptions terminalOptions)
    {
        if (plan == null || plan.IsEmpty || !terminalOptions.enableTerminalLeg || solver == null)
            return plan;

        Vector3 start = approachStart;
        if (plan.segments != null && plan.segments.Count > 0)
        {
            MultiModalSegment last = plan.segments[plan.segments.Count - 1];
            if (last?.waypoints != null && last.waypoints.Count > 0)
                start = last.waypoints[last.waypoints.Count - 1];
        }

        TravelLegMode mode = terminalOptions.autoFromProfile || !TravelLegModeExtensions.IsTerminalLeg(terminalOptions.terminalMode)
            ? profile.defaultTerminalLeg
            : terminalOptions.terminalMode;

        var zones = ParkingZoneIndex.QueryNear(goalHint, terminalOptions.terminalSearchRadius > 0f
            ? terminalOptions.terminalSearchRadius
            : 60f);

        if (!TerminalPlacementSolver.TryResolveTerminalLeg(
                start, goalHint, profile, mode, solver, zones, out MultiModalSegment terminalLeg))
            return plan;

        GenericMultiModalPathPlan copy = plan.Clone();
        copy.segments.Add(terminalLeg);
        return copy;
    }
}
