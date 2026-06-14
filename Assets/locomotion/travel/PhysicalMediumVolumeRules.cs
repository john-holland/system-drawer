/// <summary>
/// Travel-leg mode gating against <see cref="PhysicalPathingMedium"/> volumes.
/// Lives in Locomotion because <see cref="TravelLegMode"/> cannot be referenced from HierarchicalPathFinding (asmdef cycle).
/// </summary>
public static class PhysicalMediumVolumeRules
{
    /// <summary>Water blocks drive; space blocks walk/drive; air prefers fly.</summary>
    public static bool MediumAllowsMode(PhysicalPathingMedium medium, TravelLegMode mode)
    {
        switch (medium)
        {
            // todo: review: water should be fine with driving if top speed can reach
            // potential water walking mu's, we should add options to use the water plane solver
            // in the travel agent for driving vehicles. 
            // Also, submersibles or amphibious are something to think about
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
}
