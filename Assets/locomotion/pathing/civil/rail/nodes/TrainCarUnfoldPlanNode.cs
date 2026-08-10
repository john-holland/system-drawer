using UnityEngine;

/// <summary>Unfold ambulation limb and/or bay ramp; exposes resultants on the train car.</summary>
public sealed class TrainCarUnfoldPlanNode : BehaviorTreeNode
{
    public TrainVehicleRagdoll car;
    public string limbId;
    public string bayId;
    public bool unfoldLimb = true;
    public bool unfoldBayRamp = true;
    public bool simulateFailure;
    public float durationSec = 0.5f;
    float _t;
    bool _done;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _done = false;
        if (car == null && tree != null)
            car = tree.GetComponentInParent<TrainVehicleRagdoll>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (car == null)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }
        if (simulateFailure)
        {
            car.MarkFoldFailed(string.IsNullOrEmpty(limbId) ? bayId : limbId);
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }

        _t += Time.deltaTime;
        if (!_done && _t >= durationSec)
        {
            if (unfoldLimb)
                car.TryUnfoldLimb(limbId);
            if (unfoldBayRamp)
                car.SetBayRampUnfolded(bayId, true);
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
