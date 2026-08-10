using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Post-processes a GenericMultiModalPathPlan for multibody separation (peers + cached dynamics) and optional soft static clearance near the final target.
/// </summary>
public static class TravelMultibodyPathAdjuster
{
    const float RelaxationStep = 0.45f;

    static readonly List<TravelNearPathActorCache.DynamicActorEntry> s_emptyDynamicCache =
        new List<TravelNearPathActorCache.DynamicActorEntry>();

    /// <summary>
    /// Maps confidence to XZ exclusion radius multiplier (low confidence = larger berth).
    /// </summary>
    public static float EffectiveClearanceRadius(float baseClearance, float confidence01)
    {
        float t = Mathf.Clamp01(confidence01);
        return Mathf.Max(0.05f, baseClearance * Mathf.Lerp(1.85f, 0.5f, t));
    }

    /// <summary>
    /// Extra minimum separation along route tangent based on pace slot (Lead / Tail).
    /// </summary>
    /// <param name="alongPeerMinusSelf">Dot((peer - self), routeForward) on XZ; positive means peer is ahead.</param>
    public static float PaceLongitudinalExtraSep(TravelPaceMode pace, float alongPeerMinusSelf)
    {
        const float k = 0.55f;
        if (pace == TravelPaceMode.Lead && alongPeerMinusSelf > 0.02f)
            return k;
        if (pace == TravelPaceMode.Tail && alongPeerMinusSelf < -0.02f)
            return k;
        return 0f;
    }

    /// <summary>
    /// Returns a cloned plan adjusted for multibody policy, or an unmodified clone when multibody is disabled.
    /// </summary>
    public static GenericMultiModalPathPlan Adjust(
        GenericMultiModalPathPlan basePlan,
        TravelAgentMultibodySettings settings,
        Vector3 actorWorld,
        HierarchicalPathingSolver solver,
        TravelAgent selfOptional)
    {
        if (basePlan == null)
            return new GenericMultiModalPathPlan();
        if (basePlan.IsEmpty)
            return basePlan.Clone();

        GenericMultiModalPathPlan working = basePlan.Clone();
        if (settings == null || !settings.enableMultibody)
            return working;

        var peers = new List<TravelAgent>(16);
        if (selfOptional != null)
            TravelAgentRegistry.CopyPeersForMultibody(selfOptional, peers, settings, selfOptional);
        else
        {
            foreach (TravelAgent a in TravelAgentRegistry.All)
            {
                if (a != null)
                    peers.Add(a);
            }
        }

        var peerPolys = new List<List<Vector3>>(peers.Count);
        for (int i = 0; i < peers.Count; i++)
            peerPolys.Add(BuildEffectivePolyline(peers[i]));

        Bounds bounds = ComputeUnionBounds(working, peerPolys, settings.nearPathBoundsMargin);
        IReadOnlyList<TravelNearPathActorCache.DynamicActorEntry> dynamics =
            TravelNearPathActorCache.Rebuild(bounds, settings.dynamicActorAvoidanceMask);

        Vector3 travelFwd = ComputeTravelForwardXZ(working, actorWorld);
        if (travelFwd.sqrMagnitude < 1e-6f)
            travelFwd = Vector3.forward;
        travelFwd.y = 0f;
        travelFwd.Normalize();

        float selfR = EffectiveClearanceRadius(settings.clearanceRadius, settings.confidence01);

        if (working.segments == null)
            return working;

        foreach (MultiModalSegment seg in working.segments)
        {
            if (!IsAdjustableWaypointSegment(seg) || seg.waypoints == null || seg.waypoints.Count < 2)
                continue;

            List<Vector3> pts = seg.waypoints;
            var originals = new List<Vector3>(pts);

            if (settings.enableLinkedSegmentSnake
                && (seg.mode == TravelLegMode.Rail || !string.IsNullOrEmpty(seg.consistId)))
            {
                ApplyLinkedSegmentSnakeXZ(pts, settings, selfOptional);
            }
            else
            {
                RelaxSegmentXZ(pts, originals, peerPolys, dynamics, selfR, settings, travelFwd);
            }

            if (!settings.shouldCollideWithPathObstacles && solver != null)
                ApplyStaticGoalClearanceXZ(pts, originals, solver, settings);
        }

        return working;
    }

    /// <summary>
    /// Keep coupler spacing / curvature for a train consist (linked snake) instead of independent convoy push.
    /// </summary>
    public static void ApplyLinkedSegmentSnakeXZ(
        List<Vector3> pts,
        TravelAgentMultibodySettings settings,
        TravelAgent selfOptional)
    {
        if (pts == null || pts.Count < 2 || settings == null) return;
        float spacing = Mathf.Max(0.5f, settings.linkedSegmentSpacingM);
        TrainVehicleRagdoll consist = null;
        if (selfOptional != null)
        {
            consist = selfOptional.trainConsist;
            if (consist == null && !string.IsNullOrEmpty(selfOptional.consistId))
            {
                foreach (var c in Object.FindObjectsByType<TrainVehicleRagdoll>(FindObjectsSortMode.None))
                {
                    if (c != null && c.consistId == selfOptional.consistId)
                    {
                        consist = c;
                        break;
                    }
                }
            }
        }
        if (consist != null && consist.nominalCouplerSpacingM > 0.1f)
            spacing = consist.nominalCouplerSpacingM;

        // Re-sample polyline to preserve arc length while enforcing min segment length ≈ coupler spacing.
        float total = 0f;
        for (int i = 1; i < pts.Count; i++)
            total += Vector3.Distance(FlattenXZ(pts[i - 1]), FlattenXZ(pts[i]));
        if (total < 1e-3f) return;

        int carCount = consist != null ? Mathf.Max(2, consist.cars.Count) : Mathf.Max(2, settings.linkedSegmentCarCountHint);
        int samples = Mathf.Clamp(carCount, 2, 64);
        var resampled = new List<Vector3>(samples);
        for (int s = 0; s < samples; s++)
        {
            float t = samples == 1 ? 0f : s / (float)(samples - 1);
            resampled.Add(SamplePolylineXZ(pts, t * total));
        }
        // Enforce spacing from head along tangent.
        for (int i = 1; i < resampled.Count; i++)
        {
            Vector3 prev = FlattenXZ(resampled[i - 1]);
            Vector3 cur = FlattenXZ(resampled[i]);
            Vector3 dir = cur - prev;
            float d = dir.magnitude;
            if (d < 1e-5f) dir = Vector3.forward;
            else dir /= d;
            Vector3 target = prev + dir * spacing;
            resampled[i] = new Vector3(target.x, resampled[i].y, target.z);
        }
        pts.Clear();
        pts.AddRange(resampled);
    }

    static Vector3 SamplePolylineXZ(IReadOnlyList<Vector3> pts, float distAlong)
    {
        float remain = distAlong;
        for (int i = 1; i < pts.Count; i++)
        {
            Vector3 a = FlattenXZ(pts[i - 1]);
            Vector3 b = FlattenXZ(pts[i]);
            float segLen = Vector3.Distance(a, b);
            if (segLen < 1e-6f) continue;
            if (remain <= segLen)
            {
                float u = remain / segLen;
                Vector3 p = Vector3.Lerp(pts[i - 1], pts[i], u);
                return p;
            }
            remain -= segLen;
        }
        return pts[pts.Count - 1];
    }

    public static List<Vector3> BuildEffectivePolyline(TravelAgent agent)
    {
        var list = new List<Vector3>();
        if (agent == null)
            return list;

        GenericMultiModalPathPlan pref = agent.GetPlanReferenceForMultibodyPeer();
        if (pref != null && !pref.IsEmpty)
        {
            foreach (Vector3 w in pref.FlattenWaypointsForGizmos())
                list.Add(w);
            if (list.Count > 0)
                return list;
        }

        list.Add(agent.previewStartWorld);
        list.Add(agent.previewGoalWorld);
        return list;
    }

    static bool IsAdjustableWaypointSegment(MultiModalSegment seg)
    {
        if (seg == null)
            return false;
        TravelLegMode m = seg.mode;
        return m == TravelLegMode.Walk || m == TravelLegMode.Drive || m == TravelLegMode.Rail || m == TravelLegMode.Fly;
    }

    static Vector3 FlattenXZ(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    static Vector3 GetTangentXZ(IReadOnlyList<Vector3> pts, int i)
    {
        Vector3 a = i > 0 ? pts[i - 1] : pts[i];
        Vector3 b = i < pts.Count - 1 ? pts[i + 1] : pts[i];
        return FlattenXZ(b - a);
    }

    static void RelaxSegmentXZ(
        List<Vector3> pts,
        List<Vector3> originals,
        List<List<Vector3>> peerPolys,
        IReadOnlyList<TravelNearPathActorCache.DynamicActorEntry> dynamics,
        float selfR,
        TravelAgentMultibodySettings settings,
        Vector3 travelFwd)
    {
        int passes = Mathf.Clamp(settings.relaxationIterations, 1, 12);
        float peerPointR = EffectiveClearanceRadius(settings.clearanceRadius, settings.confidence01) * 0.9f;

        for (int pass = 0; pass < passes; pass++)
        {
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector3 tan = GetTangentXZ(pts, i);
                if (tan.sqrMagnitude < 1e-8f)
                    continue;
                tan.Normalize();

                Vector3 accum = Vector3.zero;
                int hits = 0;

                Vector3 selfFlat = FlattenXZ(pts[i]);

                RelaxAgainstRopeFootprints(ref accum, ref hits, selfFlat, selfR, settings);

                for (int pi = 0; pi < peerPolys.Count; pi++)
                {
                    List<Vector3> poly = peerPolys[pi];
                    if (poly == null)
                        continue;
                    for (int j = 0; j < poly.Count; j++)
                    {
                        Vector3 q = poly[j];
                        Vector3 dFlat = selfFlat - FlattenXZ(q);
                        float dist = dFlat.magnitude;
                        float along = Vector3.Dot(FlattenXZ(q) - selfFlat, travelFwd);
                        float minD = selfR + peerPointR + PaceLongitudinalExtraSep(settings.paceMode, along);
                        if (dist < minD && dist > 1e-5f)
                        {
                            Vector3 push = (dFlat / dist) * (minD - dist) * RelaxationStep;
                            accum += push;
                            hits++;
                        }
                    }
                }

                for (int di = 0; di < dynamics.Count; di++)
                {
                    TravelNearPathActorCache.DynamicActorEntry e = dynamics[di];
                    Vector3 dFlat = selfFlat - FlattenXZ(e.center);
                    float dist = dFlat.magnitude;
                    float minD = selfR + e.radiusXZ;
                    if (dist < minD && dist > 1e-5f)
                    {
                        Vector3 push = (dFlat / dist) * (minD - dist) * RelaxationStep;
                        accum += push;
                        hits++;
                    }
                }

                if (hits > 0)
                {
                    Vector3 delta = accum / hits;
                    delta.y = 0f;
                    Vector3 relaxed = pts[i] + delta;
                    pts[i] = Vector3.Lerp(relaxed, originals[i], Mathf.Clamp01(settings.aggressiveness01));
                }
            }
        }
    }

    static void ApplyStaticGoalClearanceXZ(
        List<Vector3> pts,
        List<Vector3> originals,
        HierarchicalPathingSolver solver,
        TravelAgentMultibodySettings settings)
    {
        Vector3 goal = settings.ResolveFinalTargetWorld();
        if (goal.sqrMagnitude < 1e-10f && pts.Count > 0)
            goal = pts[pts.Count - 1];

        float approachR = Mathf.Max(0.5f, settings.approachRadius);
        float radius = Mathf.Max(0.05f, solver.agentRadius * settings.staticClearanceInflate *
            Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(settings.aggressiveness01)));
        float h = Mathf.Max(0.5f, solver.agentHeight);

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 p = pts[i];
            Vector3 pXZ = new Vector3(p.x, 0f, p.z);
            Vector3 gXZ = new Vector3(goal.x, 0f, goal.z);
            if (Vector3.Distance(pXZ, gXZ) > approachR)
                continue;

            if (!CapsuleObstructed(solver, p, radius, h))
                continue;

            Vector3 best = p;
            float bestDist = float.MaxValue;
            float ring = 0.35f;
            for (int k = 0; k < 8; k++)
            {
                float ang = k * (Mathf.PI * 2f / 8f);
                Vector3 off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * ring;
                Vector3 trial = p + off;
                if (!CapsuleObstructed(solver, trial, radius, h))
                {
                    float d = off.sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = trial;
                    }
                }
            }

            pts[i] = Vector3.Lerp(best, originals[i], Mathf.Clamp01(settings.aggressiveness01));
        }
    }

    static bool CapsuleObstructed(HierarchicalPathingSolver solver, Vector3 footWorld, float radius, float height)
    {
        Vector3 c0 = footWorld + Vector3.up * (height * 0.2f);
        Vector3 c1 = footWorld + Vector3.up * (height * 0.85f);
        return Physics.CheckCapsule(c0, c1, radius, solver.obstacleMask, QueryTriggerInteraction.Ignore);
    }

    static Bounds ComputeUnionBounds(GenericMultiModalPathPlan self, List<List<Vector3>> peerPolys, float margin)
    {
        bool has = false;
        Bounds b = default;
        void Enc(Vector3 v)
        {
            if (!has)
            {
                b = new Bounds(v, Vector3.zero);
                has = true;
            }
            else
                b.Encapsulate(v);
        }

        if (self != null && self.segments != null)
        {
            foreach (MultiModalSegment seg in self.segments)
            {
                if (seg?.waypoints == null)
                    continue;
                foreach (Vector3 w in seg.waypoints)
                    Enc(w);
            }
        }

        for (int i = 0; i < peerPolys.Count; i++)
        {
            List<Vector3> poly = peerPolys[i];
            if (poly == null)
                continue;
            for (int j = 0; j < poly.Count; j++)
                Enc(poly[j]);
        }

        if (!has)
            return new Bounds(Vector3.zero, Vector3.one * 2f);

        b.Expand(margin);
        return b;
    }

    /// <summary>Flattened XZ travel direction from plan waypoints (first to last), or toward actor when a single waypoint.</summary>
    public static Vector3 ComputeTravelForwardXZ(GenericMultiModalPathPlan plan, Vector3 actorWorld)
    {
        List<Vector3> flat = plan.FlattenWaypointsForGizmos();
        if (flat.Count >= 2)
            return FlattenXZ(flat[flat.Count - 1] - flat[0]);
        if (flat.Count == 1)
            return FlattenXZ(flat[0] - actorWorld);
        return Vector3.forward;
    }

    static void RelaxAgainstRopeFootprints(
        ref Vector3 accum,
        ref int hits,
        Vector3 selfFlat,
        float selfR,
        TravelAgentMultibodySettings settings)
    {
        if (settings == null || !settings.enableRopeFootprintClearance)
            return;

        foreach (RopePathingFootprint footprint in RopePathingFootprintRegistry.All)
        {
            if (footprint == null)
                continue;
            footprint.RebuildSamples();
            float ropeR = footprint.SampleRadiusM;
            foreach (Vector3 sample in footprint.BodySamples)
            {
                Vector3 dFlat = selfFlat - FlattenXZ(sample);
                float dist = dFlat.magnitude;
                float minD = selfR + ropeR;
                if (dist < minD && dist > 1e-5f)
                {
                    accum += (dFlat / dist) * (minD - dist) * RelaxationStep;
                    hits++;
                }
            }
        }
    }

    /// <summary>Test hook: run XZ relaxation on a polyline against peer polylines with no dynamic cache entries.</summary>
    public static void RelaxPolylineAgainstPeersForTests(
        List<Vector3> pts,
        List<Vector3> originals,
        List<List<Vector3>> peerPolys,
        float selfR,
        TravelAgentMultibodySettings settings,
        Vector3 travelForwardXZ)
    {
        Vector3 fwd = travelForwardXZ;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();
        RelaxSegmentXZ(pts, originals, peerPolys, s_emptyDynamicCache, selfR, settings, fwd);
    }
}
