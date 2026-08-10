using UnityEngine;

/// <summary>SG3D/SG4D-friendly elevator floor travel action node.</summary>
public sealed class ElevatorFloorTravelPlanNode : BehaviorTreeNode
{
    public ElevatorVehicleRagdoll elevator;
    public int targetFloor;
    public float durationSec = 2f;
    float _t;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (elevator == null && tree != null)
            elevator = tree.GetComponentInParent<ElevatorVehicleRagdoll>();
        elevator?.SetDoorsOpen(false);
        elevator?.CallFloor(targetFloor);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t >= durationSec)
        {
            elevator?.SetDoorsOpen(true);
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }
}
