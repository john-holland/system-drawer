using UnityEngine;

/// <summary>
/// Immutable snapshot of travel leg / mode-transition state for activation behavior-tree nodes.
/// </summary>
public sealed class TravelExecutionContext
{
    public BehaviorTree tree { get; }
    public TravelAgent travelAgent { get; }
    public BaseAmbulatingActor ambulatingActor { get; }
    public RagdollSystem ragdollSystem { get; }
    public RagdollAnimationSetManager animationSetManager { get; }
    public VehicleActor vehicleHint { get; }
    public HierarchicalPathingSolver pathingSolver { get; }
    public int segmentIndex { get; }
    public TravelLegMode legMode { get; }
    public TravelLegMode previousLegMode { get; }
    public bool isModeTransition { get; }
    public TravelLegMode fromMode { get; }
    public TravelLegMode toMode { get; }
    public Vector3 transitionWorld { get; }
    public float estimatedLegTimeSec { get; }
    public PhysicalPathingMedium medium { get; }

    TravelExecutionContext(
        BehaviorTree tree,
        TravelAgent travelAgent,
        BaseAmbulatingActor ambulatingActor,
        RagdollSystem ragdollSystem,
        RagdollAnimationSetManager animationSetManager,
        VehicleActor vehicleHint,
        HierarchicalPathingSolver pathingSolver,
        int segmentIndex,
        TravelLegMode legMode,
        TravelLegMode previousLegMode,
        bool isModeTransition,
        TravelLegMode fromMode,
        TravelLegMode toMode,
        Vector3 transitionWorld,
        float estimatedLegTimeSec,
        PhysicalPathingMedium medium)
    {
        this.tree = tree;
        this.travelAgent = travelAgent;
        this.ambulatingActor = ambulatingActor;
        this.ragdollSystem = ragdollSystem;
        this.animationSetManager = animationSetManager;
        this.vehicleHint = vehicleHint;
        this.pathingSolver = pathingSolver;
        this.segmentIndex = segmentIndex;
        this.legMode = legMode;
        this.previousLegMode = previousLegMode;
        this.isModeTransition = isModeTransition;
        this.fromMode = fromMode;
        this.toMode = toMode;
        this.transitionWorld = transitionWorld;
        this.estimatedLegTimeSec = estimatedLegTimeSec;
        this.medium = medium;
    }

    /// <summary>Build context for a plan leg or mode transition.</summary>
    public static TravelExecutionContext Build(
        BehaviorTree tree,
        CompositeMultiModalPathNode composite,
        MultiModalSegment seg,
        int segIndex,
        TravelLegMode previousMode,
        bool isTransition,
        TravelLegMode from,
        TravelLegMode to,
        TravelAgent travelAgentOverride = null)
    {
        TravelAgent agent = travelAgentOverride;
        if (agent == null && composite != null)
        {
            agent = composite.multibodyPolicySource;
            if (agent == null && tree != null)
                agent = tree.GetComponentInParent<TravelAgent>();
        }

        BaseAmbulatingActor amb = agent != null ? agent.ambulatingActor : null;
        if (amb == null && tree != null)
            amb = tree.GetComponentInParent<BaseAmbulatingActor>();

        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null && tree != null)
            ragdoll = tree.GetComponentInParent<RagdollSystem>();

        RagdollAnimationSetManager animMgr = agent != null ? agent.ragdollAnimationSetManager : null;
        if (animMgr == null && tree != null)
            animMgr = tree.GetComponentInParent<RagdollAnimationSetManager>();

        VehicleActor vehicle = seg != null && seg.optionalVehicleHint != null
            ? seg.optionalVehicleHint
            : composite != null ? composite.preferredVehicle : null;

        HierarchicalPathingSolver solver = composite != null ? composite.pathfindingSolver : null;

        TravelLegMode leg = seg != null ? seg.mode : TravelLegMode.Walk;
        PhysicalPathingMedium med = seg != null ? seg.medium : PhysicalPathingMedium.Unspecified;
        float estTime = seg != null ? seg.estimatedTimeSec : 0f;
        Vector3 transWorld = ResolveTransitionWorld(seg);

        return new TravelExecutionContext(
            tree,
            agent,
            amb,
            ragdoll,
            animMgr,
            vehicle,
            solver,
            segIndex,
            leg,
            previousMode,
            isTransition,
            from,
            to,
            transWorld,
            estTime,
            med);
    }

    static Vector3 ResolveTransitionWorld(MultiModalSegment seg)
    {
        if (seg?.waypoints == null || seg.waypoints.Count == 0)
            return seg != null ? seg.segmentEnd : Vector3.zero;
        return seg.waypoints[0];
    }
}
