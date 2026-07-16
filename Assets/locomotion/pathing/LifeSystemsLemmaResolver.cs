using System;
using System.Collections.Generic;
using System.Globalization;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Resolves {P:life|...} prompt spans and applies/queries via LifeSystemsServices.</summary>
public static class LifeSystemsLemmaResolver
{
    public static LifeSystemsLemmaProperties ResolveFromSegments(IReadOnlyList<PromptSegment> segments)
    {
        var props = LifeSystemsLemmaProperties.Defaults;
        if (segments == null) return props;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null) continue;
            if (!seg.isPlaceholder) continue;
            if (!string.Equals(seg.placeholderName, LifeSystemsLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase))
                continue;
            ApplyParams(ref props, seg.placeholderParams);
        }
        return props;
    }

    public static void ApplyParams(ref LifeSystemsLemmaProperties props, Dictionary<string, string> parameters)
    {
        if (parameters == null) return;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Op, out var opStr))
            props.op = ParseOp(opStr);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Channel, out var ch))
            props.channel = ch;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Value, out var v))
            float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out props.value);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Delta, out var d))
            float.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out props.delta);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Duration, out var dur))
            float.TryParse(dur, NumberStyles.Float, CultureInfo.InvariantCulture, out props.durationSeconds);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Query, out var q))
            props.query = q;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.OrganId, out var id))
            props.organId = id;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.LifeForce, out var lf))
            float.TryParse(lf, NumberStyles.Float, CultureInfo.InvariantCulture, out props.lifeForce);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.BioRhythm, out var br))
            float.TryParse(br, NumberStyles.Float, CultureInfo.InvariantCulture, out props.bioRhythm);
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Label, out var label))
            props.label = label;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Difficulty, out var diff))
            props.difficulty = diff;
        if (TryGet(parameters, LifeSystemsLemmaPropertyKeys.Raw, out var raw) &&
            (raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)))
            props.useRaw = true;
    }

    public static string Execute(LifeSystemsSheet sheet, LifeSystemsLemmaProperties props)
    {
        if (sheet == null) return "no sheet";
        sheet.EnsureDefaults();
        var svc = LifeSystemsServices.Instance;
        if (svc == null)
        {
            var go = sheet.gameObject;
            svc = go.GetComponent<LifeSystemsServices>() ?? go.AddComponent<LifeSystemsServices>();
        }

        switch (props.op)
        {
            case LifeSystemsLemmaOp.Set:
                if (!string.IsNullOrEmpty(props.difficulty))
                {
                    sheet.difficulty = string.Equals(props.difficulty, "easy", StringComparison.OrdinalIgnoreCase)
                        ? LifeSystemsDifficulty.Easy
                        : LifeSystemsDifficulty.Normal;
                    return $"difficulty={sheet.difficulty}";
                }
                if (!string.IsNullOrEmpty(props.channel))
                {
                    var spec = new LifeSystemsEffectSpec
                    {
                        source = LifeSystemsEffectSource.Lemma,
                        durationSeconds = props.durationSeconds,
                        promptLabel = props.label,
                        channelDeltas = new List<LifeSystemsChannelDelta>
                        {
                            new LifeSystemsChannelDelta
                            {
                                channelId = props.channel,
                                delta01 = props.value - sheet.Get01(props.channel)
                            }
                        }
                    };
                    svc.ApplyEffect(sheet, spec);
                    return $"set {props.channel}={props.value}";
                }
                return "set: missing channel/difficulty";

            case LifeSystemsLemmaOp.Adjust:
                if (string.IsNullOrEmpty(props.channel)) return "adjust: missing channel";
                svc.ApplyEffect(sheet, new LifeSystemsEffectSpec
                {
                    source = LifeSystemsEffectSource.Lemma,
                    durationSeconds = props.durationSeconds,
                    promptLabel = props.label,
                    channelDeltas = new List<LifeSystemsChannelDelta>
                    {
                        new LifeSystemsChannelDelta { channelId = props.channel, delta01 = props.delta }
                    }
                });
                return $"adjust {props.channel}+=${props.delta}";

            case LifeSystemsLemmaOp.Buff:
                svc.ApplyEffect(sheet, new LifeSystemsEffectSpec
                {
                    source = LifeSystemsEffectSource.Supplement,
                    durationSeconds = props.durationSeconds,
                    promptLabel = string.IsNullOrEmpty(props.label) ? "supplement" : props.label,
                    lifeForceDelta = props.lifeForce,
                    bioRhythmAmplitudeDelta = props.bioRhythm
                });
                return $"buff lifeForce+={props.lifeForce}";

            case LifeSystemsLemmaOp.Illness:
                svc.ApplyEffect(sheet, new LifeSystemsEffectSpec
                {
                    source = LifeSystemsEffectSource.Illness,
                    durationSeconds = props.durationSeconds,
                    promptLabel = string.IsNullOrEmpty(props.label) ? "authored-illness" : props.label,
                    channelDeltas = string.IsNullOrEmpty(props.channel)
                        ? new List<LifeSystemsChannelDelta>()
                        : new List<LifeSystemsChannelDelta>
                        {
                            new LifeSystemsChannelDelta { channelId = props.channel, delta01 = props.delta }
                        }
                });
                return $"illness {props.channel}+=${props.delta}";

            case LifeSystemsLemmaOp.Organ:
                if (string.IsNullOrEmpty(props.organId)) return "organ: missing id";
                svc.ApplyEffect(sheet, new LifeSystemsEffectSpec
                {
                    source = LifeSystemsEffectSource.Lemma,
                    durationSeconds = props.durationSeconds,
                    promptLabel = props.label,
                    organDeltas = new List<LifeSystemsOrganDelta>
                    {
                        new LifeSystemsOrganDelta { organId = props.organId, rawDelta = props.delta }
                    }
                });
                float shown = props.useRaw
                    ? sheet.organs.GetRaw(props.organId)
                    : sheet.organs.GetNormalized(props.organId, sheet.difficulty);
                return $"organ {props.organId} rawDelta={props.delta} now={shown:0.00}";

            case LifeSystemsLemmaOp.Query:
            {
                string q = props.query ?? "mood";
                if (string.Equals(q, "organ", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(props.organId))
                    q = "organ:" + props.organId;
                var result = LifeSystemsQuery.Evaluate(sheet, q);
                return result.summary;
            }

            default:
                return "no-op";
        }
    }

    public static string ExecuteFromScript(LifeSystemsSheet sheet, string scriptText)
    {
        var segments = PromptSpanParser.Parse(scriptText ?? "");
        var props = ResolveFromSegments(segments);
        return Execute(sheet, props);
    }

    static LifeSystemsLemmaOp ParseOp(string op)
    {
        if (string.IsNullOrEmpty(op)) return LifeSystemsLemmaOp.None;
        switch (op.Trim().ToLowerInvariant())
        {
            case "set": return LifeSystemsLemmaOp.Set;
            case "adjust": return LifeSystemsLemmaOp.Adjust;
            case "query": return LifeSystemsLemmaOp.Query;
            case "buff": return LifeSystemsLemmaOp.Buff;
            case "illness": return LifeSystemsLemmaOp.Illness;
            case "organ": return LifeSystemsLemmaOp.Organ;
            default: return LifeSystemsLemmaOp.None;
        }
    }

    static bool TryGet(Dictionary<string, string> p, string key, out string value)
    {
        value = null;
        if (p == null) return false;
        if (p.TryGetValue(key, out value))
            return true;
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
