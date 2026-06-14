using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multi-leg timeline planner: shortest-path over a landmark graph restricted to forward chord order.
/// <para>
/// Backward dynamic programming on a DAG (compute cost-to-goal from each landmark) is equivalent to forward
/// Dijkstra here because all admissible edges go from a lower chord index to a higher one, so the graph is acyclic.
/// We use forward Dijkstra on an expanded state (landmark index × previous locomotion mode) to incorporate a
/// per-mode-change penalty in edge costs.
/// </para>
/// <para>
/// Keep chordSampleCount small: each unordered landmark pair tries Walk, Fly, and Drive via the real pathing
/// backend, so cost is O(N² · modes · FindPath).
/// </para>
/// </summary>
public static class TimelineMultiModalPlanner
{
    public const int PrevModeNone = 3;
    public const int NumPrevModes = 4;

    public struct SyntheticEdge
    {
        public int fromIndex;
        public int toIndex;
        public TravelLegMode mode;
        public float pathLengthMeters;
    }

    /// <summary>
    /// Dijkstra on synthetic edges (edit-mode tests). Node positions supply waypoint geometry for each leg.
    /// </summary>
    public static GenericMultiModalPathPlan SolveFromSyntheticEdges(
        IReadOnlyList<Vector3> nodePositions,
        IReadOnlyList<SyntheticEdge> edges,
        in PlannerTimelineOptions timeline,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        var plan = new GenericMultiModalPathPlan();
        if (nodePositions == null || nodePositions.Count < 2 || edges == null || edges.Count == 0)
            return plan;

        int nodeCount = nodePositions.Count;
        float hintEff = timeline.GetEffectiveHintEffectiveness();
        var adj = new List<List<(int to, TravelLegMode mode, float len, float hintRaw)>>();
        for (int i = 0; i < nodeCount; i++)
            adj.Add(new List<(int, TravelLegMode, float, float)>());

        for (int e = 0; e < edges.Count; e++)
        {
            SyntheticEdge ed = edges[e];
            if (ed.fromIndex < 0 || ed.toIndex < 0 || ed.fromIndex >= nodeCount || ed.toIndex >= nodeCount)
                continue;
            if (ed.fromIndex >= ed.toIndex)
                continue;
            float hintRaw = RawHintCostDelta(ed.mode, hints);
            adj[ed.fromIndex].Add((ed.toIndex, ed.mode, ed.pathLengthMeters, hintRaw));
        }

        var pathCache = new Dictionary<(int from, int to, TravelLegMode mode), List<Vector3>>();
        for (int e = 0; e < edges.Count; e++)
        {
            SyntheticEdge ed = edges[e];
            if (ed.fromIndex < 0 || ed.toIndex < 0 || ed.fromIndex >= nodeCount || ed.toIndex >= nodeCount)
                continue;
            var key = (ed.fromIndex, ed.toIndex, ed.mode);
            pathCache[key] = new List<Vector3>
            {
                nodePositions[ed.fromIndex],
                nodePositions[ed.toIndex]
            };
        }

        return RunDijkstraOnGraph(
            adj,
            nodeCount - 1,
            in timeline,
            hintEff,
            pathCache,
            PhysicalPathingMedium.Air);
    }

    /// <summary>
    /// Try to build a plan using the timeline multi-modal planner.
    /// </summary>
    /// <param name="start">The start position.</param>
    /// <param name="goal">The goal position.</param>
    /// <param name="solver">The hierarchical pathing solver.</param>
    /// <param name="timeline">The timeline options.</param>
    /// <param name="hints">The hints.</param>
    /// <param name="tryFlyMedium">The try fly medium.</param>
    /// <returns>The plan.</returns>
    /// <remarks>
    /// This method builds a plan using the timeline multi-modal planner.
    /// </remarks>
    public static GenericMultiModalPathPlan TryBuildPlan(
        Vector3 start,
        Vector3 goal,
        HierarchicalPathingSolver solver,
        in PlannerTimelineOptions timeline,
        in GenericTraversibilityPlannerSolver.PlannerHints hints,
        PhysicalPathingMedium tryFlyMedium)
    {
        var plan = new GenericMultiModalPathPlan();
        if (solver == null)
            return plan;

        List<Vector3> nodes = BuildLandmarks(start, goal, timeline.chordSampleCount, timeline.extraLandmarks);
        if (nodes.Count < 2)
            return plan;

        float hintEff = timeline.GetEffectiveHintEffectiveness();
        var adj = new List<List<(int to, TravelLegMode mode, float len, float hintRaw)>>();
        for (int i = 0; i < nodes.Count; i++)
            adj.Add(new List<(int, TravelLegMode, float, float)>());

        PathingMode saved = solver.pathingMode;
        try
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    Vector3 u = nodes[i];
                    Vector3 v = nodes[j];
                    TryAddEdge(adj, i, j, u, v, TravelLegMode.Walk, solver, tryFlyMedium, timeline, hints);
                    TryAddEdge(adj, i, j, u, v, TravelLegMode.Fly, solver, tryFlyMedium, timeline, hints);
                    TryAddEdge(adj, i, j, u, v, TravelLegMode.Drive, solver, tryFlyMedium, timeline, hints);
                }
            }
        }
        finally
        {
            solver.pathingMode = saved;
        }

        var pathCache = new Dictionary<(int from, int to, TravelLegMode mode), List<Vector3>>();
        CollectPathCache(adj, nodes, solver, tryFlyMedium, pathCache);

        return RunDijkstraOnGraph(adj, nodes.Count - 1, in timeline, hintEff, pathCache, PhysicalPathingMedium.Air);
    }

    static void CollectPathCache(
        List<List<(int to, TravelLegMode mode, float len, float hintRaw)>> adj,
        List<Vector3> nodes,
        HierarchicalPathingSolver solver,
        PhysicalPathingMedium tryFlyMedium,
        Dictionary<(int, int, TravelLegMode), List<Vector3>> pathCache)
    {
        var seen = new HashSet<(int, int, TravelLegMode)>();
        for (int i = 0; i < adj.Count; i++)
        {
            foreach (var edge in adj[i])
            {
                var key = (i, edge.to, edge.mode);
                if (!seen.Add(key))
                    continue;
                if (TryFindPathWorld(nodes[i], nodes[edge.to], edge.mode, solver, tryFlyMedium, out List<Vector3> path))
                    pathCache[key] = path;
            }
        }
    }

    static int ModeToPrevIndex(TravelLegMode m)
    {
        return m switch
        {
            TravelLegMode.Walk => 0,
            TravelLegMode.Fly => 1,
            TravelLegMode.Drive => 2,
            _ => PrevModeNone
        };
    }

    static GenericMultiModalPathPlan RunDijkstraOnGraph(
        List<List<(int to, TravelLegMode mode, float len, float hintRaw)>> adj,
        int goalIndex,
        in PlannerTimelineOptions timeline,
        float hintEffectiveness,
        Dictionary<(int from, int to, TravelLegMode mode), List<Vector3>> pathCache,
        PhysicalPathingMedium tryFlyMedium)
    {
        int n = adj.Count;
        int totalStates = n * NumPrevModes;
        var dist = new float[totalStates];
        var parent = new int[totalStates];
        var parentMode = new TravelLegMode[totalStates];
        for (int i = 0; i < totalStates; i++)
        {
            dist[i] = float.PositiveInfinity;
            parent[i] = -1;
            parentMode[i] = TravelLegMode.Walk;
        }

        int StartSid(int node, int prev) => node * NumPrevModes + prev;
        int S0 = StartSid(0, PrevModeNone);
        dist[S0] = 0f;

        var visited = new bool[totalStates];
        for (int iter = 0; iter < totalStates; iter++)
        {
            int bestSid = -1;
            float bestD = float.PositiveInfinity;
            for (int s = 0; s < totalStates; s++)
            {
                if (visited[s] || dist[s] >= bestD)
                    continue;
                bestD = dist[s];
                bestSid = s;
            }

            if (bestSid < 0 || float.IsPositiveInfinity(bestD))
                break;
            visited[bestSid] = true;
            int u = bestSid / NumPrevModes;
            int pm = bestSid % NumPrevModes;

            foreach (var edge in adj[u])
            {
                int v = edge.to;
                TravelLegMode m = edge.mode;
                int pmNew = ModeToPrevIndex(m);
                float travelTime = EdgeTravelTime(edge.len, m, in timeline);
                float hintTerm = hintEffectiveness * edge.hintRaw;
                float changePen = (pm != PrevModeNone && pm != pmNew)
                    ? timeline.modeChangePenaltySec
                    : 0f;
                float edgeCost = travelTime + changePen + hintTerm + timeline.distanceWeight * edge.len;
                int sidV = StartSid(v, pmNew);
                float nd = dist[bestSid] + edgeCost;
                if (nd < dist[sidV])
                {
                    dist[sidV] = nd;
                    parent[sidV] = bestSid;
                    parentMode[sidV] = m;
                }
            }
        }

        float bestGoal = float.PositiveInfinity;
        int bestGoalSid = -1;
        for (int pm = 0; pm < NumPrevModes; pm++)
        {
            int sid = StartSid(goalIndex, pm);
            if (dist[sid] < bestGoal)
            {
                bestGoal = dist[sid];
                bestGoalSid = sid;
            }
        }

        if (float.IsPositiveInfinity(bestGoal) || bestGoalSid < 0)
            return new GenericMultiModalPathPlan();

        return ReconstructPlan(bestGoalSid, S0, parent, parentMode, pathCache, tryFlyMedium, in timeline);
    }

    static GenericMultiModalPathPlan ReconstructPlan(
        int endSid,
        int startSid,
        int[] parent,
        TravelLegMode[] parentMode,
        Dictionary<(int, int, TravelLegMode), List<Vector3>> pathCache,
        PhysicalPathingMedium tryFlyMedium,
        in PlannerTimelineOptions timeline)
    {
        var plan = new GenericMultiModalPathPlan();
        var legs = new List<(int u, int v, TravelLegMode mode)>();
        int cur = endSid;
        while (cur != startSid)
        {
            if (parent[cur] < 0)
                return new GenericMultiModalPathPlan();
            int p = parent[cur];
            int u = p / NumPrevModes;
            int v = cur / NumPrevModes;
            legs.Add((u, v, parentMode[cur]));
            cur = p;
        }

        legs.Reverse();
        for (int k = 0; k < legs.Count; k++)
        {
            int u = legs[k].u;
            int v = legs[k].v;
            TravelLegMode mode = legs[k].mode;
            if (!pathCache.TryGetValue((u, v, mode), out List<Vector3> cached) || cached == null || cached.Count == 0)
                return new GenericMultiModalPathPlan();

            var wp = new List<Vector3>(cached);
            MultiModalSegment seg = null;
            switch (mode)
            {
                case TravelLegMode.Walk:
                    seg = MultiModalSegment.FromWalk(wp);
                    break;
                case TravelLegMode.Fly:
                    seg = MultiModalSegment.FromFly(wp, tryFlyMedium);
                    break;
                case TravelLegMode.Drive:
                    seg = MultiModalSegment.FromDrive(wp, null);
                    break;
            }

            if (seg != null)
            {
                seg.medium = PhysicalMediumVolumeIndex.ResolveSegmentMedium(wp);
                if (seg.medium == PhysicalPathingMedium.Unspecified && mode == TravelLegMode.Fly)
                    seg.medium = tryFlyMedium;
                seg.estimatedTimeSec = EdgeTravelTime(PolylineLength(wp), mode, in timeline);
                plan.segments.Add(seg);
            }
        }

        return plan;
    }

    static void TryAddEdge(
        List<List<(int to, TravelLegMode mode, float len, float hintRaw)>> adj,
        int i,
        int j,
        Vector3 u,
        Vector3 v,
        TravelLegMode mode,
        HierarchicalPathingSolver solver,
        PhysicalPathingMedium tryFlyMedium,
        in PlannerTimelineOptions timeline,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        Vector3 mid = Vector3.Lerp(u, v, 0.5f);
        ProjectOntoPlanet(ref u);
        ProjectOntoPlanet(ref v);
        ProjectOntoPlanet(ref mid);

        if (PhysicalMediumVolumeIndex.TryResolveMedium(mid, out PhysicalPathingMedium volMedium) &&
            !PhysicalMediumVolumeRules.MediumAllowsMode(volMedium, mode))
            return;

        if (!TryFindPathWorld(u, v, mode, solver, tryFlyMedium, out List<Vector3> path))
            return;
        float len = PolylineLength(path);
        if (mode == TravelLegMode.Drive && timeline.minDriveLegLength > 1e-4f && len + 1e-4f < timeline.minDriveLegLength)
            return;
        if (mode == TravelLegMode.Fly && timeline.minFlyLegLength > 1e-4f && len + 1e-4f < timeline.minFlyLegLength)
            return;
        float hintRaw = RawHintCostDelta(mode, hints);
        adj[i].Add((j, mode, len, hintRaw));
    }

    static bool TryFindPathWorld(
        Vector3 u,
        Vector3 v,
        TravelLegMode mode,
        HierarchicalPathingSolver solver,
        PhysicalPathingMedium tryFlyMedium,
        out List<Vector3> path)
    {
        path = null;
        if (solver == null)
            return false;

        switch (mode)
        {
            case TravelLegMode.Walk:
                solver.pathingMode = PathingMode.Walk;
                path = solver.FindPath(u, v, returnBestEffortPathWhenNoPath: false);
                return path != null && path.Count > 0;
            case TravelLegMode.Drive:
                solver.pathingMode = PathingMode.Drive;
                path = solver.FindPath(u, v, returnBestEffortPathWhenNoPath: false);
                return path != null && path.Count > 0;
            case TravelLegMode.Fly:
                path = PhysicalPathingSolverRegistry.FindPathForMedium(
                    tryFlyMedium,
                    solver,
                    u,
                    v,
                    returnBestEffortPathWhenNoPath: false);
                return path != null && path.Count > 0;
            default:
                return false;
        }
    }

    public static float RawHintCostDelta(TravelLegMode mode, in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        switch (mode)
        {
            case TravelLegMode.Drive:
                if (hints.preferredVehicle != null)
                    return -0.45f * (1f - Mathf.Clamp01(hints.requireType01));
                return 0.05f;
            case TravelLegMode.Fly:
                return 0.35f * Mathf.Clamp01(hints.requireAsset01);
            default:
                return 0f;
        }
    }

    static float EdgeTravelTime(float pathLength, TravelLegMode mode, in PlannerTimelineOptions t)
    {
        float spd = mode switch
        {
            TravelLegMode.Walk => Mathf.Max(0.05f, t.walkSpeed),
            TravelLegMode.Drive => Mathf.Max(0.05f, t.driveSpeed),
            TravelLegMode.Fly => Mathf.Max(0.05f, t.flySpeed),
            _ => 1f
        };
        return pathLength / spd;
    }

    static float PolylineLength(List<Vector3> p)
    {
        if (p == null || p.Count < 2)
            return 0f;
        float s = 0f;
        for (int i = 1; i < p.Count; i++)
            s += Vector3.Distance(p[i - 1], p[i]);
        return s;
    }

    static List<Vector3> BuildLandmarks(
        Vector3 start,
        Vector3 goal,
        int chordSamples,
        IReadOnlyList<Vector3> extra)
    {
        var list = new List<Vector3> { start };
        int n = Mathf.Clamp(chordSamples, 0, PlannerTimelineOptions.MaxChordSamples);
        if (n > 0)
        {
            for (int k = 1; k <= n; k++)
            {
                float t = k / (float)(n + 1);
                list.Add(Vector3.Lerp(start, goal, t));
            }
        }

        if (extra != null)
        {
            for (int i = 0; i < extra.Count; i++)
                list.Add(extra[i]);
        }

        list.Add(goal);
        SortLandmarksAlongChord(list, start, goal);
        DeduplicateLandmarks(list, 0.15f);
        return list;
    }

    static void SortLandmarksAlongChord(List<Vector3> nodes, Vector3 start, Vector3 goal)
    {
        Vector3 g = goal - start;
        float denom = g.sqrMagnitude;
        if (denom < 1e-8f)
            return;
        nodes.Sort((a, b) =>
        {
            float ta = Vector3.Dot(a - start, g) / denom;
            float tb = Vector3.Dot(b - start, g) / denom;
            return ta.CompareTo(tb);
        });
    }

    static void DeduplicateLandmarks(List<Vector3> nodes, float eps)
    {
        float e2 = eps * eps;
        var kept = new List<Vector3> { nodes[0] };
        for (int i = 1; i < nodes.Count; i++)
        {
            if ((nodes[i] - kept[kept.Count - 1]).sqrMagnitude < e2)
                continue;
            kept.Add(nodes[i]);
        }

        nodes.Clear();
        nodes.AddRange(kept);
    }

    static void ProjectOntoPlanet(ref Vector3 world)
    {
        var planet = UnityEngine.Object.FindAnyObjectByType<Planetary.PlanetBody>();
        if (planet == null || !planet.TrySampleHeightAtWorld(world, out float height, out _))
            return;
        Vector3 dir = (world - planet.PlanetCenter).normalized;
        world = planet.PlanetCenter + dir * (planet.PlanetRadius + height);
    }
}
