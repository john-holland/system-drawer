using UnityEngine;

/// <summary>BT node: rotating-chair swivel tool-use under seated or stand-on occupancy.</summary>
public class ChairRotateNode : BehaviorTreeNode
{
    public ChairRotateCard rotateCard;
    public SitSurfaceContact surface;
    public float yawDegrees = 45f;
    public SurfaceOccupancyMode occupancyMode = SurfaceOccupancyMode.Sit;

    bool _started;
    GoodSection _active;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        var runtime = ragdoll.GetComponent<SeatedOccupancyRuntime>();
        SitSurfaceContact contact = surface
            ?? (runtime != null ? runtime.surface : null);
        if (contact == null)
            return BehaviorTreeStatus.Failure;

        SurfaceOccupancyMode mode = runtime != null ? runtime.mode : occupancyMode;
        RagdollState state = ragdoll.GetCurrentState();
        ChairRotateCard card = rotateCard ?? ChairRotateCard.Generate(contact, yawDegrees, state, mode);

        if (!_started)
        {
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
            card.ApplyChairYawImpulse();
            card.Execute(state);
            _active = card;
            _started = true;
        }

        if (_active != null && _active.Update(ragdoll.GetCurrentState(), Time.deltaTime))
            return BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _active = null;
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_active != null)
        {
            _active.Stop();
            _active = null;
        }
        _started = false;
    }
}
