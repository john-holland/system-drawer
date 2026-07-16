using System.Collections.Generic;
using UnityEngine;

public enum GambitAdviceBias
{
    MakeEasier,
    MakeHarder
}

[System.Serializable]
public sealed class GambitPhysicsMaterialSuggestion
{
    public string reason;
    public float frictionDelta;
    public float bouncinessDelta;
    public float manifoldFrictionDelta;
    public GambitAdviceBias bias;
}

/// <summary>Suggests physics material / manifold friction tweaks after a failed gambit run.</summary>
public static class GambitPhysicsMaterialAdvisor
{
    public static List<GambitPhysicsMaterialSuggestion> Suggest(
        bool pathSucceeded,
        float minClearance,
        float narrowThreshold,
        float impactSpeed,
        GambitAdviceBias bias = GambitAdviceBias.MakeEasier)
    {
        var list = new List<GambitPhysicsMaterialSuggestion>();
        if (pathSucceeded)
            return list;

        float sign = bias == GambitAdviceBias.MakeEasier ? 1f : -1f;
        if (minClearance < narrowThreshold)
        {
            list.Add(new GambitPhysicsMaterialSuggestion
            {
                reason = "narrow_aperture_clearance",
                frictionDelta = -0.15f * sign,
                bouncinessDelta = -0.05f * sign,
                manifoldFrictionDelta = -0.1f * sign,
                bias = bias
            });
        }
        if (impactSpeed > 8f)
        {
            list.Add(new GambitPhysicsMaterialSuggestion
            {
                reason = "high_impact_speed",
                frictionDelta = -0.05f * sign,
                bouncinessDelta = 0.1f * (bias == GambitAdviceBias.MakeHarder ? 1f : -1f),
                manifoldFrictionDelta = -0.05f * sign,
                bias = bias
            });
        }
        if (list.Count == 0)
        {
            list.Add(new GambitPhysicsMaterialSuggestion
            {
                reason = "generic_fail",
                frictionDelta = -0.1f * sign,
                bouncinessDelta = 0f,
                manifoldFrictionDelta = -0.08f * sign,
                bias = bias
            });
        }
        return list;
    }
}
