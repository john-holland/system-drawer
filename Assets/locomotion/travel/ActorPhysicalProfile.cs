using System;
using Locomotion.Spaceship;
using UnityEngine;

/// <summary>Physical footprint and terminal defaults for ambulating actors.</summary>
[Serializable]
public struct ActorPhysicalProfile
{
    public Vector3 minSpace;
    public Vector3 optimalSpace;
    public Vector3 centroidOffsetFromRoot;
    public float capsuleRadius;
    public float capsuleHeight;
    public PhysicalPathingMedium preferredMedium;
    public TravelLegMode defaultTerminalLeg;
    public float maxDraftMeters;
    public float minBeachSlopeDegrees;
    public float maxBeachSlopeDegrees;
    public float planingSpeedMin;
    public float planingSpeedMax;
    public float aquaplaneMu;
    public float decelerationDistanceEstimate;
    public bool hasAquaplaneSolver;
    public bool hasFlyingConfig;
    public bool hasSpaceshipComponents;
}

/// <summary>Shared centroid and profile builder for pathing and terminal placement.</summary>
public static class ActorPhysicalCentroid
{
    public static Vector3 GetWorldCenterOfMass(GameObject root)
    {
        if (root == null)
            return Vector3.zero;
        Rigidbody rb = root.GetComponentInChildren<Rigidbody>();
        if (rb != null && rb.centerOfMass != Vector3.zero)
            return root.transform.TransformPoint(rb.centerOfMass);
        return root.transform.position;
    }

    public static bool TryBuildProfile(BaseAmbulatingActor actor, out ActorPhysicalProfile profile)
    {
        profile = default;
        if (actor == null)
            return false;

        Transform root = actor.RootTransform != null ? actor.RootTransform : actor.transform;
        Bounds bounds = ComputeBounds(root.gameObject);
        profile.minSpace = bounds.size;
        profile.optimalSpace = bounds.size;
        if (profile.optimalSpace.sqrMagnitude < 0.01f)
            profile.optimalSpace = profile.minSpace = new Vector3(1f, 2f, 2f);

        Vector3 com = GetWorldCenterOfMass(root.gameObject);
        profile.centroidOffsetFromRoot = com - root.position;
        profile.capsuleRadius = Mathf.Max(0.2f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        profile.capsuleHeight = Mathf.Max(0.5f, bounds.size.y);

        profile.hasAquaplaneSolver = actor.GetComponentInChildren<VehicleAquaplaneSolver>() != null;
        var cardSolver = actor.GetComponentInChildren<PhysicsCardSolver>();
        profile.hasFlyingConfig = cardSolver != null && cardSolver.flyingCardConfig != null;
        profile.hasSpaceshipComponents = actor.GetComponentInChildren<SpacecraftControlProxy>() != null;

        var aquaplane = actor.GetComponentInChildren<VehicleAquaplaneSolver>();
        if (aquaplane != null)
        {
            profile.aquaplaneMu = aquaplane.aquaplaneMu;
            profile.planingSpeedMin = 4f;
            profile.planingSpeedMax = 18f;
            profile.decelerationDistanceEstimate = Mathf.Max(20f, profile.planingSpeedMax * profile.planingSpeedMax * 0.5f);
            profile.maxDraftMeters = Mathf.Max(0.5f, bounds.extents.y * 0.6f);
        }

        profile.minBeachSlopeDegrees = 0f;
        profile.maxBeachSlopeDegrees = 12f;
        profile.defaultTerminalLeg = InferDefaultTerminalLeg(actor, profile);
        profile.preferredMedium = TravelLegModeExtensions.DefaultMedium(profile.defaultTerminalLeg);
        return true;
    }

    public static TravelLegMode InferDefaultTerminalLeg(BaseAmbulatingActor actor, in ActorPhysicalProfile profile)
    {
        if (actor == null)
            return TravelLegMode.Park;

        if (profile.hasSpaceshipComponents)
            return TravelLegMode.Dock;
        if (profile.hasFlyingConfig)
            return TravelLegMode.Land;
        if (profile.hasAquaplaneSolver)
            return TravelLegMode.ParkWater;
        if (actor is RagdollActor)
            return TravelLegMode.Park;
        if (actor is VehicleActor)
            return TravelLegMode.Park;
        return TravelLegMode.Park;
    }

    public static TravelLegMode ResolveTerminalLegFromZone(
        TravelLegMode requested,
        TerminalSurfaceKind surfaceKind,
        in ActorPhysicalProfile profile)
    {
        if (requested != TravelLegMode.Walk && TravelLegModeExtensions.IsTerminalLeg(requested))
            return requested;

        switch (surfaceKind)
        {
            case TerminalSurfaceKind.Runway:
            case TerminalSurfaceKind.GroundPad:
                return TravelLegMode.Park;
            case TerminalSurfaceKind.WaterOpen:
                return profile.hasFlyingConfig || profile.hasSpaceshipComponents
                    ? TravelLegMode.LandWater
                    : TravelLegMode.Moor;
            case TerminalSurfaceKind.WaterPlaningPark:
                return profile.hasAquaplaneSolver ? TravelLegMode.ParkWater : TravelLegMode.Moor;
            case TerminalSurfaceKind.BeachShore:
                return profile.hasAquaplaneSolver ? TravelLegMode.Beach : TravelLegMode.Park;
            case TerminalSurfaceKind.ShipPort:
                return TravelLegMode.Dock;
            default:
                return profile.defaultTerminalLeg;
        }
    }

    static Bounds ComputeBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
