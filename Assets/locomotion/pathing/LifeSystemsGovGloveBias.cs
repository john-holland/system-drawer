using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seeds/biases life-systems channels from gov-glove / need-aspect society snapshots.
/// Never applies illness — only gentle baseline shifts inside healthy bands.
/// </summary>
public static class LifeSystemsGovGloveBias
{
    /// <summary>
    /// societyFeatures: camelCase or snake_case keys from society snapshot (e.g. healthcareCoverage).
    /// needSatisfied01: aspect_id → satisfied01.
    /// </summary>
    public static void ApplyBaselineBias(
        LifeSystemsSheet sheet,
        IReadOnlyDictionary<string, float> societyFeatures,
        IReadOnlyDictionary<string, float> needSatisfied01)
    {
        if (sheet == null) return;
        sheet.EnsureDefaults();

        var channels = LifeSystemsChannelCatalog.Channels;
        for (int i = 0; i < channels.Count; i++)
        {
            var def = channels[i];
            float bias = 0f;
            int sources = 0;

            if (!string.IsNullOrEmpty(def.societyFeatureKey) &&
                TryFeature(societyFeatures, def.societyFeatureKey, out float feat))
            {
                // Map feature ~0.5..1 toward healthy side of soft band
                float t = Mathf.Clamp01(feat);
                float mid = (def.softBandMin01 + def.softBandMax01) * 0.5f;
                bias += Mathf.Lerp(def.softBandMin01, def.softBandMax01, t) - mid;
                sources++;
            }

            if (!string.IsNullOrEmpty(def.needAspectId) &&
                needSatisfied01 != null &&
                needSatisfied01.TryGetValue(def.needAspectId, out float sat))
            {
                float t = Mathf.Clamp01(sat);
                float mid = (def.softBandMin01 + def.softBandMax01) * 0.5f;
                bias += Mathf.Lerp(def.softBandMin01, def.softBandMax01, t) - mid;
                sources++;
            }

            if (sources == 0) continue;
            bias /= sources;

            float target = Mathf.Clamp(def.setpoint01 + bias * 0.35f, def.softBandMin01, def.softBandMax01);
            // Blend toward target without leaving healthy band — no illness
            float cur = sheet.Get01(def.id);
            sheet.Set01(def.id, Mathf.Lerp(cur, target, 0.5f));
        }
    }

    static bool TryFeature(IReadOnlyDictionary<string, float> map, string key, out float value)
    {
        value = 0f;
        if (map == null || string.IsNullOrEmpty(key)) return false;
        if (map.TryGetValue(key, out value)) return true;
        // snake_case ↔ camelCase soft match
        string snake = ToSnake(key);
        string camel = ToCamel(key);
        if (map.TryGetValue(snake, out value)) return true;
        if (map.TryGetValue(camel, out value)) return true;
        foreach (var kv in map)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kv.Key, snake, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kv.Key, camel, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }

    static string ToSnake(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var chars = new List<char>(s.Length + 4);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('_');
            chars.Add(char.ToLowerInvariant(c));
        }
        return new string(chars.ToArray());
    }

    static string ToCamel(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split('_');
        if (parts.Length == 1) return s;
        var sb = parts[0].ToLowerInvariant();
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            sb += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
        }
        return sb;
    }
}
