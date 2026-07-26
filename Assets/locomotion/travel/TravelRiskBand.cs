using System;
using UnityEngine;

/// <summary>
/// Complementary risk/safety band from PlannerHints.
/// safety01 = 1 - risk01 for band checks.
/// </summary>
public static class TravelRiskBand
{
    public const float Unset = float.NaN;

    [Serializable]
    public struct Band
    {
        public float minRisk01;
        public float maxRisk01;

        public bool Contains(float risk01)
        {
            float r = Mathf.Clamp01(risk01);
            return r >= minRisk01 - 1e-5f && r <= maxRisk01 + 1e-5f;
        }

        public float ClampRisk(float risk01) => Mathf.Clamp(risk01, minRisk01, maxRisk01);

        public float PreferredRisk => Mathf.Clamp01((minRisk01 + maxRisk01) * 0.5f);
    }

    public static bool IsSet(float v) => !float.IsNaN(v);

    /// <summary>
    /// Resolve intersection of maxRisk/minRisk/minSafety/maxSafety.
    /// Unset fields leave that side open. Defaults to [0,1].
    /// </summary>
    public static Band Resolve(in GenericTraversibilityPlannerSolver.PlannerHints hints)
    {
        float minR = 0f;
        float maxR = 1f;

        if (IsSet(hints.minRisk01))
            minR = Mathf.Max(minR, Mathf.Clamp01(hints.minRisk01));
        if (IsSet(hints.maxRisk01))
            maxR = Mathf.Min(maxR, Mathf.Clamp01(hints.maxRisk01));
        if (IsSet(hints.minSafety01))
            maxR = Mathf.Min(maxR, Mathf.Clamp01(1f - hints.minSafety01));
        if (IsSet(hints.maxSafety01))
            minR = Mathf.Max(minR, Mathf.Clamp01(1f - hints.maxSafety01));

        if (minR > maxR)
        {
            // Conflicting band: collapse to midpoint clamp
            float mid = Mathf.Clamp01((minR + maxR) * 0.5f);
            minR = maxR = mid;
        }

        return new Band { minRisk01 = minR, maxRisk01 = maxR };
    }

    public static GenericTraversibilityPlannerSolver.PlannerHints WithDefaults(
        float requireAsset01,
        float requireType01,
        VehicleActor preferredVehicle,
        float maxRisk01 = Unset,
        float minRisk01 = Unset,
        float minSafety01 = Unset,
        float maxSafety01 = Unset)
    {
        return new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = preferredVehicle,
            maxRisk01 = maxRisk01,
            minRisk01 = minRisk01,
            minSafety01 = minSafety01,
            maxSafety01 = maxSafety01
        };
    }
}
