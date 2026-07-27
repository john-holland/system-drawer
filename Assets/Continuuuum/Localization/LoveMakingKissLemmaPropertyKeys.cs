using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Property keys for {P:kiss|kiss-animation=...} / peck / smooch / making-out lemma painting.</summary>
public static class LoveMakingKissLemmaPropertyKeys
{
    public const string KissAnimation = "kiss-animation";
    public const string KissAnimationIntensity = "kiss-animation-intensity";
    public const string AliasKissIntensity = "kiss_intensity";
    public const string AliasKissIntensitySpaced = "kiss intensity";
    public const string AliasKissAnimation = "kissAnimation";

    public const float DefaultIntensity = 0.35f;

    public static readonly string[] LemmaPlaceholders =
    {
        "kiss", "peck", "smooch", "smooching", "making-out", "make-out", "making out"
    };
}

[Serializable]
public struct LoveMakingKissLemmaProperties
{
    public string kissAnimation;
    /// <summary>Negative means unset (use lemma default).</summary>
    public float kissAnimationIntensity;
    public string lemmaHint;

    public static LoveMakingKissLemmaProperties Defaults => new LoveMakingKissLemmaProperties
    {
        kissAnimation = "",
        kissAnimationIntensity = -1f,
        lemmaHint = "kiss"
    };

    public static LoveMakingKissLemmaProperties ResolveFromParams(
        Dictionary<string, string> parameters,
        string placeholderName = "kiss")
    {
        var p = Defaults;
        p.lemmaHint = placeholderName ?? "kiss";
        if (parameters == null) return p;

        if (Try(parameters, LoveMakingKissLemmaPropertyKeys.KissAnimation, out var anim) ||
            Try(parameters, LoveMakingKissLemmaPropertyKeys.AliasKissAnimation, out anim))
            p.kissAnimation = anim;

        if (Try(parameters, LoveMakingKissLemmaPropertyKeys.KissAnimationIntensity, out var inten) ||
            Try(parameters, LoveMakingKissLemmaPropertyKeys.AliasKissIntensity, out inten) ||
            Try(parameters, LoveMakingKissLemmaPropertyKeys.AliasKissIntensitySpaced, out inten))
        {
            if (float.TryParse(inten, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float f))
                p.kissAnimationIntensity = Mathf.Clamp01(f);
        }

        return p;
    }

    static bool Try(Dictionary<string, string> p, string key, out string v)
    {
        v = null;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                v = kv.Value;
                return !string.IsNullOrEmpty(v);
            }
        }
        return false;
    }
}
