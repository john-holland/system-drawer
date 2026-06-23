using UnityEngine;

/// <summary>
/// Three-phase aquaplane terminal: planing approach, bleed speed, hold at slot.
/// Shared by ParkWater and Moor legs.
/// </summary>
public sealed class AquaplaneWaterTerminalExecutor
{
    public enum Phase
    {
        PlaningApproach,
        BleedSpeed,
        HoldAtSlot,
        Complete
    }

    readonly MultiModalSegment _segment;
    readonly VehicleAquaplaneSolver _aquaplane;
    readonly Rigidbody _body;
    readonly WaterHoldPolicy _holdPolicy;

    Phase _phase = Phase.PlaningApproach;
    int _waypointIndex;
    const float ParkSpeedThreshold = 1.5f;
    const float ReachDistance = 2f;

    public Phase CurrentPhase => _phase;

    public AquaplaneWaterTerminalExecutor(
        MultiModalSegment segment,
        VehicleAquaplaneSolver aquaplane,
        Rigidbody body)
    {
        _segment = segment;
        _aquaplane = aquaplane;
        _body = body;
        _holdPolicy = segment != null ? segment.terminalHoldPolicy : WaterHoldPolicy.Park;
    }

    public BehaviorTreeStatus Tick(float deltaTime)
    {
        if (_segment == null || _body == null)
            return BehaviorTreeStatus.Failure;

        switch (_phase)
        {
            case Phase.PlaningApproach:
                return TickPlaningApproach();
            case Phase.BleedSpeed:
                return TickBleedSpeed();
            case Phase.HoldAtSlot:
                return TickHold();
            default:
                return BehaviorTreeStatus.Success;
        }
    }

    BehaviorTreeStatus TickPlaningApproach()
    {
        if (_segment.waypoints == null || _segment.waypoints.Count == 0)
        {
            _phase = Phase.BleedSpeed;
            return BehaviorTreeStatus.Running;
        }

        Vector3 target = _segment.waypoints[Mathf.Min(_waypointIndex, _segment.waypoints.Count - 1)];
        Vector3 pos = _body.position;
        Vector3 to = target - pos;
        to.y = 0f;
        if (to.magnitude <= ReachDistance)
        {
            _waypointIndex++;
            if (_waypointIndex >= _segment.waypoints.Count)
                _phase = Phase.BleedSpeed;
            return BehaviorTreeStatus.Running;
        }

        if (_aquaplane != null)
        {
            float steer = Mathf.Clamp(Vector3.Dot(_body.transform.right, to.normalized), -1f, 1f);
            _aquaplane.TrySolveSteeringLeafAquaplane(steer, null, pos, 1f, out _, out _);
        }
        else
        {
            _body.AddForce(to.normalized * 500f, ForceMode.Force);
        }

        return BehaviorTreeStatus.Running;
    }

    BehaviorTreeStatus TickBleedSpeed()
    {
        float speed = _body.linearVelocity.magnitude;
        if (speed <= ParkSpeedThreshold)
        {
            _phase = Phase.HoldAtSlot;
            return BehaviorTreeStatus.Running;
        }

        if (_aquaplane != null)
            _aquaplane.TrySolveSteeringLeafAquaplane(0f, null, _body.position, 0.2f, out _, out _);
        else
            _body.linearVelocity *= 0.95f;

        return BehaviorTreeStatus.Running;
    }

    BehaviorTreeStatus TickHold()
    {
        Vector3 target = _segment.terminalCentroidWorld;
        Vector3 pos = _body.position;
        Vector3 delta = target - pos;
        delta.y = 0f;

        if (delta.magnitude <= ReachDistance && _body.linearVelocity.magnitude <= ParkSpeedThreshold * 1.5f)
        {
            _phase = Phase.Complete;
            if (_holdPolicy == WaterHoldPolicy.Anchor)
                _body.linearVelocity *= 0.5f;
            return BehaviorTreeStatus.Success;
        }

        if (_holdPolicy == WaterHoldPolicy.Anchor)
            _body.AddForce(delta.normalized * 80f, ForceMode.Force);

        return BehaviorTreeStatus.Running;
    }
}
