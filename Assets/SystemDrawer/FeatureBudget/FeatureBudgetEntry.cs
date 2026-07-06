using System;
using UnityEngine;

[Serializable]
public sealed class FeatureBudgetEntry
{
    public string featureId = "";
    public string displayName = "";
    public int importanceRank;
    public FeatureBudgetControlMode controlMode = FeatureBudgetControlMode.Auto;
    public bool manualEnabled = true;
    public string[] perfScopePrefixes = Array.Empty<string>();
    public string[] ratioFieldIds = Array.Empty<string>();
    public bool supportsAestheticGranularity = true;

    [NonSerialized] public float granularityLevel = 1f;
    [NonSerialized] public float lastFrameMs;
    [NonSerialized] public float rollingAvgMs;
}
