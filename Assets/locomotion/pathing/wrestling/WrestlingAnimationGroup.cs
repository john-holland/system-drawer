/// <summary>Canonical wrestling animation / IK training group tags.</summary>
public static class WrestlingAnimationGroup
{
    public const string Lunge = "wrestling.lunge";
    public const string Pull = "wrestling.pull";
    public const string Push = "wrestling.push";
    public const string Lock = "wrestling.lock";
    public const string Pry = "wrestling.pry";
    public const string Block = "wrestling.block";
    public const string Deflect = "wrestling.deflect";
    public const string Lift = "wrestling.lift";
    public const string Throw = "wrestling.throw";
    public const string DropOn = "wrestling.drop_on";
    public const string Counter = "wrestling.counter";

    public static readonly string[] All =
    {
        Lunge, Pull, Push, Lock, Pry, Block, Deflect, Lift, Throw, DropOn, Counter
    };

    public static string ForMove(WrestlingMoveKind kind, bool professionalStyle = false)
    {
        string tag = kind switch
        {
            WrestlingMoveKind.LungeShootIn => Lunge,
            WrestlingMoveKind.Pull => Pull,
            WrestlingMoveKind.Push => Push,
            WrestlingMoveKind.LockGrapple => Lock,
            WrestlingMoveKind.Pry => Pry,
            WrestlingMoveKind.Block => Block,
            WrestlingMoveKind.Deflect => Deflect,
            WrestlingMoveKind.Lift => Lift,
            WrestlingMoveKind.Throw => Throw,
            WrestlingMoveKind.DropOn => DropOn,
            WrestlingMoveKind.Counter => Counter,
            _ => Lock
        };
        return professionalStyle ? tag + ".pro" : tag;
    }
}
