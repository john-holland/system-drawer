using UnityEngine;

/// <summary>Topological gear/door sequence around helicopter takeoff.</summary>
public sealed class HelicopterTakeoffPlanNode : BehaviorTreeNode
{
    public HelicopterVehicleRagdoll helicopter;
    public float durationSec = 1.5f;
    float _t;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (helicopter == null && tree != null)
            helicopter = tree.GetComponentInParent<HelicopterVehicleRagdoll>();
        helicopter?.SetLandingGearDown(false);
        helicopter?.SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.Takeoff,
            SendMessageOptions.DontRequireReceiver);
        if (helicopter?.doorOpenCloseBt != null)
            helicopter.doorOpenCloseBt.SendMessage("OnClose", helicopter.doorOpenCloseTopologyId,
                SendMessageOptions.DontRequireReceiver);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t >= durationSec)
        {
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }
}
