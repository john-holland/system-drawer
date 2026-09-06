/// <summary>Property keys for {P:door|...} carpentry / angular lemmas.</summary>
public static class DoorCarpentryLemmaPropertyKeys
{
    public const string PlaceholderName = "door";
    public const string TopRail = "top-rail";
    public const string BottomRail = "bottom-rail";
    public const string LockStile = "lock-stile";
    public const string MiddleRail = "middle-rail";
    public const string FriezeRail = "frieze-rail";
    public const string LockRail = "lock-rail";
    public const string Mullion = "mullion";
    public const string Moulding = "moulding";
    public const string MouldingSides = "moulding-sides";
    public const string StilePerpRailDeg = "stile-perp-rail-deg";
    public const string MullionParallelStileDeg = "mullion-parallel-stile-deg";
    public const string SlideMeters = "slide-meters";
    public const string OpenAngleDeg = "open-angle-deg";
    public const string LemmaPlaceStile = "place-stile";
    public const string LemmaPlaceRail = "place-rail";
    public const string LemmaPackPanels = "pack-panels";
    public const string LemmaWrapMoulding = "wrap-moulding";

    public static readonly string[] AllKeys =
    {
        TopRail, BottomRail, LockStile, MiddleRail, FriezeRail, LockRail, Mullion, Moulding,
        MouldingSides, StilePerpRailDeg, MullionParallelStileDeg, SlideMeters, OpenAngleDeg,
        LemmaPlaceStile, LemmaPlaceRail, LemmaPackPanels, LemmaWrapMoulding
    };

    public const float DefaultStilePerpRailDeg = 90f;
    public const float DefaultMullionParallelStileDeg = 0f;
}
