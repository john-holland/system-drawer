using System;
using System.Collections.Generic;
using UnityEngine;

public enum CultureKind
{
    Bacterial = 0,
    Fungal = 1,
    Biofilm = 2
}

public enum GrowthEventKind
{
    EmpowerFungus = 0,
    SporeRain = 1,
    NutrientPulse = 2,
    HeatShock = 3,
    BacterialBloom = 4,
    Custom = 5
}

[Serializable]
public sealed class MicroCulture
{
    public string cultureId;
    public CultureKind kind;
    public string speciesId;
    public string hostRetinueId;
    public string mediaCommodityKey = "media";
    public float biomass01;
    public float viability01 = 1f;
    public float tempK = 310f;
    public float ph01 = 0.5f;
    public float carryingCapacity01 = 1f;
    public float growthRate = 0.2f;
    public bool sealedVessel = true;
    public float empowerMult = 1f;
    public float empowerHoursRemaining;
    public List<string> metaboliteCommodityKeys = new List<string>();

    public static MicroCulture CreateBacterial(string cultureId, string speciesId, in StatSeed seed)
    {
        return new MicroCulture
        {
            cultureId = cultureId ?? "bact",
            kind = CultureKind.Bacterial,
            speciesId = speciesId ?? seed.cultureSpeciesId ?? "e_coli",
            hostRetinueId = seed.retinueId,
            growthRate = 0.35f,
            mediaCommodityKey = "media"
        };
    }

    public static MicroCulture CreateFungal(string cultureId, string speciesId, in StatSeed seed)
    {
        return new MicroCulture
        {
            cultureId = cultureId ?? "fungus",
            kind = CultureKind.Fungal,
            speciesId = speciesId ?? seed.cultureSpeciesId ?? "oyster",
            hostRetinueId = seed.retinueId,
            growthRate = 0.18f,
            mediaCommodityKey = "substrate"
        };
    }
}

[Serializable]
public sealed class GrowthEvent
{
    public string eventId;
    public string displayName;
    public GrowthEventKind kind;
    public string[] targetCultureIds;
    public string[] targetSpeciesIds;
    [Range(0f, 4f)] public float empowerMult = 1.5f;
    public float durationHours = 2f;
    [CronExpr] public string windowCron;
    public float minCausalDepth;
    public bool sporeBurst;
    public string lemmaHint;
    public bool armed;
    public float armedUntilHours;

    public static GrowthEvent EmpowerFungus(string eventId, float mult, float hours, params string[] species)
    {
        return new GrowthEvent
        {
            eventId = eventId ?? "fungus_bloom",
            displayName = eventId,
            kind = GrowthEventKind.EmpowerFungus,
            targetSpeciesIds = species,
            empowerMult = mult,
            durationHours = hours,
            armed = true
        };
    }
}

public struct GrowthEventResult
{
    public bool ok;
    public string eventId;
    public int culturesTouched;
    public string message;
}

/// <summary>Optional bridge so horticulture / tree growth can publish into the DAO empower bus.</summary>
public static class GrowthEventBus
{
    public static void Publish(IStatisticalRetinueDao dao, GrowthEvent ev)
    {
        if (dao == null || ev == null) return;
        dao.ArmGrowthEvent(ev);
    }

    public static void PublishFungusEmpower(IStatisticalRetinueDao dao, string eventId, float mult, float hours, params string[] species)
    {
        Publish(dao, GrowthEvent.EmpowerFungus(eventId, mult, hours, species));
    }
}
