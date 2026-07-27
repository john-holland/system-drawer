/// <summary>High-level combat intent.</summary>
public enum CombatMode
{
    Cqc,
    Melee,
    Ranged,
    VehicleWeapon,
    Explosive
}

/// <summary>Atomic combat move kinds for cards, wards, and anim tags.</summary>
public enum CombatMoveKind
{
    Strike,
    Block,
    Parry,
    Dodge,
    Aim,
    Fire,
    Reload,
    Throw,
    Slash,
    Stab,
    GrappleBreak,
    Suppress
}
