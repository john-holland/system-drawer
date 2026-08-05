using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Bounds4 / trigger volume that grafts TravelAgent traffic-flow BT goals via TrafficWardenStateMachine.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Flow Volume Trigger")]
public sealed class TrafficFlowVolumeTrigger : MonoBehaviour
{
    public Bounds4? spatiotemporalVolume;
    public TrafficWarden warden;
    public NarrativeClock clock;
    public bool useTriggerCollider = true;
    public string graftedGoalName = "traffic_flow";

    readonly HashSet<int> _graftedActors = new HashSet<int>();

    void Awake()
    {
        if (warden == null)
            warden = TrafficWarden.Instance ?? FindFirstObjectByType<TrafficWarden>();
        if (clock == null)
            clock = FindFirstObjectByType<NarrativeClock>();
        if (useTriggerCollider)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        TryGraft(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        TryClearGraft(other.gameObject);
    }

    public bool IsVolumeActiveNow()
    {
        if (!spatiotemporalVolume.HasValue) return true;
        if (clock == null) return true;
        float t = NarrativeCalendarMath.DateTimeToSeconds(clock.Now);
        var v = spatiotemporalVolume.Value;
        return t >= v.tMin && t <= v.tMax;
    }

    public bool TryGraft(GameObject actor)
    {
        if (actor == null || !IsVolumeActiveNow()) return false;
        if (warden == null)
            warden = TrafficWarden.Instance;
        if (warden == null) return false;

        var ta = actor.GetComponent<TravelAgent>() ?? actor.GetComponentInParent<TravelAgent>();
        if (ta == null) return false;

        warden.stateMachine.AdviseAgent(ta, warden);

        var bt = actor.GetComponent<BehaviorTree>() ?? actor.GetComponentInParent<BehaviorTree>();
        var nervous = actor.GetComponent<NervousSystem>() ?? actor.GetComponentInParent<NervousSystem>();
        var goal = new BehaviorTreeGoal
        {
            goalName = graftedGoalName,
            type = GoalType.TravelAgent,
            targetPosition = ta.previewGoalWorld,
            priority = 6
        };
        goal.parameters["trafficWardenMode"] = warden.stateMachine.Mode;

        if (bt != null)
            bt.SetGoal(goal);
        else
            nervous?.AddGoal(goal);

        var card = TravelAgentCard.GeneratePatrol(ta.previewGoalWorld);
        card.ApplyToActor(actor, 0f);

        _graftedActors.Add(actor.GetInstanceID());
        return true;
    }

    public void TryClearGraft(GameObject actor)
    {
        if (actor == null) return;
        int id = actor.GetInstanceID();
        if (!_graftedActors.Remove(id)) return;

        var bt = actor.GetComponent<BehaviorTree>() ?? actor.GetComponentInParent<BehaviorTree>();
        if (bt != null && bt.currentGoal != null && bt.currentGoal.goalName == graftedGoalName)
            bt.currentGoal = null;
    }
}
