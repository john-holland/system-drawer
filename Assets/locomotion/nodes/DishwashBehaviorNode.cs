using UnityEngine;

/// <summary>BT: pick → scrub/rinse at sink → place dishwasher or dry rack → tool return.</summary>
[AddComponentMenu("Locomotion/Kitchen/Dishwash Behavior Node")]
public sealed class DishwashBehaviorNode : BehaviorTreeNode
{
    public DishWashingStation station;
    public DishwashingCard boundCard;
    public SinkSpringNozzleFixture nozzle;
    public float scrubElapsed;
    enum Phase { Pick, Scrub, Place, Done }
    Phase _phase;
    bool _started;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (station == null)
            station = FindFirstObjectByType<DishWashingStation>();
        if (boundCard == null || station == null)
        {
            status = BehaviorTreeStatus.Failure;
            return status;
        }

        if (!_started)
        {
            _phase = Phase.Pick;
            scrubElapsed = 0f;
            _started = true;
            status = BehaviorTreeStatus.Running;
        }

        switch (_phase)
        {
            case Phase.Pick:
                _phase = Phase.Scrub;
                break;
            case Phase.Scrub:
                scrubElapsed += Time.deltaTime;
                if (nozzle != null &&
                    (boundCard.scrubMode == DishScrubMode.FloodProxy ||
                     boundCard.scrubMode == DishScrubMode.TimingAndFlood))
                    nozzle.Rinse(boundCard.rinseLiters * Time.deltaTime, boundCard.scrubMode);
                bool timedDone = scrubElapsed >= boundCard.scrubSeconds;
                bool floodDone = boundCard.scrubMode == DishScrubMode.FloodProxy &&
                                 nozzle != null &&
                                 nozzle.totalRinseLiters >= boundCard.rinseLiters;
                if (timedDone || floodDone)
                    _phase = Phase.Place;
                break;
            case Phase.Place:
                if (!station.TryMove(boundCard.dishItemId, boundCard.fromZone, boundCard.toZone, out _))
                {
                    _started = false;
                    status = BehaviorTreeStatus.Failure;
                    return status;
                }
                _phase = Phase.Done;
                _started = false;
                status = BehaviorTreeStatus.Success;
                break;
        }
        return status;
    }
}
