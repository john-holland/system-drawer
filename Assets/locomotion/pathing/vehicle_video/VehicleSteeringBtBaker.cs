using UnityEngine;

/// <summary>Bakes SeedVehicleVelocity + per-segment Enter + TravelLegDriveNode waypoint chain.</summary>
public static class VehicleSteeringBtBaker
{
    public struct BakeResult
    {
        public BehaviorTree tree;
        public SeedVehicleVelocityNode seed;
        public int driveWaypointCount;
        public TravelLegSequenceNode root;
    }

    public static BakeResult Bake(
        Transform parent,
        VehicleProjectionResult projection,
        VehicleActor vehicle = null,
        TravelAgent agent = null,
        CabinPolarVelocity polar = null,
        Transform occupant = null)
    {
        var result = new BakeResult();
        if (parent == null)
            return result;

        Transform host = vehicle != null ? vehicle.transform : parent;
        var treeGo = new GameObject("VehicleSteeringBT");
        treeGo.transform.SetParent(parent, false);
        var tree = treeGo.AddComponent<BehaviorTree>();
        result.tree = tree;

        var rootGo = new GameObject("SteeringRoot");
        rootGo.transform.SetParent(treeGo.transform, false);
        var root = rootGo.AddComponent<TravelLegSequenceNode>();
        root.travelAgent = agent;
        root.legMode = TravelLegMode.Drive;
        tree.rootNode = root;
        result.root = root;

        var seedGo = new GameObject("SeedVelocity");
        seedGo.transform.SetParent(rootGo.transform, false);
        var seed = seedGo.AddComponent<SeedVehicleVelocityNode>();
        if (polar != null && polar.FrameCount > 0)
        {
            Vector3 fwd = vehicle != null ? vehicle.transform.forward : Vector3.forward;
            var slot = polar.ToSeedSlot(fwd, Vector3.up);
            seed.linearVelocity = slot.linearVelocity;
            seed.angularVelocity = slot.angularVelocity;
        }
        else if (projection != null)
        {
            seed.linearVelocity = projection.seedVelocity;
            seed.angularVelocity = projection.seedAngular;
        }
        if (vehicle != null)
        {
            seed.body = vehicle.GetComponent<Rigidbody>();
            seed.velocityBridge = vehicle.GetComponent<DimensionalLemmaVelocityBridge>();
        }
        root.children.Add(seed);
        result.seed = seed;

        TrySeatOccupant(vehicle, occupant);

        PhysicsCardSolver cardSolver = host != null ? host.GetComponentInChildren<PhysicsCardSolver>() : null;
        DrivingPhysicsCardSolver drivingSolver = host != null ? host.GetComponentInChildren<DrivingPhysicsCardSolver>() : null;
        if (drivingSolver == null && vehicle != null)
            drivingSolver = vehicle.drivingPhysicsCardSolver;
        VehicleInstrumentPhysicsProxy proxy = host != null ? host.GetComponentInChildren<VehicleInstrumentPhysicsProxy>() : null;
        if (proxy == null && vehicle != null)
            proxy = vehicle.instrumentPhysicsProxy;
        GambitSteeringEnforcer enforcer = host != null ? host.GetComponentInChildren<GambitSteeringEnforcer>() : null;
        VehicleAmbulationSolver ambulation = vehicle != null ? vehicle.ambulationSolver : null;
        if (ambulation == null && host != null)
            ambulation = host.GetComponentInChildren<VehicleAmbulationSolver>();

        int wpCount = 0;
        if (projection != null)
        {
            var segs = projection.segments.Count > 0 ? projection.segments : null;
            if (segs == null)
            {
                wpCount += AppendDriveChain(
                    root, projection.waypoints, cardSolver, drivingSolver, proxy, enforcer, ambulation, vehicle, agent);
            }
            else
            {
                for (int i = 0; i < segs.Count; i++)
                {
                    var enterGo = new GameObject($"DriveEnter_{i}");
                    enterGo.transform.SetParent(rootGo.transform, false);
                    var enter = enterGo.AddComponent<ApplyDrivePhaseNode>();
                    enter.forcePhase = true;
                    enter.phaseOverride = DriveAnimationPhase.Enter;
                    root.children.Add(enter);
                    wpCount += AppendDriveChain(
                        root, segs[i].waypoints, cardSolver, drivingSolver, proxy, enforcer, ambulation, vehicle, agent);
                }
            }
        }

        result.driveWaypointCount = wpCount;
        return result;
    }

    public static bool TrySeatOccupant(VehicleActor vehicle, Transform occupant)
    {
        if (vehicle == null || occupant == null)
            return false;
        var seating = vehicle.GetComponentInChildren<VehicleSeating>();
        if (seating == null || seating.occupantAnchors == null || seating.occupantAnchors.Length == 0)
            return false;
        Transform anchor = seating.occupantAnchors[0];
        if (anchor == null)
            return false;
        occupant.SetPositionAndRotation(anchor.position, anchor.rotation);
        return true;
    }

    static int AppendDriveChain(
        TravelLegSequenceNode root,
        System.Collections.Generic.List<VehicleProjectedWaypoint> wps,
        PhysicsCardSolver cardSolver,
        DrivingPhysicsCardSolver drivingSolver,
        VehicleInstrumentPhysicsProxy proxy,
        GambitSteeringEnforcer enforcer,
        VehicleAmbulationSolver ambulation,
        VehicleActor vehicle,
        TravelAgent agent)
    {
        if (wps == null || root == null)
            return 0;
        int n = 0;
        for (int i = 0; i < wps.Count; i++)
        {
            var wp = wps[i];
            var go = new GameObject($"DriveWaypoint_{root.children.Count}");
            go.transform.SetParent(root.transform, false);
            var node = go.AddComponent<TravelLegDriveNode>();
            node.waypoint = wp.world;
            node.reachedDistance = 1.25f;
            node.travelLegMode = TravelLegMode.Drive;
            node.physicalMedium = PhysicalPathingMedium.Ground;
            node.cardSolver = cardSolver;
            node.drivingSolver = drivingSolver;
            node.vehicleHint = vehicle;
            node.instrumentProxy = proxy;
            node.steeringEnforcer = enforcer;
            node.ambulationSolver = ambulation;
            node.steerHintSigned01 = wp.steerHintSigned01;
            node.speedHint = wp.speed;
            node.pathTangent = wp.tangent;
            if (enforcer != null && agent != null)
                enforcer.travelAgent = agent;
            root.children.Add(node);
            n++;
        }
        return n;
    }
}
