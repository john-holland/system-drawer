/// <summary>
/// Travel-leg mode gating against <see cref="PhysicalPathingMedium"/> volumes.
/// Lives in Locomotion because <see cref="TravelLegMode"/> cannot be referenced from HierarchicalPathFinding (asmdef cycle).
/// </summary>
public static class PhysicalMediumVolumeRules
{
    /// <summary>Water blocks drive; space blocks walk/drive; air prefers fly. Terminal legs use <see cref="MediumAllowsTerminalLeg"/>.</summary>
    public static bool MediumAllowsMode(PhysicalPathingMedium medium, TravelLegMode mode)
    {
        if (TravelLegModeExtensions.IsTerminalLeg(mode))
            return MediumAllowsTerminalLeg(medium, mode);

        switch (medium)
        {
            case PhysicalPathingMedium.Water:
                return mode != TravelLegMode.Drive;
            case PhysicalPathingMedium.Space:
                return mode == TravelLegMode.Fly;
            case PhysicalPathingMedium.Air:
                return mode != TravelLegMode.Drive;
            default:
                return true;
        }
    }

    public static bool MediumAllowsTerminalLeg(PhysicalPathingMedium medium, TravelLegMode mode)
    {
        switch (mode)
        {
            case TravelLegMode.Park:
            case TravelLegMode.Land:
                return medium == PhysicalPathingMedium.Ground
                    || medium == PhysicalPathingMedium.Unspecified
                    || medium == PhysicalPathingMedium.Air;
            case TravelLegMode.LandWater:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
                return medium == PhysicalPathingMedium.Water || medium == PhysicalPathingMedium.Unspecified;
            case TravelLegMode.Beach:
                return medium == PhysicalPathingMedium.Ground
                    || medium == PhysicalPathingMedium.Water
                    || medium == PhysicalPathingMedium.Unspecified;
            case TravelLegMode.Dock:
                return medium == PhysicalPathingMedium.Space || medium == PhysicalPathingMedium.Unspecified;
            default:
                return true;
        }
    }
}
