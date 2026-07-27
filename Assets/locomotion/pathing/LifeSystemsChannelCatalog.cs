using System;
using System.Collections.Generic;

public enum LifeSystemsChannelUnit
{
    Unit01,
    Clinical
}

public enum LifeSystemsChannelAggregation
{
    Systemic,
    RegionalCapable
}

[Serializable]
public sealed class LifeSystemsChannelDef
{
    public string id;
    public string displayName;
    public LifeSystemsChannelUnit unit;
    public float clinicalDefault;
    public float clinicalMin;
    public float clinicalMax;
    public float setpoint01;
    public float softBandMin01;
    public float softBandMax01;
    public string societyFeatureKey;
    public string needAspectId;
    public LifeSystemsChannelAggregation aggregation;
    /// <summary>When true, lower 01 means healthier (e.g. cholesterol risk, depression).</summary>
    public bool invertHealthy;
}

/// <summary>Canonical life-systems channel definitions (clinical + 0–1 + cognition + political).</summary>
public static class LifeSystemsChannelCatalog
{
    public const string HeartRate = "heart_rate";
    public const string BloodPressureSys = "blood_pressure_sys";
    public const string BloodPressureDia = "blood_pressure_dia";
    public const string HypertensiveLoad = "hypertensive_load";
    public const string BloodSugar = "blood_sugar";
    public const string Hydration = "hydration";
    public const string Lipids = "lipids";
    public const string Cholesterol = "cholesterol";
    public const string Immune = "immune";
    public const string Lymph = "lymph";
    public const string Endocrine = "endocrine";
    public const string Vitamins = "vitamins";
    public const string Adrenaline = "adrenaline";
    public const string Fatigue = "fatigue";
    public const string HomeostasisError = "homeostasis_error";
    public const string LifeForce = "life_force";
    public const string BioRhythmAmplitude = "bio_rhythm_amplitude";

    public const string Memory = "memory";
    public const string ClearThought = "clear_thought";
    public const string LongTermRecall = "long_term_recall";
    public const string ShortTermRecall = "short_term_recall";
    public const string Sympathy = "sympathy";
    public const string Empathy = "empathy";
    public const string Morale = "morale";
    public const string Ablution = "ablution";
    public const string BladderFill = "bladder_fill";
    public const string BowelFill = "bowel_fill";
    public const string Depression = "depression";
    public const string Denial = "denial";
    public const string Mania = "mania";
    public const string Attention = "attention";
    public const string AttentionToDetail = "attention_to_detail";
    public const string AttentionToSurroundings = "attention_to_surroundings";
    public const string AddLoad = "add_load";
    public const string AdhdLoad = "adhd_load";
    public const string Liberalism = "liberalism";
    public const string Conservatism = "conservatism";
    public const string Socialism = "socialism";
    public const string Communism = "communism";

    static readonly LifeSystemsChannelDef[] All;
    static readonly Dictionary<string, LifeSystemsChannelDef> ById;

    static LifeSystemsChannelCatalog()
    {
        All = BuildAll();
        ById = new Dictionary<string, LifeSystemsChannelDef>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < All.Length; i++)
            ById[All[i].id] = All[i];
    }

    public static IReadOnlyList<LifeSystemsChannelDef> Channels => All;

    public static bool TryGet(string id, out LifeSystemsChannelDef def) => ById.TryGetValue(id ?? "", out def);

    public static float ClinicalTo01(LifeSystemsChannelDef def, float clinical)
    {
        if (def == null || def.unit == LifeSystemsChannelUnit.Unit01)
            return clinical;
        float span = def.clinicalMax - def.clinicalMin;
        if (span <= 1e-5f) return 0.5f;
        return (clinical - def.clinicalMin) / span;
    }

    public static float ClinicalFrom01(LifeSystemsChannelDef def, float unit01)
    {
        if (def == null || def.unit == LifeSystemsChannelUnit.Unit01)
            return unit01;
        return def.clinicalMin + unit01 * (def.clinicalMax - def.clinicalMin);
    }

    static LifeSystemsChannelDef[] BuildAll()
    {
        return new[]
        {
            Clinical(HeartRate, "Heart Rate", 72f, 40f, 180f, "healthcareCoverage", "need_physiological"),
            Clinical(BloodPressureSys, "Blood Pressure Sys", 120f, 70f, 220f, "healthcareCoverage", "need_physiological"),
            Clinical(BloodPressureDia, "Blood Pressure Dia", 80f, 40f, 140f, "healthcareCoverage", "need_physiological"),
            U01(HypertensiveLoad, "Hypertensive Load", 0f, 0f, 0.15f, true, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.Systemic),
            Clinical(BloodSugar, "Blood Sugar", 90f, 40f, 400f, "healthcareCoverage", "need_physiological"),
            U01(Hydration, "Hydration", 0.65f, 0.55f, 0.75f, false, "water", "need_physiological", LifeSystemsChannelAggregation.RegionalCapable),
            U01(Lipids, "Lipids Risk", 0.35f, 0.2f, 0.5f, true, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.Systemic),
            U01(Cholesterol, "Cholesterol Risk", 0.35f, 0.2f, 0.5f, true, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.Systemic),
            U01(Immune, "Immune", 0.8f, 0.7f, 0.95f, false, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.RegionalCapable),
            U01(Lymph, "Lymph", 0.8f, 0.7f, 0.95f, false, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.RegionalCapable),
            U01(Endocrine, "Endocrine", 0.75f, 0.65f, 0.9f, false, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.RegionalCapable),
            U01(Vitamins, "Vitamins", 0.75f, 0.65f, 0.9f, false, "healthcareCoverage", "need_physiological", LifeSystemsChannelAggregation.Systemic),
            U01(Adrenaline, "Adrenaline", 0.08f, 0f, 0.25f, false, null, null, LifeSystemsChannelAggregation.Systemic),
            U01(Fatigue, "Fatigue", 0.1f, 0f, 0.3f, true, null, null, LifeSystemsChannelAggregation.Systemic),
            U01(HomeostasisError, "Homeostasis Error", 0f, 0f, 0.1f, true, null, null, LifeSystemsChannelAggregation.Systemic),
            U01(LifeForce, "Life Force", 0.85f, 0.75f, 0.95f, false, null, null, LifeSystemsChannelAggregation.Systemic),
            U01(BioRhythmAmplitude, "Bio Rhythm Amplitude", 0.35f, 0.2f, 0.5f, false, null, null, LifeSystemsChannelAggregation.Systemic),

            Affect(Memory, 0.8f),
            Affect(ClearThought, 0.8f),
            Affect(LongTermRecall, 0.75f),
            Affect(ShortTermRecall, 0.75f),
            Affect(Sympathy, 0.65f, "civic_trust", "need_belonging"),
            Affect(Empathy, 0.65f, "civic_trust", "need_belonging"),
            Affect(Morale, 0.7f, "civic_trust", "need_esteem"),
            Affect(Ablution, 0.7f),
            U01(BladderFill, "Bladder Fill", 0.2f, 0f, 0.35f, true, null, "need_physiological", LifeSystemsChannelAggregation.Systemic),
            U01(BowelFill, "Bowel Fill", 0.2f, 0f, 0.35f, true, null, "need_physiological", LifeSystemsChannelAggregation.Systemic),
            Affect(Depression, 0.15f, invert: true),
            Affect(Denial, 0.15f, invert: true),
            Affect(Mania, 0.15f, invert: true),
            Affect(Attention, 0.7f),
            Affect(AttentionToDetail, 0.65f),
            Affect(AttentionToSurroundings, 0.65f, regional: true),
            Affect(AddLoad, 0.2f, invert: true),
            Affect(AdhdLoad, 0.2f, invert: true),
            Affect(Liberalism, 0.35f, "taxRate", "need_esteem"),
            Affect(Conservatism, 0.35f, "congressStability", "need_safety"),
            Affect(Socialism, 0.3f, "welfareBenefits", "need_belonging"),
            Affect(Communism, 0.25f, "welfareBenefits", "need_belonging"),
        };
    }

    static LifeSystemsChannelDef Clinical(string id, string name, float def, float min, float max, string feature, string aspect)
    {
        float sp = (def - min) / (max - min);
        return new LifeSystemsChannelDef
        {
            id = id,
            displayName = name,
            unit = LifeSystemsChannelUnit.Clinical,
            clinicalDefault = def,
            clinicalMin = min,
            clinicalMax = max,
            setpoint01 = sp,
            softBandMin01 = sp - 0.05f,
            softBandMax01 = sp + 0.05f,
            societyFeatureKey = feature,
            needAspectId = aspect,
            aggregation = LifeSystemsChannelAggregation.Systemic
        };
    }

    static LifeSystemsChannelDef U01(
        string id, string name, float setpoint, float bandMin, float bandMax, bool invert,
        string feature, string aspect, LifeSystemsChannelAggregation agg)
    {
        return new LifeSystemsChannelDef
        {
            id = id,
            displayName = name,
            unit = LifeSystemsChannelUnit.Unit01,
            clinicalDefault = setpoint,
            clinicalMin = 0f,
            clinicalMax = 1f,
            setpoint01 = setpoint,
            softBandMin01 = bandMin,
            softBandMax01 = bandMax,
            societyFeatureKey = feature,
            needAspectId = aspect,
            aggregation = agg,
            invertHealthy = invert
        };
    }

    static LifeSystemsChannelDef Affect(
        string id, float setpoint, string feature = null, string aspect = null,
        bool invert = false, bool regional = false)
    {
        float pad = 0.1f;
        return U01(
            id, id.Replace('_', ' '), setpoint,
            UnityEngine.Mathf.Clamp01(setpoint - pad),
            UnityEngine.Mathf.Clamp01(setpoint + pad),
            invert, feature, aspect,
            regional ? LifeSystemsChannelAggregation.RegionalCapable : LifeSystemsChannelAggregation.Systemic);
    }
}
