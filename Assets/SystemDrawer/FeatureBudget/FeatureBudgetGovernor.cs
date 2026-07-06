using System.Collections.Generic;
using UnityEngine;

public sealed class FeatureBudgetGovernor
{
    readonly FeatureBudgetProfile _profile;
    readonly FeatureBudgetRatioRegistry _ratios;
    float _lastWarnLogTime = -10f;

    public FeatureBudgetState State { get; private set; } = FeatureBudgetState.Normal;

    public FeatureBudgetGovernor(FeatureBudgetProfile profile, FeatureBudgetRatioRegistry ratios)
    {
        _profile = profile;
        _ratios = ratios;
    }

    public void Tick(float rollingCpuMs)
    {
        if (_profile == null)
            return;

        float target = Mathf.Max(0.01f, _profile.targetFrameCpuMs);
        float warnLine = target * Mathf.Clamp(_profile.warnThreshold, 0.5f, 1f);

        if (rollingCpuMs <= warnLine)
        {
            State = FeatureBudgetState.Normal;
            ResetAutoGranularity();
            return;
        }

        if (rollingCpuMs <= target)
        {
            State = FeatureBudgetState.Warn;
            ResetAutoGranularity();
            MaybeLogWarn(rollingCpuMs, target);
            return;
        }

        State = FeatureBudgetState.BudgetMode;
        StepDownUntilUnderBudget(rollingCpuMs, target);
        MaybeLogWarn(rollingCpuMs, target);
    }

    void ResetAutoGranularity()
    {
        if (_profile.entries == null)
            return;
        for (int i = 0; i < _profile.entries.Count; i++)
        {
            var entry = _profile.entries[i];
            if (entry.controlMode != FeatureBudgetControlMode.Auto)
                continue;
            entry.granularityLevel = 1f;
            _ratios?.SetGranularityForFeature(entry.featureId, 1f);
        }
    }

    void StepDownUntilUnderBudget(float rollingCpuMs, float target)
    {
        if (_profile.entries == null)
            return;

        var ordered = new List<FeatureBudgetEntry>(_profile.entries);
        ordered.Sort((a, b) => b.importanceRank.CompareTo(a.importanceRank));

        for (int step = 1; step < FeatureBudgetDefaults.GranularitySteps.Length && rollingCpuMs > target; step++)
        {
            float stepLevel = FeatureBudgetDefaults.GranularitySteps[step];
            bool anyChanged = false;
            for (int i = 0; i < ordered.Count; i++)
            {
                var entry = ordered[i];
                if (entry.controlMode != FeatureBudgetControlMode.Auto || !entry.supportsAestheticGranularity)
                    continue;
                if (entry.granularityLevel <= stepLevel + 0.001f)
                    continue;
                entry.granularityLevel = stepLevel;
                _ratios?.SetGranularityForFeature(entry.featureId, stepLevel);
                anyChanged = true;
                rollingCpuMs *= 0.92f;
                break;
            }
            if (!anyChanged)
                break;
        }
    }

    void MaybeLogWarn(float rollingCpuMs, float target)
    {
        if (Time.time - _lastWarnLogTime < 5f)
            return;
        _lastWarnLogTime = Time.time;
        Debug.LogWarning($"[FeatureBudget] {State}: rolling CPU {rollingCpuMs:F2}ms / target {target:F2}ms");
    }

    public static bool IsFeatureActive(FeatureBudgetEntry entry)
    {
        if (entry == null)
            return true;
        switch (entry.controlMode)
        {
            case FeatureBudgetControlMode.Off:
                return false;
            case FeatureBudgetControlMode.Manual:
                return entry.manualEnabled;
            default:
                return entry.granularityLevel > 0f;
        }
    }

    public static float GetGranularity(FeatureBudgetEntry entry)
    {
        if (entry == null)
            return 1f;
        if (entry.controlMode == FeatureBudgetControlMode.Off)
            return 0f;
        if (entry.controlMode == FeatureBudgetControlMode.Manual && !entry.manualEnabled)
            return 0f;
        return Mathf.Clamp01(entry.granularityLevel);
    }
}
