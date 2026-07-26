using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Property keys for {P:stunt|...} planner hint painting.</summary>
public static class StuntmanLemmaPropertyKeys
{
    public const string PlaceholderName = "stunt";
    public const string MaxRisk = "maxRisk";
    public const string MinRisk = "minRisk";
    public const string PreferCrash = "crash";
    public const string AnimGroup = "anim";
}

[Serializable]
public struct StuntmanLemmaProperties
{
    public float maxRisk01;
    public float minRisk01;
    public bool preferCrash;
    public string animGroup;

    public static StuntmanLemmaProperties Defaults => new StuntmanLemmaProperties
    {
        maxRisk01 = float.NaN,
        minRisk01 = float.NaN,
        preferCrash = false
    };

    public static StuntmanLemmaProperties ResolveFromParams(Dictionary<string, string> parameters)
    {
        var p = Defaults;
        if (parameters == null) return p;
        if (Try(parameters, StuntmanLemmaPropertyKeys.MaxRisk, out var mx) &&
            float.TryParse(mx, NumberStyles.Float, CultureInfo.InvariantCulture, out var mxv))
            p.maxRisk01 = mxv;
        if (Try(parameters, StuntmanLemmaPropertyKeys.MinRisk, out var mn) &&
            float.TryParse(mn, NumberStyles.Float, CultureInfo.InvariantCulture, out var mnv))
            p.minRisk01 = mnv;
        if (Try(parameters, StuntmanLemmaPropertyKeys.PreferCrash, out var c) &&
            (c == "1" || string.Equals(c, "true", StringComparison.OrdinalIgnoreCase)))
            p.preferCrash = true;
        if (Try(parameters, StuntmanLemmaPropertyKeys.AnimGroup, out var ag))
            p.animGroup = ag;
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
