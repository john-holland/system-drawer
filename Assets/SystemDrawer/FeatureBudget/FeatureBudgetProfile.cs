using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeatureBudgetProfile", menuName = "System Drawer/Feature Budget Profile")]
public sealed class FeatureBudgetProfile : ScriptableObject
{
    public float targetFrameCpuMs = 16.67f;
    [Range(0.5f, 1f)] public float warnThreshold = 0.9f;
    public int rollingWindowFrames = 60;
    public List<FeatureBudgetEntry> entries = new List<FeatureBudgetEntry>();
    public List<FeatureBudgetRatioBinding> ratioBindings = new List<FeatureBudgetRatioBinding>();

    public void EnsureDefaults()
    {
        if (entries == null || entries.Count == 0)
            entries = FeatureBudgetDefaults.CreateDefaultEntries();
        if (ratioBindings == null || ratioBindings.Count == 0)
            ratioBindings = FeatureBudgetDefaults.CreateDefaultRatioBindings();
    }

    public FeatureBudgetEntry FindEntry(string featureId)
    {
        if (entries == null)
            return null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].featureId == featureId)
                return entries[i];
        }
        return null;
    }

    public FeatureBudgetRatioBinding FindRatioBinding(string fieldId)
    {
        if (ratioBindings == null)
            return null;
        for (int i = 0; i < ratioBindings.Count; i++)
        {
            if (ratioBindings[i].fieldId == fieldId)
                return ratioBindings[i];
        }
        return null;
    }
}
