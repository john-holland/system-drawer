using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drive-leg waypoint action: prefers <see cref="DrivingPhysicsCardSolver"/> cards with pathing mode set to Drive.
/// </summary>
public class TravelLegDriveNode : MoveToWaypointNode
{
    [Header("Drive")]
    public DrivingPhysicsCardSolver drivingSolver;
    public HierarchicalPathingSolver pathingSolver;
    public VehicleActor vehicleHint;

    [Header("Instrument proxy")]
    public VehicleInstrumentPhysicsProxy instrumentProxy;
    public GambitSteeringEnforcer steeringEnforcer;
    public VehicleAmbulationSolver ambulationSolver;
    [Range(-1f, 1f)] public float steerHintSigned01;
    public float speedHint;
    public Vector3 pathTangent = Vector3.forward;

    PathingMode _savedPathingMode;
    bool _pathingModeSaved;

    void Awake()
    {
        nodeType = NodeType.Action;
        travelLegMode = TravelLegMode.Drive;
        if (drivingSolver == null)
            drivingSolver = GetComponentInParent<DrivingPhysicsCardSolver>();
        if (pathingSolver == null)
            pathingSolver = GetComponentInParent<HierarchicalPathingSolver>();
    }

    public override void OnEnter(BehaviorTree tree)
    {
        if (pathingSolver != null)
        {
            _savedPathingMode = pathingSolver.pathingMode;
            pathingSolver.pathingMode = PathingMode.Drive;
            _pathingModeSaved = true;
        }

        if (drivingSolver != null && vehicleHint != null && drivingSolver.assignedVehicle == null)
            drivingSolver.assignedVehicle = vehicleHint;

        if (instrumentProxy == null && vehicleHint != null)
            instrumentProxy = vehicleHint.instrumentPhysicsProxy;
        if (instrumentProxy == null)
            instrumentProxy = GetComponentInParent<VehicleInstrumentPhysicsProxy>();
        if (ambulationSolver == null && vehicleHint != null)
            ambulationSolver = vehicleHint.ambulationSolver;
        if (steeringEnforcer == null)
            steeringEnforcer = GetComponentInParent<GambitSteeringEnforcer>();

        base.OnEnter(tree);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        TryRouteInstrumentProxy(tree, Time.deltaTime);
        return base.Execute(tree);
    }

    public override void OnUpdate(BehaviorTree tree)
    {
        TryRouteInstrumentProxy(tree, Time.deltaTime);
        base.OnUpdate(tree);
    }

    /// <summary>Route steer/throttle/brake stubs through the instrument proxy when bound.</summary>
    public bool TryRouteInstrumentProxy(BehaviorTree tree, float dt)
    {
        var proxied = drivingSolver as ProxiedDrivingPhysicsCardSolver;
        if (proxied == null && drivingSolver != null)
            proxied = drivingSolver.GetComponent<ProxiedDrivingPhysicsCardSolver>();
        if (proxied == null)
            proxied = GetComponentInParent<ProxiedDrivingPhysicsCardSolver>();

        VehicleInstrumentPhysicsProxy proxy = instrumentProxy;
        if (proxy == null && proxied != null)
            proxy = proxied.physicsProxy;
        if (proxy == null)
            return false;

        ApplySteerDemand(tree);

        RagdollState state = null;
        if (ragdollSystem != null)
            state = ragdollSystem.GetCurrentState();

        if (proxied != null)
        {
            if (proxied.physicsProxy == null)
                proxied.physicsProxy = proxy;
            if (proxied.TryRouteFirstApplicable(state, dt))
                return true;
        }

        GoodSection card = ResolveProxyCard();
        return card != null && proxy.RouteCard(card, dt);
    }

    void ApplySteerDemand(BehaviorTree tree)
    {
        Vector3 desired = pathTangent.sqrMagnitude > 1e-6f ? pathTangent : (waypoint - transform.position);
        desired.y = 0f;
        if (steeringEnforcer != null)
            desired = steeringEnforcer.BlendSteerDirection(desired);

        Transform root = vehicleHint != null ? vehicleHint.transform : transform;
        Vector3 heading = root.forward;
        heading.y = 0f;
        float demand = steerHintSigned01;
        if (heading.sqrMagnitude > 1e-6f && desired.sqrMagnitude > 1e-6f)
        {
            heading.Normalize();
            desired.Normalize();
            demand = Mathf.Clamp(Vector3.SignedAngle(heading, desired, Vector3.up) / 45f, -1f, 1f);
        }

        if (ambulationSolver != null)
        {
            ambulationSolver.TrySolveSteeringLeaf(
                demand, null, root.position, 1f, out float steerCmd, out _);
            steerHintSigned01 = steerCmd;
        }
        else
            steerHintSigned01 = demand;
    }

    GoodSection ResolveProxyCard()
    {
        string steerKey = "vehicle_steering";
        string throttleKey = "vehicle_throttle";
        if (Mathf.Abs(steerHintSigned01) >= 0.15f)
            return PhysicalPathingGoodSectionStubs.CreateDriveSteerStub(steerKey);
        var buffer = GetComponentInParent<RagdollPlayerInputBuffer>();
        if (buffer != null && buffer.State.brake01 > 0.01f)
            return PhysicalPathingGoodSectionStubs.CreateDriveBrakeStub(steerKey);
        if (speedHint < 0.25f)
            return PhysicalPathingGoodSectionStubs.CreateDriveBrakeStub(steerKey);
        return PhysicalPathingGoodSectionStubs.CreateDriveThrottleStub(throttleKey);
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_pathingModeSaved && pathingSolver != null)
            pathingSolver.pathingMode = _savedPathingMode;
        _pathingModeSaved = false;
        base.OnExit(tree);
    }

    protected override List<GoodSection> FindMovementCards(RagdollState state)
    {
        if (drivingSolver != null)
        {
            List<GoodSection> driveCards = drivingSolver.FindApplicableCards(state);
            if (driveCards != null && driveCards.Count > 0)
            {
                if (cardSolver != null)
                    return drivingSolver.OrderCardsByFeasibility(driveCards, cardSolver, state);
                return driveCards;
            }
        }

        return base.FindMovementCards(state);
    }
}
