using System.Collections.Generic;
using UnityEngine;

/// <summary>Enriches travel plans with nearby stunt zones / crash apertures (Stuntman consider).</summary>
public static class ConsiderStuntmanHints
{
    public const string TagStunt = "consider_stuntman";

    public static void EnrichPlan(
        GenericMultiModalPathPlan plan,
        GameObject actor,
        Vector3 start,
        Vector3 goal,
        float scanRange = 18f)
    {
        if (plan?.segments == null || actor == null) return;
        var zones = Object.FindObjectsByType<StuntZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            if (z == null) continue;
            if (Vector3.Distance(z.Center, start) > scanRange && Vector3.Distance(z.Center, goal) > scanRange)
                continue;
            if (!z.IsRunway) continue;
            // Annotate existing acrobatics legs near the zone
            for (int s = 0; s < plan.segments.Count; s++)
            {
                var seg = plan.segments[s];
                if (seg == null) continue;
                if (seg.mode != TravelLegMode.Acrobatics && seg.mode != TravelLegMode.ToolBridge) continue;
                if (seg.stuntZoneRef == null)
                    seg.stuntZoneRef = z.gameObject;
                if (string.IsNullOrEmpty(seg.animationGroupTag))
                    seg.animationGroupTag = ParkourAnimationGroup.SpringRollJump;
                if (z.linkedAperture != null && string.IsNullOrEmpty(seg.apertureId))
                    seg.apertureId = z.linkedAperture.apertureId;
            }
        }
    }

    /// <summary>Apply lemma-resolved planner hints onto a TravelAgent.</summary>
    public static void ApplyLemmaHints(TravelAgent agent, StuntmanLemmaProperties props)
    {
        if (agent == null) return;
        if (!float.IsNaN(props.maxRisk01)) agent.maxRisk01 = props.maxRisk01;
        if (!float.IsNaN(props.minRisk01)) agent.minRisk01 = props.minRisk01;
        if (props.preferCrash)
            agent.requireAsset01 = Mathf.Min(agent.requireAsset01, 0.35f);
    }
}
