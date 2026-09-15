/// <summary>Shelf / mill / farm commodity keys for clothing venues.</summary>
public static class ClothingCommodities
{
    public const string Fiber = "fiber";
    public const string Thread = "thread";
    public const string Bolt = "bolt";
    public const string Dye = "dye";
    public const string Stuffing = "stuffing";
    public const string Garment = "garment";

    public static readonly string[] All =
    {
        Fiber, Thread, Bolt, Dye, Stuffing, Garment
    };

    public static readonly string[] FiberSpecies = { "flax", "hemp", "cotton" };
}
