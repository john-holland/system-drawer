using UnityEngine;

/// <summary>Suture BT: stitch hold, center line, close progress, pin/drag sites, hold potential.</summary>
public sealed class SutureBehaviorNode : BehaviorTreeNode
{
    public WoundSiteRuntime woundHost;
    public WoundSite wound;
    public Transform pinSite;
    public Transform dragSite;
    public Transform stitchHold;
    [Range(0f, 1f)] public float stitchHoldPotential = 0.5f;
    public float closeSpeed = 0.25f;
    float _t;

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        if (woundHost == null && tree != null)
            woundHost = tree.GetComponent<WoundSiteRuntime>();
        if (wound == null && woundHost != null && woundHost.wounds.Count > 0)
            wound = woundHost.wounds[woundHost.wounds.Count - 1];
        if (wound?.spec != null)
        {
            wound.sutured = true;
            wound.spec.stitchHoldPotential = stitchHoldPotential;
            wound.spec.open = false;
        }
        status = BehaviorTreeStatus.Running;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (wound?.spec == null) return BehaviorTreeStatus.Failure;
        _t += Time.deltaTime;
        float rip = WoundSiteRuntime.EffectiveRipRisk(wound.spec.stitchHoldPotential);
        if (rip > 0.8f && Random.value < rip * Time.deltaTime)
        {
            wound.spec.closeAmount = Mathf.Max(0f, wound.spec.closeAmount - 0.2f);
            wound.spec.open = true;
            return BehaviorTreeStatus.Failure;
        }

        // Center-line close along spline progress
        wound.spec.closeAmount = Mathf.MoveTowards(wound.spec.closeAmount, 1f, closeSpeed * Time.deltaTime);
        if (pinSite != null && dragSite != null && stitchHold != null)
        {
            Vector3 mid = Vector3.Lerp(pinSite.position, dragSite.position, wound.spec.closeAmount);
            stitchHold.position = Vector3.Lerp(stitchHold.position, mid, 0.4f);
        }

        if (wound.spec.closeAmount >= 0.999f)
        {
            wound.spec.showHealedFillet = true;
            wound.spec.healStartTime = Time.time;
            return BehaviorTreeStatus.Success;
        }
        return BehaviorTreeStatus.Running;
    }
}
