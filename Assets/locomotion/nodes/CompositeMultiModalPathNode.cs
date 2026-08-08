using System.Collections.Generic;
using UnityEngine;
using Weather;

/// <summary>
/// Behavior-tree sequence that builds children from <see cref="GenericTraversibilityPlannerSolver"/> output.
/// </summary>
public class CompositeMultiModalPathNode : BehaviorTreeNode
{
    [Header("Planning")]
    public Vector3 origin;
    public Vector3 destination;
    public HierarchicalPathingSolver pathfindingSolver;
    public List<GoodSection> toolSections = new List<GoodSection>();
    public List<GoodSection> acrobaticsSections = new List<GoodSection>();
    [Range(0f, 1f)] public float requireAsset01 = 0.5f;
    [Range(0f, 1f)] public float requireType01 = 0.5f;
    public VehicleActor preferredVehicle;

    [Header("Risk / safety band (NaN = unset)")]
    public float maxRisk01 = float.NaN;
    public float minRisk01 = float.NaN;
    public float minSafety01 = float.NaN;
    public float maxSafety01 = float.NaN;
    public StuntmanPlannerService stuntmanPlanner;
    public SafetyWardenPlannerService safetyWardenPlanner;
    public WrestlingPlannerService wrestlingPlanner;
    public RefereeWardenPlannerService refereeWardenPlanner;
    public LoveMakingPlannerService loveMakingPlanner;
    public ConsentWardenPlannerService consentWardenPlanner;
    public CombatPlannerService combatPlanner;
    public SafetyLockWardenPlannerService safetyLockWardenPlanner;

    [Header("Execution")]
    public float waypointReachedDistance = 0.5f;
    [Tooltip("When > 0, limits the number of plan legs (not individual waypoints).")]
    public int maxPathLength = 0;
    public bool tryToolBridgeWhenNoWalk = true;

    [Header("Flying cards (Fly segments)")]
    public bool useFlyingCardsForFlySegments;
    public GameObject goalTarget;

    [Header("Multibody travel (optional)")]
    [Tooltip("When enabled, post-processes the built plan against other TravelAgent instances and cached dynamic actors.")]
    public bool applyMultibodyAdjustment;

    [Tooltip("Policy source for multibody fields (clearance, masks, pace). Defaults to TravelAgent on the behavior tree object hierarchy when null.")]
    public TravelAgent multibodyPolicySource;

    [Header("Timeline planner (optional)")]
    public PlannerTimelineOptions plannerTimelineOptions = PlannerTimelineOptions.DefaultLegacy();

    [Header("Mode transition activations")]
    [Tooltip("Run matching activation subtrees once when TravelLegMode changes between plan legs.")]
    public List<TravelModeTransitionBinding> modeTransitionBindings = new List<TravelModeTransitionBinding>();

    bool pathBuilt;
    TravelExecutionContextProvider _contextProvider;
    TravelAgent _resolvedTravelAgent;

    void Awake()
    {
        nodeType = NodeType.Sequence;
        if (pathfindingSolver == null)
            SceneServiceLookup.TryResolve("pathing.hierarchical", out pathfindingSolver);
    }

    public override void OnEnter(BehaviorTree tree)
    {
        bool btOk = tree != null && tree.lastStatus == BehaviorTreeStatus.Success;
        if (!PathReplacementGate.CanReplacePath(btOk))
            return;
        pathBuilt = false;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (pathfindingSolver == null)
            return BehaviorTreeStatus.Failure;

        if (!pathBuilt)
        {
            if (!BuildFromPlanner(tree))
                return BehaviorTreeStatus.Failure;
            pathBuilt = true;
        }

        if (children == null || children.Count == 0)
            return BehaviorTreeStatus.Success;

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] == null)
                continue;
            BehaviorTreeStatus childStatus = children[i].Execute(tree);
            if (childStatus == BehaviorTreeStatus.Failure)
            {
                _contextProvider?.Clear();
                return BehaviorTreeStatus.Failure;
            }
            if (childStatus == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
        }

        _contextProvider?.Clear();
        return BehaviorTreeStatus.Success;
    }

    /// <summary>Test hook: build leg hierarchy from a plan without running the planner.</summary>
    public bool BuildChildrenFromPlanForTests(GenericMultiModalPathPlan plan, BehaviorTree tree)
    {
        return BuildChildrenFromPlan(plan, tree);
    }

    bool BuildFromPlanner(BehaviorTree tree)
    {
        Vector3 queryPos = origin;
        float queryT = 0f;
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll != null && ragdoll.transform != null)
            queryPos = ragdoll.transform.position;

        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = preferredVehicle,
            maxRisk01 = maxRisk01,
            minRisk01 = minRisk01,
            minSafety01 = minSafety01,
            maxSafety01 = maxSafety01
        };

        PlannerTimelineOptions tl = plannerTimelineOptions;
        GenericMultiModalPathPlan plan = GenericTraversibilityPlannerSolver.BuildPlan(
            origin,
            destination,
            pathfindingSolver,
            toolSections,
            acrobaticsSections,
            queryPos,
            queryT,
            hints,
            tryToolBridgeWhenNoWalk,
            goalTarget,
            PhysicalPathingMedium.Air,
            in tl);

        GameObject actorGo = tree != null ? tree.gameObject : gameObject;
        if (plan != null)
        {
            ConsiderStuntmanHints.EnrichPlan(plan, actorGo, origin, destination);
            ConsiderSafetyWardenHints.EnrichPlan(plan, actorGo, origin, destination);
            plan = TravelRiskPlannerPipeline.Apply(plan, hints, actorGo, stuntmanPlanner, safetyWardenPlanner,
                wrestlingPlanner, refereeWardenPlanner, loveMakingPlanner, consentWardenPlanner,
                combatPlanner, safetyLockWardenPlanner);
        }

        if (plan == null || plan.IsEmpty)
            return false;

        if (applyMultibodyAdjustment)
        {
            TravelAgent policy = ResolveTravelAgent(tree);
            if (policy != null && policy.multibody != null && policy.multibody.enableMultibody)
            {
                Vector3 actorWorld = policy.ResolveMultibodyActorWorld();
                plan = TravelMultibodyPathAdjuster.Adjust(plan, policy.multibody, actorWorld, pathfindingSolver, policy);
            }
        }

        return BuildChildrenFromPlan(plan, tree);
    }

    TravelAgent ResolveTravelAgent(BehaviorTree tree)
    {
        if (multibodyPolicySource != null)
            return multibodyPolicySource;
        return tree != null ? tree.GetComponentInParent<TravelAgent>() : null;
    }

    bool BuildChildrenFromPlan(GenericMultiModalPathPlan plan, BehaviorTree tree)
    {
        if (children == null)
            children = new List<BehaviorTreeNode>();
        else
        {
            foreach (BehaviorTreeNode child in children)
            {
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }
            children.Clear();
        }

        _resolvedTravelAgent = ResolveTravelAgent(tree);
        _contextProvider = TravelExecutionContextProvider.Ensure(gameObject, tree, _resolvedTravelAgent);

        PhysicsCardSolver cardSolver = tree != null ? tree.GetComponentInParent<PhysicsCardSolver>() : null;

        if (plan.segments == null)
            return false;

        for (int segIndex = 0; segIndex < plan.segments.Count; segIndex++)
        {
            MultiModalSegment seg = plan.segments[segIndex];
            if (seg == null)
                continue;

            TravelLegMode prevMode = segIndex > 0 ? plan.segments[segIndex - 1].mode : seg.mode;

            var legGo = new GameObject($"Leg_{segIndex}_{seg.mode}");
            legGo.transform.SetParent(transform, worldPositionStays: false);
            var legNode = legGo.AddComponent<TravelLegSequenceNode>();
            legNode.provider = _contextProvider;
            legNode.composite = this;
            legNode.segment = seg.CloneShallowRefs();
            legNode.segmentIndex = segIndex;
            legNode.legMode = seg.mode;
            legNode.previousLegMode = prevMode;
            legNode.transitionWorld = ResolveTransitionWorld(seg);
            legNode.travelAgent = _resolvedTravelAgent;

            if (segIndex > 0 && prevMode != seg.mode)
                TryAddTransitionChild(legNode, prevMode, seg.mode, seg, segIndex, tree);

            AddLegLocomotionChildren(legNode, seg, tree, cardSolver);
            children.Add(legNode);
        }

        if (maxPathLength > 0 && children.Count > maxPathLength)
        {
            while (children.Count > maxPathLength)
            {
                BehaviorTreeNode last = children[children.Count - 1];
                children.RemoveAt(children.Count - 1);
                if (last != null)
                    DestroyImmediate(last.gameObject);
            }
        }

        return children.Count > 0;
    }

    static Vector3 ResolveTransitionWorld(MultiModalSegment seg)
    {
        if (seg?.waypoints != null && seg.waypoints.Count > 0)
            return seg.waypoints[0];
        return seg != null ? seg.segmentEnd : Vector3.zero;
    }

    void TryAddTransitionChild(
        TravelLegSequenceNode legNode,
        TravelLegMode from,
        TravelLegMode to,
        MultiModalSegment seg,
        int segIndex,
        BehaviorTree tree)
    {
        if (!TravelModeTransitionBinding.TryResolve(from, to, modeTransitionBindings, out TravelModeTransitionBinding binding))
            return;

        var transGo = new GameObject($"Transition_{from}_to_{to}");
        transGo.transform.SetParent(legNode.transform, worldPositionStays: false);
        var transNode = transGo.AddComponent<TravelModeTransitionSequenceNode>();
        transNode.provider = _contextProvider;
        transNode.composite = this;
        transNode.segment = legNode.segment;
        transNode.segmentIndex = segIndex;
        transNode.fromMode = from;
        transNode.toMode = to;
        transNode.previousLegMode = legNode.previousLegMode;
        transNode.travelAgent = _resolvedTravelAgent;

        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();
        legNode.children.Add(transNode);

        CloneActivationSubtree(transNode, binding, tree);
    }

    void CloneActivationSubtree(TravelModeTransitionSequenceNode host, TravelModeTransitionBinding binding, BehaviorTree tree)
    {
        if (host.children == null)
            host.children = new List<BehaviorTreeNode>();

        if (binding.activationRoot != null)
        {
            GameObject cloneRoot = Instantiate(binding.activationRoot, host.transform);
            cloneRoot.name = binding.activationRoot.name + "_Instance";
            BehaviorTreeNode rootBt = cloneRoot.GetComponent<BehaviorTreeNode>();
            if (rootBt != null)
                host.children.Add(rootBt);
            for (int c = 0; c < cloneRoot.transform.childCount; c++)
            {
                BehaviorTreeNode childBt = cloneRoot.transform.GetChild(c).GetComponent<BehaviorTreeNode>();
                if (childBt != null && childBt != rootBt)
                    host.children.Add(childBt);
            }
        }

        if (binding.activationNodes != null)
        {
            for (int i = 0; i < binding.activationNodes.Count; i++)
            {
                BehaviorTreeNode template = binding.activationNodes[i];
                if (template == null)
                    continue;
                GameObject cloneGo = Instantiate(template.gameObject, host.transform);
                cloneGo.name = template.gameObject.name + "_Instance";
                BehaviorTreeNode cloneNode = cloneGo.GetComponent<BehaviorTreeNode>();
                if (cloneNode != null)
                    host.children.Add(cloneNode);
            }
        }
    }

    void AddLegLocomotionChildren(
        TravelLegSequenceNode legNode,
        MultiModalSegment seg,
        BehaviorTree tree,
        PhysicsCardSolver cardSolver)
    {
        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();

        switch (seg.mode)
        {
            case TravelLegMode.ToolBridge:
                AddToolSegment(legNode, seg);
                break;

            case TravelLegMode.Acrobatics:
                if (ParkourLandAnimationDriver.IsLandingTag(seg != null ? seg.animationGroupTag : null))
                    AddPrepareLandNode(legNode, seg);
                AddToolSegment(legNode, seg);
                break;

            case TravelLegMode.Park:
            case TravelLegMode.Land:
            case TravelLegMode.LandWater:
            case TravelLegMode.Moor:
            case TravelLegMode.ParkWater:
            case TravelLegMode.Beach:
            case TravelLegMode.Dock:
                AddTerminalSegment(legNode, seg);
                break;

            case TravelLegMode.Fly:
                if (useFlyingCardsForFlySegments && cardSolver != null && cardSolver.flyingCardConfig != null &&
                    seg.waypoints != null && seg.waypoints.Count >= 2)
                {
                    if (!AppendFlyingCardChildren(legNode, seg.waypoints, tree, cardSolver))
                        AppendWaypointChain(legNode, seg.waypoints, seg);
                }
                else
                    AppendWaypointChain(legNode, seg.waypoints, seg);
                break;

            case TravelLegMode.Drive:
                AppendDriveWaypointChain(legNode, seg, tree);
                break;

            default:
                AppendWaypointChain(legNode, seg.waypoints, seg);
                break;
        }
    }

    void AppendWaypointChain(TravelLegSequenceNode legNode, List<Vector3> path, MultiModalSegment seg)
    {
        if (path == null || legNode == null)
            return;

        TravelLegMode mode = seg != null ? seg.mode : TravelLegMode.Walk;
        PhysicalPathingMedium medium = seg != null ? seg.medium : PhysicalPathingMedium.Unspecified;

        foreach (Vector3 wp in path)
        {
            GameObject go = new GameObject($"Waypoint_{legNode.children.Count}");
            go.transform.SetParent(legNode.transform, worldPositionStays: false);
            MoveToWaypointNode node = go.AddComponent<MoveToWaypointNode>();
            node.waypoint = wp;
            node.reachedDistance = waypointReachedDistance;
            node.travelLegMode = mode;
            node.physicalMedium = medium;
            legNode.children.Add(node);
        }
    }

    void AddTerminalSegment(TravelLegSequenceNode legNode, MultiModalSegment seg)
    {
        if (legNode == null || seg == null)
            return;
        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();

        GameObject go = new GameObject($"Terminal_{seg.mode}");
        go.transform.SetParent(legNode.transform, worldPositionStays: false);
        ExecuteTerminalLegNode node = go.AddComponent<ExecuteTerminalLegNode>();
        node.segment = seg;
        node.reachedDistance = waypointReachedDistance;
        legNode.children.Add(node);
    }

    void AppendDriveWaypointChain(TravelLegSequenceNode legNode, MultiModalSegment seg, BehaviorTree tree)
    {
        if (seg?.waypoints == null || legNode == null)
            return;

        PhysicsCardSolver cardSolver = tree != null ? tree.GetComponentInParent<PhysicsCardSolver>() : null;
        DrivingPhysicsCardSolver drivingSolver = tree != null ? tree.GetComponentInParent<DrivingPhysicsCardSolver>() : null;
        HierarchicalPathingSolver pathSolver = pathfindingSolver;

        foreach (Vector3 wp in seg.waypoints)
        {
            GameObject go = new GameObject($"DriveWaypoint_{legNode.children.Count}");
            go.transform.SetParent(legNode.transform, worldPositionStays: false);
            TravelLegDriveNode node = go.AddComponent<TravelLegDriveNode>();
            node.waypoint = wp;
            node.reachedDistance = waypointReachedDistance;
            node.travelLegMode = TravelLegMode.Drive;
            node.physicalMedium = seg.medium;
            node.cardSolver = cardSolver;
            node.drivingSolver = drivingSolver;
            node.pathingSolver = pathSolver;
            node.vehicleHint = seg.optionalVehicleHint != null ? seg.optionalVehicleHint : preferredVehicle;
            legNode.children.Add(node);
        }
    }

    bool AppendFlyingCardChildren(
        TravelLegSequenceNode legNode,
        List<Vector3> path,
        BehaviorTree tree,
        PhysicsCardSolver solver)
    {
        if (path == null || path.Count < 2 || solver == null || solver.flyingCardConfig == null || legNode == null)
            return false;

        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        RagdollState state = ragdoll != null ? ragdoll.GetCurrentState() : null;
        float fuel = solver.flyingCardConfig.fuelCapacity;
        Vector3 prev = path[0];
        int countBefore = legNode.children.Count;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 to = path[i];
            GoodSection card = solver.GenerateFlyingCard(prev, to, state, solver.flyingCardConfig, solver.useJetModeForFlyingGoal, ref fuel);
            if (card == null)
                return false;

            GameObject nodeObj = new GameObject($"Flying_{legNode.children.Count}");
            nodeObj.transform.SetParent(legNode.transform, worldPositionStays: false);
            ExecuteToolTraversabilityNode execNode = nodeObj.AddComponent<ExecuteToolTraversabilityNode>();
            execNode.card = card;
            execNode.toolUseTo = to;
            execNode.reachedDistance = waypointReachedDistance;
            legNode.children.Add(execNode);
            prev = to;
            if (state != null)
            {
                state = state.CopyState();
                state.rootPosition = to;
            }
        }

        return legNode.children.Count > countBefore;
    }

    void AddPrepareLandNode(TravelLegSequenceNode legNode, MultiModalSegment seg)
    {
        if (legNode == null || seg == null)
            return;
        if (legNode.children == null)
            legNode.children = new List<BehaviorTreeNode>();

        GameObject go = new GameObject($"PrepareLand_{legNode.children.Count}");
        go.transform.SetParent(legNode.transform, worldPositionStays: false);
        PrepareLandAnimationNode node = go.AddComponent<PrepareLandAnimationNode>();
        node.segment = seg;
        legNode.children.Add(node);
    }

    void AddToolSegment(TravelLegSequenceNode legNode, MultiModalSegment seg)
    {
        if (seg.card == null || legNode == null)
            return;

        GameObject toolNodeObj = new GameObject($"ToolUse_{legNode.children.Count}");
        toolNodeObj.transform.SetParent(legNode.transform, worldPositionStays: false);
        ExecuteToolTraversabilityNode toolNode = toolNodeObj.AddComponent<ExecuteToolTraversabilityNode>();
        toolNode.card = seg.card;
        if (seg.tools != null && seg.tools.Count > 0)
        {
            toolNode.tools = new List<GameObject>(seg.tools);
            toolNode.tool = seg.tools[0];
        }
        toolNode.toolUseTo = destination;
        if (seg.waypoints != null && seg.waypoints.Count > 0)
            toolNode.toolUseTo = seg.waypoints[seg.waypoints.Count - 1];
        else if (seg.segmentEnd.sqrMagnitude > 1e-6f)
            toolNode.toolUseTo = seg.segmentEnd;
        toolNode.reachedDistance = waypointReachedDistance;
        legNode.children.Add(toolNode);
    }
}
