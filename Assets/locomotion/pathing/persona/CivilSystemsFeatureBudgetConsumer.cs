using UnityEngine;

/// <summary>Scales PersonaDayManager tick interval from FeatureBudget civil_systems granularity.</summary>
[AddComponentMenu("Locomotion/Persona/Civil Systems Feature Budget Consumer")]
public sealed class CivilSystemsFeatureBudgetConsumer : MonoBehaviour, IFeatureGranularityConsumer
{
    public PersonaDayManager dayManager;
    public float baseTickIntervalSeconds = 0.5f;

    public string FeatureId => FeatureBudgetIds.CivilSystems;

    void OnEnable()
    {
        if (dayManager == null) dayManager = GetComponent<PersonaDayManager>() ?? PersonaDayManager.Instance;
        FeatureBudget.RegisterConsumer(this);
    }

    void OnDisable()
    {
        FeatureBudget.UnregisterConsumer(this);
    }

    public void ApplyFeatureGranularity(FeatureBudgetRatioRegistry ratios)
    {
        if (dayManager == null) dayManager = GetComponent<PersonaDayManager>() ?? PersonaDayManager.Instance;
        if (dayManager == null) return;
        float g = FeatureBudget.GetGranularity(FeatureBudgetIds.CivilSystems);
        dayManager.tickIntervalSeconds = FeatureBudgetGranularityBridge.ScaleIntervalByGranularity(baseTickIntervalSeconds, g);
        if (!FeatureBudget.IsFeatureActive(FeatureBudgetIds.CivilSystems))
            dayManager.tickIntervalSeconds = Mathf.Max(dayManager.tickIntervalSeconds, 5f);
    }
}
