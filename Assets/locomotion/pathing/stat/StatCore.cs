using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

public enum StatReconcileMode
{
    None = 0,
    Quotas = 1,
    Slices = 2,
    Funding = 3,
    TradeShares = 4
}

public enum StatSpecificityLevel
{
    Map = 0,
    Cohort = 1,
    Role = 2,
    Persona = 3,
    Actor = 4,
    Authored = 5
}

[Serializable]
public struct StatSeed
{
    public string cityId;
    public string venueStableId;
    public string personaKey;
    public CivilSystemKind civilKind;
    public int rngSeed;
    public string companyId;
    public string retinueId;
    public string rulePackId;
    public AuditMode auditMode;
    public string cultureSpeciesId;
    public IReadOnlyDictionary<string, float> societyFeatures;
    public IReadOnlyDictionary<string, float> needSatisfied01;
    public int population;

    public static StatSeed ForCity(string cityId, IReadOnlyDictionary<string, float> features = null, int population = 100)
    {
        return new StatSeed
        {
            cityId = cityId ?? "city",
            societyFeatures = features,
            population = Mathf.Max(1, population),
            auditMode = AuditMode.Playable
        };
    }
}

[Serializable]
public struct StatQuery
{
    public string featureKey;
    public string commodityKey;
    public CivilSystemKind civilKind;
    public Vector3 worldPos;
    public int seed;
    public string retinueId;
    public string personaKey;
}

[Serializable]
public struct SpecificityKey
{
    public StatSpecificityLevel level;
    public string cohortId;
    public string role;
    public string personaKey;
    public string actorId;

    public static SpecificityKey Map() => new SpecificityKey { level = StatSpecificityLevel.Map };
    public static SpecificityKey Cohort(string id) => new SpecificityKey { level = StatSpecificityLevel.Cohort, cohortId = id };
    public static SpecificityKey Role(string role) => new SpecificityKey { level = StatSpecificityLevel.Role, role = role };
    public static SpecificityKey Persona(string key) => new SpecificityKey { level = StatSpecificityLevel.Persona, personaKey = key };
    public static SpecificityKey Actor(string id) => new SpecificityKey { level = StatSpecificityLevel.Actor, actorId = id };
    public static SpecificityKey Authored(string key) => new SpecificityKey { level = StatSpecificityLevel.Authored, personaKey = key };
}

[Serializable]
public struct StatOverride
{
    public string channelOrFeature;
    public float value;
    public string role;
    public int peckingOrder;
    public string notes;

    public static StatOverride Channel(string id, float v) =>
        new StatOverride { channelOrFeature = id, value = v };

    public static StatOverride Role(string role, int pecking) =>
        new StatOverride { role = role, peckingOrder = pecking };
}

[Serializable]
public struct StatSample
{
    public string key;
    public float value;
    public SpecificityKey specificity;
}

public struct StatContext
{
    public IStatisticalRetinueDao Dao;
    public string RetinueId;
    public string PersonaKey;
    public string ActorId;
    public string CommodityKey;
    public string EdgeId;
    public float CausalDepth;
    public float SpeedMps;
    public DateTime UtcNow;
    public bool ActorPinned;
    public float LodScale;
    public MicroCulture Culture;
    public TradeEdge Edge;
    public EconomicOverlay Overlay;
}

public struct StatCurveParams
{
    public float softMin;
    public float softMax;
    public float setpoint;
    public float delta;
    public float fudge01;
    public float needMean;
    public float healthcare;
    public float vmax;
    public float tempK;
    public float ks;
    public float mumax;
    public float carryingK;
    public float empowerMult;
    public float dtHours;
    public float lodScale;
}

public delegate bool StatPredicate(in StatContext ctx);
public delegate float StatCurve(float t, in StatCurveParams p);

public interface IStatisticsModel
{
    string ModelId { get; }
    bool TrySample(in StatQuery q, out float value);
    IEnumerable<StatSample> SampleBatch(in StatQuery q, int count, int seed);
    bool TryGetSpecific(in SpecificityKey key, out StatOverride ov);
    void PutSpecific(in SpecificityKey key, in StatOverride ov);
    void Reconcile(StatReconcileMode mode);
    void PutFeature(string key, float value);
}

public interface IStatisticalRetinueDao
{
    string ScopeId { get; }
    IStatisticsModel GetModel(string modelId);
    IStatisticsModel EnsureModel(string modelId, in StatSeed seed);
    bool TryGet(string modelId, in StatQuery q, out float value);
    float Sample(string modelId, in StatQuery q, int seed);
    IReadOnlyList<StatSample> SampleMap(string modelId, in StatQuery q, int count, int seed);
    void PutIndividual(string modelId, in SpecificityKey key, in StatOverride value);
    bool TryGetIndividual(string modelId, in SpecificityKey key, out StatOverride value);
    void ClearIndividual(string modelId, in SpecificityKey key);
    PersonaRequestBundle GetOrRequestBundle(string personaKey, CivilSystemKind kind);
    IReadOnlyList<RetinuePeckingEntry> GetRetinue(CivilVenueNode venue);
    RetinuePeckingEntry UpsertPecking(CivilVenueNode venue, RetinuePeckingEntry entry);
    void ApplyGovGloveBias(LifeSystemsSheet sheet, PersonaRequestBundle bundle);
    TradeDemographics GetTradeDemographics();
    TradeEdge UpsertTradeEdge(TradeEdge edge);
    bool TryResolveTrade(in TradeDealRequest req, out TradeDealResult result);
    EconomicOverlay EnsureEconomicOverlay(string retinueId, in StatSeed seed);
    void TickEconomy(string retinueId, float dtHours);
    IRulesEngine EnsureRulesEngine(string engineId, in StatSeed seed);
    bool TryFireRule(string engineId, string ruleId, in RuleFireContext ctx, out RuleFireResult result);
    AuditMode GetAuditMode(string engineId);
    void SetAuditMode(string engineId, AuditMode mode);
    float ApplyAuditCorrection(string engineId, string correctionId, float measured, float expected, in AuditCorrectionParams p);
    IReadOnlyList<AuditEntry> GetAuditTrail(string engineId, int maxEntries = 64);
    MicroCulture EnsureCulture(string cultureId, CultureKind kind, in StatSeed seed);
    void TickCulture(string cultureId, float dtHours);
    GrowthEvent ArmGrowthEvent(GrowthEvent ev);
    bool TryFireGrowthEvent(string eventId, out GrowthEventResult result);
    CellularRetinue EnsureCellularRetinue(string hostRetinueId, string cultureId, in StatSeed seed);
    CellularPeckingEntry SampleCellularMember(string hostRetinueId, string cellKey, int seed, Vector3? spawnPos = null);
    StatPredicate Predicate(string id);
    StatCurve Curve(string id);
}


// ---- AssetDB compile hosts (civil/persona orphans pending under _PendingAssetDbImport) ----

/// <summary>Marks a string field as a 5-field cron expression for Inspector humanization.</summary>
public sealed class CronExprAttribute : PropertyAttribute
{
}

/// <summary>Minimal cron due check (5-field: min hour dom month dow).</summary>
public static class CronDue
{
    public static bool IsDue(string cronExpr, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cronExpr)) return true;
        var truncated = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, 0, DateTimeKind.Utc);
        return Matches(cronExpr.Trim(), truncated);
    }

    public static bool IsActiveSchedule(string cronExpr, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cronExpr)) return true;
        return Matches(cronExpr.Trim(), utcNow.ToUniversalTime());
    }

    public static bool Matches(string cronExpr, DateTime t)
    {
        var parts = cronExpr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return false;
        return FieldMatches(parts[0], t.Minute, 0, 59)
               && FieldMatches(parts[1], t.Hour, 0, 23)
               && FieldMatches(parts[2], t.Day, 1, 31)
               && FieldMatches(parts[3], t.Month, 1, 12)
               && FieldMatches(parts[4], (int)t.DayOfWeek, 0, 6);
    }

    static bool FieldMatches(string field, int value, int min, int max)
    {
        if (field == "*") return true;
        if (field.Contains(","))
        {
            var bits = field.Split(',');
            for (int i = 0; i < bits.Length; i++)
                if (FieldMatches(bits[i].Trim(), value, min, max)) return true;
            return false;
        }
        if (field.Contains("/"))
        {
            var slash = field.Split('/');
            if (slash.Length != 2 || !int.TryParse(slash[1], out int step) || step <= 0) return false;
            int start = min, end = max;
            if (slash[0] != "*")
            {
                if (slash[0].Contains("-"))
                {
                    var r = slash[0].Split('-');
                    if (r.Length != 2 || !int.TryParse(r[0], out start) || !int.TryParse(r[1], out end)) return false;
                }
                else if (!int.TryParse(slash[0], out start)) return false;
            }
            if (value < start || value > end) return false;
            return (value - start) % step == 0;
        }
        if (field.Contains("-"))
        {
            var r = field.Split('-');
            if (r.Length != 2 || !int.TryParse(r[0], out int a) || !int.TryParse(r[1], out int b)) return false;
            return value >= a && value <= b;
        }
        return int.TryParse(field, out int exact) && exact == value;
    }
}

public enum CivilSystemKind
{
    Generic = 0, Kitchen = 1, School = 2, Mall = 3, Library = 4, Church = 5, SoupKitchen = 6,
    Factory = 7, LiquorStore = 8, PoliceStation = 9, Bathroom = 10, CarRepair = 11, Gym = 12,
    House = 13, GasStation = 14, TownHall = 15, NightClub = 16, Bar = 17, Inn = 18, Hotel = 19,
    MilitaryCheckpoint = 20, SpyAgency = 21, Embassy = 22, GovLegislative = 23, Monarchic = 24,
    Spa = 25, PrivateIndustry = 26, BarberShop = 27, FireStation = 28, BusDepot = 29,
    TransitHub = 30, Airport = 31, TrainStation = 32, GrainSilo = 33, RailMaintenanceDepot = 34,
    Park = 35, SanitationFacility = 36, Prison = 37, UnemploymentOffice = 38, CourtHouse = 39,
    VotingPlace = 40, ClothingStore = 41
}

public enum CivilLodTier { FullSim = 0, Proxy = 1, Ghost = 2, Culled = 3 }

[Serializable]
public sealed class RetinuePeckingEntry
{
    public string personaKey;
    public string role;
    public int peckingOrder = 100;
    public GameObject actor;
    public string agencyAffinity;
}

[Serializable]
public sealed class PersonaRequestBundle
{
    public string personaKey;
    public string actorType;
    public string cityId;
    public string venueStableId;
    public CivilSystemKind civilKind = CivilSystemKind.Generic;
    [CronExpr] public string dutyCron;
    public int peckingOrder = 100;
    public float biorhythmAmplitudeSeed = 0.5f;
    public float biorhythmPhase01;
    public Dictionary<string, float> societyFeatures = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, float> needSatisfied01 = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    public string govAgencyId;
    public string contractorId;

    public static PersonaRequestBundle CreateDefault(string personaKey, CivilSystemKind kind)
    {
        return new PersonaRequestBundle
        {
            personaKey = personaKey ?? "persona",
            actorType = kind.ToString().ToLowerInvariant(),
            civilKind = kind,
            biorhythmAmplitudeSeed = 0.5f
        };
    }
}

[Serializable]
public sealed class CivilVenueNode
{
    public string stableId;
    public CivilSystemKind kind = CivilSystemKind.Generic;
    public string buildingTypeId;
    public GameObject contextOwner;
    [CronExpr] public string hoursCron = "* 8-20 * * *";
    public List<RetinuePeckingEntry> retinue = new List<RetinuePeckingEntry>();
    public CivilLodTier currentTier = CivilLodTier.Culled;
    public bool isOpen;
    public PersonaRequestBundle lastBundle;

    public Vector3 WorldPosition =>
        contextOwner != null ? contextOwner.transform.position : Vector3.zero;
}

public interface ICompanyHost
{
    CompanyRegistration Company { get; }
}

[Serializable]
public sealed class CompanyFundingSource
{
    public string sourceId;
    public string label;
    [Range(0f, 1f)] public float share01 = 1f;
}

[DisallowMultipleComponent]
public sealed class CompanyRegistration : MonoBehaviour, ICompanyHost
{
    public string companyId;
    public string displayName;
    public string parentCompanyId;
    public List<CompanyFundingSource> fundingSources = new List<CompanyFundingSource>();
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();
    public CompanyRegistration Company => this;

    void Awake()
    {
        if (string.IsNullOrEmpty(companyId)) companyId = gameObject.name;
        if (string.IsNullOrEmpty(displayName)) displayName = companyId;
    }
}

[Serializable]
public sealed class StoreShelfSlot
{
    public string shelfId;
    public string commodityKey;
    public string displayName;
    public float quantity = 1f;
    public float price;
    public Vector3 localPosition;
}

[DisallowMultipleComponent]
public class StoreBase : MonoBehaviour
{
    public string storeStableId;
    public string storeType = "generic";
    [CronExpr] public string hoursCron = "* 10-21 * * *";
    public bool isOpen;
    public List<StoreShelfSlot> shelves = new List<StoreShelfSlot>();
    public List<RetinuePeckingEntry> staff = new List<RetinuePeckingEntry>();

    void Awake()
    {
        if (string.IsNullOrEmpty(storeStableId)) storeStableId = gameObject.name;
    }
}

[Serializable]
public sealed class ElectorateSlice
{
    public string sliceId = "slice";
    public string groupProperty = "party";
    public string groupValue = "democrat";
    [Range(0f, 1f)] public float share01 = 0.5f;
    [Range(0f, 1f)] public float yesTilt01 = 0.5f;
}

[Serializable]
public sealed class ElectorateDemographics
{
    public List<ElectorateSlice> slices = new List<ElectorateSlice>();

    public static ElectorateDemographics DefaultTwoParty()
    {
        var d = new ElectorateDemographics();
        d.slices = new List<ElectorateSlice>
        {
            new ElectorateSlice { sliceId = "dem", groupProperty = "party", groupValue = "democrat", share01 = 0.5f, yesTilt01 = 0.62f },
            new ElectorateSlice { sliceId = "rep", groupProperty = "party", groupValue = "republican", share01 = 0.5f, yesTilt01 = 0.38f }
        };
        d.Renormalize();
        return d;
    }

    public static ElectorateDemographics FromSocietyFeatures(IReadOnlyDictionary<string, float> societyFeatures)
    {
        var d = DefaultTwoParty();
        if (societyFeatures == null) return d;
        if (societyFeatures.TryGetValue("congressStability", out float stab))
        {
            d.slices[0].yesTilt01 = Mathf.Clamp01(0.5f + stab * 0.2f);
            d.slices[1].yesTilt01 = Mathf.Clamp01(0.5f - stab * 0.2f);
        }
        if (societyFeatures.TryGetValue("lobbyistActivity", out float lobby))
        {
            d.slices[0].share01 = Mathf.Clamp01(0.5f - lobby * 0.1f);
            d.slices[1].share01 = Mathf.Clamp01(0.5f + lobby * 0.1f);
        }
        d.Renormalize();
        return d;
    }

    public void Renormalize()
    {
        if (slices == null || slices.Count == 0) return;
        float sum = 0f;
        for (int i = 0; i < slices.Count; i++)
            if (slices[i] != null) sum += Mathf.Max(0f, slices[i].share01);
        if (sum <= 1e-5f)
        {
            float even = 1f / slices.Count;
            for (int i = 0; i < slices.Count; i++)
                if (slices[i] != null) slices[i].share01 = even;
            return;
        }
        for (int i = 0; i < slices.Count; i++)
            if (slices[i] != null) slices[i].share01 = Mathf.Max(0f, slices[i].share01) / sum;
    }

    public ElectorateSlice Sample(int seed)
    {
        Renormalize();
        if (slices == null || slices.Count == 0) return null;
        var rng = new System.Random(seed);
        float pick = (float)rng.NextDouble();
        float acc = 0f;
        for (int i = 0; i < slices.Count; i++)
        {
            var sl = slices[i];
            if (sl == null) continue;
            acc += sl.share01;
            if (pick <= acc) return sl;
        }
        return slices[slices.Count - 1];
    }

    public string TiltYesNo(ElectorateSlice slice, bool actorAlreadyPicked, string actorChoice, int seed)
    {
        if (actorAlreadyPicked && !string.IsNullOrEmpty(actorChoice)) return actorChoice;
        float tilt = slice != null ? slice.yesTilt01 : 0.5f;
        var rng = new System.Random(seed);
        return rng.NextDouble() < tilt ? "yes" : "no";
    }
}

public enum CivilianAgeBand { Child0To17 = 0, Adult18To64 = 1, Senior65Plus = 2 }
public enum CivilianEducationAttainment { None = 0, Certification = 1, Degree = 2 }
public enum CivilianEmploymentStatus { Unemployed = 0, Employed = 1, Student = 2, Training = 3 }

public sealed class CivilianPaperDoll : ScriptableObject
{
    public string personaKey = "civilian";
    public CivilianAgeBand ageBand = CivilianAgeBand.Adult18To64;
    public CivilianEducationAttainment education = CivilianEducationAttainment.None;
    public CivilianEmploymentStatus employment = CivilianEmploymentStatus.Unemployed;
    public string currentRoleId;
    public string employerCompanyId;
    public bool isGovernmentJob;
}


/// <summary>City-scoped quotas so unemployed paper dolls stay inside statistical limits.</summary>
[Serializable]
public sealed class CivilianDemographics
{
    public int cityPopulation = 100;
    [Range(0f, 1f)] public float unemploymentRate01 = 0.08f;
    [Range(0f, 1f)] public float ageChild01 = 0.22f;
    [Range(0f, 1f)] public float ageAdult01 = 0.62f;
    [Range(0f, 1f)] public float ageSenior01 = 0.16f;
    [Range(0f, 1f)] public float eduNone01 = 0.45f;
    [Range(0f, 1f)] public float eduCert01 = 0.35f;
    [Range(0f, 1f)] public float eduDegree01 = 0.2f;
    [Range(0f, 1f)] public float slack01 = 0.08f;
    public DemographicSpatialGradient spatialGradient;

    public int UnemployedQuota =>
        Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, cityPopulation) * Mathf.Clamp01(unemploymentRate01)));

    public static CivilianDemographics FromSocietyFeatures(
        IReadOnlyDictionary<string, float> societyFeatures,
        int population = 100)
    {
        var d = new CivilianDemographics { cityPopulation = Mathf.Max(1, population) };
        if (TryFeature(societyFeatures, "unemploymentRate", out float u) ||
            TryFeature(societyFeatures, "unemployment_rate", out u))
            d.unemploymentRate01 = Mathf.Clamp01(u);
        else if (TryFeature(societyFeatures, "welfareBenefits", out float w) ||
                 TryFeature(societyFeatures, "welfare_benefits", out w))
            d.unemploymentRate01 = Mathf.Clamp01(1f - w);
        return d;
    }

    /// <summary>Additive spatial modulation then renormalize age/edu. Quotas use city unemploymentRate01 (unchanged).</summary>
    public CivilianDemographics ResolveAt(Vector3 lookupPos)
    {
        var copy = CloneShares();
        if (spatialGradient == null || !spatialGradient.IsActive)
            return copy;
        spatialGradient.AccumulateCivDeltas(lookupPos,
            out float unemp, out float child, out float adult, out float senior,
            out float eduNone, out float eduCert, out float eduDeg);
        copy.unemploymentRate01 = Mathf.Clamp01(unemploymentRate01 + unemp);
        copy.ageChild01 = Mathf.Max(0f, ageChild01 + child);
        copy.ageAdult01 = Mathf.Max(0f, ageAdult01 + adult);
        copy.ageSenior01 = Mathf.Max(0f, ageSenior01 + senior);
        Renorm3(ref copy.ageChild01, ref copy.ageAdult01, ref copy.ageSenior01);
        copy.eduNone01 = Mathf.Max(0f, eduNone01 + eduNone);
        copy.eduCert01 = Mathf.Max(0f, eduCert01 + eduCert);
        copy.eduDegree01 = Mathf.Max(0f, eduDegree01 + eduDeg);
        Renorm3(ref copy.eduNone01, ref copy.eduCert01, ref copy.eduDegree01);
        return copy;
    }

    public bool TryAcceptUnemployed(CivilianPaperDoll candidate, IList<CivilianPaperDoll> existing)
    {
        if (candidate == null || candidate.employment != CivilianEmploymentStatus.Unemployed)
            return false;
        int unemployed = CountUnemployed(existing);
        if (unemployed + 1 > UnemployedQuota)
            return false;
        if (!BandOk(candidate.ageBand, existing, AgeShare))
            return false;
        if (!BandOk(candidate.education, existing, EduShare))
            return false;
        return true;
    }

    public CivilianPaperDoll SampleUnemployed(string personaKey, IList<CivilianPaperDoll> existing, int seed = 0)
        => SampleUnemployed(personaKey, existing, seed, null);

    /// <param name="spawnPos">Placement position. When set with spatialGradient, demographic mix uses samplePos from bounds.</param>
    public CivilianPaperDoll SampleUnemployed(
        string personaKey, IList<CivilianPaperDoll> existing, int seed, Vector3? spawnPos)
    {
        CivilianDemographics local = this;
        if (spawnPos.HasValue && spatialGradient != null && spatialGradient.IsActive)
        {
            Vector3 samplePos = spatialGradient.SamplePosForDemographics(spawnPos.Value, seed);
            local = ResolveAt(samplePos);
        }
        var rng = new System.Random(seed);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
            doll.personaKey = string.IsNullOrEmpty(personaKey) ? "civilian" : personaKey;
            doll.employment = CivilianEmploymentStatus.Unemployed;
            doll.ageBand = local.PickAge(rng);
            doll.education = local.PickEdu(rng);
            // Quotas always against city baseline (this), not spatially modulated prior
            if (TryAcceptUnemployed(doll, existing))
                return doll;
            UnityEngine.Object.DestroyImmediate(doll);
        }
        return null;
    }

    CivilianDemographics CloneShares()
    {
        return new CivilianDemographics
        {
            cityPopulation = cityPopulation,
            unemploymentRate01 = unemploymentRate01,
            ageChild01 = ageChild01,
            ageAdult01 = ageAdult01,
            ageSenior01 = ageSenior01,
            eduNone01 = eduNone01,
            eduCert01 = eduCert01,
            eduDegree01 = eduDegree01,
            slack01 = slack01,
            spatialGradient = spatialGradient
        };
    }

    static void Renorm3(ref float a, ref float b, ref float c)
    {
        float s = a + b + c;
        if (s <= 1e-6f) { a = b = c = 1f / 3f; return; }
        a /= s; b /= s; c /= s;
    }

    float AgeShare(CivilianAgeBand b)
    {
        switch (b)
        {
            case CivilianAgeBand.Child0To17: return ageChild01;
            case CivilianAgeBand.Senior65Plus: return ageSenior01;
            default: return ageAdult01;
        }
    }

    float EduShare(CivilianEducationAttainment e)
    {
        switch (e)
        {
            case CivilianEducationAttainment.Certification: return eduCert01;
            case CivilianEducationAttainment.Degree: return eduDegree01;
            default: return eduNone01;
        }
    }

    bool BandOk<T>(T band, IList<CivilianPaperDoll> existing, Func<T, float> share)
    {
        int unemployed = CountUnemployed(existing);
        int next = unemployed + 1;
        int match = 0;
        if (existing != null)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var d = existing[i];
                if (d == null || d.employment != CivilianEmploymentStatus.Unemployed) continue;
                if (EqualityComparer<T>.Default.Equals(GetBand(d, band), band))
                    match++;
            }
        }
        match++;
        float target = Mathf.Clamp01(share(band));
        float allowed = target + slack01;
        return match <= Mathf.Max(1, Mathf.CeilToInt(next * allowed));
    }

    static T GetBand<T>(CivilianPaperDoll d, T probe)
    {
        if (probe is CivilianAgeBand)
            return (T)(object)d.ageBand;
        return (T)(object)d.education;
    }

    static int CountUnemployed(IList<CivilianPaperDoll> existing)
    {
        if (existing == null) return 0;
        int n = 0;
        for (int i = 0; i < existing.Count; i++)
            if (existing[i] != null && existing[i].employment == CivilianEmploymentStatus.Unemployed)
                n++;
        return n;
    }

    CivilianAgeBand PickAge(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        if (r < ageChild01) return CivilianAgeBand.Child0To17;
        if (r < ageChild01 + ageAdult01) return CivilianAgeBand.Adult18To64;
        return CivilianAgeBand.Senior65Plus;
    }

    CivilianEducationAttainment PickEdu(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        if (r < eduNone01) return CivilianEducationAttainment.None;
        if (r < eduNone01 + eduCert01) return CivilianEducationAttainment.Certification;
        return CivilianEducationAttainment.Degree;
    }

    static bool TryFeature(IReadOnlyDictionary<string, float> map, string key, out float value)
    {
        value = 0f;
        if (map == null || string.IsNullOrEmpty(key)) return false;
        return map.TryGetValue(key, out value);
    }
}
