using System;
using System.Collections.Generic;
using UnityEngine;

public enum AuditMode
{
    Strict = 0,
    ReportOnly = 1,
    Playable = 2,
    AuthoritativeExpected = 3,
    FrozenTrail = 4
}

[Serializable]
public struct AuditCorrectionParams
{
    public float epsilon;
    [Range(0f, 1f)] public float fudge01;
    public string correctionId;
    public bool recordCounterfactual;
    public float lodScale;
    public float dtHours;

    public static AuditCorrectionParams Default => new AuditCorrectionParams
    {
        epsilon = 0.05f,
        fudge01 = 0.35f,
        correctionId = "audit_blend",
        recordCounterfactual = true,
        lodScale = 1f
    };
}

[Serializable]
public sealed class AuditEntry
{
    public double utcTicks;
    public string engineId;
    public string ruleId;
    public AuditMode mode;
    public float measured;
    public float expected;
    public float committed;
    public string correctionId;
    public string retinueId;
    public string notes;
}

public delegate float AuditCorrectionFn(float measured, float expected, in AuditCorrectionParams p);

public struct RuleFireContext
{
    public IStatisticalRetinueDao Dao;
    public string fromRetinueId;
    public string toRetinueId;
    public float tempK;
    public float dtHours;
    public float lodScale;
    public Dictionary<string, float> reagents;
}

public struct RuleFireResult
{
    public bool ok;
    public string ruleId;
    public float measured;
    public float expected;
    public float committed;
    public string message;
    public Dictionary<string, float> productDeltas;
    public Dictionary<string, float> reactantDeltas;
}

public interface IStatRule
{
    string RuleId { get; }
    string[] ReadsModels { get; }
    string[] WritesModels { get; }
    bool Preconditions(in RuleFireContext ctx);
    RuleFireResult Evaluate(in RuleFireContext ctx);
    void Commit(in RuleFireResult result, IStatisticalRetinueDao dao);
}

public interface IRulesEngine
{
    string EngineId { get; }
    AuditMode AuditMode { get; set; }
    IReadOnlyList<IStatRule> Rules { get; }
    bool TryFire(string ruleId, in RuleFireContext ctx, out RuleFireResult result);
    void RegisterCorrection(string correctionId, AuditCorrectionFn fn);
    float ApplyCorrection(string correctionId, float measured, float expected, in AuditCorrectionParams p);
    IReadOnlyList<AuditEntry> Trail { get; }
}

[Serializable]
public sealed class ChemicalSpeciesRef
{
    public string commodityKey;
    public float molesPerUnit = 1f;
}

[Serializable]
public sealed class ChemicalReactionRule : IStatRule
{
    public string ruleId;
    public List<ChemicalSpeciesRef> reactants = new List<ChemicalSpeciesRef>();
    public List<ChemicalSpeciesRef> products = new List<ChemicalSpeciesRef>();
    public float rateConstant = 1f;
    public float enthalpyHint;
    public string fromRetinueId;
    public string toRetinueId;

    public string RuleId => ruleId;
    public string[] ReadsModels => new[] { "econ.ledger", "econ.overlay" };
    public string[] WritesModels => new[] { "econ.ledger", "rules.audit" };

    public bool Preconditions(in RuleFireContext ctx)
    {
        if (reactants == null || reactants.Count == 0) return false;
        var overlay = ctx.Dao?.EnsureEconomicOverlay(fromRetinueId ?? ctx.fromRetinueId, default);
        if (overlay == null || !overlay.enabled) return false;
        for (int i = 0; i < reactants.Count; i++)
        {
            var r = reactants[i];
            if (r == null || string.IsNullOrEmpty(r.commodityKey)) return false;
            if (!overlay.TryGetBalance(r.commodityKey, out float qty) || qty < r.molesPerUnit * 0.01f)
                return false;
        }
        return true;
    }

    public RuleFireResult Evaluate(in RuleFireContext ctx)
    {
        float temp = ctx.tempK > 1f ? ctx.tempK : 298.15f;
        float rate = rateConstant * StatCurveCatalog.Get("arrhenius_rate")(temp, new StatCurveParams { tempK = temp });
        float limiting = float.MaxValue;
        var reactantDeltas = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reactants.Count; i++)
        {
            var r = reactants[i];
            if (r == null) continue;
            float avail = 0f;
            if (ctx.reagents != null && ctx.reagents.TryGetValue(r.commodityKey, out float v))
                avail = v;
            else
            {
                var o = ctx.Dao?.EnsureEconomicOverlay(fromRetinueId ?? ctx.fromRetinueId, default);
                o?.TryGetBalance(r.commodityKey, out avail);
            }
            float need = Mathf.Max(1e-6f, r.molesPerUnit);
            limiting = Mathf.Min(limiting, avail / need);
            reactantDeltas[r.commodityKey] = -need;
        }
        if (limiting >= float.MaxValue * 0.5f) limiting = 0f;
        limiting *= Mathf.Clamp01(rate) * Mathf.Max(0.01f, ctx.dtHours > 0 ? ctx.dtHours : 1f);
        var keys = new List<string>(reactantDeltas.Keys);
        for (int i = 0; i < keys.Count; i++)
            reactantDeltas[keys[i]] *= limiting;

        var productDeltas = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        float expected = 0f;
        if (products != null)
            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                if (p == null) continue;
                float qty = p.molesPerUnit * limiting;
                productDeltas[p.commodityKey] = qty;
                expected += qty;
            }

        float measured = expected * (0.85f + 0.15f * Mathf.Clamp01(rate)); // noisy measured
        return new RuleFireResult
        {
            ok = limiting > 1e-6f,
            ruleId = ruleId,
            measured = measured,
            expected = expected,
            committed = measured,
            message = limiting > 0 ? "ok" : "no limiting reagent",
            productDeltas = productDeltas,
            reactantDeltas = reactantDeltas
        };
    }

    public void Commit(in RuleFireResult result, IStatisticalRetinueDao dao)
    {
        if (!result.ok || dao == null) return;
        string from = fromRetinueId;
        string to = string.IsNullOrEmpty(toRetinueId) ? from : toRetinueId;
        var fromOverlay = dao.EnsureEconomicOverlay(from, default);
        var toOverlay = dao.EnsureEconomicOverlay(to, default);
        if (result.reactantDeltas != null)
            foreach (var kv in result.reactantDeltas)
                fromOverlay.AdjustBalance(kv.Key, kv.Value);
        if (result.productDeltas != null)
            foreach (var kv in result.productDeltas)
                toOverlay.AdjustBalance(kv.Key, kv.Value);
    }
}

public sealed class RulesEngine : IRulesEngine
{
    readonly List<IStatRule> _rules = new List<IStatRule>();
    readonly Dictionary<string, AuditCorrectionFn> _corrections =
        new Dictionary<string, AuditCorrectionFn>(StringComparer.OrdinalIgnoreCase);
    readonly List<AuditEntry> _trail = new List<AuditEntry>();
    readonly IStatisticalRetinueDao _dao;

    public string EngineId { get; }
    public AuditMode AuditMode { get; set; } = AuditMode.Playable;
    public IReadOnlyList<IStatRule> Rules => _rules;
    public IReadOnlyList<AuditEntry> Trail => _trail;

    public RulesEngine(string engineId, IStatisticalRetinueDao dao)
    {
        EngineId = engineId ?? "rules";
        _dao = dao;
        AuditCorrectionCatalog.RegisterDefaults(_corrections);
    }

    public void AddRule(IStatRule rule)
    {
        if (rule != null) _rules.Add(rule);
    }

    public void RegisterCorrection(string correctionId, AuditCorrectionFn fn)
    {
        if (!string.IsNullOrEmpty(correctionId) && fn != null)
            _corrections[correctionId] = fn;
    }

    public float ApplyCorrection(string correctionId, float measured, float expected, in AuditCorrectionParams p)
    {
        if (string.IsNullOrEmpty(correctionId) || !_corrections.TryGetValue(correctionId, out var fn))
            fn = AuditCorrectionCatalog.Blend;
        return fn(measured, expected, p);
    }

    public bool TryFire(string ruleId, in RuleFireContext ctx, out RuleFireResult result)
    {
        result = default;
        IStatRule rule = null;
        for (int i = 0; i < _rules.Count; i++)
            if (_rules[i] != null && string.Equals(_rules[i].RuleId, ruleId, StringComparison.OrdinalIgnoreCase))
            {
                rule = _rules[i];
                break;
            }
        if (rule == null)
        {
            result = new RuleFireResult { ok = false, message = "missing rule" };
            return false;
        }
        var fireCtx = ctx;
        if (fireCtx.Dao == null)
            fireCtx.Dao = _dao;
        if (!rule.Preconditions(fireCtx))
        {
            result = new RuleFireResult { ok = false, ruleId = ruleId, message = "preconditions" };
            return false;
        }
        result = rule.Evaluate(fireCtx);
        var p = AuditCorrectionParams.Default;
        p.lodScale = ctx.lodScale > 0 ? ctx.lodScale : 1f;
        p.dtHours = ctx.dtHours;
        string correctionId = "audit_blend";
        switch (AuditMode)
        {
            case AuditMode.FrozenTrail:
                Log(ruleId, result.measured, result.expected, result.measured, correctionId, "frozen");
                result.ok = false;
                result.message = "frozen";
                return false;
            case AuditMode.Strict:
                correctionId = "audit_reject_band";
                float strict = ApplyCorrection(correctionId, result.measured, result.expected, p);
                if (float.IsNaN(strict))
                {
                    Log(ruleId, result.measured, result.expected, result.measured, correctionId, "reject");
                    result.ok = false;
                    result.message = "strict reject";
                    return false;
                }
                result.committed = strict;
                break;
            case AuditMode.ReportOnly:
                correctionId = "audit_snap_measured";
                result.committed = ApplyCorrection(correctionId, result.measured, result.expected, p);
                break;
            case AuditMode.AuthoritativeExpected:
                correctionId = "audit_snap_expected";
                result.committed = ApplyCorrection(correctionId, result.measured, result.expected, p);
                break;
            case AuditMode.Playable:
            default:
                correctionId = ctx.lodScale > 0 && ctx.lodScale < 0.99f ? "audit_science_rt" : "audit_blend";
                p.correctionId = correctionId;
                result.committed = ApplyCorrection(correctionId, result.measured, result.expected, p);
                break;
        }
        // Scale product deltas by committed/expected ratio when expected > 0
        if (result.expected > 1e-6f && result.productDeltas != null)
        {
            float scale = result.committed / result.expected;
            var keys = new List<string>(result.productDeltas.Keys);
            for (int i = 0; i < keys.Count; i++)
                result.productDeltas[keys[i]] *= scale;
        }
        rule.Commit(result, fireCtx.Dao);
        Log(ruleId, result.measured, result.expected, result.committed, correctionId, result.message);
        return result.ok;
    }

    void Log(string ruleId, float measured, float expected, float committed, string correctionId, string notes)
    {
        _trail.Add(new AuditEntry
        {
            utcTicks = DateTime.UtcNow.Ticks,
            engineId = EngineId,
            ruleId = ruleId,
            mode = AuditMode,
            measured = measured,
            expected = expected,
            committed = committed,
            correctionId = correctionId,
            notes = notes
        });
        if (_trail.Count > 256)
            _trail.RemoveAt(0);
    }
}

public static class ChemicalTradeRules
{
    public static RulesEngine CreateDefault(IStatisticalRetinueDao dao, in StatSeed seed)
    {
        var engine = new RulesEngine("chemical.trade", dao);
        engine.AuditMode = seed.auditMode;
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "acid_base_neutralize",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "acid", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "base", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "salt", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "water", molesPerUnit = 1f }
            },
            rateConstant = 1f
        });
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "esterification",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "acid", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "alcohol", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "ester", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "water", molesPerUnit = 1f }
            },
            rateConstant = 0.8f
        });
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "allergy_crosslink",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "allergen", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "sensor", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "histamine", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "sensitized", molesPerUnit = 1f }
            },
            rateConstant = 1.1f
        });
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "glue_cure",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "adhesive", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "substrate", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "bonded", molesPerUnit = 1f }
            },
            rateConstant = 0.9f
        });
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "glue_solvent_release",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "solvent", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "bonded", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "substrate", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "adhesive_waste", molesPerUnit = 0.5f }
            },
            rateConstant = 1f
        });
        engine.AddRule(new ChemicalReactionRule
        {
            ruleId = "tongue_freeze_bond",
            fromRetinueId = seed.retinueId,
            toRetinueId = seed.retinueId,
            reactants = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "moisture", molesPerUnit = 1f },
                new ChemicalSpeciesRef { commodityKey = "cold_metal", molesPerUnit = 1f }
            },
            products = new List<ChemicalSpeciesRef>
            {
                new ChemicalSpeciesRef { commodityKey = "ice_bond", molesPerUnit = 1f }
            },
            rateConstant = 1.2f
        });
        return engine;
    }
}

public static class AuditCorrectionCatalog
{
    public static float Blend(float measured, float expected, in AuditCorrectionParams p) =>
        Mathf.Lerp(measured, expected, Mathf.Clamp01(p.fudge01));

    public static float SnapExpected(float measured, float expected, in AuditCorrectionParams p) => expected;

    public static float SnapMeasured(float measured, float expected, in AuditCorrectionParams p) => measured;

    public static float RejectBand(float measured, float expected, in AuditCorrectionParams p)
    {
        float eps = p.epsilon > 0 ? p.epsilon : 0.05f;
        return Mathf.Abs(measured - expected) <= eps ? measured : float.NaN;
    }

    public static float ScienceRt(float measured, float expected, in AuditCorrectionParams p)
    {
        float fudge = Mathf.Clamp01(p.fudge01);
        if (p.lodScale > 0f && p.lodScale < 1f)
            fudge = Mathf.Clamp01(fudge + (1f - p.lodScale) * 0.35f);
        if (p.dtHours > 1f)
            fudge = Mathf.Clamp01(fudge + 0.1f);
        return Mathf.Lerp(measured, expected, fudge);
    }

    public static void RegisterDefaults(Dictionary<string, AuditCorrectionFn> map)
    {
        if (map == null) return;
        map["audit_blend"] = Blend;
        map["audit_snap_expected"] = SnapExpected;
        map["audit_snap_measured"] = SnapMeasured;
        map["audit_reject_band"] = RejectBand;
        map["audit_science_rt"] = ScienceRt;
        map["audit_retinue_fudge"] = Blend;
    }
}
