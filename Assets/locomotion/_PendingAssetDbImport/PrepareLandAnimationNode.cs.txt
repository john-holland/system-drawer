using UnityEngine;

/// <summary>
/// Prepares parkour land BT IK: resolves terminus landing goal, loads landPrep from ABT, activates
/// <see cref="ParkourLandAnimationDriver"/> and sets <see cref="GoalType.Land"/> on the tree.
/// </summary>
public sealed class PrepareLandAnimationNode : TravelContextBehaviorTreeNode, ITravelExecutionContextConsumer
{
    [Tooltip("Acrobatics segment providing animationGroupTag / stunt zone / terminus.")]
    public MultiModalSegment segment;

    [Tooltip("Override landing world point; otherwise terminus / segment end.")]
    public Vector3 landingGoalOverride;

    [Tooltip("When true, use landingGoalOverride instead of segment terminus.")]
    public bool useLandingGoalOverride;

    [Tooltip("How long the land driver stays active.")]
    public float landDurationSeconds = 1.2f;

    TravelExecutionContext _injected;

    void Awake()
    {
        nodeType = NodeType.Action;
    }

    public void SetTravelExecutionContext(TravelExecutionContext ctx) => _injected = ctx;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        TravelExecutionContext ctx = Ctx ?? _injected;
        string tag = ResolveTag(ctx);
        if (!ParkourLandAnimationDriver.IsLandingTag(tag))
            return BehaviorTreeStatus.Success;

        Vector3 goalWorld = ResolveLandingGoal(ctx);
        GameObject host = ResolveHost(tree, ctx);
        if (host == null)
            return BehaviorTreeStatus.Success;

        ParkourLandAnimationDriver driver = ParkourLandAnimationDriver.FindOrCreate(host);
        PhysicsIKTrainingCategory category = ParkourLandAnimationDriver.CategoryForTag(tag);
        RagdollAnimationSetManager animMgr = ctx != null ? ctx.animationSetManager : null;
        if (animMgr == null && host != null)
            animMgr = host.GetComponentInChildren<RagdollAnimationSetManager>();

        LandAnimationPrep prep = ParkourLandAnimationDriver.ResolvePrepFromSets(animMgr, tag, category);
        float duration = landDurationSeconds;
        if (prep != null && prep.prepareLeadSeconds > 0f)
            duration = Mathf.Max(duration, prep.prepareLeadSeconds + 0.5f);

        driver.PlayLanding(tag, goalWorld, prep, duration);

        if (tree != null)
            tree.currentGoal = prep.BuildGoalAt(goalWorld);

        return BehaviorTreeStatus.Success;
    }

    string ResolveTag(TravelExecutionContext ctx)
    {
        if (segment != null && !string.IsNullOrEmpty(segment.animationGroupTag))
            return segment.animationGroupTag;
        if (ctx != null && !string.IsNullOrEmpty(ctx.animationGroupTag))
            return ctx.animationGroupTag;
        return null;
    }

    Vector3 ResolveLandingGoal(TravelExecutionContext ctx)
    {
        if (useLandingGoalOverride)
            return landingGoalOverride;

        if (segment != null)
        {
            if (segment.stuntZoneRef != null)
                return segment.stuntZoneRef.transform.position;
            if (segment.waypoints != null && segment.waypoints.Count > 0)
                return segment.waypoints[segment.waypoints.Count - 1];
            if (segment.segmentEnd.sqrMagnitude > 1e-6f)
                return segment.segmentEnd;
        }

        if (ctx != null && ctx.transitionWorld.sqrMagnitude > 1e-6f)
            return ctx.transitionWorld;

        return landingGoalOverride;
    }

    static GameObject ResolveHost(BehaviorTree tree, TravelExecutionContext ctx)
    {
        if (ctx?.ragdollSystem != null)
            return ctx.ragdollSystem.gameObject;
        if (ctx?.travelAgent != null)
            return ctx.travelAgent.gameObject;
        if (tree != null)
        {
            RagdollSystem ragdoll = tree.GetComponent<RagdollSystem>() ?? tree.GetComponentInParent<RagdollSystem>();
            if (ragdoll != null)
                return ragdoll.gameObject;
            return tree.gameObject;
        }
        return null;
    }
}
