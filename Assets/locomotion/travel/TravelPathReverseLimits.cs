using UnityEngine;

/// <summary>
/// Helpers for reverse-leg budget on travel paths (normalized limit vs total arc length).
/// </summary>
public static class TravelPathReverseLimits
{
    public const float ShortPathThresholdMeters = 500f;

    public static float ComputeTotalPathLengthMeters(GenericMultiModalPathPlan plan)
    {
        if (plan?.segments == null)
            return 0f;

        float total = 0f;
        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg?.waypoints == null || seg.waypoints.Count < 2)
                continue;
            for (int i = 1; i < seg.waypoints.Count; i++)
                total += Vector3.Distance(seg.waypoints[i - 1], seg.waypoints[i]);
        }

        return total;
    }

    /// <summary>Default 1.0 when total &lt; 500 m, else 0.5.</summary>
    public static float ResolveDefaultReverseLegLimit01(float totalPathMeters) =>
        totalPathMeters < ShortPathThresholdMeters ? 1f : 0.5f;

    public static float ReverseBudgetMeters(float limit01, float totalMeters) =>
        limit01 <= 0f ? 0f : totalMeters * Mathf.Clamp01(limit01);

    public static bool AllowsReverse(float limit01) => limit01 > 0f;

    public static string FormatDistanceLabel(float reverseBudgetMeters, float totalMeters)
    {
        if (totalMeters <= 0f)
            return "0 m reverse · 0 m total";
        return $"{FormatMeters(reverseBudgetMeters)} reverse · {FormatMeters(totalMeters)} total";
    }

    public static string FormatMeters(float meters)
    {
        if (meters >= 1000f)
            return $"{meters / 1000f:0.##} km";
        return $"{meters:0.#} m";
    }
}
