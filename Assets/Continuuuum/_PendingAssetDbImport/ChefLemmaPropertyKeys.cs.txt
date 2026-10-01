using System;

/// <summary>Property keys for {P:chef|...} / {P:cook|...} lemma painting.</summary>
public static class ChefLemmaPropertyKeys
{
    public const string PlaceholderName = "chef";
    public const string CookPlaceholderName = "cook";
    public const string Op = "op";
    public const string Activity = "activity";
    public const string Mode = "mode";
    public const string Station = "station";
    public const string Item = "item";
    public const string Order = "order";

    public const string SpecOp = "chef-op";
    public const string SpecActivity = "chef-activity";
    public const string SpecMode = "chef-mode";
    public const string SpecStation = "chef-station";
    public const string SpecItem = "chef-item";
    public const string SpecOrder = "chef-order";

    public static readonly string[] AllKeys = { Op, Activity, Mode, Station, Item, Order };
}

public enum ChefLemmaOp
{
    None,
    Duty,
    Activity,
    Wash,
    Ticket
}

[Serializable]
public struct ChefLemmaProperties
{
    public ChefLemmaOp op;
    public string activity;
    public string mode;
    public string station;
    public string item;
    public string orderId;

    public static ChefLemmaProperties ResolveFromParams(System.Collections.Generic.IReadOnlyDictionary<string, string> p)
    {
        var props = new ChefLemmaProperties();
        if (p == null) return props;
        if (p.TryGetValue(ChefLemmaPropertyKeys.Op, out var op))
        {
            if (string.Equals(op, "duty", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Duty;
            else if (string.Equals(op, "activity", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Activity;
            else if (string.Equals(op, "wash", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Wash;
            else if (string.Equals(op, "ticket", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Ticket;
        }
        p.TryGetValue(ChefLemmaPropertyKeys.Activity, out props.activity);
        p.TryGetValue(ChefLemmaPropertyKeys.Mode, out props.mode);
        p.TryGetValue(ChefLemmaPropertyKeys.Station, out props.station);
        p.TryGetValue(ChefLemmaPropertyKeys.Item, out props.item);
        p.TryGetValue(ChefLemmaPropertyKeys.Order, out props.orderId);
        return props;
    }
}
