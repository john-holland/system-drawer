using UnityEngine;

/// <summary>Static access to active Feature Budget runtime.</summary>
public static class FeatureBudget
{
    static FeatureBudgetRuntime _active;

    public static FeatureBudgetRuntime Active => _active;
    public static bool IsAvailable => _active != null;
    public static float FrameCpuMs => _active != null ? _active.FrameCpuMs : 0f;
    public static float RollingCpuMs => _active != null ? _active.RollingCpuMs : 0f;
    public static FeatureBudgetState BudgetState => _active != null ? _active.BudgetState : FeatureBudgetState.Normal;
    public static bool IsBudgetMode => BudgetState == FeatureBudgetState.BudgetMode;
    public static FeatureBudgetRatioRegistry Ratios => _active != null ? _active.RatioRegistry : null;

    internal static void SetActive(FeatureBudgetRuntime runtime) => _active = runtime;

    public static float GetGranularity(string featureId)
    {
        if (_active == null)
            return 1f;
        return _active.GetGranularity(featureId);
    }

    public static bool IsFeatureActive(string featureId)
    {
        if (_active == null)
            return true;
        return _active.IsFeatureActive(featureId);
    }

    public static float GetRatioEffective(string fieldId)
    {
        if (_active?.RatioRegistry == null)
            return 0f;
        return _active.RatioRegistry.GetEffectiveValue(fieldId);
    }

    public static void RegisterPlanetSource(IPlanetRatioSource source)
    {
        _active?.RegisterPlanetSource(source);
    }

    public static void RegisterConsumer(IFeatureGranularityConsumer consumer)
    {
        _active?.RegisterConsumer(consumer);
    }

    public static void UnregisterConsumer(IFeatureGranularityConsumer consumer)
    {
        _active?.UnregisterConsumer(consumer);
    }
}
