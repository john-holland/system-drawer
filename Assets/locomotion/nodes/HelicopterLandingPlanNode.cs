using UnityEngine;

/// <summary>Topological gear/door sequence around helicopter landing onto RoadLot / pad / anywhere.</summary>
public sealed class HelicopterLandingPlanNode : BehaviorTreeNode
{
    public HelicopterVehicleRagdoll helicopter;
    public RoadLot targetRoadLot;
    public HelipadAnywhereBounds anywhereBounds;
    public float durationSec = 2f;
    float _t;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (helicopter == null && tree != null)
            helicopter = tree.GetComponentInParent<HelicopterVehicleRagdoll>();
        ResolvePad();
        helicopter?.SetLandingGearDown(true);
        helicopter?.SendMessage("OnNarrativeSchedulerAction", HelicopterNarrativeActionIds.Landing,
            SendMessageOptions.DontRequireReceiver);
    }

    void ResolvePad()
    {
        if (helicopter == null) return;
        Vector3 pos = helicopter.transform.position;
        if (targetRoadLot == null)
            targetRoadLot = RoadLot.FindNearest(pos);
        if (anywhereBounds == null)
            anywhereBounds = Object.FindFirstObjectByType<HelipadAnywhereBounds>();
        if (targetRoadLot != null)
            helicopter.transform.position = new Vector3(
                helicopter.transform.position.x,
                targetRoadLot.SampleHeight(helicopter.transform.position),
                helicopter.transform.position.z);
        else if (anywhereBounds != null)
            helicopter.transform.position = anywhereBounds.PickLandingPoint(pos);
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
