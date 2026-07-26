using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>In-memory session mirror of API nsm_fuzzy_variable_cache.</summary>
public static class NsmFuzzyVariableCache
{
    public sealed class Entry
    {
        public string varKey;
        public string varKind;
        public float? grade;
        public string payloadJson;
        public string sourceSpan;
    }

    static readonly Dictionary<string, Dictionary<string, Entry>> Sessions =
        new Dictionary<string, Dictionary<string, Entry>>(StringComparer.Ordinal);

    static Dictionary<string, Entry> Bag(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            sessionId = "default";
        if (!Sessions.TryGetValue(sessionId, out var bag))
        {
            bag = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            Sessions[sessionId] = bag;
        }
        return bag;
    }

    public static void Clear(string sessionId = null)
    {
        if (string.IsNullOrEmpty(sessionId))
            Sessions.Clear();
        else
            Sessions.Remove(sessionId);
    }

    public static Entry Get(string sessionId, string varKey)
    {
        if (string.IsNullOrEmpty(varKey)) return null;
        var bag = Bag(sessionId);
        return bag.TryGetValue(varKey, out var e) ? e : null;
    }

    public static Entry Set(string sessionId, string varKey, string varKind, float? grade = null,
        string payloadJson = null, string sourceSpan = null)
    {
        var bag = Bag(sessionId);
        var e = new Entry
        {
            varKey = varKey,
            varKind = varKind ?? "predicate",
            grade = grade.HasValue ? Mathf.Clamp01(grade.Value) : (float?)null,
            payloadJson = payloadJson,
            sourceSpan = sourceSpan
        };
        bag[varKey] = e;
        return e;
    }

    public static Entry Adjust(string sessionId, string varKey, float? delta = null, string hedgeId = null,
        string createKind = "predicate")
    {
        var cur = Get(sessionId, varKey);
        float baseG = cur?.grade ?? 0.5f;
        float g = baseG;
        if (!string.IsNullOrEmpty(hedgeId))
            g = NsmFuzzyHedgeCurves.Evaluate(hedgeId, baseG);
        else if (delta.HasValue)
            g = Mathf.Clamp01(baseG + delta.Value);
        return Set(sessionId, varKey, cur?.varKind ?? createKind, g, cur?.payloadJson, cur?.sourceSpan);
    }

    public static Entry RememberEvent(string sessionId, string eventKey, float grade = 1f, string payloadJson = null)
    {
        string key = eventKey.StartsWith("event:", StringComparison.OrdinalIgnoreCase)
            ? eventKey
            : "event:" + eventKey;
        var prior = Get(sessionId, key);
        if (prior != null)
            Set(sessionId, "prior:" + key, "similarity_anchor", prior.grade, prior.payloadJson, prior.sourceSpan);
        return Set(sessionId, key, "event", grade, payloadJson);
    }

    public static Entry FindPriorSimilar(string sessionId, string eventKey)
    {
        string key = eventKey.StartsWith("event:", StringComparison.OrdinalIgnoreCase)
            ? eventKey
            : "event:" + eventKey;
        return Get(sessionId, "prior:" + key);
    }

    public static IReadOnlyDictionary<string, Entry> All(string sessionId) => Bag(sessionId);
}
