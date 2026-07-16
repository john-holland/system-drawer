using UnityEngine;

/// <summary>BT node: non-rotating chair schooch with tool-hold lift and re-seat/re-plant.</summary>
public class ChairSchoochNode : BehaviorTreeNode
{
    public ChairSchoochCard schoochCard;
    public SitSurfaceContact surface;
    public Vector3 scootWorldDelta = new Vector3(0f, 0f, 0.25f);
    public SurfaceOccupancyMode occupancyMode = SurfaceOccupancyMode.Sit;

    bool _started;
    bool _translated;
    GoodSection _active;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        var runtime = ragdoll.GetComponent<SeatedOccupancyRuntime>();
        SitSurfaceContact contact = surface ?? (runtime != null ? runtime.surface : null);
        if (contact == null)
            return BehaviorTreeStatus.Failure;

        SurfaceOccupancyMode mode = runtime != null ? runtime.mode : occupancyMode;
        RagdollState state = ragdoll.GetCurrentState();
        ChairSchoochCard card = schoochCard ?? ChairSchoochCard.Generate(contact, scootWorldDelta, state, mode);

        if (!_started)
        {
            if (!card.IsFeasible(state))
                return BehaviorTreeStatus.Failure;
            card.Execute(state);
            _active = card;
            _started = true;
        }

        bool running = _active != null && _active.Update(ragdoll.GetCurrentState(), Time.deltaTime);
        if (!running && !_translated)
        {
            card.ApplyChairTranslate();
            _translated = true;
            // Re-lock tow after scoot
            if (runtime != null)
            {
                if (mode == SurfaceOccupancyMode.StandOn)
                    runtime.BeginStandOn(contact);
                else
                    runtime.BeginSit(contact);
            }
            return BehaviorTreeStatus.Success;
        }

        return running ? BehaviorTreeStatus.Running : BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _started = false;
        _translated = false;
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
        _translated = false;
    }
}
