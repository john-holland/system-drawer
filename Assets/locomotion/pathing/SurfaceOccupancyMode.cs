/// <summary>How an actor occupies a sit surface (chair, stack, wall ledge, books).</summary>
public enum SurfaceOccupancyMode
{
    /// <summary>Pelvis on seat contact; feet may hang free.</summary>
    Sit = 0,
    /// <summary>Feet planted on seat contact; standing on the surface.</summary>
    StandOn = 1
}
