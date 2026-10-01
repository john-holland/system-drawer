using System;
using System.Collections.Generic;
using System.Globalization;
using Locomotion.Narrative;
using UnityEngine;

public static class StatLemmaPropertyKeys
{
    public const string PlaceholderName = "stat";
    public const string Op = "op";
    public const string Model = "model";
    public const string Key = "key";
    public const string Channel = "channel";
    public const string Curve = "curve";
    public const string Predicate = "predicate";
    public const string Delta = "delta";
    public const string Value = "value";
    public const string Persona = "persona";
    public const string Role = "role";
    public const string Pecking = "pecking";
    public const string From = "from";
    public const string To = "to";
    public const string Commodity = "commodity";
    public const string Share = "share";
    public const string Retinue = "retinue";
    public const string Engine = "engine";
    public const string Reaction = "reaction";
    public const string Audit = "audit";
    public const string Mode = "mode";
    public const string Correct = "correct";
    public const string Epsilon = "epsilon";
    public const string Species = "species";
    public const string Event = "event";
    public const string Mult = "mult";
    public const string Count = "count";
    public const string Seed = "seed";
    public const string Enable = "enable";
    public const string Item = "item";
    public const string Relation = "relation";
    public const string Part = "part";
    public const string Allergen = "allergen";
    public const string Kind = "kind";
    public const string X = "x";
    public const string Y = "y";
    public const string Z = "z";
    public const string Id = "id";
}

public enum StatLemmaOp
{
    Sample = 0,
    Query = 1,
    Adjust = 2,
    Put = 3,
    Patch = 4,
    Materialize = 5,
    Reconcile = 6,
    When = 7,
    Trade = 8,
    Overlay = 9,
    Rule = 10,
    Audit = 11,
    Culture = 12,
    Growth = 13,
    Curve = 14,
    HealthInpaint = 15,
    HealthInpaintEvent = 16
}

public struct StatLemmaProperties
{
    public StatLemmaOp op;
    public string model;
    public string key;
    public string channel;
    public string curve;
    public string predicate;
    public float delta;
    public float value;
    public string persona;
    public string role;
    public int pecking;
    public string from;
    public string to;
    public string commodity;
    public float share;
    public string retinue;
    public string engine;
    public string reaction;
    public string audit;
    public string mode;
    public string correct;
    public float epsilon;
    public string species;
    public string eventId;
    public float mult;
    public int count;
    public int seed;
    public int enable;
    public string item;
    public string relation;
    public string part;
    public string allergen;
    public string kind;
    public float x, y, z;
    public bool hasWorldPos;
    public string eventIdOverride;

    public static StatLemmaProperties Defaults => new StatLemmaProperties
    {
        op = StatLemmaOp.Sample,
        epsilon = 0.05f,
        mult = 1f,
        count = 1,
        enable = 1
    };
}

/// <summary>Resolves {P:stat|...} spans against StatisticalRetinueDao.</summary>
public static class StatLemmaResolver
{
    public static StatLemmaProperties ResolveFromSegments(IReadOnlyList<PromptSegment> segments)
    {
        var props = StatLemmaProperties.Defaults;
        if (segments == null) return props;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            if (!string.Equals(seg.placeholderName, StatLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase))
                continue;
            ApplyParams(ref props, seg.placeholderParams);
        }
        return props;
    }

    public static void ApplyParams(ref StatLemmaProperties props, Dictionary<string, string> p)
    {
        if (p == null) return;
        if (TryGet(p, StatLemmaPropertyKeys.Op, out var op)) props.op = ParseOp(op);
        if (TryGet(p, StatLemmaPropertyKeys.Model, out var model)) props.model = model;
        if (TryGet(p, StatLemmaPropertyKeys.Key, out var key)) props.key = key;
        if (TryGet(p, StatLemmaPropertyKeys.Channel, out var ch)) props.channel = ch;
        if (TryGet(p, StatLemmaPropertyKeys.Curve, out var curve)) props.curve = curve;
        if (TryGet(p, StatLemmaPropertyKeys.Predicate, out var pred)) props.predicate = pred;
        if (TryGet(p, StatLemmaPropertyKeys.Delta, out var d))
            float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out props.delta);
        if (TryGet(p, StatLemmaPropertyKeys.Value, out var v))
            float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out props.value);
        if (TryGet(p, StatLemmaPropertyKeys.Persona, out var persona)) props.persona = persona;
        if (TryGet(p, StatLemmaPropertyKeys.Role, out var role)) props.role = role;
        if (TryGet(p, StatLemmaPropertyKeys.Pecking, out var peck) &&
            int.TryParse(peck, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pi))
            props.pecking = pi;
        if (TryGet(p, StatLemmaPropertyKeys.From, out var from)) props.from = from;
        if (TryGet(p, StatLemmaPropertyKeys.To, out var to)) props.to = to;
        if (TryGet(p, StatLemmaPropertyKeys.Commodity, out var commodity)) props.commodity = commodity;
        if (TryGet(p, StatLemmaPropertyKeys.Share, out var share))
            float.TryParse(share, NumberStyles.Float, CultureInfo.InvariantCulture, out props.share);
        if (TryGet(p, StatLemmaPropertyKeys.Retinue, out var retinue)) props.retinue = retinue;
        if (TryGet(p, StatLemmaPropertyKeys.Engine, out var engine)) props.engine = engine;
        if (TryGet(p, StatLemmaPropertyKeys.Reaction, out var reaction)) props.reaction = reaction;
        if (TryGet(p, StatLemmaPropertyKeys.Audit, out var audit)) props.audit = audit;
        if (TryGet(p, StatLemmaPropertyKeys.Mode, out var mode)) props.mode = mode;
        if (TryGet(p, StatLemmaPropertyKeys.Correct, out var correct)) props.correct = correct;
        if (TryGet(p, StatLemmaPropertyKeys.Epsilon, out var eps))
            float.TryParse(eps, NumberStyles.Float, CultureInfo.InvariantCulture, out props.epsilon);
        if (TryGet(p, StatLemmaPropertyKeys.Species, out var species)) props.species = species;
        if (TryGet(p, StatLemmaPropertyKeys.Event, out var ev)) props.eventId = ev;
        if (TryGet(p, StatLemmaPropertyKeys.Mult, out var mult))
            float.TryParse(mult, NumberStyles.Float, CultureInfo.InvariantCulture, out props.mult);
        if (TryGet(p, StatLemmaPropertyKeys.Count, out var count) &&
            int.TryParse(count, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ci))
            props.count = ci;
        if (TryGet(p, StatLemmaPropertyKeys.Seed, out var seed) &&
            int.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int si))
            props.seed = si;
        if (TryGet(p, StatLemmaPropertyKeys.Enable, out var en) &&
            int.TryParse(en, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ei))
            props.enable = ei;
        if (TryGet(p, StatLemmaPropertyKeys.Item, out var item)) props.item = item;
        if (TryGet(p, StatLemmaPropertyKeys.Relation, out var relation)) props.relation = relation;
        if (TryGet(p, StatLemmaPropertyKeys.Part, out var part)) props.part = part;
        if (TryGet(p, StatLemmaPropertyKeys.Allergen, out var allergen)) props.allergen = allergen;
        if (TryGet(p, StatLemmaPropertyKeys.Kind, out var kind)) props.kind = kind;
        if (TryGet(p, StatLemmaPropertyKeys.Id, out var id) && string.IsNullOrEmpty(props.eventId))
            props.eventId = id;
        bool hasX = TryGet(p, StatLemmaPropertyKeys.X, out var xs) &&
                    float.TryParse(xs, NumberStyles.Float, CultureInfo.InvariantCulture, out props.x);
        bool hasY = TryGet(p, StatLemmaPropertyKeys.Y, out var ys) &&
                    float.TryParse(ys, NumberStyles.Float, CultureInfo.InvariantCulture, out props.y);
        bool hasZ = TryGet(p, StatLemmaPropertyKeys.Z, out var zs) &&
                    float.TryParse(zs, NumberStyles.Float, CultureInfo.InvariantCulture, out props.z);
        if (hasX || hasY || hasZ) props.hasWorldPos = true;
    }

    public static string Execute(StatLemmaProperties props, IStatisticalRetinueDao dao = null)
    {
        dao ??= StatisticalRetinueDao.Instance;
        if (dao == null)
        {
            var go = new GameObject("StatisticalRetinueDao");
            dao = go.AddComponent<StatisticalRetinueDao>();
        }

        switch (props.op)
        {
            case StatLemmaOp.Sample:
            case StatLemmaOp.Query:
            {
                string model = string.IsNullOrEmpty(props.model) ? "society.features" : props.model;
                string key = !string.IsNullOrEmpty(props.key) ? props.key : props.channel;
                var q = new StatQuery
                {
                    featureKey = key,
                    seed = props.seed,
                    worldPos = props.hasWorldPos ? new Vector3(props.x, props.y, props.z) : Vector3.zero
                };
                dao.TryGet(model, q, out float v);
                return $"{model}:{key}={v:0.###}";
            }
            case StatLemmaOp.Adjust:
            {
                string model = string.IsNullOrEmpty(props.model) ? "life.baseline" : props.model;
                string curve = string.IsNullOrEmpty(props.curve) ? "nudge_clamped" : props.curve;
                float t = props.value;
                float r = dao.Curve(curve)(t, new StatCurveParams { delta = props.delta, softMin = 0f, softMax = 1f });
                if (!string.IsNullOrEmpty(props.channel) || !string.IsNullOrEmpty(props.persona))
                    dao.PutIndividual(model, SpecificityKey.Persona(props.persona ?? "lemma"),
                        StatOverride.Channel(props.channel ?? props.key, r));
                return $"adjust {model}={r:0.###}";
            }
            case StatLemmaOp.Put:
            case StatLemmaOp.Patch:
            {
                if (!string.IsNullOrEmpty(props.model) &&
                    props.model.IndexOf("trade", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var kind = ParseRelation(props.relation);
                    dao.UpsertTradeEdge(new TradeEdge
                    {
                        fromRetinueId = props.from,
                        toRetinueId = props.to,
                        commodityKey = props.commodity ?? "generic",
                        allergenOrBondKey = props.allergen ?? props.commodity,
                        volumeShare01 = props.share > 0 ? props.share : 0.1f,
                        affinity01 = 0.7f,
                        relationKind = kind,
                        bondStrength01 = props.share > 0 ? props.share : 0.5f,
                        sensitization01 = props.share > 0 ? props.share : 0.5f
                    });
                    return $"trade edge {props.from}->{props.to} {kind}";
                }
                if (!string.IsNullOrEmpty(props.persona))
                {
                    dao.PutIndividual(string.IsNullOrEmpty(props.model) ? "retinue.pecking" : props.model,
                        SpecificityKey.Persona(props.persona),
                        StatOverride.Role(props.role, props.pecking != 0 ? props.pecking : 100));
                    return $"put persona={props.persona}";
                }
                return "put noop";
            }
            case StatLemmaOp.Trade:
            {
                var req = new TradeDealRequest
                {
                    fromRetinueId = props.from,
                    toRetinueId = props.to,
                    commodityKey = props.commodity ?? props.item,
                    utcNow = DateTime.UtcNow
                };
                bool ok = dao.TryResolveTrade(req, out var res);
                return ok ? $"trade ok {res.reason}" : $"trade deny {res.reason}";
            }
            case StatLemmaOp.Overlay:
            {
                var o = dao.EnsureEconomicOverlay(props.retinue ?? "retinue", default);
                o.enabled = props.enable != 0;
                return $"overlay {o.retinueId} enabled={o.enabled}";
            }
            case StatLemmaOp.Rule:
            {
                string engine = string.IsNullOrEmpty(props.engine) ? "chemical.trade" : props.engine;
                if (!string.IsNullOrEmpty(props.audit) &&
                    Enum.TryParse(props.audit, true, out AuditMode mode))
                    dao.SetAuditMode(engine, mode);
                var ctx = new RuleFireContext
                {
                    Dao = dao,
                    fromRetinueId = props.from ?? props.retinue ?? "lab",
                    toRetinueId = props.to ?? props.from ?? "lab",
                    tempK = 298.15f,
                    dtHours = 1f
                };
                // Ensure reagents for demo fire
                var overlay = dao.EnsureEconomicOverlay(ctx.fromRetinueId, default);
                EconomicOverlay.UpsertBalance(overlay, "acid", 5f);
                EconomicOverlay.UpsertBalance(overlay, "base", 5f);
                EconomicOverlay.UpsertBalance(overlay, "alcohol", 5f);
                EconomicOverlay.UpsertBalance(overlay, "allergen", 5f);
                EconomicOverlay.UpsertBalance(overlay, "sensor", 5f);
                EconomicOverlay.UpsertBalance(overlay, "adhesive", 5f);
                EconomicOverlay.UpsertBalance(overlay, "substrate", 5f);
                EconomicOverlay.UpsertBalance(overlay, "solvent", 5f);
                EconomicOverlay.UpsertBalance(overlay, "bonded", 5f);
                EconomicOverlay.UpsertBalance(overlay, "moisture", 5f);
                EconomicOverlay.UpsertBalance(overlay, "cold_metal", 5f);
                bool fired = dao.TryFireRule(engine, props.reaction ?? "acid_base_neutralize", ctx, out var result);
                if (fired)
                {
                    var runner = UnityEngine.Object.FindFirstObjectByType<HealthInpaintEventRunner>();
                    runner?.FireForRule(result.ruleId, null, props.part, props.allergen);
                }
                return fired ? $"rule {result.ruleId} committed={result.committed:0.###}" : $"rule fail {result.message}";
            }
            case StatLemmaOp.Audit:
            {
                string engine = string.IsNullOrEmpty(props.engine) ? "chemical.trade" : props.engine;
                if (!string.IsNullOrEmpty(props.mode) && Enum.TryParse(props.mode, true, out AuditMode mode))
                    dao.SetAuditMode(engine, mode);
                var p = AuditCorrectionParams.Default;
                p.epsilon = props.epsilon > 0 ? props.epsilon : 0.05f;
                float c = dao.ApplyAuditCorrection(engine, props.correct ?? "audit_blend", props.value, props.value + props.delta, p);
                return $"audit {c:0.###}";
            }
            case StatLemmaOp.Culture:
            {
                if (!string.IsNullOrEmpty(props.model) &&
                    props.model.IndexOf("cellular", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Vector3? spawn = props.hasWorldPos ? new Vector3(props.x, props.y, props.z) : (Vector3?)null;
                    var member = dao.SampleCellularMember(props.retinue ?? "host", props.species ?? "cell",
                        props.seed, spawn);
                    return member != null
                        ? $"cellular {member.cellKey} role={member.role}"
                        : "cellular sample fail";
                }
                var kind = !string.IsNullOrEmpty(props.model) &&
                           props.model.IndexOf("fungus", StringComparison.OrdinalIgnoreCase) >= 0
                    ? CultureKind.Fungal
                    : CultureKind.Bacterial;
                var c = dao.EnsureCulture(props.retinue ?? props.species ?? "culture", kind,
                    new StatSeed { cultureSpeciesId = props.species, retinueId = props.retinue });
                if (!string.IsNullOrEmpty(props.eventId))
                {
                    dao.ArmGrowthEvent(GrowthEvent.EmpowerFungus(props.eventId, props.mult > 0 ? props.mult : 1.5f, 2f, props.species));
                    dao.TryFireGrowthEvent(props.eventId, out _);
                }
                dao.TickCulture(c.cultureId, 0.25f);
                return $"culture {c.cultureId} biomass={c.biomass01:0.###}";
            }
            case StatLemmaOp.HealthInpaint:
            {
                HealthInpaintKind kind = HealthInpaintKind.Custom;
                if (!string.IsNullOrEmpty(props.kind))
                    Enum.TryParse(props.kind, true, out kind);
                var req = ActorHealthInpaintBridge.Request(new HealthInpaintRequest
                {
                    kind = kind,
                    partId = props.part,
                    allergenOrBondKey = props.allergen ?? props.commodity,
                    promptHint = $"{kind} {props.allergen}",
                    intensity01 = props.value > 0 ? props.value : 0.5f
                });
                return req != null
                    ? $"health_inpaint {req.kind} part={req.partId ?? "body"} specific={req.partSpecific}"
                    : "health_inpaint fail";
            }
            case StatLemmaOp.HealthInpaintEvent:
            {
                var runner = UnityEngine.Object.FindFirstObjectByType<HealthInpaintEventRunner>();
                if (runner == null)
                {
                    var go = new GameObject("HealthInpaintEventRunner");
                    runner = go.AddComponent<HealthInpaintEventRunner>();
                }
                var fired = runner.Fire(props.eventId ?? "hives_from_toad", null, props.part,
                    props.allergen ?? "toad", props.retinue ?? "venue");
                return fired != null
                    ? $"health_inpaint_event {runner.lastEventId} prompt={runner.lastPrompt}"
                    : "health_inpaint_event fail";
            }
            case StatLemmaOp.Growth:
            {
                var ev = GrowthEvent.EmpowerFungus(props.eventId ?? "fungus_bloom",
                    props.mult > 0 ? props.mult : 1.5f, 2f, props.species);
                dao.ArmGrowthEvent(ev);
                bool ok = dao.TryFireGrowthEvent(ev.eventId, out var gr);
                return ok ? $"growth {gr.culturesTouched}" : $"growth fail {gr.message}";
            }
            case StatLemmaOp.Curve:
            {
                string id = string.IsNullOrEmpty(props.curve) ? props.key : props.curve;
                float r = dao.Curve(id ?? "soft_band_lerp")(props.value,
                    new StatCurveParams { needMean = props.value, healthcare = 0.7f, empowerMult = props.mult });
                return $"curve {id}={r:0.###}";
            }
            case StatLemmaOp.Reconcile:
                dao.GetModel(props.model ?? "civ.demographics")?.Reconcile(StatReconcileMode.Quotas);
                return "reconcile";
            case StatLemmaOp.When:
                bool pass = dao.Predicate(props.predicate ?? "cron_active")(new StatContext { Dao = dao });
                return pass ? "when pass" : "when fail";
            case StatLemmaOp.Materialize:
                return "materialize ok";
            default:
                return "stat noop";
        }
    }

    public static string ExecuteFromScript(string scriptText, IStatisticalRetinueDao dao = null)
    {
        var segments = PromptSpanParser.Parse(scriptText ?? "");
        return Execute(ResolveFromSegments(segments), dao);
    }

    static StatLemmaOp ParseOp(string s)
    {
        if (string.IsNullOrEmpty(s)) return StatLemmaOp.Sample;
        return s.ToLowerInvariant() switch
        {
            "sample" or "query" => StatLemmaOp.Sample,
            "adjust" => StatLemmaOp.Adjust,
            "put" or "patch" => StatLemmaOp.Put,
            "materialize" => StatLemmaOp.Materialize,
            "reconcile" => StatLemmaOp.Reconcile,
            "when" => StatLemmaOp.When,
            "trade" => StatLemmaOp.Trade,
            "overlay" => StatLemmaOp.Overlay,
            "rule" => StatLemmaOp.Rule,
            "audit" => StatLemmaOp.Audit,
            "culture" => StatLemmaOp.Culture,
            "growth" => StatLemmaOp.Growth,
            "curve" => StatLemmaOp.Curve,
            "health_inpaint" => StatLemmaOp.HealthInpaint,
            "health_inpaint_event" => StatLemmaOp.HealthInpaintEvent,
            _ => StatLemmaOp.Sample
        };
    }

    static TradeRelationKind ParseRelation(string s)
    {
        if (string.IsNullOrEmpty(s)) return TradeRelationKind.Commodity;
        s = s.Replace("-", "_").ToLowerInvariant();
        return s switch
        {
            "allergy_avoid" or "allergyavoid" => TradeRelationKind.AllergyAvoid,
            "allergy_sensitize" or "allergysensitize" => TradeRelationKind.AllergySensitize,
            "chemical_bond" or "chemicalbond" or "glue" => TradeRelationKind.ChemicalBond,
            "chemical_release" or "chemicalrelease" or "solvent" => TradeRelationKind.ChemicalRelease,
            _ => TradeRelationKind.Commodity
        };
    }

    static bool TryGet(Dictionary<string, string> p, string key, out string value)
    {
        value = null;
        if (p == null || string.IsNullOrEmpty(key)) return false;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }
}
