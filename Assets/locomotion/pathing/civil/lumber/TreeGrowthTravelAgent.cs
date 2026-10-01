using System;
using System.Collections.Generic;
using UnityEngine;

public enum TreeGrowthStepKind
{
    Germinate = 0,
    Sapling = 1,
    Cambium = 2,
    Canopy = 3,
    Harvest = 4,
    Replant = 5
}

[Serializable]
public sealed class TreeGrowthStep
{
    public TreeGrowthStepKind kind;
    public string sgInstanceId;
    public Vector3 predictedWorld;
    [Range(0f, 1f)] public float progress01;
    [Range(0f, 1f)] public float weather01 = 0.6f;
    [Range(0f, 1f)] public float water01 = 0.6f;
    [Range(0f, 1f)] public float sun01 = 0.7f;
    [Range(0f, 1f)] public float minerals01 = 0.5f;
    [Range(0f, 1f)] public float optimalWeather01 = 0.7f;
    [Range(0f, 1f)] public float optimalWater01 = 0.7f;
    [Range(0f, 1f)] public float optimalSun01 = 0.8f;
    [Range(0f, 1f)] public float optimalMinerals01 = 0.6f;
    [Range(0f, 1f)] public float limitWeather01 = 1f;
    [Range(0f, 1f)] public float limitWater01 = 0.2f;
    [Range(0f, 1f)] public float limitSun01 = 0.15f;
    [Range(0f, 1f)] public float limitMinerals01 = 0.1f;
    public string weatherEventId;
    public bool success;
    public bool hasFailureEvent;
    public string failureEventId;
}

[Serializable]
public sealed class GroundCompositionNarrativeEvent
{
    public string compositionId;
    public bool openCloseComplete;
    public bool manifoldApplied;
}

/// <summary>Tree gen stages with weather/water/sun/mineral diamonds. Extends park plant defs.</summary>
[AddComponentMenu("Locomotion/Travel/Tree Growth Travel Agent")]
public class TreeGrowthTravelAgent : TravelAgent
{
    public static readonly string[] DiamondAxes = { "Weather", "Water", "Sun", "Minerals" };

    public List<TreeGrowthStep> steps = new List<TreeGrowthStep>();
    public int selectedStepIndex;
    public LotGrassPlantDef plantDef;
    public bool enforceNaturalGrowthFromPhysicsManifolds;
    public string[] mineralWhitelist = { "loam", "silt" };
    public string[] mineralBlacklist = { "salt", "bedrock" };
    public float waterRequirement01 = 0.4f;
    public float sunRequirement01 = 0.35f;
    public GroundCompositionNarrativeEvent groundEvent = new GroundCompositionNarrativeEvent();
    public bool replantOnSamePlot = true;

    public TreeGrowthStep SelectedStep =>
        steps != null && selectedStepIndex >= 0 && selectedStepIndex < steps.Count
            ? steps[selectedStepIndex]
            : null;

    public static List<TreeGrowthStep> DefaultPipeline()
    {
        var list = new List<TreeGrowthStep>();
        foreach (TreeGrowthStepKind k in Enum.GetValues(typeof(TreeGrowthStepKind)))
            list.Add(new TreeGrowthStep { kind = k, sgInstanceId = k.ToString().ToLowerInvariant() });
        return list;
    }

    void Awake()
    {
        if (steps == null || steps.Count == 0)
            steps = DefaultPipeline();
    }

    public bool CompleteSelected(bool success)
    {
        var s = SelectedStep;
        if (s == null) return false;
        s.success = success;
        s.progress01 = success ? 1f : s.progress01;
        if (!success && !s.hasFailureEvent)
            return false;
        return true;
    }

    public void ApplyGroundCompositionAfterOpenClose()
    {
        if (groundEvent == null) return;
        if (!groundEvent.openCloseComplete) return;
        groundEvent.manifoldApplied = true;
    }

    public bool MineralsAllowed(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (mineralBlacklist != null)
        {
            for (int i = 0; i < mineralBlacklist.Length; i++)
                if (string.Equals(mineralBlacklist[i], id, StringComparison.OrdinalIgnoreCase))
                    return false;
        }
        if (mineralWhitelist == null || mineralWhitelist.Length == 0)
            return true;
        for (int i = 0; i < mineralWhitelist.Length; i++)
            if (string.Equals(mineralWhitelist[i], id, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public float[] BlueOptimal01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.7f, 0.7f, 0.8f, 0.6f };
        return new[] { s.optimalWeather01, s.optimalWater01, s.optimalSun01, s.optimalMinerals01 };
    }

    public float[] RedLimit01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 1f, 0.2f, 0.15f, 0.1f };
        return new[] { s.limitWeather01, s.limitWater01, s.limitSun01, s.limitMinerals01 };
    }

    public float[] DashedWhiteActive01()
    {
        var s = SelectedStep;
        if (s == null) return new[] { 0.5f, 0.5f, 0.5f, 0.5f };
        return new[] { s.weather01, s.water01, s.sun01, s.minerals01 };
    }

    public bool OverLimit()
    {
        var s = SelectedStep;
        if (s == null) return false;
        return s.water01 < s.limitWater01 - 1e-4f
               || s.sun01 < s.limitSun01 - 1e-4f
               || s.minerals01 < s.limitMinerals01 - 1e-4f;
    }

    /// <summary>Publish a growth empowerment into StatisticalRetinueDao (fungus/plant empower bus).</summary>
    public void PublishGrowthEmpower(float mult = 1.25f, float hours = 1f)
    {
        GrowthEventBus.Publish(
            StatisticalRetinueDao.Resolve(this),
            GrowthEvent.EmpowerFungus("tree_growth_" + (SelectedStep != null ? SelectedStep.kind.ToString() : "step"),
                mult, hours));
    }
}
