using UnityEngine;

/// <summary>Shared horticulture BT helpers for park tool use.</summary>
public class ParkHorticultureNode : BehaviorTreeNode
{
    public LotGrassGrowthController grass;
    public ParkRuntime park;
    public float durationSec = 1.5f;
    protected float _t;
    protected bool _done;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        _done = false;
        if (grass == null && tree != null)
            grass = tree.GetComponentInParent<LotGrassGrowthController>();
        if (park == null)
            park = Object.FindFirstObjectByType<ParkRuntime>();
        if (grass == null && park?.PrimaryLot() != null)
            grass = park.PrimaryLot().grass;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        if (_t < durationSec)
        {
            status = BehaviorTreeStatus.Running;
            return BehaviorTreeStatus.Running;
        }
        if (!_done)
        {
            Apply();
            _done = true;
        }
        status = BehaviorTreeStatus.Success;
        return status;
    }

    protected virtual void Apply() { }
}

public sealed class ParkWeedPullNode : ParkHorticultureNode
{
    protected override void Apply()
    {
        grass?.ApplyCut(transform.position, 0.15f, 0.35f, grass != null ? grass.stageIndex : 0);
        SendMessage("OnNarrativeSchedulerAction", "park_weeding", SendMessageOptions.DontRequireReceiver);
    }
}

public sealed class ParkSeedSpreadNode : ParkHorticultureNode
{
    public string seedId = "seed";
    public bool ridePushHand = true;

    protected override void Apply()
    {
        if (grass != null)
            grass.nextSectionSpawnChance = Mathf.Clamp01(grass.nextSectionSpawnChance + 0.2f);
        SendMessage("OnNarrativeSchedulerAction", "park_seed_spread", SendMessageOptions.DontRequireReceiver);
    }
}

public sealed class ParkHandSeedSowNode : ParkHorticultureNode
{
    protected override void Apply()
    {
        if (grass != null)
        {
            grass.growth01 = Mathf.Max(grass.growth01, 0.05f);
            grass.nextSectionSpawnChance = Mathf.Clamp01(grass.nextSectionSpawnChance + 0.1f);
        }
        SendMessage("OnNarrativeSchedulerAction", "park_hand_seed_sow", SendMessageOptions.DontRequireReceiver);
    }
}

public sealed class ParkWateringNode : ParkHorticultureNode
{
    protected override void Apply()
    {
        if (grass != null)
            grass.TickGrowth(2f);
        SendMessage("OnNarrativeSchedulerAction", "park_watering", SendMessageOptions.DontRequireReceiver);
    }
}

public sealed class ParkHoeingNode : ParkHorticultureNode
{
    protected override void Apply()
    {
        grass?.ApplyCut(transform.position, 0.2f, 0.2f, grass != null ? grass.stageIndex : 0);
        SendMessage("OnNarrativeSchedulerAction", "park_hoeing", SendMessageOptions.DontRequireReceiver);
    }
}

public sealed class ParkFlowerTendingNode : ParkHorticultureNode
{
    protected override void Apply()
    {
        grass?.TryAdvanceStage();
        SendMessage("OnNarrativeSchedulerAction", "park_flower_tending", SendMessageOptions.DontRequireReceiver);
    }
}
