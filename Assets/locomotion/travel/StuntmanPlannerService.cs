using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proposes at-speed apertures, parkour bridges, crash-through, and runway→terminus chains within the risk band.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Stuntman Planner Service")]
public sealed class StuntmanPlannerService : MonoBehaviour, ITravelRiskPlannerService
{
    public string ServiceId => "stuntman";

    [Range(0f, 1f)] public float softCrowdRiskWeight = 0.25f;
    [Min(1f)] public float discoverRadius = 24f;

    StuntDiscoveryContext _ctx;

    public void EnrichDiscovery(StuntDiscoveryContext ctx)
    {
        _ctx = ctx ?? _ctx;
        if (_ctx == null) return;
        if (_ctx.apertures != null)
        {
            for (int i = 0; i < _ctx.apertures.Length; i++)
            {
                var a = _ctx.apertures[i];
                if (a == null) continue;
                ApertureCrowdSampler.Refresh(a, _ctx.crowdSampleRadius);
            }
        }
    }

    public float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx)
    {
        if (seg == null) return 0f;
        float r = seg.runningTotals.risk;
        if (!string.IsNullOrEmpty(seg.apertureId) && ctx?.apertures != null)
        {
            for (int i = 0; i < ctx.apertures.Length; i++)
            {
                var a = ctx.apertures[i];
                if (a == null || !string.Equals(a.apertureId, seg.apertureId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                float crowd = ApertureCrowdSampler.GetOccupancy01(a);
                r = Mathf.Clamp01(r + crowd * softCrowdRiskWeight);
                if (a.passMode == PathingAperturePassMode.CrashThrough)
                    r = Mathf.Clamp01(r + 0.2f);
                break;
            }
        }
        if (seg.mode == TravelLegMode.Acrobatics)
            r = Mathf.Clamp01(r + 0.15f);
        return r;
    }

    public GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        if (plan?.segments == null) return plan;
        var band = TravelRiskBand.Resolve(in hints);
        var ctx = _ctx ?? StuntDiscoveryContext.FromScene(Vector3.zero, Vector3.zero, null);
        EnrichDiscovery(ctx);

        if (plan.rejectedForks == null)
            plan.rejectedForks = new List<MultiModalSegment>();

        TryAppendRunwayTerminusChain(plan, band, ctx);

        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            float risk = EstimateSegmentRisk(seg, ctx);
            var totals = seg.runningTotals;
            totals.risk = risk;
            seg.runningTotals = totals;

            if (!string.IsNullOrEmpty(seg.apertureId))
            {
                PathingAperture ap = FindAperture(ctx, seg.apertureId);
                float dmg = RagdollSectionStrengthMarker.EstimateDamageBias(ctx.actor, seg.segmentEnd);
                bool strong = RagdollSectionStrengthMarker.HasStrongLead(ctx.actor);
                seg.animationGroupTag = ParkourDamageMinAnimSelect.SelectForAperture(ap, dmg, strong);
                totals.damage = Mathf.Max(totals.damage, dmg);
                seg.runningTotals = totals;
            }
        }

        // Soft-prefer risk near band preferred when under minRisk
        float planRisk = 0f;
        int n = 0;
        for (int i = 0; i < plan.segments.Count; i++)
        {
            if (plan.segments[i] == null) continue;
            planRisk += plan.segments[i].runningTotals.risk;
            n++;
        }
        if (n > 0) planRisk /= n;
        if (planRisk < band.minRisk01 - 1e-4f)
            BiasTowardRiskierBridge(plan, band, ctx);

        plan.RecomputePlanTotals();
        return plan;
    }

    void TryAppendRunwayTerminusChain(GenericMultiModalPathPlan plan, TravelRiskBand.Band band, StuntDiscoveryContext ctx)
    {
        if (ctx.stuntZones == null || ctx.stuntZones.Length == 0) return;
        StuntZone runway = null;
        StuntZone terminus = null;
        for (int i = 0; i < ctx.stuntZones.Length; i++)
        {
            var z = ctx.stuntZones[i];
            if (z == null) continue;
            if (runway == null && z.IsRunway) runway = z;
            if (terminus == null && z.IsTerminus && z != runway) terminus = z;
            if (z.kind == StuntZoneKind.Both)
            {
                runway = z;
                terminus = z;
            }
        }
        if (runway == null || terminus == null) return;

        float approach = Vector3.Distance(ctx.startWorld, runway.RunwayStart);
        if (!runway.HasAdequateRunwayForSpeed(approach + runway.lengthMeters))
            return;

        float yaw = Vector3.SignedAngle(Vector3.forward, runway.Forward, Vector3.up);
        var turn = TravelPlanRunningTotals.FromTurnCost(yaw);
        float crowd = runway.linkedAperture != null
            ? ApertureCrowdSampler.GetOccupancy01(runway.linkedAperture)
            : 0f;
        var jump = TravelPlanRunningTotals.FromJump(runway.requiredEntrySpeed01, crowd, 0.1f);
        jump = jump.Add(turn);
        if (!band.Contains(jump.risk))
        {
            var rejected = MultiModalSegment.FromAcrobatics(null, null, runway.RunwayStart, terminus.RunwayEnd);
            rejected.stuntZoneRef = runway.gameObject;
            rejected.runningTotals = jump;
            rejected.animationGroupTag = ParkourAnimationGroup.SpringRollJump;
            plan.rejectedForks.Add(rejected);
            return;
        }

        var approachSeg = MultiModalSegment.FromWalk(new List<Vector3> { ctx.startWorld, runway.RunwayStart, runway.RunwayEnd });
        approachSeg.stuntZoneRef = runway.gameObject;
        approachSeg.runningTotals = turn;
        approachSeg.animationGroupTag = ParkourAnimationGroup.LopingStrides;

        var bridge = MultiModalSegment.FromAcrobatics(null, null, runway.RunwayEnd, terminus.Center);
        bridge.stuntZoneRef = terminus.gameObject;
        bridge.runningTotals = jump;
        bridge.animationGroupTag = ParkourDamageMinAnimSelect.SelectLanding(runway.requiredEntrySpeed01, jump.damage);
        if (runway.linkedAperture != null)
        {
            bridge.apertureId = runway.linkedAperture.apertureId;
            bridge.animationGroupTag = ParkourDamageMinAnimSelect.SelectForAperture(
                runway.linkedAperture, jump.damage, RagdollSectionStrengthMarker.HasStrongLead(ctx.actor));
        }

        plan.segments.Add(approachSeg);
        plan.segments.Add(bridge);
    }

    void BiasTowardRiskierBridge(GenericMultiModalPathPlan plan, TravelRiskBand.Band band, StuntDiscoveryContext ctx)
    {
        if (ctx.apertures == null) return;
        PathingAperture best = null;
        float bestScore = -1f;
        for (int i = 0; i < ctx.apertures.Length; i++)
        {
            var a = ctx.apertures[i];
            if (a == null) continue;
            if (a.passMode == PathingAperturePassMode.SelectOnly) continue;
            float crowd = ApertureCrowdSampler.GetOccupancy01(a);
            float r = a.passMode == PathingAperturePassMode.CrashThrough ? 0.35f + crowd * 0.3f : 0.2f + crowd * 0.2f;
            if (!band.Contains(r)) continue;
            float score = Mathf.Abs(r - band.PreferredRisk);
            score = 1f - score;
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }
        if (best == null) return;
        var seg = MultiModalSegment.FromAcrobatics(null, null, best.ApproachPointWorld, best.transform.position);
        seg.apertureId = best.apertureId;
        seg.runningTotals = TravelPlanRunningTotals.FromJump(0.5f, ApertureCrowdSampler.GetOccupancy01(best), 0.15f);
        seg.animationGroupTag = ParkourDamageMinAnimSelect.SelectForAperture(
            best, seg.runningTotals.damage, RagdollSectionStrengthMarker.HasStrongLead(ctx.actor));
        plan.segments.Add(seg);
    }

    static PathingAperture FindAperture(StuntDiscoveryContext ctx, string id)
    {
        if (ctx?.apertures == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < ctx.apertures.Length; i++)
        {
            var a = ctx.apertures[i];
            if (a != null && string.Equals(a.apertureId, id, System.StringComparison.OrdinalIgnoreCase))
                return a;
        }
        return null;
    }
}
