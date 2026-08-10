using UnityEngine;

/// <summary>BT action — rail-parallel refill at a gas station pump matching train railSegmentId.</summary>
public sealed class GasStationRailRefuelNode : BehaviorTreeNode
{
    public GasStationRuntime station;
    public TrainVehicleRagdoll train;
    public float fill01 = 1f;
    public float durationSec = 2f;
    float _t;
    bool _ok;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _ok = false;
        if (train == null && tree != null)
            train = tree.GetComponentInParent<TrainVehicleRagdoll>();
        if (station == null)
            station = Object.FindFirstObjectByType<GasStationRuntime>();
        var pump = station?.FindRailPump(train != null ? train.railSegmentId : null);
        _ok = pump != null && pump.TryRefuelTrain(train, fill01);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return BehaviorTreeStatus.Running;
        }
        status = _ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}
