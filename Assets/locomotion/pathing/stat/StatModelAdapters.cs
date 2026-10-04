using System;
using System.Collections.Generic;
using Planetary.AsteroidBelt;
using UnityEngine;

public abstract class StatModelBase : IStatisticsModel
{
    protected readonly Dictionary<string, StatOverride> Specifics =
        new Dictionary<string, StatOverride>(StringComparer.OrdinalIgnoreCase);

    public abstract string ModelId { get; }

    public virtual bool TrySample(in StatQuery q, out float value)
    {
        value = 0f;
        return false;
    }

    public virtual IEnumerable<StatSample> SampleBatch(in StatQuery q, int count, int seed)
    {
        var list = new List<StatSample>();
        for (int i = 0; i < count; i++)
        {
            var qq = q;
            qq.seed = seed + i;
            if (TrySample(qq, out float v))
                list.Add(new StatSample { key = q.featureKey, value = v, specificity = SpecificityKey.Map() });
        }
        return list;
    }

    public virtual bool TryGetSpecific(in SpecificityKey key, out StatOverride ov)
    {
        ov = default;
        string k = SpecificStorageKey(key);
        return !string.IsNullOrEmpty(k) && Specifics.TryGetValue(k, out ov);
    }

    public virtual void PutSpecific(in SpecificityKey key, in StatOverride ov)
    {
        string k = SpecificStorageKey(key);
        if (!string.IsNullOrEmpty(k))
            Specifics[k] = ov;
    }

    public virtual void Reconcile(StatReconcileMode mode) { }

    public virtual void PutFeature(string key, float value) { }

    protected static string SpecificStorageKey(in SpecificityKey key)
    {
        switch (key.level)
        {
            case StatSpecificityLevel.Actor: return "a:" + key.actorId;
            case StatSpecificityLevel.Persona: return "p:" + key.personaKey;
            case StatSpecificityLevel.Role: return "r:" + key.role;
            case StatSpecificityLevel.Cohort: return "c:" + key.cohortId;
            case StatSpecificityLevel.Authored: return "x:" + key.personaKey;
            default: return "m";
        }
    }
}

public sealed class FeatureMapModel : StatModelBase
{
    readonly Dictionary<string, float> _map = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    public override string ModelId { get; }

    public FeatureMapModel(string modelId) { ModelId = modelId ?? "features"; }

    public static FeatureMapModel FromFeatures(string modelId, IReadOnlyDictionary<string, float> src)
    {
        var m = new FeatureMapModel(modelId);
        if (src == null) return m;
        foreach (var kv in src)
            m._map[kv.Key] = kv.Value;
        return m;
    }

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(q.featureKey)) return false;
        if (TryGetSpecific(SpecificityKey.Persona(q.personaKey), out var ov) &&
            string.Equals(ov.channelOrFeature, q.featureKey, StringComparison.OrdinalIgnoreCase))
        {
            value = ov.value;
            return true;
        }
        return _map.TryGetValue(q.featureKey, out value);
    }

    public override void PutFeature(string key, float value)
    {
        if (!string.IsNullOrEmpty(key))
            _map[key] = value;
    }

    public IReadOnlyDictionary<string, float> Snapshot => _map;
}

public sealed class LifeBaselineModel : StatModelBase
{
    public override string ModelId => "life.baseline";

    public void Apply(LifeSystemsSheet sheet, IReadOnlyDictionary<string, float> features,
        IReadOnlyDictionary<string, float> needs)
    {
        LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, features, needs);
        // Re-apply persona/actor pins lightly
        foreach (var kv in Specifics)
        {
            if (string.IsNullOrEmpty(kv.Value.channelOrFeature) || sheet == null) continue;
            sheet.Set01(kv.Value.channelOrFeature, Mathf.Clamp01(kv.Value.value));
        }
    }
}

public sealed class CivDemographicsModel : StatModelBase
{
    public CivilianDemographics Demographics { get; private set; }
    public override string ModelId => "civ.demographics";

    public CivDemographicsModel(CivilianDemographics d) { Demographics = d ?? new CivilianDemographics(); }

    public override bool TrySample(in StatQuery q, out float value)
    {
        var d = Demographics;
        if (d != null && d.spatialGradient != null && d.spatialGradient.IsActive &&
            (q.worldPos != Vector3.zero || q.seed != 0))
        {
            Vector3 samplePos = d.spatialGradient.SamplePosForDemographics(q.worldPos, q.seed);
            d = d.ResolveAt(samplePos);
        }
        value = d != null ? d.unemploymentRate01 : 0f;
        if (!string.IsNullOrEmpty(q.featureKey))
        {
            if (q.featureKey.IndexOf("unemploy", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (string.Equals(q.featureKey, "x", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.featureKey, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.featureKey, "z", StringComparison.OrdinalIgnoreCase))
            {
                value = q.featureKey switch
                {
                    "x" or "X" => q.worldPos.x,
                    "y" or "Y" => q.worldPos.y,
                    _ => q.worldPos.z
                };
                return true;
            }
            if (q.featureKey.IndexOf("age_child", StringComparison.OrdinalIgnoreCase) >= 0)
            { value = d != null ? d.ageChild01 : 0f; return true; }
            if (q.featureKey.IndexOf("spatial", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                value = Demographics?.spatialGradient != null
                    ? Demographics.spatialGradient.WeightAt(q.worldPos) : 0f;
                return true;
            }
        }
        value = d != null ? d.cityPopulation : 0f;
        return d != null;
    }

    public override void PutFeature(string key, float value)
    {
        if (Demographics == null) Demographics = new CivilianDemographics();
        if (key != null && key.IndexOf("unemploy", StringComparison.OrdinalIgnoreCase) >= 0)
            Demographics.unemploymentRate01 = Mathf.Clamp01(value);
    }

    public override void Reconcile(StatReconcileMode mode)
    {
        if (Demographics == null) return;
        Demographics.unemploymentRate01 = Mathf.Clamp01(Demographics.unemploymentRate01);
    }

    public void Replace(CivilianDemographics d) => Demographics = d ?? new CivilianDemographics();
}

public sealed class CellularDemographicsModel : StatModelBase
{
    public CellularDemographics Demographics { get; private set; }
    public CellularRetinue Retinue { get; set; }
    public override string ModelId => "culture.cellular";

    public CellularDemographicsModel(CellularDemographics d)
    {
        Demographics = d ?? CellularDemographics.DefaultTissue();
        Retinue = new CellularRetinue { demographics = Demographics };
    }

    public override bool TrySample(in StatQuery q, out float value)
    {
        var d = Demographics;
        if (d != null && d.spatialGradient != null && d.spatialGradient.IsActive)
        {
            Vector3 samplePos = d.spatialGradient.SamplePosForDemographics(q.worldPos, q.seed);
            d = d.ResolveAt(samplePos);
        }
        value = d != null ? d.allergyPrevalence01 : 0f;
        if (!string.IsNullOrEmpty(q.featureKey))
        {
            if (q.featureKey.IndexOf("glue", StringComparison.OrdinalIgnoreCase) >= 0)
            { value = d != null ? d.glueReadiness01 : 0f; return true; }
            if (q.featureKey.IndexOf("sensor", StringComparison.OrdinalIgnoreCase) >= 0)
            { value = d != null ? d.sensor01 : 0f; return true; }
            if (q.featureKey.IndexOf("allergy", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return d != null;
    }

    public void Replace(CellularDemographics d)
    {
        Demographics = d ?? CellularDemographics.DefaultTissue();
        if (Retinue == null) Retinue = new CellularRetinue();
        Retinue.demographics = Demographics;
    }
}

public sealed class ElectorateStatModel : StatModelBase
{
    public ElectorateDemographics Demographics { get; private set; }
    public override string ModelId => "vote.electorate";

    public ElectorateStatModel(ElectorateDemographics d)
    {
        Demographics = d ?? ElectorateDemographics.DefaultTwoParty();
    }

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = 0f;
        if (Demographics == null) return false;
        var slice = Demographics.Sample(q.seed);
        value = slice != null ? slice.yesTilt01 : 0.5f;
        return true;
    }

    public override void Reconcile(StatReconcileMode mode) => Demographics?.Renormalize();

    public string TiltYesNo(bool actorPinned, string actorChoice, int seed)
    {
        var slice = Demographics?.Sample(seed);
        if (actorPinned)
            return Demographics.TiltYesNo(slice, true, actorChoice, seed);
        return Demographics.TiltYesNo(slice, false, null, seed);
    }
}

public sealed class PersonaBundleModel : StatModelBase
{
    public PersonaRequestBundle Bundle { get; private set; }
    public override string ModelId => "persona.bundle";

    public PersonaBundleModel(PersonaRequestBundle b) { Bundle = b ?? PersonaRequestBundle.CreateDefault("persona", CivilSystemKind.Generic); }

    public void Set(PersonaRequestBundle b) { if (b != null) Bundle = b; }

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = Bundle != null ? Bundle.biorhythmAmplitudeSeed : 0.5f;
        return Bundle != null;
    }
}

public sealed class PeckingStatModel : StatModelBase
{
    public List<RetinuePeckingEntry> Entries { get; } = new List<RetinuePeckingEntry>();
    public override string ModelId => "retinue.pecking";

    public override bool TryGetSpecific(in SpecificityKey key, out StatOverride ov)
    {
        if (base.TryGetSpecific(key, out ov)) return true;
        if (string.IsNullOrEmpty(key.personaKey)) return false;
        for (int i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            if (e != null && string.Equals(e.personaKey, key.personaKey, StringComparison.OrdinalIgnoreCase))
            {
                ov = StatOverride.Role(e.role, e.peckingOrder);
                return true;
            }
        }
        return false;
    }

    public override void PutSpecific(in SpecificityKey key, in StatOverride ov)
    {
        base.PutSpecific(key, ov);
        if (string.IsNullOrEmpty(key.personaKey)) return;
        for (int i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            if (e != null && string.Equals(e.personaKey, key.personaKey, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(ov.role)) e.role = ov.role;
                if (ov.peckingOrder != 0) e.peckingOrder = ov.peckingOrder;
                return;
            }
        }
        Entries.Add(new RetinuePeckingEntry
        {
            personaKey = key.personaKey,
            role = ov.role ?? "",
            peckingOrder = ov.peckingOrder != 0 ? ov.peckingOrder : 100
        });
    }
}

public sealed class TradeDemographicsModel : StatModelBase
{
    public TradeDemographics Trade { get; }
    public override string ModelId => "trade.demographics";

    public TradeDemographicsModel(TradeDemographics t) { Trade = t ?? new TradeDemographics(); }

    public override void Reconcile(StatReconcileMode mode) => Trade.Reconcile();
}

public sealed class EconomicOverlayModel : StatModelBase
{
    public EconomicOverlay Overlay { get; }
    public override string ModelId => "econ.overlay";

    public EconomicOverlayModel(EconomicOverlay o) { Overlay = o ?? new EconomicOverlay(); }

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = Overlay != null ? Overlay.velocity01 : 0f;
        if (!string.IsNullOrEmpty(q.featureKey) &&
            q.featureKey.IndexOf("liquid", StringComparison.OrdinalIgnoreCase) >= 0)
            value = Overlay != null ? Overlay.liquidity01 : 0f;
        return Overlay != null;
    }
}

public sealed class CultureStatModel : StatModelBase
{
    public MicroCulture Culture { get; }
    public override string ModelId => Culture != null && Culture.kind == CultureKind.Fungal ? "culture.fungus" : "culture.micro";

    public CultureStatModel(MicroCulture c) { Culture = c ?? MicroCulture.CreateBacterial("c", "e_coli", default); }

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = Culture != null ? Culture.biomass01 : 0f;
        return Culture != null;
    }
}

public sealed class SpaceManifoldStatModel : StatModelBase
{
    public AsteroidBeltStatisticalManifold Manifold;
    public override string ModelId => "space.manifold";

    public override bool TrySample(in StatQuery q, out float value)
    {
        value = 0f;
        if (Manifold == null)
        {
            value = 0.35f;
            return true;
        }
        value = Manifold.SampleDensity(q.worldPos);
        return true;
    }
}
