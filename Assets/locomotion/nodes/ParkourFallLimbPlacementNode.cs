using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parkour fall BT action — drives procedural fall AnimationCurves and places limb IK targets over time.
/// Samples <see cref="ParkourLandAnimationDriver"/> impact when present.
/// </summary>
[AddComponentMenu("Locomotion/Parkour/Parkour Fall Limb Placement Node")]
public sealed class ParkourFallLimbPlacementNode : BehaviorTreeNode
{
    public ParkourFallProceduralCurve fallCurve = new ParkourFallProceduralCurve();
    public ParkourLandAnimationDriver landDriver;
    public Transform bodyAnchor;
    public float durationSec = 1.25f;
    public string animationGroupTag = ParkourAnimationGroup.FallRolls;
    public bool setLandGoal = true;

    float _t;
    Vector3 _startWorld;
    readonly List<string> _limbNames = new List<string>();

    void Awake()
    {
        nodeType = NodeType.Action;
        fallCurve?.EnsureDefaultLimbs();
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _t = 0f;
        EnsureAnchors(tree);
        _startWorld = bodyAnchor != null ? bodyAnchor.position : (tree != null ? tree.transform.position : transform.position);
        if (landDriver == null && tree != null)
        {
            var host = tree.GetComponentInParent<RagdollSystem>()?.gameObject ?? tree.gameObject;
            landDriver = ParkourLandAnimationDriver.FindOrCreate(host);
        }
        if (landDriver != null && !landDriver.hasLandingGoal)
        {
            Vector3 goal = _startWorld + Vector3.down * 1.1f + (bodyAnchor != null ? bodyAnchor.forward : transform.forward) * 0.8f;
            var prep = new LandAnimationPrep();
            prep.EnsureReady();
            landDriver.PlayLanding(animationGroupTag, goal, prep, durationSec);
        }
        if (setLandGoal && tree != null)
        {
            Vector3 landAt = landDriver != null && landDriver.hasLandingGoal
                ? landDriver.landingGoalWorld
                : _startWorld + Vector3.down;
            tree.currentGoal = new BehaviorTreeGoal
            {
                goalName = "parkour_fall",
                type = GoalType.Land,
                targetPosition = landAt,
                priority = 9,
                hitLimbNames = new List<string>(_limbNames)
            };
        }
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        _t += Time.deltaTime;
        float raw = durationSec > 1e-4f ? Mathf.Clamp01(_t / durationSec) : 1f;
        float t01 = fallCurve != null ? fallCurve.EvaluateProgress(raw) : raw;
        ApplyPlacement(t01);

        if (raw < 1f)
        {
            status = BehaviorTreeStatus.Running;
            return status;
        }
        status = BehaviorTreeStatus.Success;
        return status;
    }

    void ApplyPlacement(float t01)
    {
        if (fallCurve == null || bodyAnchor == null) return;
        float drop = fallCurve.bodyDropMeters != null ? fallCurve.bodyDropMeters.Evaluate(t01) : 0f;
        float fwd = fallCurve.forwardMeters != null ? fallCurve.forwardMeters.Evaluate(t01) : 0f;
        Vector3 bodyPos = _startWorld
                          + Vector3.up * drop
                          + bodyAnchor.forward * fwd;
        // Soft pull body toward curve sample (does not fight rigidbody when kinematic-only).
        if (!Application.isPlaying || bodyAnchor.GetComponentInParent<Rigidbody>() == null)
            bodyAnchor.position = Vector3.Lerp(bodyAnchor.position, bodyPos, 0.5f);

        float impact = landDriver != null ? landDriver.SampleImpact01(t01) : t01;
        for (int i = 0; i < fallCurve.limbs.Count; i++)
        {
            var slot = fallCurve.limbs[i];
            if (slot == null || slot.target == null) continue;
            Vector3 local = slot.SampleLocal(t01);
            // Bias placement toward ground as impact rises.
            local.y -= impact * 0.15f;
            slot.target.position = bodyAnchor.TransformPoint(local);
        }
    }

    void EnsureAnchors(BehaviorTree tree)
    {
        fallCurve ??= new ParkourFallProceduralCurve();
        fallCurve.EnsureDefaultLimbs();
        if (bodyAnchor == null)
        {
            var ragdoll = tree != null
                ? tree.GetComponentInParent<RagdollSystem>() ?? tree.GetComponent<RagdollSystem>()
                : null;
            bodyAnchor = ragdoll != null ? ragdoll.transform : (tree != null ? tree.transform : transform);
        }

        _limbNames.Clear();
        Transform targetsRoot = transform.Find("LimbTargets");
        if (targetsRoot == null)
        {
            var go = new GameObject("LimbTargets");
            go.transform.SetParent(transform, false);
            targetsRoot = go.transform;
        }

        for (int i = 0; i < fallCurve.limbs.Count; i++)
        {
            var slot = fallCurve.limbs[i];
            if (slot == null) continue;
            _limbNames.Add(slot.limbId);
            if (slot.target != null) continue;
            Transform existing = targetsRoot.Find(slot.limbId);
            if (existing == null)
            {
                var tGo = new GameObject(slot.limbId);
                tGo.transform.SetParent(targetsRoot, false);
                existing = tGo.transform;
            }
            slot.target = existing;
        }
    }
}
