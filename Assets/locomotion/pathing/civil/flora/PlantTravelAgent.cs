using System;
using UnityEngine;

[Serializable]
public sealed class GroundCompositionNarrativeEvent
{
    public string compositionId;
    public bool openCloseComplete;
    public bool manifoldApplied;
}

public struct PlantRootResult
{
    public bool accepted;
    public float pressure01;
    public float moisture01;
    public string mineralId;
}

/// <summary>Soil volume plus manifold pressure gate for mineral uptake.</summary>
public static class PlantRootAction
{
    public static bool Accepts(bool soilPresent, float pressure01, string mineralId, PlantTravelAgent plant)
    {
        if (plant == null || !soilPresent) return false;
        if (pressure01 < 0.02f) return false;
        return plant.MineralsAllowed(mineralId);
    }
}

/// <summary>
/// Shared plant travel base. Trees keep their weather/water/sun/mineral diamonds.
/// Moss, lichen, and undergrowth stop here and do not run a flower pipeline.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Plant Travel Agent")]
public class PlantTravelAgent : TravelAgent
{
    public PlantOrganismDef organism;
    public bool enforceNaturalGrowthFromPhysicsManifolds;
    public string[] mineralWhitelist = { "loam", "silt" };
    public string[] mineralBlacklist = { "salt", "bedrock" };
    public float waterRequirement01 = 0.4f;
    public float sunRequirement01 = 0.35f;
    public GroundCompositionNarrativeEvent groundEvent = new GroundCompositionNarrativeEvent();

    [Range(0f, 1f)] public float compositionWaterScale01 = 1f;
    [Range(0f, 1f)] public float compositionSunScale01 = 1f;
    [Range(0f, 1f)] public float compositionGroundScale01 = 1f;
    [Range(0f, 1f)] public float liveMoisture01 = 0.5f;
    [Range(0f, 1f)] public float liveSun01 = 0.5f;
    [Range(0f, 1f)] public float liveGround01 = 0.5f;

    public void ApplyOrganismDef()
    {
        if (organism == null) return;
        waterRequirement01 = organism.waterRequirement01;
        sunRequirement01 = organism.sunRequirement01;
        if (organism.mineralWhitelist != null && organism.mineralWhitelist.Length > 0)
            mineralWhitelist = organism.mineralWhitelist;
        if (organism.mineralBlacklist != null && organism.mineralBlacklist.Length > 0)
            mineralBlacklist = organism.mineralBlacklist;
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

    public void ApplyCompositionSample(float moisture01, float sunSample01, string mineralId)
    {
        liveMoisture01 = Mathf.Clamp01(moisture01);
        liveSun01 = Mathf.Clamp01(sunSample01);
        compositionWaterScale01 = Mathf.Clamp01(liveMoisture01 / Mathf.Max(0.05f, waterRequirement01));
        compositionSunScale01 = Mathf.Clamp01(liveSun01 / Mathf.Max(0.05f, sunRequirement01));
        compositionGroundScale01 = MineralsAllowed(mineralId) ? 1f : 0.25f;
        liveGround01 = compositionGroundScale01;
    }

    public PlantRootResult EvaluateRoot(Weather.WeatherPhysicsManifold manifold, DiggableVolume soil, string mineralId)
    {
        float pressure = 1013f;
        if (manifold != null)
            pressure = manifold.GetPressureAtPosition(transform.position);
        float pressure01 = Mathf.InverseLerp(870f, 1080f, pressure);
        bool soilPresent = soil != null && soil.diggable && soil.volumeKind == DiggableVolumeKind.Soil;
        string mineral = mineralId;
        if (string.IsNullOrEmpty(mineral) && soil != null)
            mineral = soil.materialClass;
        float moisture = soilPresent ? Mathf.Clamp01(soil.destructibility01) : liveMoisture01;
        bool accepted = PlantRootAction.Accepts(soilPresent, pressure01, mineral, this);
        if (accepted && enforceNaturalGrowthFromPhysicsManifolds)
            ApplyCompositionSample(moisture, liveSun01, mineral);
        return new PlantRootResult
        {
            accepted = accepted,
            pressure01 = pressure01,
            moisture01 = moisture,
            mineralId = mineral
        };
    }

    public void PublishGrowthEmpower(string eventPrefix, string stepName, float mult = 1.25f, float hours = 1f)
    {
        GrowthEventBus.Publish(
            StatisticalRetinueDao.Resolve(this),
            GrowthEvent.EmpowerFungus(
                (string.IsNullOrEmpty(eventPrefix) ? "plant" : eventPrefix) + "_" + (stepName ?? "step"),
                mult, hours));
    }
}
