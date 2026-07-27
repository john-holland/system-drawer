using System;
using System.Collections.Generic;

/// <summary>Property keys for {P:wrestling|...} mode / move / professional flags.</summary>
public static class WrestlingLemmaPropertyKeys
{
    public const string PlaceholderName = "wrestling";
    public const string Mode = "mode";
    public const string Move = "move";
    public const string Professional = "pro";
    public const string SpecMode = "wrestling-mode";
    public const string SpecMove = "wrestling-move";
    public const string SpecProfessional = "wrestling-pro";
}

[Serializable]
public struct WrestlingLemmaProperties
{
    public string mode;
    public string move;
    public bool professional;

    public static WrestlingLemmaProperties Defaults => new WrestlingLemmaProperties
    {
        mode = "Play",
        move = "",
        professional = false
    };

    public static WrestlingLemmaProperties ResolveFromParams(Dictionary<string, string> parameters)
    {
        var p = Defaults;
        if (parameters == null) return p;
        if (Try(parameters, WrestlingLemmaPropertyKeys.Mode, out var m))
            p.mode = m;
        if (Try(parameters, WrestlingLemmaPropertyKeys.Move, out var mv))
            p.move = mv;
        if (Try(parameters, WrestlingLemmaPropertyKeys.Professional, out var pro) &&
            (pro == "1" || string.Equals(pro, "true", StringComparison.OrdinalIgnoreCase)))
            p.professional = true;
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
