using System;
using UnityEngine;

/// <summary>Ratio binding for budget-governed aesthetic granularity (mirrors Composition UI fields).</summary>
[Serializable]
public sealed class FeatureBudgetRatioBinding
{
    public string fieldId = "";
    public float ratio;
    public bool ratioLocked = true;
    public bool budgetGoverned = true;
    public string unlockReason = "";
    public float manualOverride;
    public string sourceFeatureId = "";

    [NonSerialized] public float granularityLevel = 1f;

    public float CurrentValue(float anchorR) =>
        ratioLocked ? ratio * anchorR : manualOverride;

    public float EffectiveValue(float anchorR)
    {
        if (!budgetGoverned || !ratioLocked)
            return CurrentValue(anchorR);
        return ratio * anchorR * Mathf.Clamp01(granularityLevel);
    }

    public bool CanUnlock => !string.IsNullOrWhiteSpace(unlockReason);

    public static bool IsValidUnlockReason(string reason) =>
        !string.IsNullOrWhiteSpace(reason);
}
