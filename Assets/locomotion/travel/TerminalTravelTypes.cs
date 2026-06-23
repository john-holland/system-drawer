using UnityEngine;

/// <summary>Hold policy for aquaplane water terminal legs (ParkWater vs Moor).</summary>
public enum WaterHoldPolicy
{
    Park,
    Anchor
}

/// <summary>Authoring surface kind for parking zones (not a parallel fit enum).</summary>
public enum TerminalSurfaceKind
{
    Unspecified,
    GroundPad,
    Runway,
    WaterOpen,
    WaterPlaningPark,
    BeachShore,
    ShipPort
}

/// <summary>Helpers for terminal <see cref="TravelLegMode"/> values.</summary>
public static class TravelLegModeExtensions
{
    public static bool IsTerminalLeg(TravelLegMode mode)
    {
        switch (mode)
        {
            case TravelLegMode.Park:
            case TravelLegMode.Land:
            case TravelLegMode.LandWater:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
            case TravelLegMode.Beach:
            case TravelLegMode.Dock:
                return true;
            default:
                return false;
        }
    }

    public static PathingMode ToPathingMode(TravelLegMode mode)
    {
        switch (mode)
        {
            case TravelLegMode.Fly:
            case TravelLegMode.Land:
            case TravelLegMode.LandWater:
            case TravelLegMode.Dock:
                return PathingMode.Fly;
            case TravelLegMode.Drive:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
            case TravelLegMode.Beach:
                return PathingMode.Drive;
            default:
                return PathingMode.Walk;
        }
    }

    public static PhysicalPathingMedium DefaultMedium(TravelLegMode mode)
    {
        switch (mode)
        {
            case TravelLegMode.LandWater:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
                return PhysicalPathingMedium.Water;
            case TravelLegMode.Dock:
                return PhysicalPathingMedium.Space;
            case TravelLegMode.Land:
                return PhysicalPathingMedium.Ground;
            case TravelLegMode.Beach:
                return PhysicalPathingMedium.Ground;
            default:
                return PhysicalPathingMedium.Ground;
        }
    }

    public static WaterHoldPolicy DefaultHoldPolicy(TravelLegMode mode)
    {
        return mode == TravelLegMode.Moor ? WaterHoldPolicy.Anchor : WaterHoldPolicy.Park;
    }
}
