using UnityEngine;

/// <summary>Close: refold limb and/or park contained vehicle (topology close semantics).</summary>
public sealed class TrainCarClosePlanNode : BehaviorTreeNode
{
    public TrainCarVehicleRagdoll car;
    public TrainCarCloseMode closeMode = TrainCarCloseMode.Both;
    public string limbId;
    public string bayId;
    public VehicleRagdoll vehicleToPark;
    public float durationSec = 0.5f;
    float _t;
    bool _done;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _done = false;
        if (car == null && tree != null)
            car = tree.GetComponentInParent<TrainCarVehicleRagdoll>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (car == null)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        _t += Time.deltaTime;
        if (!_done && _t >= durationSec)
        {
            if (closeMode == TrainCarCloseMode.RefoldLimb || closeMode == TrainCarCloseMode.Both)
                car.TryRefoldLimb(limbId);

            if (closeMode == TrainCarCloseMode.ParkContainedVehicle || closeMode == TrainCarCloseMode.Both)
            {
                var v = vehicleToPark;
                if (v == null)
                {
                    var bay = car.FindBay(bayId);
                    if (bay != null && bay.containedVehicles.Count > 0)
                        v = bay.containedVehicles[0];
                }
                if (v != null)
                    car.TryParkVehicle(v, bayId);
                car.SetBayRampUnfolded(bayId, false);
            }
            _done = true;
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        if (_done)
        {
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }
}
