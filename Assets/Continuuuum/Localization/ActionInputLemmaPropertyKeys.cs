using System;
using System.Collections.Generic;

/// <summary>Property keys for {P:action|id=jump|maps-to=x} / keymap lemma painting.</summary>
public static class ActionInputLemmaPropertyKeys
{
    public const string Id = "id";
    public const string AliasAction = "action";
    public const string MapsTo = "maps-to";
    public const string AliasTo = "to";
    public const string AliasMap = "map";
    public const string Subscribe = "subscribe";
    public const string AliasEdge = "edge";
    public const string AliasOn = "on";
    public const string AndMapsTo = "and-maps-to";
    public const string AliasAlso = "also";
    public const string Clear = "clear";

    public static readonly string[] LemmaPlaceholders =
    {
        "action", "keymap", "maps"
    };
}

public enum ActionInputSubscribeMode
{
    KeyDown,
    KeyUp,
    Held,
    Axis
}

[Serializable]
public struct ActionInputLemmaProperties
{
    public string actionId;
    public string mapsTo;
    public string andMapsTo;
    public ActionInputSubscribeMode subscribe;
    public bool clear;
    public string lemmaHint;

    public static ActionInputLemmaProperties Defaults => new ActionInputLemmaProperties
    {
        actionId = "",
        mapsTo = "",
        andMapsTo = "",
        subscribe = ActionInputSubscribeMode.KeyDown,
        clear = false,
        lemmaHint = "action"
    };

    public static ActionInputLemmaProperties ResolveFromParams(
        Dictionary<string, string> parameters,
        string placeholderName = "action")
    {
        var p = Defaults;
        p.lemmaHint = placeholderName ?? "action";
        if (parameters == null) return p;

        if (Try(parameters, ActionInputLemmaPropertyKeys.Id, out var id) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasAction, out id))
            p.actionId = id;

        if (Try(parameters, ActionInputLemmaPropertyKeys.MapsTo, out var maps) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasTo, out maps) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasMap, out maps))
            p.mapsTo = maps;

        if (Try(parameters, ActionInputLemmaPropertyKeys.AndMapsTo, out var also) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasAlso, out also))
            p.andMapsTo = also;

        if (Try(parameters, ActionInputLemmaPropertyKeys.Subscribe, out var sub) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasEdge, out sub) ||
            Try(parameters, ActionInputLemmaPropertyKeys.AliasOn, out sub))
            p.subscribe = ParseSubscribe(sub);

        if (Try(parameters, ActionInputLemmaPropertyKeys.Clear, out var clr))
            p.clear = ParseBool(clr);

        return p;
    }

    public static ActionInputSubscribeMode ParseSubscribe(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ActionInputSubscribeMode.KeyDown;
        string n = NormalizeToken(raw);
        switch (n)
        {
            case "KEY_UP":
            case "UP":
            case "RELEASE":
            case "RELEASED":
                return ActionInputSubscribeMode.KeyUp;
            case "KEY_HELD":
            case "HELD":
            case "HOLD":
            case "DOWN_HELD":
                return ActionInputSubscribeMode.Held;
            case "AXIS":
            case "ANALOG":
                return ActionInputSubscribeMode.Axis;
            default:
                return ActionInputSubscribeMode.KeyDown;
        }
    }

    public static string NormalizeToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return raw.Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
    }

    static bool ParseBool(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return raw == "1" ||
               string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
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
