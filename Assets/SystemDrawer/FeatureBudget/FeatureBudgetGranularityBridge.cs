using UnityEngine;

public static class FeatureBudgetGranularityBridge
{
    public static int MapGranularityToLodTierOffset(float granularityLevel)
    {
        if (granularityLevel >= 0.65f)
            return 0;
        if (granularityLevel >= 0.35f)
            return 1;
        if (granularityLevel > 0f)
            return 2;
        return 3;
    }

    public static int ApplyLodTierBump(int baseTier, float granularityLevel, int maxTier = 3)
    {
        int offset = MapGranularityToLodTierOffset(granularityLevel);
        return Mathf.Min(baseTier + offset, maxTier);
    }

    public static float ScaleIntervalByGranularity(float baseIntervalSeconds, float granularityLevel)
    {
        float g = Mathf.Max(granularityLevel, 0.1f);
        return baseIntervalSeconds / g;
    }

    public static float ResolveHorizonFullSimKm(FeatureBudgetRatioRegistry registry, float fallbackKm)
    {
        if (registry == null)
            return fallbackKm;
        float v = registry.GetEffectiveValue(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm);
        return v > 0f ? v : fallbackKm;
    }

    public static float ResolveHorizonDistanceKm(FeatureBudgetRatioRegistry registry, float fallbackKm)
    {
        if (registry == null)
            return fallbackKm;
        float v = registry.GetEffectiveValue(FeatureBudgetRatioFieldIds.HorizonDistanceKm);
        return v > 0f ? v : fallbackKm;
    }

    public static float ResolveSdfNearFullKm(FeatureBudgetRatioRegistry registry, float fallbackKm)
    {
        if (registry == null)
            return fallbackKm;
        float v = registry.GetEffectiveValue(FeatureBudgetRatioFieldIds.SdfNearFullKm);
        return v > 0f ? v : fallbackKm;
    }

    public static float ResolveSdfFarFullKm(FeatureBudgetRatioRegistry registry, float fallbackKm)
    {
        if (registry == null)
            return fallbackKm;
        float v = registry.GetEffectiveValue(FeatureBudgetRatioFieldIds.SdfFarFullKm);
        return v > 0f ? v : fallbackKm;
    }
}
