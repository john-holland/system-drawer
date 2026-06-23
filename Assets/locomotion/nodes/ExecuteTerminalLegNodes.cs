using UnityEngine;

/// <summary>Executes a terminal travel leg (Park/Land/Moor/…) until centroid tolerance met.</summary>
public class ExecuteTerminalLegNode : TravelContextBehaviorTreeNode
{
    public MultiModalSegment segment;
    public float reachedDistance = 1.5f;
    public float parkSpeedThreshold = 1.5f;

    AquaplaneWaterTerminalExecutor _aquaplaneExecutor;
    Rigidbody _resolvedBody;
    VehicleAquaplaneSolver _aquaplane;

    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _aquaplaneExecutor = null;
        _resolvedBody = ResolveBody(tree);
        _aquaplane = tree != null ? tree.GetComponentInChildren<VehicleAquaplaneSolver>() : null;

        if (segment != null && (segment.mode == TravelLegMode.ParkWater || segment.mode == TravelLegMode.Moor))
            _aquaplaneExecutor = new AquaplaneWaterTerminalExecutor(segment, _aquaplane, _resolvedBody);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (segment == null || !TravelLegModeExtensions.IsTerminalLeg(segment.mode))
            return BehaviorTreeStatus.Failure;

        if (_aquaplaneExecutor != null)
        {
            BehaviorTreeStatus s = _aquaplaneExecutor.Tick(Time.deltaTime);
            return s == BehaviorTreeStatus.Success ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
        }

        return TickStandardTerminal(tree);
    }

    public override void OnUpdate(BehaviorTree tree)
    {
        if (_aquaplaneExecutor != null && _aquaplaneExecutor.CurrentPhase != AquaplaneWaterTerminalExecutor.Phase.Complete)
            _aquaplaneExecutor.Tick(Time.deltaTime);
    }

    BehaviorTreeStatus TickStandardTerminal(BehaviorTree tree)
    {
        Vector3 target = segment.terminalCentroidWorld;
        if (target.sqrMagnitude < 1e-8f && segment.waypoints != null && segment.waypoints.Count > 0)
            target = segment.waypoints[segment.waypoints.Count - 1];

        Vector3 pos = ResolvePosition(tree);
        float dist = Vector3.Distance(pos, target);

        if (segment.mode == TravelLegMode.Land || segment.mode == TravelLegMode.LandWater || segment.mode == TravelLegMode.Dock)
        {
            if (_resolvedBody != null)
            {
                Vector3 to = target - _resolvedBody.position;
                _resolvedBody.AddForce(to.normalized * 200f, ForceMode.Force);
                if (dist <= reachedDistance && _resolvedBody.linearVelocity.magnitude <= parkSpeedThreshold * 2f)
                    return BehaviorTreeStatus.Success;
                return BehaviorTreeStatus.Running;
            }
        }

        if (dist <= reachedDistance)
            return BehaviorTreeStatus.Success;

        if (_resolvedBody != null)
            _resolvedBody.AddForce((target - pos).normalized * 150f, ForceMode.Force);

        return BehaviorTreeStatus.Running;
    }

    Vector3 ResolvePosition(BehaviorTree tree)
    {
        if (_resolvedBody != null)
            return _resolvedBody.position;
        RagdollSystem ragdoll = tree != null ? tree.GetComponentInParent<RagdollSystem>() : null;
        if (ragdoll != null)
            return ragdoll.GetCurrentState().rootPosition;
        return transform.position;
    }

    Rigidbody ResolveBody(BehaviorTree tree)
    {
        VehicleActor vehicle = tree != null ? tree.GetComponentInChildren<VehicleActor>() : null;
        if (vehicle != null)
        {
            Rigidbody rb = vehicle.GetComponentInChildren<Rigidbody>();
            if (rb != null)
                return rb;
        }
        return tree != null ? tree.GetComponentInChildren<Rigidbody>() : null;
    }
}

public sealed class ExecuteParkLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteLandLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteLandWaterLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteParkWaterLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteMoorLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteBeachLegNode : ExecuteTerminalLegNode { }
public sealed class ExecuteDockLegNode : ExecuteTerminalLegNode { }
