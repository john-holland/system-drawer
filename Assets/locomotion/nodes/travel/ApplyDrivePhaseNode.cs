using UnityEngine;

/// <summary>
/// Applies enter/exit drive animation phase to <see cref="DrivingPartialAnimationDriveBridge"/> at mode transitions.
/// </summary>
public sealed class ApplyDrivePhaseNode : TravelContextBehaviorTreeNode, ITravelExecutionContextConsumer
{
    public DrivingPartialAnimationDriveBridge driveBridge;

    TravelExecutionContext _injected;

    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public void SetTravelExecutionContext(TravelExecutionContext ctx) => _injected = ctx;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        TravelExecutionContext ctx = Ctx ?? _injected;
        if (ctx == null || !ctx.isModeTransition)
            return BehaviorTreeStatus.Success;

        DrivingPartialAnimationDriveBridge bridge = driveBridge;
        if (bridge == null && tree != null)
            bridge = tree.GetComponentInChildren<DrivingPartialAnimationDriveBridge>();
        if (bridge == null)
            return BehaviorTreeStatus.Success;

        DriveAnimationPhase phase = ResolvePhase(ctx);
        bridge.ApplyPhaseToSolver(phase);
        return BehaviorTreeStatus.Success;
    }

    static DriveAnimationPhase ResolvePhase(TravelExecutionContext ctx)
    {
        if (ctx.toMode == TravelLegMode.Drive)
            return DriveAnimationPhase.Enter;
        if (ctx.fromMode == TravelLegMode.Drive)
            return DriveAnimationPhase.Exit;
        return DriveAnimationPhase.Drive;
    }
}
