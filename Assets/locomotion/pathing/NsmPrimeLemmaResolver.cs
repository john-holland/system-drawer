using System;
using System.Collections.Generic;
using System.Globalization;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Shared {P:nsm|term=...} dispatcher for all 65 NSM primes (group handlers, not per-word classes).
/// </summary>
public static class NsmPrimeLemmaResolver
{
    public sealed class ExecutionContext
    {
        public string SessionId = "default";
        public LifeSystemsSheet LifeSheet;
        public Dictionary<string, string> Flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public float PreferenceBias;
        public float ScaleBias = 1f;
        public string LastSpatialRelation;
        public string LastStatus;
    }

    static readonly Dictionary<string, string> GroupByTerm =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "I", "substantive" }, { "you", "substantive" }, { "someone", "substantive" },
            { "something", "substantive" }, { "people", "substantive" }, { "body", "substantive" },
            { "kind", "relational" }, { "part", "relational" },
            { "this", "determiner" }, { "the-same", "determiner" }, { "other", "determiner" },
            { "one", "quantifier" }, { "two", "quantifier" }, { "some", "quantifier" },
            { "all", "quantifier" }, { "much", "quantifier" }, { "little", "quantifier" },
            { "good", "evaluator" }, { "bad", "evaluator" },
            { "big", "descriptor" }, { "small", "descriptor" },
            { "know", "mental" }, { "think", "mental" }, { "want", "mental" },
            { "dont-want", "mental" }, { "feel", "mental" }, { "see", "mental" }, { "hear", "mental" },
            { "say", "speech" }, { "words", "speech" }, { "true", "speech" },
            { "do", "action" }, { "happen", "action" }, { "move", "action" }, { "touch", "action" },
            { "be-somewhere", "existence" }, { "there-is", "existence" },
            { "be-someone", "existence" }, { "have", "existence" },
            { "live", "life" }, { "die", "life" },
            { "when", "time" }, { "now", "time" }, { "before", "time" }, { "after", "time" },
            { "a-long-time", "time" }, { "a-short-time", "time" },
            { "for-some-time", "time" }, { "moment", "time" },
            { "where", "space" }, { "here", "space" }, { "above", "space" }, { "below", "space" },
            { "far", "space" }, { "near", "space" }, { "side", "space" }, { "inside", "space" },
            { "not", "logical" }, { "maybe", "logical" }, { "can", "logical" },
            { "because", "logical" }, { "if", "logical" },
            { "very", "intensifier" }, { "more", "intensifier" },
            { "like", "similarity" },
        };

    public static readonly HashSet<string> CausalityDiscourseTerms =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "if", "because", "not", "when", "before", "after", "maybe", "can"
        };

    public static bool IsKnownPrime(string term) =>
        !string.IsNullOrEmpty(term) && GroupByTerm.ContainsKey(term);

    public static IReadOnlyCollection<string> AllPrimeTerms => GroupByTerm.Keys;

    public static string GroupOf(string term) =>
        GroupByTerm.TryGetValue(term ?? "", out var g) ? g : "";

    public static NsmPrimeLemmaProperties ResolveFromSegments(IReadOnlyList<PromptSegment> segments)
    {
        var props = NsmPrimeLemmaProperties.Defaults;
        if (segments == null) return props;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            if (!string.Equals(seg.placeholderName, NsmLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase))
                continue;
            ApplyParams(ref props, seg.placeholderParams);
        }
        return props;
    }

    public static void ApplyParams(ref NsmPrimeLemmaProperties props, Dictionary<string, string> parameters)
    {
        if (parameters == null) return;
        if (TryGet(parameters, NsmLemmaPropertyKeys.Term, out var term))
        {
            props.term = term;
            props.group = GroupOf(term);
        }
        if (TryGet(parameters, NsmLemmaPropertyKeys.Op, out var opStr))
            props.op = ParseOp(opStr);
        if (TryGet(parameters, NsmLemmaPropertyKeys.Session, out var session))
            props.sessionId = session;
        if (TryGet(parameters, NsmLemmaPropertyKeys.VarKey, out var vk))
            props.varKey = vk;
        if (TryGet(parameters, NsmLemmaPropertyKeys.Hedge, out var hedge))
            props.hedgeId = hedge;
        if (TryGet(parameters, NsmLemmaPropertyKeys.EventKey, out var ek))
            props.eventKey = ek;
        if (TryGet(parameters, NsmLemmaPropertyKeys.Grade, out var g) &&
            float.TryParse(g, NumberStyles.Float, CultureInfo.InvariantCulture, out var gf))
            props.grade = gf;
        if (TryGet(parameters, NsmLemmaPropertyKeys.Delta, out var d) &&
            float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out var df))
            props.delta = df;
    }

    static NsmPrimeLemmaOp ParseOp(string op)
    {
        if (string.IsNullOrEmpty(op)) return NsmPrimeLemmaOp.None;
        switch (op.Trim().ToLowerInvariant())
        {
            case "evaluate": case "eval": return NsmPrimeLemmaOp.Evaluate;
            case "bind": return NsmPrimeLemmaOp.Bind;
            case "adjust": return NsmPrimeLemmaOp.Adjust;
            case "remember": case "event": return NsmPrimeLemmaOp.RememberEvent;
            case "prior": case "queryprior": return NsmPrimeLemmaOp.QueryPrior;
            case "discourse": return NsmPrimeLemmaOp.Discourse;
            default: return NsmPrimeLemmaOp.Evaluate;
        }
    }

    public static float EvaluateFuzzy(string hedgeId, float x) =>
        NsmFuzzyHedgeCurves.Evaluate(hedgeId, x);

    public static string Execute(NsmPrimeLemmaProperties props, ExecutionContext ctx = null)
    {
        ctx ??= new ExecutionContext();
        if (!string.IsNullOrEmpty(props.sessionId))
            ctx.SessionId = props.sessionId;
        if (string.IsNullOrEmpty(props.term))
            return ctx.LastStatus = "nsm: missing term";
        if (!IsKnownPrime(props.term))
            return ctx.LastStatus = "nsm: unknown prime " + props.term;

        string group = string.IsNullOrEmpty(props.group) ? GroupOf(props.term) : props.group;
        string status;
        switch (group)
        {
            case "substantive": status = HandleSubstantive(props, ctx); break;
            case "relational": status = HandleRelational(props, ctx); break;
            case "determiner": status = HandleDeterminer(props, ctx); break;
            case "quantifier": status = HandleQuantifier(props, ctx); break;
            case "evaluator": status = HandleEvaluator(props, ctx); break;
            case "descriptor": status = HandleDescriptor(props, ctx); break;
            case "mental": status = HandleMental(props, ctx); break;
            case "speech": status = HandleSpeech(props, ctx); break;
            case "action": status = HandleAction(props, ctx); break;
            case "existence": status = HandleExistence(props, ctx); break;
            case "life": status = HandleLife(props, ctx); break;
            case "time": status = HandleTime(props, ctx); break;
            case "space": status = HandleSpace(props, ctx); break;
            case "logical": status = HandleLogical(props, ctx); break;
            case "intensifier": status = HandleIntensifier(props, ctx); break;
            case "similarity": status = HandleSimilarity(props, ctx); break;
            default: status = "nsm: unhandled group " + group; break;
        }
        return ctx.LastStatus = status;
    }

    static string HandleSubstantive(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        string key = "referent:" + props.term.ToLowerInvariant();
        NsmFuzzyVariableCache.Set(ctx.SessionId, key, "referent", props.grade, props.term);
        ctx.Flags["referent"] = props.term;
        return $"substantive bind {key}={props.grade:0.##}";
    }

    static string HandleRelational(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        string key = "rel:" + props.term.ToLowerInvariant();
        NsmFuzzyVariableCache.Set(ctx.SessionId, key, "predicate", props.grade, props.varKey);
        ctx.Flags["relation"] = props.term;
        return $"relational {props.term}";
    }

    static string HandleDeterminer(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["deictic"] = props.term;
        NsmFuzzyVariableCache.Set(ctx.SessionId, "deictic:" + props.term, "referent", 1f);
        return "determiner " + props.term;
    }

    static string HandleQuantifier(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        float g = props.grade;
        if (string.Equals(props.term, "one", StringComparison.OrdinalIgnoreCase)) g = 1f / 10f;
        else if (string.Equals(props.term, "two", StringComparison.OrdinalIgnoreCase)) g = 2f / 10f;
        else if (string.Equals(props.term, "all", StringComparison.OrdinalIgnoreCase)) g = 1f;
        else if (string.Equals(props.term, "some", StringComparison.OrdinalIgnoreCase)) g = 0.4f;
        else if (string.Equals(props.term, "much", StringComparison.OrdinalIgnoreCase)) g = EvaluateFuzzy("much", props.grade);
        else if (string.Equals(props.term, "little", StringComparison.OrdinalIgnoreCase)) g = EvaluateFuzzy("little", props.grade);
        string key = string.IsNullOrEmpty(props.varKey) ? "qty:" + props.term : props.varKey;
        NsmFuzzyVariableCache.Set(ctx.SessionId, key, "predicate", g);
        return $"quantifier {props.term}={g:0.##}";
    }

    static string HandleEvaluator(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        float bias = string.Equals(props.term, "good", StringComparison.OrdinalIgnoreCase) ? props.grade : -props.grade;
        ctx.PreferenceBias += bias;
        NsmFuzzyVariableCache.Set(ctx.SessionId, "eval:" + props.term, "predicate", props.grade);
        return $"evaluator {props.term} bias={ctx.PreferenceBias:0.##}";
    }

    static string HandleDescriptor(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        float scale = string.Equals(props.term, "big", StringComparison.OrdinalIgnoreCase)
            ? 1f + props.grade
            : Mathf.Max(0.1f, 1f - props.grade);
        ctx.ScaleBias *= scale;
        NsmFuzzyVariableCache.Set(ctx.SessionId, "desc:" + props.term, "predicate", props.grade);
        return $"descriptor {props.term} scale={ctx.ScaleBias:0.##}";
    }

    static string HandleMental(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["mental:" + props.term] = "1";
        float g = props.grade;
        if (string.Equals(props.term, "dont-want", StringComparison.OrdinalIgnoreCase))
            g = 1f - props.grade;
        NsmFuzzyVariableCache.Set(ctx.SessionId, "mental:" + props.term, "predicate", g);
        return $"mental {props.term}={g:0.##}";
    }

    static string HandleSpeech(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["speech:" + props.term] = "1";
        float g = string.Equals(props.term, "true", StringComparison.OrdinalIgnoreCase) ? props.grade : 1f;
        NsmFuzzyVariableCache.Set(ctx.SessionId, "speech:" + props.term, "predicate", g);
        return $"speech {props.term}";
    }

    static string HandleAction(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        string ek = string.IsNullOrEmpty(props.eventKey) ? props.term : props.eventKey;
        NsmFuzzyVariableCache.RememberEvent(ctx.SessionId, ek, props.grade);
        ctx.Flags["action"] = props.term;
        if (string.Equals(props.term, "move", StringComparison.OrdinalIgnoreCase))
            ctx.Flags["travel:move"] = "1";
        if (string.Equals(props.term, "touch", StringComparison.OrdinalIgnoreCase))
            ctx.Flags["contact"] = "1";
        return $"action {props.term} event={ek}";
    }

    static string HandleExistence(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["existence:" + props.term] = "1";
        NsmFuzzyVariableCache.Set(ctx.SessionId, "existence:" + props.term, "predicate", props.grade);
        return $"existence {props.term}";
    }

    static string HandleLife(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        if (ctx.LifeSheet != null)
        {
            var lifeProps = LifeSystemsLemmaProperties.Defaults;
            lifeProps.op = LifeSystemsLemmaOp.Adjust;
            lifeProps.channel = "life_force";
            lifeProps.delta = string.Equals(props.term, "live", StringComparison.OrdinalIgnoreCase)
                ? Mathf.Abs(props.delta > 0 ? props.delta : 0.1f)
                : -Mathf.Abs(props.delta > 0 ? props.delta : 0.5f);
            lifeProps.label = "nsm:" + props.term;
            return "life " + LifeSystemsLemmaResolver.Execute(ctx.LifeSheet, lifeProps);
        }
        NsmFuzzyVariableCache.Set(ctx.SessionId, "life:" + props.term, "predicate",
            string.Equals(props.term, "live", StringComparison.OrdinalIgnoreCase) ? 1f : 0f);
        return "life " + props.term + " (no sheet)";
    }

    static string HandleTime(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["time:" + props.term] = "1";
        NsmFuzzyVariableCache.Set(ctx.SessionId, "time:" + props.term, "predicate", props.grade);
        if (string.Equals(props.term, "before", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(props.eventKey))
        {
            var prior = NsmFuzzyVariableCache.FindPriorSimilar(ctx.SessionId, props.eventKey);
            return prior != null
                ? $"time before prior={prior.varKey} grade={prior.grade}"
                : "time before (no prior)";
        }
        if (string.Equals(props.term, "now", StringComparison.OrdinalIgnoreCase))
            ctx.Flags["temporal_anchor"] = "now";
        float dur = 0f;
        if (props.term.IndexOf("long", StringComparison.OrdinalIgnoreCase) >= 0) dur = 0.9f;
        else if (props.term.IndexOf("short", StringComparison.OrdinalIgnoreCase) >= 0) dur = 0.2f;
        else if (string.Equals(props.term, "moment", StringComparison.OrdinalIgnoreCase)) dur = 0.05f;
        else if (props.term.IndexOf("some-time", StringComparison.OrdinalIgnoreCase) >= 0) dur = 0.5f;
        if (dur > 0f)
            NsmFuzzyVariableCache.Set(ctx.SessionId, "duration", "predicate", dur);
        return "time " + props.term;
    }

    static string HandleSpace(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.LastSpatialRelation = props.term;
        ctx.Flags["space:" + props.term] = "1";
        NsmFuzzyVariableCache.Set(ctx.SessionId, "space:" + props.term, "predicate", props.grade);
        // LayoutSpatialRelation lives in Narrative.Inference (refs this assembly) — map locally to avoid cycle.
        if (TryMapSpaceRelation(props.term, out string layoutRel))
            ctx.Flags["layoutRelation"] = layoutRel;
        return "space " + props.term;
    }

    static bool TryMapSpaceRelation(string term, out string layoutRelation)
    {
        layoutRelation = null;
        if (string.IsNullOrEmpty(term)) return false;
        switch (term.Trim().ToLowerInvariant())
        {
            case "here":
            case "where":
            case "near":
                layoutRelation = "Near";
                return true;
            case "inside":
                layoutRelation = "Inside";
                return true;
            case "above":
                layoutRelation = "Above";
                return true;
            case "below":
                layoutRelation = "Below";
                return true;
            case "far":
                layoutRelation = "Far";
                return true;
            case "side":
                layoutRelation = "Side";
                return true;
            default:
                return false;
        }
    }

    static string HandleLogical(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        ctx.Flags["logical:" + props.term] = "1";
        ctx.Flags["discourse_splitter"] = props.term;
        float g = props.grade;
        if (string.Equals(props.term, "not", StringComparison.OrdinalIgnoreCase))
            g = 1f - props.grade;
        else if (string.Equals(props.term, "maybe", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(props.term, "can", StringComparison.OrdinalIgnoreCase))
            g = EvaluateFuzzy("maybe", props.grade);
        NsmFuzzyVariableCache.Set(ctx.SessionId, "logical:" + props.term, "predicate", g);
        return $"logical {props.term}={g:0.##}";
    }

    static string HandleIntensifier(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        string key = string.IsNullOrEmpty(props.varKey) ? "intensity" : props.varKey;
        var e = NsmFuzzyVariableCache.Adjust(ctx.SessionId, key, hedgeId: props.term);
        return $"intensifier {props.term} {key}={e.grade:0.##}";
    }

    static string HandleSimilarity(NsmPrimeLemmaProperties props, ExecutionContext ctx)
    {
        string ek = string.IsNullOrEmpty(props.eventKey) ? props.varKey : props.eventKey;
        float sim = EvaluateFuzzy(
            string.IsNullOrEmpty(props.hedgeId) ? "just-like" : props.hedgeId,
            props.grade);
        if (!string.IsNullOrEmpty(ek))
        {
            var prior = NsmFuzzyVariableCache.FindPriorSimilar(ctx.SessionId, ek);
            if (prior?.grade != null)
                sim = Mathf.Min(sim, prior.grade.Value);
            NsmFuzzyVariableCache.Set(ctx.SessionId, "similarity:" + ek, "similarity_anchor", sim);
        }
        ctx.Flags["like"] = sim.ToString("0.###", CultureInfo.InvariantCulture);
        return $"similarity like={sim:0.##}";
    }

    static bool TryGet(Dictionary<string, string> parameters, string key, out string value)
    {
        value = null;
        if (parameters == null) return false;
        foreach (var kv in parameters)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return !string.IsNullOrEmpty(value);
            }
        }
        return false;
    }
}
