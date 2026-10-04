using System;

/// <summary>Property keys for {P:threat|...} alertness lemmas.</summary>
public static class ThreatLemmaPropertyKeys
{
    public const string PlaceholderName = "threat";
    public const string Op = "op";
    public const string Level = "level";
    public const string Alert = "alert";
    public const string Agency = "agency";
    public const string Kind = "kind";
    public const string Lemma = "lemma";

    public const string SpecOp = "threat-op";
    public const string SpecLevel = "threat-level";
    public const string SpecAlert = "threat-alert";
    public const string SpecAgency = "threat-agency";
    public const string SpecKind = "threat-kind";
    public const string SpecLemma = "threat-lemma";

    public static readonly string[] LemmaTags =
    {
        "on-edge", "all-clear", "under-attack", "potential-intruders", "advisory", "elevated"
    };

    public static readonly string[] AllKeys = { Op, Level, Alert, Agency, Kind, Lemma };
}

public enum ThreatLemmaOp
{
    None,
    Raise,
    Clear,
    Query,
    Dialog
}

[Serializable]
public struct ThreatLemmaProperties
{
    public ThreatLemmaOp op;
    public string level;
    public string alert;
    public string agency;
    public string kind;
    public string lemma;

    public static ThreatLemmaProperties ResolveFromParams(System.Collections.Generic.IReadOnlyDictionary<string, string> p)
    {
        var props = new ThreatLemmaProperties();
        if (p == null) return props;
        if (p.TryGetValue(ThreatLemmaPropertyKeys.Op, out var op))
        {
            if (string.Equals(op, "raise", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Raise;
            else if (string.Equals(op, "clear", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Clear;
            else if (string.Equals(op, "query", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Query;
            else if (string.Equals(op, "dialog", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Dialog;
        }
        p.TryGetValue(ThreatLemmaPropertyKeys.Level, out props.level);
        p.TryGetValue(ThreatLemmaPropertyKeys.Alert, out props.alert);
        p.TryGetValue(ThreatLemmaPropertyKeys.Agency, out props.agency);
        p.TryGetValue(ThreatLemmaPropertyKeys.Kind, out props.kind);
        p.TryGetValue(ThreatLemmaPropertyKeys.Lemma, out props.lemma);
        return props;
    }
}
