using UnityEngine;

/// <summary>Optional pre-park refuel when fuel01 below threshold or route notes refuel=true.</summary>
public sealed class AircraftRefuelNode : BehaviorTreeNode
{
    public AirplaneVehicleRagdoll airplane;
    [Range(0f, 1f)] public float targetFuel01 = 1f;
    public float fillRatePerSec = 0.15f;
    public float durationSec = 3f;
    float _t;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (airplane == null && tree != null)
            airplane = tree.GetComponentInParent<AirplaneVehicleRagdoll>();
        airplane?.NotifyNarrative(AirplaneNarrativeActionIds.Refuel);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (airplane != null)
            airplane.fuel01 = Mathf.MoveTowards(airplane.fuel01, targetFuel01, fillRatePerSec * Time.deltaTime);
        _t += Time.deltaTime;
        bool fueled = airplane == null || airplane.fuel01 >= targetFuel01 - 0.01f;
        if ((fueled && _t >= durationSec * 0.25f) || _t >= durationSec)
        {
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }
}
