using UnityEngine;

/// <summary>Fork lift bin — uses TrashWarden judgment.</summary>
public sealed class GarbageForkLiftNode : BehaviorTreeNode
{
    public GarbageTruckVehicleRagdoll truck;
    public TrashBinRuntime bin;
    public TrashWarden warden;
    public float durationSec = 1.2f;
    float _t;
    bool _ok;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _ok = false;
        if (truck == null && tree != null)
            truck = tree.GetComponentInParent<GarbageTruckVehicleRagdoll>();
        if (warden == null)
            warden = Object.FindFirstObjectByType<TrashWarden>();
        if (bin == null && warden != null && warden.targets.Count > 0)
            bin = warden.targets[0].bin;
        if (warden != null && warden.JudgeBinInteract(bin, truck) == PetJudgment.Deny)
        {
            _ok = false;
            return;
        }
        _ok = truck != null && truck.LiftBin(bin);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return status;
        }
        status = _ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}

/// <summary>Shake trash out into truck — gated by TrashWarden.ShouldShakeOut.</summary>
public sealed class GarbageShakeIntoTruckNode : BehaviorTreeNode
{
    public GarbageTruckVehicleRagdoll truck;
    public TrashBinRuntime bin;
    public TrashWarden warden;
    public float durationSec = 1.5f;
    float _t;
    float _taken;
    bool _ready;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _taken = 0f;
        _ready = false;
        if (truck == null && tree != null)
            truck = tree.GetComponentInParent<GarbageTruckVehicleRagdoll>();
        if (warden == null)
            warden = Object.FindFirstObjectByType<TrashWarden>();
        if (bin == null && warden != null && warden.targets.Count > 0)
            bin = warden.targets[0].bin;
        if (warden != null && !warden.ShouldShakeOut(bin))
        {
            _ready = false;
            return;
        }
        _ready = true;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!_ready)
        {
            status = BehaviorTreeStatus.Failure;
            return status;
        }
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return status;
        }
        if (_taken <= 0f)
            _taken = truck != null ? truck.ShakeBinIntoHopper(bin, warden) : 0f;
        status = _taken > 0f ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}

public sealed class GarbageCompactNode : BehaviorTreeNode
{
    public GarbageTruckVehicleRagdoll truck;
    public float durationSec = 2f;
    float _t;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (truck == null && tree != null)
            truck = tree.GetComponentInParent<GarbageTruckVehicleRagdoll>();
        truck?.SetCompactionActive(true);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return status;
        }
        truck?.SetCompactionActive(false);
        status = BehaviorTreeStatus.Success;
        return status;
    }
}

public sealed class SanitationBagCutNode : BehaviorTreeNode
{
    public SanitationSortingStation station;
    public bool actorIk;
    public float durationSec = 1f;
    float _t;
    bool _ok;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (station == null)
            station = Object.FindFirstObjectByType<SanitationSortingStation>();
        _ok = station != null && station.CutBag(actorIk);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return status;
        }
        status = _ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}
