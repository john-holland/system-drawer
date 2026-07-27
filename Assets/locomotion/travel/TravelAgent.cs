using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Planetary.Celestial;
using Weather;

/// <summary>
/// Read-only snapshot of a planner / behavior node discovered under the actor hierarchy (no UnityEngine.Object refs).
/// </summary>
[Serializable]
public struct TravelDiscoveredNodeInfo
{
    public string displayName;
    public string hierarchyPath;
    public string nodeTypeName;
    public string serializedSummary;
}

/// <summary>
/// Scene visualization, planner snapshot, and hierarchy discovery for multi-modal travel.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Travel Agent")]
public class TravelAgent : MonoBehaviour
{
    [Header("Actor hierarchy")]
    [Tooltip("Optional explicit root for discovery (defaults to this transform).")]
    public Transform actorRootOverride;

    [Tooltip("Ambulating actor marker (human or vehicle); RootTransform used when set.")]
    public BaseAmbulatingActor ambulatingActor;

    [Header("Composition (no animation duplication)")]
    [Tooltip("Ragdoll animation sets composed into path segments (referenced, not duplicated on TravelAgent).")]
    public RagdollAnimationSetManager ragdollAnimationSetManager;

    [Tooltip("When true, travel mode transitions prefer Non-IK kinematic playback when policy resolves.")]
    public bool preferNonIkPlayback;

    [Tooltip("Optional vehicle actor for vehicle-specific modality and path hints.")]
    public VehicleActor hintVehicle;

    [Header("Preview / solver inputs")]
    [Tooltip("Hierarchical pathing solver used when rebuilding the cached multi-modal preview plan.")]
    public HierarchicalPathingSolver pathingSolverForPreview;

    [Tooltip("World-space start position for preview plan rebuild and solver query origin.")]
    public Vector3 previewStartWorld;

    [Tooltip("World-space goal the traversibility planner aims for during preview.")]
    public Vector3 previewGoalWorld;

    [Range(0f, 1f)]
    [Tooltip("Planner bias toward tool/asset segments (0 = prefer acrobatics, 1 = prefer tools).")]
    public float requireAsset01 = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Secondary modality mix bias used by timeline and traversibility scoring.")]
    public float requireType01 = 0.5f;

    [Header("Risk / safety band (NaN = unset)")]
    [Tooltip("Refuse routes with risk above this (e.g. 0.3 = jump over, not out a window).")]
    public float maxRisk01 = float.NaN;
    [Tooltip("Require at least this much risk (e.g. with maxSafety=0.9 ⇒ min risk 0.1).")]
    public float minRisk01 = float.NaN;
    [Tooltip("Require safety >= this (safety = 1 - risk).")]
    public float minSafety01 = float.NaN;
    [Tooltip("Cap safety so some risk remains (maxSafety=0.9 ⇒ risk >= 0.1).")]
    public float maxSafety01 = float.NaN;

    [Header("Stunt / Safety planners")]
    [Tooltip("When set, Stuntman proposes runway/acrobatics/crash legs within the risk band.")]
    public StuntmanPlannerService stuntmanPlanner;
    [Tooltip("When set, Safety Warden gates/rewrites plans outside the risk band.")]
    public SafetyWardenPlannerService safetyWardenPlanner;
    [Tooltip("When set, wrestling planner expands move branches and stamps anim tags.")]
    public WrestlingPlannerService wrestlingPlanner;
    [Tooltip("When set, referee soft-gates high-damage Play spots.")]
    public RefereeWardenPlannerService refereeWardenPlanner;

    [Tooltip("GoodSection cards offered as tool modality candidates for preview planning.")]
    public List<GoodSection> toolSectionsForPreview = new List<GoodSection>();

    [Tooltip("GoodSection cards offered as acrobatics modality candidates for preview planning.")]
    public List<GoodSection> acrobaticsSectionsForPreview = new List<GoodSection>();

    [Tooltip("How authoring-row positions are interpreted (world, narrative volume, or Continuuuum asset ref).")]
    public TravelCoordinateMode coordinateMode = TravelCoordinateMode.World;

    [Header("Spatial authoring (Bedoga / Continuuuum-friendly)")]
    [Tooltip("Wide slot for SpatialGenerator / SpatialGenerator4D when assigned from editor.")]
    public UnityEngine.Object spatialGeneratorSlot;

    [Tooltip("When true and a spatial generator is assigned, raw world fields are treated as overridden by the generator workflow.")]
    public bool disableRawLocationWhenSpatialGeneratorAssigned;

    [Tooltip("When true with static seed mode, location + asset slot enabled per authoring flow.")]
    public bool staticGeneratorSeedMode;

    [Header("Road network (optional)")]
    [Tooltip("Assign a GameObject with Roads.RoadNetwork (resolved at runtime to avoid asmdef cycle).")]
    public MonoBehaviour roadNetwork;

    [Tooltip("Optional binding that maps road network segments to travel authoring and wear snapshots.")]
    public RoadTravelBinding roadTravelBinding;

    [Header("Preview navigation (editor)")]
    [Tooltip("Scene-view zoom target when using Zoom to fit or stepping segments.")]
    public TravelPreviewFitMode previewFitMode = TravelPreviewFitMode.EntirePath;

    [Tooltip("Active plan segment index for preview stepping and segment-only framing.")]
    public int previewSegmentIndex;

    [Header("Travel script (editor authoring)")]
    [Tooltip("Ordered travel script: coordinates, planner hints, narrative nodes, and spatial nodes.")]
    public List<TravelAuthoringRow> authoringRows = new List<TravelAuthoringRow>();

    [Header("Gizmos")]
    [Tooltip("Draw cached travel path, segments, and kinematic overlays in the Scene view.")]
    public bool drawTravelGizmos = true;
    [Tooltip("When multibody runs, also draw the pre-adjustment plan (magenta in scene handles).")]
    public bool drawMultibodyBasePlan = true;

    [Header("Path kinematics (skier track)")]
    [Range(0f, 1f)]
    [Tooltip("Max fraction of total path arc length assignable to reverse samples.")]
    public float reverseLegLimit01 = 0.5f;

    [Tooltip("Read-only after rebuild: sum of segment polyline lengths.")]
    [SerializeField] float totalPathLengthMeters;

    [Tooltip("Read-only: totalPathLengthMeters * reverseLegLimit01.")]
    [SerializeField] float reverseBudgetMeters;

    [Tooltip("Draw speed-colored tick marks along the path in the Scene view.")]
    public bool showVelocityTrack = true;

    [Tooltip("Draw IK solve sample points on path gizmos.")]
    public bool showIkSamples;

    [Tooltip("Highlight how much of the reverse budget the path consumes.")]
    public bool showReverseBudget = true;

    [Range(0.5f, 10f)]
    [Tooltip("Spacing between velocity track tick marks along the path (meters).")]
    public float velocityTrackSpacingMeters = 2f;

    public float TotalPathLengthMeters => totalPathLengthMeters;
    public float ReverseBudgetMeters => reverseBudgetMeters;

    [Header("Multibody travel")]
    [Tooltip("Convoy spacing, peer avoidance, and formation offsets applied after the base planner path.")]
    public TravelAgentMultibodySettings multibody = new TravelAgentMultibodySettings();

    [Header("Multibody formation (optional)")]
    [Tooltip("Agents with the same non-empty id share a formation cohort for slot assignment and optional peer filtering.")]
    public string multibodyFormationGroupId = "";

    [Tooltip("Slot index within cohort when >= 0; -1 = order by stable instance id sort within group.")]
    public int formationSlotIndex = -1;

    [Header("Timeline planner (optional)")]
    public PlannerTimelineOptions plannerTimelineOptions = PlannerTimelineOptions.DefaultLegacy();

    [Header("Terminal placement (Park/Land/Moor/…)")]
    public PlannerTerminalOptions plannerTerminalOptions = PlannerTerminalOptions.Disabled;

    [Tooltip("Optional extra landmark positions merged into the timeline chord graph (world space).")]
    public List<Vector3> timelineExtraLandmarks = new List<Vector3>();

    [SerializeField] GenericMultiModalPathPlan cachedPlan = new GenericMultiModalPathPlan();

    [SerializeField] GenericMultiModalPathPlan cachedPlanBeforeMultibody;

    [SerializeField] List<TravelDiscoveredNodeInfo> discoveredNodes = new List<TravelDiscoveredNodeInfo>();

    [Header("Galactic travel / night sky")]
    [Tooltip("Emit galactic position snapshots for night-sky blending.")]
    public bool emitGalacticPositionEvents = true;
    public float galacticSnapshotMinMoveMeters = 5f;
    public string galacticNearestBodyId;

    [Header("Gravity-aware space pathing")]
    public bool gravityAwarePathingForPreview;
    public Locomotion.Spaceship.GravityAwarePathingSolver gravityPathing = new Locomotion.Spaceship.GravityAwarePathingSolver();

    [Header("Feature Budget / Pathing")]
    [Tooltip("Minimum seconds between automatic preview replans when pathing budget is active.")]
    public float replanIntervalSeconds = 2f;

    GalacticTravelSnapshot _lastGalacticSnapshot;
    Vector3 _lastGalacticEmitPos;
    float _replanTimer;

    /// <summary>Fired when observer crosses SOI, lattice cell, or significant move.</summary>
    public event Action<GalacticTravelSnapshot> GalacticPositionChanged;

    /// <summary>Last rebuilt multi-modal plan (preview / runtime); multibody-adjusted when multibody is enabled.</summary>
    public GenericMultiModalPathPlan CachedPlan => cachedPlan;

    /// <summary>Plan from the traversibility solver before multibody post-process (null when multibody was off for last rebuild).</summary>
    public GenericMultiModalPathPlan CachedPlanBeforeMultibody => cachedPlanBeforeMultibody;

    /// <summary>Discovered nodes from last <see cref="RefreshDiscoveredNodes"/>.</summary>
    public IReadOnlyList<TravelDiscoveredNodeInfo> DiscoveredNodes => discoveredNodes;

    void OnEnable()
    {
        TravelAgentRegistry.Register(this);
    }

    void OnDisable()
    {
        TravelAgentRegistry.Unregister(this);
    }

    void Awake()
    {
        if (ragdollAnimationSetManager == null)
            ragdollAnimationSetManager = GetComponentInChildren<RagdollAnimationSetManager>();
    }

    void Update()
    {
        TickGalacticSnapshots();
        TickPathingBudget();
    }

    void TickGalacticSnapshots()
    {
        if (!emitGalacticPositionEvents)
            return;
        Vector3 pos = ResolveMultibodyActorWorld();
        if ((pos - _lastGalacticEmitPos).sqrMagnitude < galacticSnapshotMinMoveMeters * galacticSnapshotMinMoveMeters
            && !string.IsNullOrEmpty(_lastGalacticSnapshot.nearestBodyId))
            return;

        var registry = GalacticBodyRegistry.Instance;
        string nearestId = galacticNearestBodyId;
        if (registry != null)
        {
            var nearest = registry.FindNearestSceneBody(pos, out _);
            if (nearest != null)
                nearestId = nearest.BodyId;
        }

        var snap = new GalacticTravelSnapshot
        {
            worldPos = pos,
            nearestBodyId = nearestId ?? "",
            surfaceAnchor = pos,
            cellBlendWeight = 1f,
            altitudeBand = Planetary.Composition.LodTier.FullSim
        };
        _lastGalacticSnapshot = snap;
        _lastGalacticEmitPos = pos;
        GalacticPositionChanged?.Invoke(snap);
    }

    void TickPathingBudget()
    {
        if (!Application.isPlaying || pathingSolverForPreview == null)
            return;
        if (!FeatureBudget.IsFeatureActive(FeatureBudgetIds.Pathing))
            return;

        float g = FeatureBudget.GetGranularity(FeatureBudgetIds.Pathing);
        float horizonKm = FeatureBudget.GetRatioEffective(FeatureBudgetRatioFieldIds.HorizonDistanceKm);
        float interval = FeatureBudgetGranularityBridge.ScaleIntervalByGranularity(replanIntervalSeconds, g);
        if (horizonKm > 0f)
            interval *= Mathf.Clamp(horizonKm / 2f, 0.5f, 4f);

        _replanTimer += Time.deltaTime;
        if (_replanTimer < interval)
            return;
        _replanTimer = 0f;
        RebuildCachedPlan();
    }

    public Transform ResolveHierarchyRoot()
    {
        if (ambulatingActor != null)
            return ambulatingActor.transform;
        if (actorRootOverride != null)
            return actorRootOverride;
        return transform;
    }

    /// <summary>World position used as the multibody actor origin at runtime.</summary>
    public Vector3 ResolveMultibodyActorWorld()
    {
        if (ambulatingActor != null)
            return ambulatingActor.transform.position;
        return transform.position;
    }

    /// <summary>Plan polyline peers should use for inference (base plan if present, else current).</summary>
    public GenericMultiModalPathPlan GetPlanReferenceForMultibodyPeer()
    {
        if (cachedPlanBeforeMultibody != null && !cachedPlanBeforeMultibody.IsEmpty)
            return cachedPlanBeforeMultibody;
        if (cachedPlan != null && !cachedPlan.IsEmpty)
            return cachedPlan;
        return null;
    }

    /// <summary>
    /// Scan Pathfinding and behavior-tree nodes under the actor root. Call from editor buttons / validation — not every frame.
    /// </summary>
    public void RefreshDiscoveredNodes()
    {
        discoveredNodes.Clear();
        Transform root = ResolveHierarchyRoot();
        if (root == null)
            return;

        var btNodes = root.GetComponentsInChildren<BehaviorTreeNode>(true);
        if (btNodes == null)
            return;

        foreach (BehaviorTreeNode bt in btNodes)
        {
            if (bt == null)
                continue;
            discoveredNodes.Add(new TravelDiscoveredNodeInfo
            {
                displayName = bt.gameObject.name,
                hierarchyPath = BuildHierarchyPath(bt.transform, root),
                nodeTypeName = bt.GetType().Name,
                serializedSummary = SummarizeBehaviorNode(bt)
            });
        }
    }

    GenericMultiModalPathPlan ApplyRiskPlannerServices(
        GenericMultiModalPathPlan plan,
        GenericTraversibilityPlannerSolver.PlannerHints hints,
        GameObject actorGo)
    {
        var stunt = stuntmanPlanner != null ? stuntmanPlanner : GetComponent<StuntmanPlannerService>();
        var warden = safetyWardenPlanner != null ? safetyWardenPlanner : GetComponent<SafetyWardenPlannerService>();
        var wrestle = wrestlingPlanner != null ? wrestlingPlanner : GetComponent<WrestlingPlannerService>();
        var referee = refereeWardenPlanner != null ? refereeWardenPlanner : GetComponent<RefereeWardenPlannerService>();
        return TravelRiskPlannerPipeline.Apply(plan, hints, actorGo, stunt, warden, wrestle, referee);
    }

    /// <summary>
    /// Rebuild cached plan using preview positions and configured sections (editor preview / runtime tooling).
    /// </summary>
    public void RebuildCachedPlan(GameObject goalTarget = null)
    {
        cachedPlanBeforeMultibody = null;
        cachedPlan = new GenericMultiModalPathPlan();
        HierarchicalPathingSolver solver = pathingSolverForPreview;
        if (solver == null)
            SceneServiceLookup.TryResolve("pathing.hierarchical", out solver);

        if (solver == null)
            return;

        Vector3 queryPos = previewStartWorld;
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = hintVehicle,
            maxRisk01 = maxRisk01,
            minRisk01 = minRisk01,
            minSafety01 = minSafety01,
            maxSafety01 = maxSafety01
        };

        PlannerTimelineOptions tl = plannerTimelineOptions;
        if (timelineExtraLandmarks != null && timelineExtraLandmarks.Count > 0)
            tl.extraLandmarks = timelineExtraLandmarks;

        GenericMultiModalPathPlan built = GenericTraversibilityPlannerSolver.BuildPlan(
            previewStartWorld,
            previewGoalWorld,
            solver,
            toolSectionsForPreview,
            acrobaticsSectionsForPreview,
            queryPos,
            0f,
            hints,
            tryToolBridgeWhenNoWalk: true,
            goalTarget,
            gravityAwarePathingForPreview ? PhysicalPathingMedium.Space : PhysicalPathingMedium.Air,
            in tl);

        GameObject actorGo = ambulatingActor != null ? ambulatingActor.gameObject : gameObject;
        if (built != null)
        {
            ConsiderPathingPrep.EnrichPlan(built, actorGo);
            ConsiderStuntmanHints.EnrichPlan(built, actorGo, previewStartWorld, previewGoalWorld);
            ConsiderSafetyWardenHints.EnrichPlan(built, actorGo, previewStartWorld, previewGoalWorld);
        }

        if (built != null)
            built = ApplyRiskPlannerServices(built, hints, actorGo);

        if (gravityAwarePathingForPreview && built?.segments != null && gravityPathing != null)
        {
            for (int i = 0; i < built.segments.Count; i++)
            {
                var seg = built.segments[i];
                if (seg?.waypoints == null || seg.waypoints.Count < 2)
                    continue;
                Vector3 a = seg.waypoints[0];
                Vector3 b = seg.waypoints[seg.waypoints.Count - 1];
                seg.waypoints = gravityPathing.FindPath(solver, a, b);
            }
        }

        if (built == null || built.IsEmpty)
        {
            cachedPlan = built ?? new GenericMultiModalPathPlan();
            return;
        }

        if (plannerTerminalOptions.enableTerminalLeg && ambulatingActor != null
            && ActorPhysicalCentroid.TryBuildProfile(ambulatingActor, out ActorPhysicalProfile profile))
        {
            built = GenericTraversibilityPlannerSolver.AppendTerminalLegIfEnabled(
                built,
                previewStartWorld,
                previewGoalWorld,
                solver,
                profile,
                in plannerTerminalOptions);

            MultiModalSegment last = built.segments != null && built.segments.Count > 0
                ? built.segments[built.segments.Count - 1]
                : null;
            if (last != null && last.HasTerminalPayload && multibody != null)
                multibody.finalTargetWorld = last.terminalCentroidWorld;
        }

        Vector3 actorWorld = Application.isPlaying ? ResolveMultibodyActorWorld() : previewStartWorld;

        GenericMultiModalPathPlan working = built.Clone();
        if (TravelFormationPathOffset.ShouldApply(this))
            TravelFormationPathOffset.ApplyToPlan(this, working, actorWorld);

        if (multibody != null && multibody.enableMultibody)
        {
            cachedPlanBeforeMultibody = working.Clone();
            cachedPlan = TravelMultibodyPathAdjuster.Adjust(working, multibody, actorWorld, solver, this);
        }
        else
        {
            cachedPlanBeforeMultibody = null;
            cachedPlan = working;
        }

        EnrichPlanWithRoads(cachedPlan);
        UpdatePathLengthMetrics();
    }

    void UpdatePathLengthMetrics()
    {
        totalPathLengthMeters = TravelPathReverseLimits.ComputeTotalPathLengthMeters(cachedPlan);
        reverseBudgetMeters = TravelPathReverseLimits.ReverseBudgetMeters(reverseLegLimit01, totalPathLengthMeters);
    }

    /// <summary>Reset reverse limit to plan default (0.5 at ≥500 m, else 1.0).</summary>
    public void ResetReverseLegLimitToDefault()
    {
        reverseLegLimit01 = TravelPathReverseLimits.ResolveDefaultReverseLegLimit01(totalPathLengthMeters);
        reverseBudgetMeters = TravelPathReverseLimits.ReverseBudgetMeters(reverseLegLimit01, totalPathLengthMeters);
    }

    /// <summary>Editor hook to refresh computed path metrics after slider edits.</summary>
    public void UpdatePathLengthMetricsPublic() => UpdatePathLengthMetrics();

    void EnrichPlanWithRoads(GenericMultiModalPathPlan plan)
    {
        if (plan == null || plan.IsEmpty)
            return;
        if (roadTravelBinding == null)
            roadTravelBinding = GetComponent<RoadTravelBinding>();
        if (roadTravelBinding != null)
        {
            if (roadTravelBinding.roadNetwork == null)
                roadTravelBinding.roadNetwork = roadNetwork != null ? roadNetwork : RoadTravelBinding.FindRoadNetworkInstance();
            roadTravelBinding.EnrichPlan(plan);
        }
    }

    static string BuildHierarchyPath(Transform leaf, Transform root)
    {
        if (leaf == null)
            return "";
        var sb = new StringBuilder();
        Transform t = leaf;
        while (t != null)
        {
            if (sb.Length > 0)
                sb.Insert(0, "/");
            sb.Insert(0, t.name);
            if (t == root)
                break;
            t = t.parent;
        }
        return sb.ToString();
    }

    static string SummarizeBehaviorNode(BehaviorTreeNode bt)
    {
        if (bt is PathfindingNode pn)
            return $"origin={pn.origin}, dest={pn.destination}, drive={pn.useDrivePathfinding}, fly={pn.useFlyingPathfinding}";
        if (bt is MoveToWaypointNode m)
            return $"waypoint={m.waypoint}";
        if (bt is ExecuteToolTraversabilityNode ex)
            return ex.card != null ? $"card={ex.card.sectionName}" : "card=null";
        return $"nodeType={bt.nodeType}";
    }

    void OnDrawGizmosSelected()
    {
        if (!drawTravelGizmos)
            return;

        if (drawMultibodyBasePlan && cachedPlanBeforeMultibody != null && !cachedPlanBeforeMultibody.IsEmpty)
        {
            Gizmos.color = new Color(1f, 0.2f, 1f, 0.85f);
            List<Vector3> basePts = cachedPlanBeforeMultibody.FlattenWaypointsForGizmos();
            for (int i = 1; i < basePts.Count; i++)
                Gizmos.DrawLine(basePts[i - 1], basePts[i]);
        }

        if (cachedPlan == null || cachedPlan.IsEmpty)
            return;

        Gizmos.color = Color.cyan;
        List<Vector3> pts = cachedPlan.FlattenWaypointsForGizmos();
        for (int i = 1; i < pts.Count; i++)
            Gizmos.DrawLine(pts[i - 1], pts[i]);

        if (multibody != null)
        {
            if (multibody.finalTarget != null)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
                Gizmos.DrawWireSphere(multibody.finalTarget.position, 0.35f);
            }
            else if (multibody.finalTargetWorld.sqrMagnitude > 1e-6f)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
                Gizmos.DrawWireSphere(multibody.finalTargetWorld, 0.35f);
            }
        }

        Gizmos.color = Color.yellow;
        if (cachedPlan.segments != null)
        {
            for (int i = 1; i < cachedPlan.segments.Count; i++)
            {
                MultiModalSegment prev = cachedPlan.segments[i - 1];
                MultiModalSegment cur = cachedPlan.segments[i];
                if (prev == null || cur == null || cur.waypoints == null || cur.waypoints.Count == 0)
                    continue;
                if (prev.mode != cur.mode)
                    Gizmos.DrawSphere(cur.waypoints[0], 0.25f);
            }
        }
    }
}

/// <summary>Binds road network to TravelAgent drive legs (runtime bridge; no Roads asmdef reference).</summary>
[AddComponentMenu("Locomotion/Travel/Road Travel Binding")]
public class RoadTravelBinding : MonoBehaviour
{
    public TravelAgent travelAgent;
    public MonoBehaviour roadNetwork;
    public float snapDistance = 8f;
    public bool snapDriveLegs = true;

    void Awake()
    {
        if (travelAgent == null)
            travelAgent = GetComponent<TravelAgent>();
        if (roadNetwork == null)
            roadNetwork = FindRoadNetworkInstance();
    }

    public static MonoBehaviour FindRoadNetworkInstance()
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb != null && mb.GetType().FullName == "Roads.RoadNetwork")
                return mb;
        }
        return null;
    }

    public void EnrichPlan(GenericMultiModalPathPlan plan)
    {
        if (plan?.segments == null)
            return;
        foreach (var seg in plan.segments)
            EnrichDriveSegment(seg);
    }

    public void EnrichDriveSegment(MultiModalSegment segment)
    {
        if (!snapDriveLegs || segment == null || segment.mode != TravelLegMode.Drive || roadNetwork == null)
            return;

        var networkType = roadNetwork.GetType();
        MethodInfo snap = networkType.GetMethod("SnapWaypointsToRoad", BindingFlags.Instance | BindingFlags.Public);
        if (snap != null && segment.waypoints != null)
            segment.waypoints = snap.Invoke(roadNetwork, new object[] { segment.waypoints, snapDistance }) as List<Vector3>;

        MethodInfo nearest = networkType.GetMethod("TryFindNearestSegment", BindingFlags.Instance | BindingFlags.Public);
        if (nearest == null || segment.waypoints == null || segment.waypoints.Count == 0)
            return;

        object[] argsStart = { segment.waypoints[0], null, 0f, 0f };
        if (!(bool)nearest.Invoke(roadNetwork, argsStart))
            return;

        var segObj = argsStart[1];
        if (segObj != null)
        {
            var idField = segObj.GetType().GetField("roadSegmentId", BindingFlags.Instance | BindingFlags.Public);
            if (idField != null)
                segment.roadSegmentId = idField.GetValue(segObj) as string;
        }
        segment.distanceAlongStart = (float)argsStart[2];

        object[] argsEnd = { segment.waypoints[segment.waypoints.Count - 1], null, 0f, 0f };
        if ((bool)nearest.Invoke(roadNetwork, argsEnd))
            segment.distanceAlongEnd = (float)argsEnd[2];
    }
}
