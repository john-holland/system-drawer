using UnityEngine;

/// <summary>Shared contract for Stuntman / Safety Warden planner component services.</summary>
public interface ITravelRiskPlannerService
{
    string ServiceId { get; }

    void EnrichDiscovery(StuntDiscoveryContext ctx);

    GenericMultiModalPathPlan RescoreOrRewrite(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints);

    float EstimateSegmentRisk(MultiModalSegment seg, StuntDiscoveryContext ctx);
}

/// <summary>Shared discovery bag for stunt / safety scoring.</summary>
public sealed class StuntDiscoveryContext
{
    public Vector3 startWorld;
    public Vector3 goalWorld;
    public GameObject actor;
    public StuntZone[] stuntZones;
    public PathingAperture[] apertures;
    public float crowdSampleRadius = 3f;

    public static StuntDiscoveryContext FromScene(Vector3 start, Vector3 goal, GameObject actor, float crowdRadius = 3f)
    {
        return new StuntDiscoveryContext
        {
            startWorld = start,
            goalWorld = goal,
            actor = actor,
            stuntZones = Object.FindObjectsByType<StuntZone>(FindObjectsSortMode.None),
            apertures = Object.FindObjectsByType<PathingAperture>(FindObjectsSortMode.None),
            crowdSampleRadius = crowdRadius
        };
    }
}

/// <summary>Applies Stuntman then Safety Warden (propose → gate), then optional wrestling/referee, love-making/consent, combat/safety-lock.</summary>
public static class TravelRiskPlannerPipeline
{
    public static GenericMultiModalPathPlan Apply(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints,
        GameObject actor,
        StuntmanPlannerService stuntman,
        SafetyWardenPlannerService warden)
    {
        return Apply(plan, in hints, actor, stuntman, warden, null, null, null, null, null, null);
    }

    public static GenericMultiModalPathPlan Apply(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints,
        GameObject actor,
        StuntmanPlannerService stuntman,
        SafetyWardenPlannerService warden,
        WrestlingPlannerService wrestling,
        RefereeWardenPlannerService referee)
    {
        return Apply(plan, in hints, actor, stuntman, warden, wrestling, referee, null, null, null, null);
    }

    public static GenericMultiModalPathPlan Apply(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints,
        GameObject actor,
        StuntmanPlannerService stuntman,
        SafetyWardenPlannerService warden,
        WrestlingPlannerService wrestling,
        RefereeWardenPlannerService referee,
        LoveMakingPlannerService loveMaking,
        ConsentWardenPlannerService consentWarden)
    {
        return Apply(plan, in hints, actor, stuntman, warden, wrestling, referee, loveMaking, consentWarden, null, null);
    }

    public static GenericMultiModalPathPlan Apply(
        GenericMultiModalPathPlan plan,
        in GenericTraversibilityPlannerSolver.PlannerHints hints,
        GameObject actor,
        StuntmanPlannerService stuntman,
        SafetyWardenPlannerService warden,
        WrestlingPlannerService wrestling,
        RefereeWardenPlannerService referee,
        LoveMakingPlannerService loveMaking,
        ConsentWardenPlannerService consentWarden,
        CombatPlannerService combat,
        SafetyLockWardenPlannerService safetyLock)
    {
        if (plan == null)
            return plan;

        Vector3 start = plan.segments != null && plan.segments.Count > 0 && plan.segments[0].waypoints != null &&
                        plan.segments[0].waypoints.Count > 0
            ? plan.segments[0].waypoints[0]
            : (actor != null ? actor.transform.position : Vector3.zero);
        Vector3 goal = plan.segments != null && plan.segments.Count > 0
            ? (plan.segments[plan.segments.Count - 1].waypoints != null &&
               plan.segments[plan.segments.Count - 1].waypoints.Count > 0
                ? plan.segments[plan.segments.Count - 1].waypoints[
                    plan.segments[plan.segments.Count - 1].waypoints.Count - 1]
                : plan.segments[plan.segments.Count - 1].segmentEnd)
            : start;

        var ctx = StuntDiscoveryContext.FromScene(start, goal, actor);
        if (stuntman != null && stuntman.isActiveAndEnabled)
        {
            stuntman.EnrichDiscovery(ctx);
            plan = stuntman.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (warden != null && warden.isActiveAndEnabled)
        {
            warden.EnrichDiscovery(ctx);
            plan = warden.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (wrestling != null && wrestling.isActiveAndEnabled)
        {
            wrestling.EnrichDiscovery(ctx);
            plan = wrestling.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (referee != null && referee.isActiveAndEnabled)
        {
            referee.EnrichDiscovery(ctx);
            plan = referee.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (loveMaking != null && loveMaking.isActiveAndEnabled)
        {
            loveMaking.EnrichDiscovery(ctx);
            plan = loveMaking.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (consentWarden != null && consentWarden.isActiveAndEnabled)
        {
            consentWarden.EnrichDiscovery(ctx);
            plan = consentWarden.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (combat != null && combat.isActiveAndEnabled)
        {
            combat.EnrichDiscovery(ctx);
            plan = combat.RescoreOrRewrite(plan, in hints) ?? plan;
        }
        if (safetyLock != null && safetyLock.isActiveAndEnabled)
        {
            safetyLock.EnrichDiscovery(ctx);
            plan = safetyLock.RescoreOrRewrite(plan, in hints) ?? plan;
        }

        plan.RecomputePlanTotals();
        StuntPlanEmergenceBuffer.Publish(plan);
        return plan;
    }
}
