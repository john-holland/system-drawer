/// <summary>Canonical combat animation / IK training group tags.</summary>
public static class CombatAnimationGroup
{
    public const string Strike = "combat.strike";
    public const string Block = "combat.block";
    public const string Parry = "combat.parry";
    public const string Dodge = "combat.dodge";
    public const string Aim = "combat.aim";
    public const string Fire = "combat.fire";
    public const string Reload = "combat.reload";
    public const string Throw = "combat.throw";
    public const string Slash = "combat.slash";
    public const string Stab = "combat.stab";
    public const string GrappleBreak = "combat.grapple_break";
    public const string Suppress = "combat.suppress";

    public static string ForMove(CombatMoveKind kind)
    {
        return kind switch
        {
            CombatMoveKind.Strike => Strike,
            CombatMoveKind.Block => Block,
            CombatMoveKind.Parry => Parry,
            CombatMoveKind.Dodge => Dodge,
            CombatMoveKind.Aim => Aim,
            CombatMoveKind.Fire => Fire,
            CombatMoveKind.Reload => Reload,
            CombatMoveKind.Throw => Throw,
            CombatMoveKind.Slash => Slash,
            CombatMoveKind.Stab => Stab,
            CombatMoveKind.GrappleBreak => GrappleBreak,
            CombatMoveKind.Suppress => Suppress,
            _ => Strike
        };
    }
}
