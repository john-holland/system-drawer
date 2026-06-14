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

        base.OnEnter(tree);
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
