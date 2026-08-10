using UnityEngine;

/// <summary>Wait for ATC landing-queue slot before PrepareLand / gear / park.</summary>
public sealed class AircraftLandingQueueNode : BehaviorTreeNode
{
    public AirplaneVehicleRagdoll airplane;
    public AirTrafficControlBioRhythm atc;
    public string flightId;
    public float timeoutSec = 120f;
    float _t;
    bool _claimed;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _claimed = false;
        if (airplane == null && tree != null)
            airplane = tree.GetComponentInParent<AirplaneVehicleRagdoll>();
        if (atc == null)
            atc = Object.FindFirstObjectByType<AirTrafficControlBioRhythm>();
        if (string.IsNullOrEmpty(flightId) && airplane != null)
            flightId = airplane.activeFlightId;
        if (string.IsNullOrEmpty(flightId))
            flightId = "flt_" + GetInstanceID();
        atc?.EnqueueLanding(flightId, airplane);
        airplane?.NotifyNarrative(AirplaneNarrativeActionIds.LandingQueueWait);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (!_claimed && atc != null && atc.TryClaimLandingSlot(flightId))
            _claimed = true;
        if (_claimed || atc == null)
        {
            status = BehaviorTreeStatus.Success;
            return BehaviorTreeStatus.Success;
        }
        if (_t >= timeoutSec)
        {
            status = BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Failure;
        }
        status = BehaviorTreeStatus.Running;
        return BehaviorTreeStatus.Running;
    }
}
