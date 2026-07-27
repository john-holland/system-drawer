/// <summary>High-level wrestling intent / rule set.</summary>
public enum WrestlingMode
{
    /// <summary>Sport / kayfabe — theatrical spots, pro anim variants.</summary>
    Play,
    /// <summary>Control / restrain — pulls, locks, blocks; lower throw damage.</summary>
    Subdue,
    /// <summary>Hold down for a count — pin pressure and surface gates.</summary>
    Pin
}

/// <summary>Atomic wrestling move kinds for cards, hotkeys, and anim tags.</summary>
public enum WrestlingMoveKind
{
    LungeShootIn,
    Pull,
    Push,
    LockGrapple,
    Pry,
    Block,
    Deflect,
    Lift,
    Throw,
    DropOn,
    Counter
}
