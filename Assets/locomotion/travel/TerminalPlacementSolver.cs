using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves terminal Park/Land/Moor legs using Bedoga slot layout and hierarchical pathing reachability.
/// </summary>
public static class TerminalPlacementSolver
{
    const float WCentroid = 0.35f;
    const float WGravity = 0.25f;
    const float WPlaning = 0.4f;
    const int MaxSlotsPerZone = 32;

    public static bool TryResolveTerminalLeg(
        Vector3 approachStart,
        Vector3 goalHint,
        ActorPhysicalProfile profile,
        TravelLegMode terminalMode,
        HierarchicalPathingSolver pathingSolver,
        IReadOnlyList<ParkingZoneVolume> candidateZones,
        out MultiModalSegment terminalLeg,
        IGravitySampleProvider gravityProvider = null)
    {
        terminalLeg = null;
        if (pathingSolver == null)
            return false;

        if (candidateZones == null || candidateZones.Count == 0)
            candidateZones = ParkingZoneIndex.QueryNearGoal(goalHint, 80f, terminalMode);

        gravityProvider ??= ChainedGravitySampleProvider.CreateDefault(pathingSolver);

        float bestScore = float.MaxValue;
        MultiModalSegment best = null;
        PathingMode savedMode = pathingSolver.pathingMode;

        try
        {
            pathingSolver.pathingMode = TravelLegModeExtensions.ToPathingMode(terminalMode);

            for (int z = 0; z < candidateZones.Count; z++)
            {
                ParkingZoneVolume zone = candidateZones[z];
                if (zone == null || !zone.AllowsLeg(terminalMode))
                    continue;

                TravelLegMode legForZone = ActorPhysicalCentroid.ResolveTerminalLegFromZone(
                    terminalMode, zone.terminalSurfaceKind, profile);
                if (legForZone != terminalMode && terminalMode != TravelLegMode.Walk)
                    continue;

                Bounds searchBounds = zone.GetWorldBounds();
                PlacementSlotConfig slotConfig = zone.GetPlacementSlotConfig();
                Vector3 minSpace = profile.minSpace;
                Vector3 optimalSpace = profile.optimalSpace;

                for (int slot = 0; slot < MaxSlotsPerZone; slot++)
                {
                    if (!PlacementSlotConfig.ComputeSlotCenter3D(
                            searchBounds, optimalSpace, minSpace, slot, slotConfig, out Vector3 slotCenter))
                        break;

                    if (!ValidateSurface(legForZone, zone, profile, slotCenter, approachStart, out Vector3 adjustedCenter))
                        continue;

                    if (pathingSolver.IsBlockedAtWorld(adjustedCenter))
                        continue;

                    List<Vector3> path = pathingSolver.FindPath(approachStart, adjustedCenter);
                    if (path == null || path.Count == 0)
                        continue;

                    float pathLen = PathLength(path);
                    GravitySample grav = gravityProvider.Sample(adjustedCenter);
                    float score = pathLen
                        + WCentroid * Vector3.Distance(adjustedCenter, goalHint)
                        + WGravity * (1f - Mathf.Clamp01(Vector3.Dot(grav.up, Vector3.up)))
                        + PlaningPenalty(legForZone, zone, profile, approachStart, adjustedCenter);

                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    best = BuildSegment(legForZone, path, adjustedCenter, slot, slotConfig, zone, profile, grav, score);
                }
            }
        }
        finally
        {
            pathingSolver.pathingMode = savedMode;
        }

        if (best == null)
            return false;

        terminalLeg = best;
        return true;
    }

    public static bool TryAutoResolveTerminalLeg(
        Vector3 approachStart,
        Vector3 goalHint,
        ActorPhysicalProfile profile,
        HierarchicalPathingSolver pathingSolver,
        float searchRadius,
        out MultiModalSegment terminalLeg)
    {
        TravelLegMode mode = profile.defaultTerminalLeg;
        var zones = ParkingZoneIndex.QueryNear(goalHint, searchRadius);
        return TryResolveTerminalLeg(approachStart, goalHint, profile, mode, pathingSolver, zones, out terminalLeg);
    }

    static MultiModalSegment BuildSegment(
        TravelLegMode mode,
        List<Vector3> path,
        Vector3 centroid,
        int slotIndex,
        PlacementSlotConfig slotConfig,
        ParkingZoneVolume zone,
        ActorPhysicalProfile profile,
        GravitySample grav,
        float score)
    {
        MultiModalSegment seg = MultiModalSegment.FromTerminal(mode, path, centroid);
        seg.terminalSlotIndex = slotIndex;
        seg.placementSlotConfig = slotConfig;
        seg.parkingZoneRef = zone;
        seg.terminalUpWorld = grav.up;
        seg.terminalSurfaceNormal = grav.up;
        seg.terminalScore = score;
        seg.terminalSlotBounds = new Bounds(centroid, profile.optimalSpace);
        seg.terminalHoldPolicy = mode == TravelLegMode.Moor ? WaterHoldPolicy.Anchor : TravelLegModeExtensions.DefaultHoldPolicy(mode);

        if (mode == TravelLegMode.ParkWater || mode == TravelLegMode.Moor || mode == TravelLegMode.LandWater)
        {
            seg.terminalWaterSurfaceY = float.IsNaN(centroid.y) ? centroid.y : centroid.y;
            seg.terminalPlaningSpeed = profile.planingSpeedMax > 0f
                ? profile.planingSpeedMax
                : 12f;
        }

        if (mode == TravelLegMode.Beach && path.Count >= 2)
        {
            Vector3 shallow = path[path.Count - 2];
            shallow.y = seg.terminalWaterSurfaceY;
            path[path.Count - 2] = shallow;
        }

        return seg;
    }

    static bool ValidateSurface(
        TravelLegMode mode,
        ParkingZoneVolume zone,
        ActorPhysicalProfile profile,
        Vector3 slotCenter,
        Vector3 approachStart,
        out Vector3 adjustedCenter)
    {
        adjustedCenter = slotCenter;

        switch (mode)
        {
            case TravelLegMode.LandWater:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
                if (zone.medium != PhysicalPathingMedium.Water
                    && zone.terminalSurfaceKind != TerminalSurfaceKind.WaterOpen
                    && zone.terminalSurfaceKind != TerminalSurfaceKind.WaterPlaningPark)
                    return false;
                adjustedCenter.y = ResolveWaterY(slotCenter, zone);
                if (mode == TravelLegMode.ParkWater || mode == TravelLegMode.Moor)
                {
                    float runOut = Vector3.Distance(
                        new Vector3(approachStart.x, adjustedCenter.y, approachStart.z),
                        new Vector3(adjustedCenter.x, adjustedCenter.y, adjustedCenter.z));
                    float need = Mathf.Max(zone.minPlaningRunOutMeters, profile.decelerationDistanceEstimate);
                    if (runOut < need * 0.5f)
                        return false;
                }
                return true;

            case TravelLegMode.Beach:
                if (zone.terminalSurfaceKind == TerminalSurfaceKind.BeachShore)
                {
                    float slope = zone.maxShoreSlopeDegrees;
                    if (slope > profile.maxBeachSlopeDegrees && profile.maxBeachSlopeDegrees > 0f)
                        return false;
                }
                return true;

            case TravelLegMode.Dock:
                if (zone.isShipPort || zone.terminalSurfaceKind == TerminalSurfaceKind.ShipPort)
                    return true;
                return zone.allowedTerminalLegs == null || zone.allowedTerminalLegs.Count == 0
                    || zone.allowedTerminalLegs.Contains(TravelLegMode.Dock);

            default:
                return true;
        }
    }

    static float ResolveWaterY(Vector3 slotCenter, ParkingZoneVolume zone)
    {
        Bounds b = zone.GetWorldBounds();
        return b.max.y > b.min.y ? (b.min.y + b.max.y) * 0.5f : slotCenter.y;
    }

    static float PlaningPenalty(
        TravelLegMode mode,
        ParkingZoneVolume zone,
        ActorPhysicalProfile profile,
        Vector3 approachStart,
        Vector3 slotCenter)
    {
        if (mode != TravelLegMode.ParkWater && mode != TravelLegMode.Moor)
            return 0f;
        float runOut = Vector3.Distance(
            new Vector3(approachStart.x, slotCenter.y, approachStart.z),
            new Vector3(slotCenter.x, slotCenter.y, slotCenter.z));
        float need = Mathf.Max(zone.minPlaningRunOutMeters, profile.decelerationDistanceEstimate);
        return WPlaning * Mathf.Max(0f, need - runOut);
    }

    static float PathLength(List<Vector3> path)
    {
        if (path == null || path.Count < 2)
            return 0f;
        float len = 0f;
        for (int i = 1; i < path.Count; i++)
            len += Vector3.Distance(path[i - 1], path[i]);
        return len;
    }
}
