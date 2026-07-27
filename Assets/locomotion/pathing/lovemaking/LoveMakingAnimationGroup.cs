/// <summary>Canonical love-making animation / IK training group tags.</summary>
public static class LoveMakingAnimationGroup
{
    public const string Approach = "lovemaking.approach";
    public const string Embrace = "lovemaking.embrace";
    public const string Kiss = "lovemaking.kiss";
    public const string Hold = "lovemaking.hold";
    public const string Caress = "lovemaking.caress";
    public const string Nuzzle = "lovemaking.nuzzle";
    public const string DanceClose = "lovemaking.dance_close";
    public const string Part = "lovemaking.part";

    public static readonly string[] All =
    {
        Approach, Embrace, Kiss, Hold, Caress, Nuzzle, DanceClose, Part
    };

    public static string ForMove(LoveMakingMoveKind kind, bool intimateStyle = false)
    {
        string tag = kind switch
        {
            LoveMakingMoveKind.Approach => Approach,
            LoveMakingMoveKind.Embrace => Embrace,
            LoveMakingMoveKind.Kiss => Kiss,
            LoveMakingMoveKind.Hold => Hold,
            LoveMakingMoveKind.Caress => Caress,
            LoveMakingMoveKind.Nuzzle => Nuzzle,
            LoveMakingMoveKind.DanceClose => DanceClose,
            LoveMakingMoveKind.Part => Part,
            _ => Embrace
        };
        return intimateStyle ? tag + ".intimate" : tag;
    }
}
