using UnityEngine;

/// <summary>Enriches travel plans / TravelAgent from safety warden lemma hints.</summary>
public static class ConsiderSafetyWardenHints
{
    public const string TagSafety = "consider_safety_warden";

    public static void EnrichPlan(
        GenericMultiModalPathPlan plan,
        GameObject actor,
        Vector3 start,
        Vector3 goal,
        float scanRange = 18f)
    {
        if (plan?.segments == null) return;
        // Tag high-crowd apertures for warden hard inflation
        var apertures = Object.FindObjectsByType<PathingAperture>(FindObjectsSortMode.None);
        for (int i = 0; i < apertures.Length; i++)
        {
            var a = apertures[i];
            if (a == null) continue;
            ApertureCrowdSampler.Refresh(a, a.radius * 2f);
            if (a.crowdOccupancy01 < 0.25f) continue;
            for (int s = 0; s < plan.segments.Count; s++)
            {
                var seg = plan.segments[s];
                if (seg == null) continue;
                if (!string.IsNullOrEmpty(seg.apertureId) &&
                    string.Equals(seg.apertureId, a.apertureId, System.StringComparison.OrdinalIgnoreCase))
                {
                    var t = seg.runningTotals;
                    t.risk = Mathf.Clamp01(t.risk + a.crowdOccupancy01 * 0.2f);
                    seg.runningTotals = t;
                }
            }
        }
    }

    public static void ApplyLemmaHints(TravelAgent agent, SafetyWardenLemmaProperties props)
    {
        if (agent == null) return;
        if (!float.IsNaN(props.riskMin01)) agent.minRisk01 = props.riskMin01;
        if (!float.IsNaN(props.riskMax01)) agent.maxRisk01 = props.riskMax01;
        if (!float.IsNaN(props.safetyMin01)) agent.minSafety01 = props.safetyMin01;
        if (!float.IsNaN(props.safetyMax01)) agent.maxSafety01 = props.safetyMax01;
    }
}
