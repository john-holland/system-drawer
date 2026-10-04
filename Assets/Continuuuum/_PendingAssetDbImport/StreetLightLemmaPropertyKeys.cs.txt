using System;

/// <summary>Lemma keys for describing / controlling street &amp; traffic lights.</summary>
public static class StreetLightLemmaPropertyKeys
{
    public const string PlaceholderName = "street_light";
    public const string TrafficSignal = "traffic_signal";

    public const string ChangedTo = "changed-to";
    public const string Red = "red";
    public const string Green = "green";
    public const string Yellow = "yellow";
    public const string Amber = "amber";

    public const string SpecChangedTo = "street-light-changed-to";
    public const string SpecRed = "street-light-red";
    public const string SpecGreen = "street-light-green";
    public const string SpecYellow = "street-light-yellow";

    public static readonly string[] AllKeys =
    {
        ChangedTo, Red, Green, Yellow, Amber
    };
}

public enum StreetLightLemmaOp
{
    None,
    ChangedTo,
    SetRed,
    SetGreen,
    SetYellow
}

[Serializable]
public struct StreetLightLemmaProperties
{
    public StreetLightLemmaOp op;
    public string color;

    public static StreetLightLemmaProperties Defaults => new StreetLightLemmaProperties
    {
        op = StreetLightLemmaOp.None,
        color = "red"
    };
}
