using System;

public static class InventoryLemmaPropertyKeys
{
    public const string PlaceholderName = "have";
    public const string Op = "op";
    public const string Item = "item";
    public const string From = "from";
    public const string To = "to";
}

public enum InventoryLemmaOp
{
    None,
    Have,
    Assert,
    Give,
    Take,
    Transfer
}

public struct InventoryLemmaProperties
{
    public InventoryLemmaOp op;
    public string item;
    public string fromActorId;
    public string toActorId;

    public static InventoryLemmaProperties Defaults => new InventoryLemmaProperties
    {
        op = InventoryLemmaOp.Have
    };
}

public static class WaypointLemmaPropertyKeys
{
    public const string PlaceholderName = "waypoint";
    public const string Name = "name";
    public const string X = "x";
    public const string Y = "y";
    public const string Z = "z";
    public const string From = "from";
    public const string To = "to";
    public const string Formation = "formation";
    public const string Vec = "v";
}

public static class FormationLemmaPropertyKeys
{
    public const string PlaceholderName = "formation";
    public const string Id = "id";
    public const string From = "from";
    public const string To = "to";
}
