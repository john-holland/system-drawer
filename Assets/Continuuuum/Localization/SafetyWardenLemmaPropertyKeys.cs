using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Property keys for {P:safely|riskMin=0.1|safetyMin=0.9} planner hint painting.</summary>
public static class SafetyWardenLemmaPropertyKeys
{
    public const string PlaceholderName = "safely";
    public const string RiskMin = "riskMin";
    public const string RiskMax = "riskMax";
    public const string SafetyMin = "safetyMin";
    public const string SafetyMax = "safetyMax";
}

[Serializable]
public struct SafetyWardenLemmaProperties
{
    public float riskMin01;
    public float riskMax01;
    public float safetyMin01;
    public float safetyMax01;

    public static SafetyWardenLemmaProperties Defaults => new SafetyWardenLemmaProperties
    {
        riskMin01 = float.NaN,
        riskMax01 = float.NaN,
        safetyMin01 = float.NaN,
        safetyMax01 = float.NaN
    };

    public static SafetyWardenLemmaProperties ResolveFromParams(Dictionary<string, string> parameters)
    {
        var p = Defaults;
        if (parameters == null) return p;
        Parse(parameters, SafetyWardenLemmaPropertyKeys.RiskMin, ref p.riskMin01);
        Parse(parameters, SafetyWardenLemmaPropertyKeys.RiskMax, ref p.riskMax01);
        Parse(parameters, SafetyWardenLemmaPropertyKeys.SafetyMin, ref p.safetyMin01);
        Parse(parameters, SafetyWardenLemmaPropertyKeys.SafetyMax, ref p.safetyMax01);
        return p;
    }

    static void Parse(Dictionary<string, string> parameters, string key, ref float field)
    {
        foreach (var kv in parameters)
        {
            if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
            if (float.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                field = v;
            return;
        }
    }
}
