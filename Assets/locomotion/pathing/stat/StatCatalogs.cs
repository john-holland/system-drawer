using System;
using System.Collections.Generic;
using UnityEngine;

public static class StatPredicateCatalog
{
    static readonly Dictionary<string, StatPredicate> Map =
        new Dictionary<string, StatPredicate>(StringComparer.OrdinalIgnoreCase)
        {
            ["within_quota"] = (in StatContext ctx) => true,
            ["causal_depth"] = (in StatContext ctx) => ctx.CausalDepth >= 0f,
            ["cron_active"] = (in StatContext ctx) => true,
            ["lod_allows"] = (in StatContext ctx) => ctx.LodScale >= 0.25f || ctx.LodScale <= 0f,
            ["actor_not_pinned"] = (in StatContext ctx) => !ctx.ActorPinned,
            ["trade_edge_allows"] = TradeEdgeAllows,
            ["within_trade_quota"] = (in StatContext ctx) => ctx.Edge == null || ctx.Edge.volumeShare01 <= 1f,
            ["ledger_solvent"] = LedgerSolvent,
            ["economy_active"] = EconomyActive,
            ["rule_preconditions"] = (in StatContext ctx) => true,
            ["audit_accuracy_ok"] = (in StatContext ctx) => true,
            ["culture_viable"] = CultureViable,
            ["growth_event_armed"] = (in StatContext ctx) => true,
            ["allergy_blocks"] = AllergyBlocks,
            ["bond_ready"] = BondReady,
            ["manifold_cold_stick"] = (in StatContext ctx) => true,
            ["manifold_bond_ready"] = BondReady
        };

    public static StatPredicate Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return (in StatContext _) => true;
        return Map.TryGetValue(id, out var p) ? p : ((in StatContext _) => false);
    }

    public static void Register(string id, StatPredicate pred)
    {
        if (!string.IsNullOrEmpty(id) && pred != null)
            Map[id] = pred;
    }

    static bool TradeEdgeAllows(in StatContext ctx)
    {
        if (ctx.Dao == null) return true;
        var trade = ctx.Dao.GetTradeDemographics();
        if (trade?.edges == null || trade.edges.Count == 0) return true; // no edges configured → allow
        if (ctx.Edge != null)
            return CronDue.IsActiveSchedule(ctx.Edge.openCron, ctx.UtcNow == default ? DateTime.UtcNow : ctx.UtcNow);
        for (int i = 0; i < trade.edges.Count; i++)
        {
            var e = trade.edges[i];
            if (e == null) continue;
            bool from = string.IsNullOrEmpty(ctx.RetinueId) ||
                        string.Equals(e.fromRetinueId, ctx.RetinueId, StringComparison.OrdinalIgnoreCase) ||
                        (e.bidirectional && string.Equals(e.toRetinueId, ctx.RetinueId, StringComparison.OrdinalIgnoreCase));
            if (!from) continue;
            if (!string.IsNullOrEmpty(ctx.CommodityKey) &&
                !string.Equals(e.commodityKey, ctx.CommodityKey, StringComparison.OrdinalIgnoreCase))
                continue;
            if (CronDue.IsActiveSchedule(e.openCron, ctx.UtcNow == default ? DateTime.UtcNow : ctx.UtcNow))
                return true;
        }
        return false;
    }

    static bool LedgerSolvent(in StatContext ctx)
    {
        if (ctx.Overlay == null) return true;
        if (string.IsNullOrEmpty(ctx.CommodityKey)) return ctx.Overlay.liquidity01 > 0f;
        return ctx.Overlay.TryGetBalance(ctx.CommodityKey, out float q) && q > 0f;
    }

    static bool EconomyActive(in StatContext ctx)
    {
        if (ctx.Overlay == null || !ctx.Overlay.enabled) return false;
        return CronDue.IsActiveSchedule(ctx.Overlay.marketHoursCron,
            ctx.UtcNow == default ? DateTime.UtcNow : ctx.UtcNow);
    }

    static bool CultureViable(in StatContext ctx)
    {
        var c = ctx.Culture;
        if (c == null) return false;
        return c.viability01 > 0.05f && c.ph01 > 0.15f && c.ph01 < 0.9f && c.tempK > 250f && c.tempK < 350f;
    }

    static bool AllergyBlocks(in StatContext ctx)
    {
        if (ctx.Edge != null && ctx.Edge.relationKind == TradeRelationKind.AllergyAvoid)
            return false; // blocked
        var trade = ctx.Dao?.GetTradeDemographics();
        if (trade?.edges == null) return true;
        for (int i = 0; i < trade.edges.Count; i++)
        {
            var e = trade.edges[i];
            if (e == null || e.relationKind != TradeRelationKind.AllergyAvoid) continue;
            if (!string.IsNullOrEmpty(ctx.CommodityKey) &&
                (string.Equals(e.commodityKey, ctx.CommodityKey, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(e.allergenOrBondKey, ctx.CommodityKey, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }

    static bool BondReady(in StatContext ctx)
    {
        if (ctx.Edge != null && ctx.Edge.relationKind == TradeRelationKind.ChemicalBond)
            return ctx.Edge.bondStrength01 > 0.05f;
        if (ctx.Overlay != null && ctx.Overlay.TryGetBalance("adhesive", out float q))
            return q > 0.01f;
        return true;
    }
}

public static class StatCurveCatalog
{
    static readonly Dictionary<string, StatCurve> Map =
        new Dictionary<string, StatCurve>(StringComparer.OrdinalIgnoreCase)
        {
            ["soft_band_lerp"] = SoftBandLerp,
            ["nudge_clamped"] = NudgeClamped,
            ["speed_log_lod"] = SpeedLogLod,
            ["biorhythm_amp"] = BiorhythmAmp,
            ["renormalize_slices"] = (float t, in StatCurveParams p) => Mathf.Clamp01(t),
            ["hash_density"] = (float t, in StatCurveParams p) => Mathf.Clamp01(t),
            ["price_soft_band"] = PriceSoftBand,
            ["trade_affinity_lerp"] = (float t, in StatCurveParams p) => Mathf.Lerp(0.2f, 0.95f, Mathf.Clamp01(t)),
            ["velocity_decay"] = VelocityDecay,
            ["funding_share_renorm"] = (float t, in StatCurveParams p) => Mathf.Clamp01(t),
            ["stoich_yield"] = (float t, in StatCurveParams p) => Mathf.Max(0f, t),
            ["audit_blend"] = (float t, in StatCurveParams p) => Mathf.Lerp(t, p.setpoint, Mathf.Clamp01(p.fudge01)),
            ["monod_growth"] = MonodGrowth,
            ["logistic_biomass"] = LogisticBiomass,
            ["fungus_empower"] = (float t, in StatCurveParams p) => t * Mathf.Max(0.01f, p.empowerMult > 0 ? p.empowerMult : 1f),
            ["arrhenius_rate"] = ArrheniusRate,
            ["spatial_falloff"] = SpatialFalloff,
            ["manifold_temp_lerp"] = ManifoldTempLerp
        };

    public static StatCurve Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return (float t, in StatCurveParams _) => t;
        return Map.TryGetValue(id, out var c) ? c : ((float t, in StatCurveParams _) => t);
    }

    public static void Register(string id, StatCurve curve)
    {
        if (!string.IsNullOrEmpty(id) && curve != null)
            Map[id] = curve;
    }

    static float SoftBandLerp(float t, in StatCurveParams p)
    {
        float lo = p.softMin;
        float hi = p.softMax > lo ? p.softMax : lo + 1f;
        return Mathf.Lerp(lo, hi, Mathf.Clamp01(t));
    }

    static float NudgeClamped(float t, in StatCurveParams p)
    {
        float v = t + p.delta;
        if (p.softMax > p.softMin)
            return Mathf.Clamp(v, p.softMin, p.softMax);
        return Mathf.Clamp01(v);
    }

    static float SpeedLogLod(float v, in StatCurveParams p)
    {
        float vmax = p.vmax > 1e-3f ? p.vmax : 1f;
        return Mathf.Clamp(1f / (1f + Mathf.Log(1f + Mathf.Max(0f, v) / vmax)), 0.05f, 1f);
    }

    static float BiorhythmAmp(float t, in StatCurveParams p)
    {
        float need = p.needMean > 0 ? p.needMean : t;
        float hc = p.healthcare > 0 ? p.healthcare : 0.7f;
        return Mathf.Clamp01(0.35f * need + 0.65f * hc);
    }

    static float PriceSoftBand(float basePrice, in StatCurveParams p)
    {
        float idx = p.setpoint > 0 ? p.setpoint : 1f;
        return Mathf.Max(0.01f, basePrice * idx);
    }

    static float VelocityDecay(float v, in StatCurveParams p)
    {
        float dt = p.dtHours > 0 ? p.dtHours : 0.1f;
        return Mathf.Clamp01(v * Mathf.Exp(-dt * 0.35f));
    }

    static float MonodGrowth(float s, in StatCurveParams p)
    {
        float mumax = p.mumax > 0 ? p.mumax : 0.35f;
        float ks = p.ks > 0 ? p.ks : 0.2f;
        return mumax * Mathf.Clamp01(s) / (ks + Mathf.Clamp01(s));
    }

    static float LogisticBiomass(float n, in StatCurveParams p)
    {
        float k = p.carryingK > 0 ? p.carryingK : 1f;
        float r = p.mumax > 0 ? p.mumax : 0.2f;
        float dt = p.dtHours > 0 ? p.dtHours : 0.1f;
        float nn = Mathf.Clamp01(n);
        return Mathf.Clamp01(nn + r * nn * (1f - nn / k) * dt);
    }

    static float ArrheniusRate(float tempK, in StatCurveParams p)
    {
        float t = tempK > 1f ? tempK : (p.tempK > 1f ? p.tempK : 298.15f);
        // Relative to 298K; mild curve for gameplay
        return Mathf.Clamp(Mathf.Exp(8f * (1f / 298.15f - 1f / t)), 0.05f, 3f);
    }

    /// <summary>t = distance/radius; returns 1/(1+t^2).</summary>
    static float SpatialFalloff(float t, in StatCurveParams p) => 1f / (1f + t * t);

    /// <summary>Maps °C (t) toward stick weight; softMin/softMax as warm/cold ends.</summary>
    static float ManifoldTempLerp(float t, in StatCurveParams p)
    {
        float warm = p.softMax != 0 || p.softMin != 0 ? p.softMax : 10f;
        float cold = p.softMin != 0 || p.softMax != 0 ? p.softMin : -10f;
        float u = InverseLerpSafe(warm, cold, t);
        return Mathf.Clamp01(u);
    }

    static float InverseLerpSafe(float a, float b, float v)
    {
        if (Mathf.Abs(b - a) < 1e-5f) return 0f;
        return Mathf.Clamp01((v - a) / (b - a));
    }
}

public static class StatisticsModelFactory
{
    static readonly Dictionary<string, Func<StatSeed, IStatisticsModel>> Ctors =
        new Dictionary<string, Func<StatSeed, IStatisticsModel>>(StringComparer.OrdinalIgnoreCase);

    static StatisticsModelFactory()
    {
        Register("society.features", s => FeatureMapModel.FromFeatures("society.features", s.societyFeatures));
        Register("needs.satisfied", s => FeatureMapModel.FromFeatures("needs.satisfied", s.needSatisfied01));
        Register("life.baseline", _ => new LifeBaselineModel());
        Register("civ.demographics", s => new CivDemographicsModel(CivilianDemographics.FromSocietyFeatures(s.societyFeatures, s.population > 0 ? s.population : 100)));
        Register("vote.electorate", s => new ElectorateStatModel(ElectorateDemographics.FromSocietyFeatures(s.societyFeatures)));
        Register("persona.bundle", s => new PersonaBundleModel(PersonaRequestBundle.CreateDefault(s.personaKey, s.civilKind)));
        Register("retinue.pecking", _ => new PeckingStatModel());
        Register("trade.demographics", s => new TradeDemographicsModel(TradeDemographics.FromSocietyFeatures(s.societyFeatures, s.cityId)));
        Register("econ.overlay", s => new EconomicOverlayModel(EconomicOverlay.FromRetinue(s.retinueId, s)));
        Register("econ.ledger", s => new EconomicOverlayModel(EconomicOverlay.FromRetinue(s.retinueId, s)));
        Register("space.manifold", _ => new SpaceManifoldStatModel());
        Register("culture.micro", s => new CultureStatModel(MicroCulture.CreateBacterial("default", s.cultureSpeciesId, s)));
        Register("culture.fungus", s => new CultureStatModel(MicroCulture.CreateFungal("default", s.cultureSpeciesId, s)));
        Register("culture.cellular", s => new CellularDemographicsModel(CellularDemographics.DefaultTissue()));
    }

    public static void Register(string modelId, Func<StatSeed, IStatisticsModel> ctor)
    {
        if (!string.IsNullOrEmpty(modelId) && ctor != null)
            Ctors[modelId] = ctor;
    }

    public static IStatisticsModel Create(string modelId, in StatSeed seed)
    {
        if (string.IsNullOrEmpty(modelId)) return null;
        if (Ctors.TryGetValue(modelId, out var ctor))
            return ctor(seed);
        return FeatureMapModel.FromFeatures(modelId, null);
    }
}
