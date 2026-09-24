using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Scoped statistical retinue DAO — map access, individuality, trade, economy, rules, cultures.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Stat/Statistical Retinue Dao")]
public sealed class StatisticalRetinueDao : MonoBehaviour, IStatisticalRetinueDao
{
    public string scopeId = "city";
    public string cityId = "demo-city";

    readonly Dictionary<string, IStatisticsModel> _models =
        new Dictionary<string, IStatisticsModel>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, PersonaRequestBundle> _bundleCache =
        new Dictionary<string, PersonaRequestBundle>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, EconomicOverlay> _overlays =
        new Dictionary<string, EconomicOverlay>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, IRulesEngine> _engines =
        new Dictionary<string, IRulesEngine>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, MicroCulture> _cultures =
        new Dictionary<string, MicroCulture>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, GrowthEvent> _growthEvents =
        new Dictionary<string, GrowthEvent>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, CellularRetinue> _cellular =
        new Dictionary<string, CellularRetinue>(StringComparer.OrdinalIgnoreCase);

    TradeDemographics _trade;

    static StatisticalRetinueDao _instance;
    public static StatisticalRetinueDao Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<StatisticalRetinueDao>();
            return _instance;
        }
    }

    public string ScopeId => string.IsNullOrEmpty(scopeId) ? cityId : scopeId;

    void Awake()
    {
        _instance = this;
        if (string.IsNullOrEmpty(scopeId))
            scopeId = cityId;
        if (_trade == null)
            _trade = TradeDemographics.FromSocietyFeatures(null, ScopeId);
    }

    public static StatisticalRetinueDao Resolve(MonoBehaviour host)
    {
        if (host != null)
        {
            var local = host.GetComponent<StatisticalRetinueDao>() ??
                        host.GetComponentInParent<StatisticalRetinueDao>();
            if (local != null) return local;
        }
        return Instance ?? (host != null
            ? host.gameObject.AddComponent<StatisticalRetinueDao>()
            : null);
    }

    public IStatisticsModel GetModel(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return null;
        return _models.TryGetValue(modelId, out var m) ? m : null;
    }

    public IStatisticsModel EnsureModel(string modelId, in StatSeed seed)
    {
        if (string.IsNullOrEmpty(modelId)) return null;
        if (_models.TryGetValue(modelId, out var existing) && existing != null)
            return existing;
        var seeded = seed;
        if (string.IsNullOrEmpty(seeded.cityId))
            seeded.cityId = cityId;
        var created = StatisticsModelFactory.Create(modelId, seeded);
        if (created != null)
            _models[modelId] = created;
        if (modelId == "trade.demographics" && created is TradeDemographicsModel tm)
            _trade = tm.Trade;
        if ((modelId == "econ.overlay" || modelId == "econ.ledger") &&
            created is EconomicOverlayModel em && em.Overlay != null &&
            !string.IsNullOrEmpty(em.Overlay.retinueId))
            _overlays[em.Overlay.retinueId] = em.Overlay;
        return created;
    }

    public bool TryGet(string modelId, in StatQuery q, out float value)
    {
        value = 0f;
        var m = GetModel(modelId) ?? EnsureModel(modelId, StatSeed.ForCity(cityId));
        return m != null && m.TrySample(q, out value);
    }

    public float Sample(string modelId, in StatQuery q, int seed)
    {
        var qq = q;
        qq.seed = seed;
        return TryGet(modelId, qq, out float v) ? v : 0f;
    }

    public IReadOnlyList<StatSample> SampleMap(string modelId, in StatQuery q, int count, int seed)
    {
        var m = GetModel(modelId) ?? EnsureModel(modelId, StatSeed.ForCity(cityId));
        if (m == null) return Array.Empty<StatSample>();
        var list = new List<StatSample>();
        foreach (var s in m.SampleBatch(q, count, seed))
            list.Add(s);
        return list;
    }

    public void PutIndividual(string modelId, in SpecificityKey key, in StatOverride value)
    {
        var m = GetModel(modelId) ?? EnsureModel(modelId, StatSeed.ForCity(cityId));
        m?.PutSpecific(key, value);
    }

    public bool TryGetIndividual(string modelId, in SpecificityKey key, out StatOverride value)
    {
        value = default;
        var m = GetModel(modelId);
        return m != null && m.TryGetSpecific(key, out value);
    }

    public void ClearIndividual(string modelId, in SpecificityKey key)
    {
        var m = GetModel(modelId);
        if (m == null) return;
        m.PutSpecific(key, default);
    }

    public void CacheBundle(PersonaRequestBundle bundle)
    {
        if (bundle == null || string.IsNullOrEmpty(bundle.personaKey)) return;
        _bundleCache[bundle.personaKey] = bundle;
        if (EnsureModel("persona.bundle",
                new StatSeed { personaKey = bundle.personaKey, civilKind = bundle.civilKind }) is PersonaBundleModel pm)
            pm.Set(bundle);
        SyncBundleMaps(bundle);
    }

    void SyncBundleMaps(PersonaRequestBundle bundle)
    {
        if (bundle == null) return;
        if (EnsureModel("society.features", default) is FeatureMapModel sf && bundle.societyFeatures != null)
            foreach (var kv in bundle.societyFeatures)
                sf.PutFeature(kv.Key, kv.Value);
        if (EnsureModel("needs.satisfied", default) is FeatureMapModel ns && bundle.needSatisfied01 != null)
            foreach (var kv in bundle.needSatisfied01)
                ns.PutFeature(kv.Key, kv.Value);
    }

    public PersonaRequestBundle GetOrRequestBundle(string personaKey, CivilSystemKind kind)
    {
        if (!string.IsNullOrEmpty(personaKey) && _bundleCache.TryGetValue(personaKey, out var cached))
            return cached;
        var b = PersonaRequestBundle.CreateDefault(personaKey, kind);
        CacheBundle(b);
        return b;
    }

    public IReadOnlyList<RetinuePeckingEntry> GetRetinue(CivilVenueNode venue)
    {
        if (venue?.retinue != null) return venue.retinue;
        if (EnsureModel("retinue.pecking", default) is PeckingStatModel pm)
            return pm.Entries;
        return Array.Empty<RetinuePeckingEntry>();
    }

    public RetinuePeckingEntry UpsertPecking(CivilVenueNode venue, RetinuePeckingEntry entry)
    {
        if (entry == null) return null;
        var list = venue != null
            ? (venue.retinue ?? (venue.retinue = new List<RetinuePeckingEntry>()))
            : (EnsureModel("retinue.pecking", default) as PeckingStatModel)?.Entries;
        if (list == null) return entry;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null &&
                string.Equals(list[i].personaKey, entry.personaKey, StringComparison.OrdinalIgnoreCase))
            {
                list[i].role = entry.role ?? list[i].role;
                list[i].peckingOrder = entry.peckingOrder;
                list[i].actor = entry.actor ?? list[i].actor;
                return list[i];
            }
        }
        list.Add(entry);
        PutIndividual("retinue.pecking", SpecificityKey.Persona(entry.personaKey),
            StatOverride.Role(entry.role, entry.peckingOrder));
        return entry;
    }

    public void ApplyGovGloveBias(LifeSystemsSheet sheet, PersonaRequestBundle bundle)
    {
        if (sheet == null) return;
        sheet.EnsureDefaults();
        if (bundle != null)
            CacheBundle(bundle);
        var life = EnsureModel("life.baseline", default) as LifeBaselineModel;
        life?.Apply(sheet, bundle?.societyFeatures, bundle?.needSatisfied01);
    }

    public TradeDemographics GetTradeDemographics()
    {
        if (_trade == null)
            _trade = (EnsureModel("trade.demographics", StatSeed.ForCity(cityId)) as TradeDemographicsModel)?.Trade
                     ?? TradeDemographics.FromSocietyFeatures(null, ScopeId);
        return _trade;
    }

    public TradeEdge UpsertTradeEdge(TradeEdge edge)
    {
        if (edge == null) return null;
        var trade = GetTradeDemographics();
        if (edge.relationKind != TradeRelationKind.Commodity)
        {
            var rel = trade.EnsureRelation(edge.fromRetinueId, edge.toRetinueId, edge.commodityKey,
                edge.relationKind, edge.affinity01,
                edge.relationKind == TradeRelationKind.ChemicalBond ||
                edge.relationKind == TradeRelationKind.ChemicalRelease
                    ? edge.bondStrength01
                    : edge.sensitization01);
            if (!string.IsNullOrEmpty(edge.allergenOrBondKey))
                rel.allergenOrBondKey = edge.allergenOrBondKey;
            rel.irreversible = edge.irreversible;
            rel.volumeShare01 = edge.volumeShare01 > 0 ? edge.volumeShare01 : rel.volumeShare01;
            return rel;
        }
        return trade.BetweenRetinues(edge.fromRetinueId, edge.toRetinueId, edge.commodityKey,
            edge.volumeShare01, edge.affinity01);
    }

    public bool TryResolveTrade(in TradeDealRequest req, out TradeDealResult result)
    {
        result = new TradeDealResult { allowed = false, reason = "no edge" };
        var trade = GetTradeDemographics();
        TradeEdge edge = null;
        if (trade?.edges != null)
            for (int i = 0; i < trade.edges.Count; i++)
            {
                var e = trade.edges[i];
                if (e == null) continue;
                bool match =
                    string.Equals(e.fromRetinueId, req.fromRetinueId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.toRetinueId, req.toRetinueId, StringComparison.OrdinalIgnoreCase);
                if (!match && e.bidirectional)
                    match = string.Equals(e.fromRetinueId, req.toRetinueId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.toRetinueId, req.fromRetinueId, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
                if (!string.IsNullOrEmpty(req.commodityKey) &&
                    !string.Equals(e.commodityKey, req.commodityKey, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(e.allergenOrBondKey, req.commodityKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                edge = e;
                break;
            }
        if (edge == null && (trade?.edges == null || trade.edges.Count == 0))
        {
            result = new TradeDealResult { allowed = true, reason = "open market" };
            return true;
        }
        if (edge == null)
            return false;
        if (edge.relationKind == TradeRelationKind.AllergyAvoid)
        {
            result = new TradeDealResult { allowed = false, edge = edge, reason = "allergy avoid" };
            return false;
        }
        var ctx = new StatContext
        {
            Dao = this,
            RetinueId = req.fromRetinueId,
            CommodityKey = req.commodityKey,
            Edge = edge,
            UtcNow = req.utcNow == default ? DateTime.UtcNow : req.utcNow,
            Overlay = EnsureEconomicOverlay(req.fromRetinueId, default)
        };
        if (!Predicate("allergy_blocks")(ctx))
        {
            result = new TradeDealResult { allowed = false, edge = edge, reason = "allergy blocks" };
            return false;
        }
        if (edge.relationKind == TradeRelationKind.ChemicalBond && !Predicate("bond_ready")(ctx))
        {
            result = new TradeDealResult { allowed = false, edge = edge, reason = "bond not ready" };
            return false;
        }
        if (!Predicate("trade_edge_allows")(ctx))
        {
            result = new TradeDealResult { allowed = false, edge = edge, reason = "edge closed" };
            return false;
        }
        if (!Predicate("within_trade_quota")(ctx))
        {
            result = new TradeDealResult { allowed = false, edge = edge, reason = "quota" };
            return false;
        }
        result = new TradeDealResult { allowed = true, edge = edge, reason = "ok" };
        return true;
    }

    public EconomicOverlay EnsureEconomicOverlay(string retinueId, in StatSeed seed)
    {
        string id = string.IsNullOrEmpty(retinueId) ? seed.retinueId : retinueId;
        if (string.IsNullOrEmpty(id)) id = "retinue";
        if (_overlays.TryGetValue(id, out var existing) && existing != null)
            return existing;
        var seeded = seed;
        seeded.retinueId = id;
        var o = EconomicOverlay.FromRetinue(id, seeded);
        _overlays[id] = o;
        return o;
    }

    public EconomicOverlay EnsureEconomicOverlayFromCompany(CompanyRegistration company, StoreBase store)
    {
        var seed = new StatSeed { cityId = cityId, companyId = company != null ? company.companyId : null };
        var o = EconomicOverlay.FromCompany(company, store, seed);
        if (!string.IsNullOrEmpty(o.retinueId))
            _overlays[o.retinueId] = o;
        return o;
    }

    public void TickEconomy(string retinueId, float dtHours)
    {
        var o = EnsureEconomicOverlay(retinueId, default);
        if (o == null || !o.enabled) return;
        float dt = Mathf.Max(0f, dtHours);
        o.velocity01 = StatCurveCatalog.Get("velocity_decay")(o.velocity01, new StatCurveParams { dtHours = dt });
        var trade = GetTradeDemographics();
        if (trade?.edges == null) return;
        for (int i = 0; i < trade.edges.Count; i++)
        {
            var e = trade.edges[i];
            if (e == null || e.settlement != TradeSettlementMode.ContractStanding) continue;
            if (!string.Equals(e.fromRetinueId, retinueId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.toRetinueId, retinueId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!CronDue.IsActiveSchedule(e.openCron, DateTime.UtcNow)) continue;
            float vol = e.volumeShare01 * dt;
            var from = EnsureEconomicOverlay(e.fromRetinueId, default);
            var to = EnsureEconomicOverlay(e.toRetinueId, default);
            from.AdjustBalance(e.commodityKey, -vol);
            to.AdjustBalance(e.commodityKey, vol);
            o.velocity01 = Mathf.Clamp01(o.velocity01 + vol * 0.1f);
        }
    }

    public IRulesEngine EnsureRulesEngine(string engineId, in StatSeed seed)
    {
        string id = string.IsNullOrEmpty(engineId) ? "chemical.trade" : engineId;
        if (_engines.TryGetValue(id, out var existing) && existing != null)
            return existing;
        IRulesEngine engine;
        if (id.IndexOf("chemical", StringComparison.OrdinalIgnoreCase) >= 0)
            engine = ChemicalTradeRules.CreateDefault(this, seed);
        else
            engine = new RulesEngine(id, this) { AuditMode = seed.auditMode };
        _engines[id] = engine;
        return engine;
    }

    public bool TryFireRule(string engineId, string ruleId, in RuleFireContext ctx, out RuleFireResult result)
    {
        var engine = EnsureRulesEngine(engineId, new StatSeed { retinueId = ctx.fromRetinueId, auditMode = AuditMode.Playable });
        return engine.TryFire(ruleId, ctx, out result);
    }

    public AuditMode GetAuditMode(string engineId)
    {
        var e = EnsureRulesEngine(engineId, default);
        return e.AuditMode;
    }

    public void SetAuditMode(string engineId, AuditMode mode)
    {
        var e = EnsureRulesEngine(engineId, default);
        e.AuditMode = mode;
    }

    public float ApplyAuditCorrection(string engineId, string correctionId, float measured, float expected,
        in AuditCorrectionParams p)
    {
        var e = EnsureRulesEngine(engineId, default);
        return e.ApplyCorrection(correctionId, measured, expected, p);
    }

    public IReadOnlyList<AuditEntry> GetAuditTrail(string engineId, int maxEntries = 64)
    {
        var e = EnsureRulesEngine(engineId, default);
        var trail = e.Trail;
        if (trail == null || trail.Count == 0) return Array.Empty<AuditEntry>();
        int n = Mathf.Min(maxEntries, trail.Count);
        var arr = new AuditEntry[n];
        int start = trail.Count - n;
        for (int i = 0; i < n; i++)
            arr[i] = trail[start + i];
        return arr;
    }

    public MicroCulture EnsureCulture(string cultureId, CultureKind kind, in StatSeed seed)
    {
        string id = string.IsNullOrEmpty(cultureId) ? "culture" : cultureId;
        if (_cultures.TryGetValue(id, out var existing) && existing != null)
            return existing;
        var c = kind == CultureKind.Fungal
            ? MicroCulture.CreateFungal(id, seed.cultureSpeciesId, seed)
            : MicroCulture.CreateBacterial(id, seed.cultureSpeciesId, seed);
        if (kind == CultureKind.Biofilm)
            c.kind = CultureKind.Biofilm;
        _cultures[id] = c;
        return c;
    }

    public void TickCulture(string cultureId, float dtHours)
    {
        if (!_cultures.TryGetValue(cultureId, out var c) || c == null) return;
        float dt = Mathf.Max(0f, dtHours);
        if (c.empowerHoursRemaining > 0f)
        {
            c.empowerHoursRemaining = Mathf.Max(0f, c.empowerHoursRemaining - dt);
            if (c.empowerHoursRemaining <= 0f)
                c.empowerMult = 1f;
        }
        var ctx = new StatContext { Dao = this, Culture = c, RetinueId = c.hostRetinueId };
        if (!Predicate("culture_viable")(ctx))
        {
            c.viability01 = Mathf.Clamp01(c.viability01 - 0.01f * dt);
            return;
        }
        var overlay = EnsureEconomicOverlay(c.hostRetinueId ?? "lab", default);
        float media = 0.5f;
        if (!string.IsNullOrEmpty(c.mediaCommodityKey) &&
            overlay.TryGetBalance(c.mediaCommodityKey, out float m))
            media = Mathf.Clamp01(m / 10f);
        else
            EconomicOverlay.UpsertBalance(overlay, c.mediaCommodityKey, 5f);

        float consume = 0.1f * dt * Mathf.Max(0.05f, c.biomass01);
        overlay.AdjustBalance(c.mediaCommodityKey, -consume);

        var curveParams = new StatCurveParams
        {
            mumax = c.growthRate * Mathf.Max(0.01f, c.empowerMult),
            ks = 0.2f,
            carryingK = c.carryingCapacity01 > 0 ? c.carryingCapacity01 : 1f,
            dtHours = dt,
            empowerMult = c.empowerMult,
            tempK = c.tempK
        };
        float growth = c.kind == CultureKind.Bacterial
            ? StatCurveCatalog.Get("monod_growth")(media, curveParams)
            : 0f;
        if (c.kind == CultureKind.Bacterial)
            c.biomass01 = Mathf.Clamp01(c.biomass01 + growth * dt * c.viability01);
        else
            c.biomass01 = StatCurveCatalog.Get("logistic_biomass")(c.biomass01, curveParams);

        if (c.metaboliteCommodityKeys != null)
            for (int i = 0; i < c.metaboliteCommodityKeys.Count; i++)
                if (!string.IsNullOrEmpty(c.metaboliteCommodityKeys[i]))
                    overlay.AdjustBalance(c.metaboliteCommodityKeys[i], 0.05f * dt * c.biomass01);
    }

    public GrowthEvent ArmGrowthEvent(GrowthEvent ev)
    {
        if (ev == null) return null;
        ev.armed = true;
        ev.armedUntilHours = ev.durationHours;
        _growthEvents[ev.eventId ?? "event"] = ev;
        return ev;
    }

    public bool TryFireGrowthEvent(string eventId, out GrowthEventResult result)
    {
        result = new GrowthEventResult { ok = false, eventId = eventId, message = "missing" };
        if (string.IsNullOrEmpty(eventId) || !_growthEvents.TryGetValue(eventId, out var ev) || ev == null)
            return false;
        if (!ev.armed)
        {
            result.message = "not armed";
            return false;
        }
        int touched = 0;
        foreach (var kv in _cultures)
        {
            var c = kv.Value;
            if (c == null) continue;
            if (ev.targetCultureIds != null && ev.targetCultureIds.Length > 0)
            {
                bool hit = false;
                for (int i = 0; i < ev.targetCultureIds.Length; i++)
                    if (string.Equals(ev.targetCultureIds[i], c.cultureId, StringComparison.OrdinalIgnoreCase))
                        hit = true;
                if (!hit) continue;
            }
            if (ev.targetSpeciesIds != null && ev.targetSpeciesIds.Length > 0)
            {
                bool hit = false;
                for (int i = 0; i < ev.targetSpeciesIds.Length; i++)
                    if (string.Equals(ev.targetSpeciesIds[i], c.speciesId, StringComparison.OrdinalIgnoreCase))
                        hit = true;
                if (!hit) continue;
            }
            bool fungusEvent = ev.kind == GrowthEventKind.EmpowerFungus || ev.kind == GrowthEventKind.SporeRain;
            bool bactEvent = ev.kind == GrowthEventKind.BacterialBloom;
            if (fungusEvent && c.kind != CultureKind.Fungal) continue;
            if (bactEvent && c.kind != CultureKind.Bacterial) continue;
            c.empowerMult = Mathf.Max(c.empowerMult, ev.empowerMult);
            c.empowerHoursRemaining = Mathf.Max(c.empowerHoursRemaining, ev.durationHours);
            touched++;
            if (ev.sporeBurst && c.kind == CultureKind.Fungal)
            {
                string childId = c.cultureId + ".spore." + touched;
                if (!_cultures.ContainsKey(childId))
                {
                    var child = MicroCulture.CreateFungal(childId, c.speciesId,
                        new StatSeed { retinueId = c.hostRetinueId, cultureSpeciesId = c.speciesId });
                    child.biomass01 = 0.05f;
                    _cultures[childId] = child;
                }
            }
            if (ev.kind == GrowthEventKind.NutrientPulse)
            {
                var o = EnsureEconomicOverlay(c.hostRetinueId ?? "lab", default);
                o.AdjustBalance(c.mediaCommodityKey, 2f);
            }
            if (ev.kind == GrowthEventKind.HeatShock)
                c.viability01 = Mathf.Clamp01(c.viability01 * 0.85f);
        }
        ev.armed = false;
        result = new GrowthEventResult { ok = touched > 0, eventId = eventId, culturesTouched = touched, message = "ok" };
        return result.ok;
    }

    public CellularRetinue EnsureCellularRetinue(string hostRetinueId, string cultureId, in StatSeed seed)
    {
        string host = string.IsNullOrEmpty(hostRetinueId) ? (seed.retinueId ?? "host") : hostRetinueId;
        string key = host + ":" + (cultureId ?? "tissue");
        if (_cellular.TryGetValue(key, out var existing) && existing != null)
            return existing;
        EnsureCulture(cultureId ?? "tissue", CultureKind.Bacterial, seed);
        var model = EnsureModel("culture.cellular", seed) as CellularDemographicsModel;
        var ret = model?.Retinue ?? new CellularRetinue
        {
            demographics = CellularDemographics.DefaultTissue()
        };
        ret.hostRetinueId = host;
        ret.cultureId = cultureId ?? "tissue";
        if (model != null) model.Retinue = ret;
        _cellular[key] = ret;
        return ret;
    }

    public CellularPeckingEntry SampleCellularMember(
        string hostRetinueId, string cellKey, int seed, Vector3? spawnPos = null)
    {
        var ret = EnsureCellularRetinue(hostRetinueId, "tissue",
            new StatSeed { retinueId = hostRetinueId, cityId = cityId });
        return ret.SampleAndAdd(cellKey, seed, spawnPos);
    }

    public StatPredicate Predicate(string id) => StatPredicateCatalog.Get(id);
    public StatCurve Curve(string id) => StatCurveCatalog.Get(id);

    public string TiltYesNoViaDao(ElectorateDemographics demo, bool actorPinned, string actorChoice, int seed)
    {
        var model = EnsureModel("vote.electorate", default) as ElectorateStatModel;
        if (demo != null && model != null)
        {
            // Prefer caller's demographics instance
            return demo.TiltYesNo(demo.Sample(seed), actorPinned, actorChoice, seed);
        }
        return model != null
            ? model.TiltYesNo(actorPinned, actorChoice, seed)
            : (actorPinned && !string.IsNullOrEmpty(actorChoice) ? actorChoice : "yes");
    }
}
