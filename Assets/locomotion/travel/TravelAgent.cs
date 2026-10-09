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

    [Header("Traffic avoidance")]
    [Tooltip("Developer inpaint / balloon override — skip avoid-cop soft costs.")]
    public bool ignoreTrafficAvoidance;
    public List<Transform> avoidActors = new List<Transform>();
    public float avoidRadius = 12f;
    public float avoidCostMultiplier = 4f;

    [Header("Crowd / ambulation cache")]
    public CityPixelCrowdHint crowdHint = CityPixelCrowdHint.None;
    public string flockGroupId;
    public string ambulationCacheKey;
    [Tooltip("-1 = species default (humans lower, vehicles/animals higher).")]
    public float ambulationCacheLikelihood01 = -1f;
    public float cacheToleranceM = 1.5f;
    public TravelAuthoringRow travelHintRow;

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
    [Tooltip("When set, love-making planner stamps intimacy tags and filters consent.")]
    public LoveMakingPlannerService loveMakingPlanner;
    [Tooltip("When set, consent warden soft-gates love-making physicality.")]
    public ConsentWardenPlannerService consentWardenPlanner;
    [Tooltip("When set, combat planner expands fight branches and stamps combat.* tags.")]
    public CombatPlannerService combatPlanner;
    [Tooltip("When set, safety-lock warden gates vehicle/weapon fire force.")]
    public SafetyLockWardenPlannerService safetyLockWardenPlanner;

    [Tooltip("Optional waypoint-troupe feature gates (Stuntman / Safety Warden / multibody / …).")]
    public TravelFeatureCoefficients waypointFeatureCoeffs = new TravelFeatureCoefficients();

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

    [Header("Lane grid")]
    public TravelLanePolicy lanePolicy = TravelLanePolicy.StayInLanes;
    [Range(0f, 1f)] public float stayInLanes01 = 1f;
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0f)] public float gridCarLengths = 1f;
    [Range(0f, 1f)] public float travelSpeedScale = 1f;
    public float holdUntilUnscaledTime;
    [Range(0f, 1f)] public float intersectionYield01;
    public bool preferWalkAcross;
    public RoadLaneOccupancy laneOccupancy;

    [Header("Multibody travel")]
    [Tooltip("Convoy spacing, peer avoidance, and formation offsets applied after the base planner path.")]
    public TravelAgentMultibodySettings multibody = new TravelAgentMultibodySettings();

    [Header("Multibody formation (optional)")]
    [Tooltip("Agents with the same non-empty id share a formation cohort for slot assignment and optional peer filtering.")]
    public string multibodyFormationGroupId = "";

    [Tooltip("Slot index within cohort when >= 0; -1 = order by stable instance id sort within group.")]
    public int formationSlotIndex = -1;

    [Header("Rail / train consist")]
    [Tooltip("Train consist id for linked-segment snake multibody and Rail legs.")]
    public string consistId = "";
    [Tooltip("Optional rail track segment id.")]
    public string railSegmentId = "";
    [Tooltip("Car index within consist (0 = head).")]
    public int trainCarIndex;
    [Tooltip("Optional consist runtime for coupler snake spacing.")]
    public TrainVehicleRagdoll trainConsist;

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

    /// <summary>Replace the cached plan (video steering projector / tests). Enriches drive legs when a RoadNetwork is present.</summary>
    public void ReplaceCachedPlan(GenericMultiModalPathPlan plan)
    {
        cachedPlan = plan ?? new GenericMultiModalPathPlan();
        cachedPlanBeforeMultibody = null;
        EnrichPlanWithRoads(cachedPlan);
        UpdatePathLengthMetrics();
    }

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
        var coeffs = waypointFeatureCoeffs ?? new TravelFeatureCoefficients();
        var stunt = coeffs.AllowStuntman
            ? (stuntmanPlanner != null ? stuntmanPlanner : GetComponent<StuntmanPlannerService>())
            : null;
        var warden = coeffs.AllowSafetyWarden
            ? (safetyWardenPlanner != null ? safetyWardenPlanner : GetComponent<SafetyWardenPlannerService>())
            : null;
        var wrestle = wrestlingPlanner != null ? wrestlingPlanner : GetComponent<WrestlingPlannerService>();
        var referee = refereeWardenPlanner != null ? refereeWardenPlanner : GetComponent<RefereeWardenPlannerService>();
        var love = loveMakingPlanner != null ? loveMakingPlanner : GetComponent<LoveMakingPlannerService>();
        var consent = consentWardenPlanner != null ? consentWardenPlanner : GetComponent<ConsentWardenPlannerService>();
        var combat = combatPlanner != null ? combatPlanner : GetComponent<CombatPlannerService>();
        var safetyLock = safetyLockWardenPlanner != null ? safetyLockWardenPlanner : GetComponent<SafetyLockWardenPlannerService>();
        return TravelRiskPlannerPipeline.Apply(plan, hints, actorGo, stunt, warden, wrestle, referee, love, consent, combat, safetyLock);
    }

    /// <summary>
    /// Rebuild cached plan using preview positions and configured sections (editor preview / runtime tooling).
    /// </summary>
    public void RebuildCachedPlan(GameObject goalTarget = null)
    {
        if (AmbulationPathCache.TryReuse(this, out GenericMultiModalPathPlan reused))
        {
            cachedPlanBeforeMultibody = null;
            cachedPlan = reused;
            UpdatePathLengthMetrics();
            return;
        }

        cachedPlanBeforeMultibody = null;
        cachedPlan = new GenericMultiModalPathPlan();
        HierarchicalPathingSolver solver = pathingSolverForPreview;
        if (solver == null)
            SceneServiceLookup.TryResolve("pathing.hierarchical", out solver);

        if (solver == null)
            return;

        ApplyAvoidHintsFromWarden();
        ApplySoftAvoidToPathingSolver(solver);

        Vector3 queryPos = previewStartWorld;
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = hintVehicle,
            maxRisk01 = maxRisk01,
            minRisk01 = minRisk01,
            minSafety01 = minSafety01,
            maxSafety01 = maxSafety01,
            avoidPoints = CollectAvoidPoints(),
            avoidRadius = avoidRadius,
            avoidCostMultiplier = avoidCostMultiplier,
            ignoreAvoidance = ignoreTrafficAvoidance
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
        AmbulationPathCache.Remember(this, cachedPlan);
    }

    void UpdatePathLengthMetrics()
    {
        totalPathLengthMeters = TravelPathReverseLimits.ComputeTotalPathLengthMeters(cachedPlan);
        reverseBudgetMeters = TravelPathReverseLimits.ReverseBudgetMeters(reverseLegLimit01, totalPathLengthMeters);
    }

    /// <summary>Pull active avoid sources from <see cref="TrafficWarden"/> into local avoidActors.</summary>
    public void ApplyAvoidHintsFromWarden()
    {
        if (ignoreTrafficAvoidance) return;
        var warden = TrafficWarden.Instance;
        if (warden == null) return;
        for (int i = 0; i < warden.avoidSources.Count; i++)
        {
            var t = warden.avoidSources[i];
            if (t != null && !avoidActors.Contains(t))
                avoidActors.Add(t);
        }
    }

    Vector3[] CollectAvoidPoints()
    {
        if (ignoreTrafficAvoidance) return Array.Empty<Vector3>();
        var list = new List<Vector3>();
        for (int i = 0; i < avoidActors.Count; i++)
        {
            if (avoidActors[i] != null)
                list.Add(avoidActors[i].position);
        }
        var warden = TrafficWarden.Instance;
        if (warden != null)
        {
            for (int i = 0; i < warden.avoidSources.Count; i++)
            {
                var t = warden.avoidSources[i];
                if (t != null)
                    list.Add(t.position);
            }
        }
        return list.ToArray();
    }

    /// <summary>Push current avoid points onto a hierarchical solver (also used from tests).</summary>
    public void ApplySoftAvoidToPathingSolver(HierarchicalPathingSolver solver = null)
    {
        solver = solver != null ? solver : pathingSolverForPreview;
        if (solver == null) return;
        solver.SetSoftAvoid(
            CollectAvoidPoints(),
            avoidRadius,
            avoidCostMultiplier,
            enabled: !ignoreTrafficAvoidance);
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
        EnrichPlanWithRoadLots(plan);
    }

    /// <summary>Walk/commuter enrich — tag RoadLot and optionally sample ribbon waypoints toward an outlet.</summary>
    public void EnrichWalkSegmentWithRoadLot(MultiModalSegment segment)
    {
        if (segment == null || segment.mode != TravelLegMode.Walk) return;
        if (segment.waypoints == null || segment.waypoints.Count == 0) return;
        float snap = roadTravelBinding != null ? roadTravelBinding.snapDistance : 8f;
        Vector3 end = segment.waypoints[segment.waypoints.Count - 1];
        var sidewalk = SidewalkRibbon.FindNearest(end, snap * 6f);
        if (sidewalk != null && sidewalk.TrySampleWalk(end, out Vector3 walkPt))
        {
            segment.waypoints[segment.waypoints.Count - 1] = walkPt;
            if (!string.IsNullOrEmpty(sidewalk.roadLotId))
                segment.roadLotId = sidewalk.roadLotId;
        }
        RoadLot lot = RoadLot.FindNearest(end, snap * 6f);
        if (lot == null) return;
        if (!lot.ContainsXZ(end) && (lot.ArrivalWorld - end).sqrMagnitude > (snap * 6f) * (snap * 6f))
            return;
        segment.roadLotId = lot.lotId;
        Vector3 pad = lot.ArrivalWorld;
        pad.y = lot.SampleHeight(pad);
        if ((end - pad).sqrMagnitude < (snap * 6f) * (snap * 6f))
            segment.waypoints[segment.waypoints.Count - 1] = pad;

        if (lot.pathRibbons != null && lot.pathRibbons.Count > 0)
        {
            var ribbon = lot.pathRibbons[0];
            if (ribbon != null && ribbon.controlPoints != null && ribbon.controlPoints.Count >= 2)
            {
                Vector3 a = ribbon.transform.TransformPoint(ribbon.controlPoints[0]);
                Vector3 b = ribbon.transform.TransformPoint(ribbon.controlPoints[ribbon.controlPoints.Count - 1]);
                a.y = lot.SampleHeight(a);
                b.y = lot.SampleHeight(b);
                if (segment.waypoints.Count == 1)
                {
                    segment.waypoints.Clear();
                    segment.waypoints.Add(a);
                    segment.waypoints.Add(b);
                }
            }
        }
    }

    /// <summary>Enrich all walk + drive segments that touch RoadLots.</summary>
    public void EnrichPlanWithRoadLots(GenericMultiModalPathPlan plan)
    {
        if (plan?.segments == null) return;
        if (roadTravelBinding == null)
            roadTravelBinding = GetComponent<RoadTravelBinding>();
        for (int i = 0; i < plan.segments.Count; i++)
        {
            var seg = plan.segments[i];
            if (seg == null) continue;
            if (seg.mode == TravelLegMode.Drive)
                roadTravelBinding?.EnrichDriveSegmentWithRoadLot(seg);
            else if (seg.mode == TravelLegMode.Walk)
                EnrichWalkSegmentWithRoadLot(seg);
        }
    }

    /// <summary>Apply road-work suggested-detour legs; skip legs marked ignorable when planner prefers.</summary>
    public void ApplyRoadWorkDetours(TARoadWorkRequest roadWork, bool honorIgnorable = true)
    {
        if (roadWork?.detours == null) return;
        for (int i = 0; i < roadWork.detours.Count; i++)
        {
            var d = roadWork.detours[i];
            if (d == null) continue;
            if (honorIgnorable && d.ignorable) continue;
            previewGoalWorld = d.detourGoalWorld;
            if (!honorIgnorable || !d.ignorable)
                TrafficWarden.Instance?.OnSuggestedDetour(d.detourGoalWorld);
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
        MethodInfo snap = networkType.GetMethod("SnapWaypointsToRoad", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IList<Vector3>), typeof(float) }, null)
                          ?? networkType.GetMethod("SnapWaypointsToRoad", BindingFlags.Instance | BindingFlags.Public);
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

        ApplyLanePolicySnap(segment, nearest, segObj as Component);
        EnrichDriveSegmentWithRoadLot(segment);
    }

    void ApplyLanePolicySnap(MultiModalSegment segment, MethodInfo nearest, Component splineMb)
    {
        if (travelAgent == null)
            travelAgent = GetComponent<TravelAgent>();
        if (travelAgent == null || segment?.waypoints == null || nearest == null)
            return;

        var binding = splineMb != null ? splineMb.GetComponent<RoadLaneSplineBinding>() : null;
        var layout = binding != null ? binding.ResolveLayout() : new RoadLaneLayout();
        var grid = binding != null ? binding.ResolveGrid() : new RoadLaneGridSettings();
        grid.followTimeSec = travelAgent.followTimeSec > 0.01f ? travelAgent.followTimeSec : grid.followTimeSec;
        grid.gridCarLengths = travelAgent.gridCarLengths;
        float speed = 10f;
        if (travelAgent.CachedPlan != null)
            speed = Mathf.Max(1f, travelAgent.TotalPathLengthMeters * 0.1f);
        float cell = grid.CellLengthM(speed, travelAgent.multibody != null ? travelAgent.multibody.aggressiveness01 : 0.5f);
        if (!PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(travelAgent))
            cell = Mathf.Max(0.5f, grid.carLengthM);

        var getSample = splineMb != null
            ? splineMb.GetType().GetMethod("GetSampleAtDistance", BindingFlags.Instance | BindingFlags.Public)
            : null;
        RoadLaneSnap.SampleAt sampleAt = (float d, out Vector3 pos, out Vector3 bin) =>
        {
            pos = Vector3.zero;
            bin = Vector3.right;
            if (getSample == null) return;
            object sample = getSample.Invoke(splineMb, new object[] { d });
            if (sample == null) return;
            var t = sample.GetType();
            var p = t.GetField("position");
            var b = t.GetField("binormal");
            if (p != null) pos = (Vector3)p.GetValue(sample);
            if (b != null) bin = (Vector3)b.GetValue(sample);
        };

        var distances = new List<float>();
        var laterals = new List<float>();
        for (int i = 0; i < segment.waypoints.Count; i++)
        {
            object[] args = { segment.waypoints[i], null, 0f, 0f };
            if ((bool)nearest.Invoke(roadNetwork, args))
            {
                distances.Add((float)args[2]);
                laterals.Add((float)args[3]);
            }
            else
            {
                distances.Add(0f);
                laterals.Add(0f);
            }
        }

        segment.waypoints = RoadLaneSnap.SnapList(
            segment.waypoints,
            distances,
            laterals,
            travelAgent.lanePolicy,
            travelAgent.stayInLanes01,
            layout,
            cell,
            sampleAt);

        if (travelAgent.laneOccupancy == null)
            travelAgent.laneOccupancy = new RoadLaneOccupancy();
        if (segment.waypoints.Count > 0 && layout.LaneEnabled(layout.LaneFromLateral(laterals[laterals.Count - 1])))
        {
            int lane = layout.LaneFromLateral(laterals[laterals.Count - 1]);
            int cellIndex = Mathf.RoundToInt(distances[distances.Count - 1] / Mathf.Max(0.5f, cell));
            string key = RoadLaneOccupancy.SlotKey(segment.roadSegmentId, lane, cellIndex);
            travelAgent.laneOccupancy.TryOccupy(key, travelAgent);
        }
    }

    /// <summary>If the drive end is near a RoadLot connected to this segment (or any nearest lot), tag roadLotId and leave pad unsnapped.</summary>
    public void EnrichDriveSegmentWithRoadLot(MultiModalSegment segment)
    {
        if (segment?.waypoints == null || segment.waypoints.Count == 0) return;
        Vector3 end = segment.waypoints[segment.waypoints.Count - 1];
        var ix = IntersectionLot.FindNearest(end, snapDistance * 4f);
        if (ix != null && ix.ContainsWaypoint(end) && ix.TrySnapDriveOutlet(segment.roadSegmentId, end, out Vector3 outlet))
        {
            segment.roadLotId = ix.lotId;
            segment.waypoints[segment.waypoints.Count - 1] = outlet;
            return;
        }
        RoadLot lot = null;
        if (!string.IsNullOrEmpty(segment.roadSegmentId))
            lot = RoadLot.FindConnectedToRoad(segment.roadSegmentId, end);
        if (lot == null)
            lot = RoadLot.FindNearest(end, snapDistance * 4f);
        if (lot == null) return;
        segment.roadLotId = lot.lotId;
        // Leave last waypoint on lot pad (height sampled).
        Vector3 pad = lot.ArrivalWorld;
        pad.y = lot.SampleHeight(pad);
        if ((end - pad).sqrMagnitude < (snapDistance * 4f) * (snapDistance * 4f))
            segment.waypoints[segment.waypoints.Count - 1] = pad;
    }
}

// <auto-merged-travel-orphan-types>
// CityPixelCrowdHint (from CityPixelGrid.cs)
public enum CityPixelCrowdHint
{
    None = 0,
    Flock = 1,
    Congregate = 2,
    Commute = 3
}

// ---- from RoadLaneLayout.cs ----
public enum TravelLanePolicy
{
    StayInLanes = 0,
    IgnoreLaneGrid = 1,
    AlignGridIgnoreLanes = 2
}

[Serializable]
public sealed class RoadLaneGridSettings
{
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0)] public float gridCarLengths = 1f;
    [Range(0f, 1f)] public float occupancy01 = 0.85f;
    [Min(0.5f)] public float carLengthM = 4.5f;

    public float CellLengthM(float currentSpeedMps, float aggressiveness01 = 0.5f)
    {
        float temporal = followTimeSec * Mathf.Max(0f, currentSpeedMps);
        if (gridCarLengths <= 1e-4f)
            return Mathf.Max(0.05f, temporal);
        float spatial = gridCarLengths * carLengthM;
        return Mathf.Max(spatial, temporal);
    }

    /// <summary>When gridCarLengths is 0, high aggressiveness shrinks bumper gap toward 0.</summary>
    public float MinSeparationM(float currentSpeedMps, float aggressiveness01 = 0.5f)
    {
        float cell = CellLengthM(currentSpeedMps, aggressiveness01);
        if (gridCarLengths <= 1e-4f)
            return cell * Mathf.Lerp(1f, 0.05f, Mathf.Clamp01(aggressiveness01));
        return cell;
    }
}

[Serializable]
public sealed class RoadLaneLayout
{
    [Min(1)] public int laneCount = 2;
    [Min(0.5f)] public float laneWidthM = 3.5f;
    public int[] directionSign = { 1, -1 };

    public float LaneCenterOffset(int laneIndex)
    {
        int n = Mathf.Max(1, laneCount);
        int i = Mathf.Clamp(laneIndex, 0, n - 1);
        return (i - (n - 1) * 0.5f) * laneWidthM;
    }

    public int LaneFromLateral(float lateralOffset)
    {
        int n = Mathf.Max(1, laneCount);
        float half = (n - 1) * 0.5f;
        int i = Mathf.RoundToInt(lateralOffset / Mathf.Max(0.1f, laneWidthM) + half);
        return Mathf.Clamp(i, 0, n - 1);
    }

    public int DirectionSign(int laneIndex)
    {
        if (directionSign == null || directionSign.Length == 0)
            return 1;
        int i = Mathf.Clamp(laneIndex, 0, directionSign.Length - 1);
        return directionSign[i] == 0 ? 0 : (directionSign[i] > 0 ? 1 : -1);
    }

    public bool LaneEnabled(int laneIndex) => DirectionSign(laneIndex) != 0;
}

/// <summary>Live occupancy slots on a road ribbon.</summary>
public sealed class RoadLaneOccupancy
{
    readonly System.Collections.Generic.Dictionary<string, TravelAgent> _slots =
        new System.Collections.Generic.Dictionary<string, TravelAgent>();

    public int OccupiedCount => _slots.Count;

    public static string SlotKey(string roadSegmentId, int laneIndex, int cellIndex) =>
        (roadSegmentId ?? "") + ":" + laneIndex + ":" + cellIndex;

    public int Cap(RoadLaneLayout layout, RoadLaneGridSettings grid, float roadLengthM)
    {
        if (layout == null || grid == null) return 0;
        float cell = Mathf.Max(0.5f, grid.CellLengthM(10f));
        int cellsAlong = Mathf.Max(1, Mathf.FloorToInt(roadLengthM / cell));
        return Mathf.Max(1, Mathf.RoundToInt(grid.occupancy01 * layout.laneCount * cellsAlong));
    }

    public bool TryOccupy(string key, TravelAgent agent)
    {
        if (string.IsNullOrEmpty(key) || agent == null) return false;
        if (_slots.TryGetValue(key, out var occ) && occ != null && occ != agent)
            return false;
        _slots[key] = agent;
        return true;
    }

    public void Release(string key)
    {
        if (!string.IsNullOrEmpty(key))
            _slots.Remove(key);
    }

    public TravelAgent Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _slots.TryGetValue(key, out var a);
        return a;
    }
}

// ---- from TASanitationRequestCards.cs ----
/// <summary>TA maintenance request with repair BT hook for sanitation / road assets.</summary>
[Serializable]
public class TAMaintenanceRequest : TravelAgentCard
{
    public DispatchRequest request;
    public string repairBtActionId = "ta_maintenance_repair";
    public float integrityTarget01 = 0.85f;
    public GameObject repairTarget;

    public TAMaintenanceRequest()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "ta_maintenance_request";
        traversabilityTag = "maintenance";
    }

    public static TAMaintenanceRequest Generate(DispatchRequest request)
    {
        var c = new TAMaintenanceRequest();
        c.request = request;
        c.sectionName = "ta_maintenance_request";
        c.description = request != null ? request.kind : "ta_maintenance_request";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        if (!string.IsNullOrEmpty(request?.notes))
            c.repairBtActionId = request.notes;
        return c;
    }

    public void ApplyRepair(VehicleRagdoll vehicle)
    {
        if (vehicle != null)
            vehicle.integrity01 = Mathf.Max(vehicle.integrity01, integrityTarget01);
        SendMessageSafe(repairTarget != null ? repairTarget : vehicle != null ? vehicle.gameObject : null);
    }

    void SendMessageSafe(GameObject go)
    {
        if (go == null) return;
        go.SendMessage("OnNarrativeSchedulerAction", repairBtActionId, SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TARoadWorkDetourLeg
{
    public string routeTag = "suggested-detour";
    public Vector3 detourGoalWorld;
    public bool ignorable = true;
    public string roadSegmentId;
}

/// <summary>Road work request — repair BT + suggested-detour legs (ignorable for AI/planner).</summary>
[Serializable]
public class TARoadWorkRequest : TravelAgentCard
{
    public DispatchRequest request;
    public string repairBtActionId = "ta_road_work_repair";
    public List<TARoadWorkDetourLeg> detours = new List<TARoadWorkDetourLeg>();

    public TARoadWorkRequest()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "ta_road_work";
        traversabilityTag = "road_work";
        waypointGroup = "suggested-detour";
    }

    public static TARoadWorkRequest Generate(DispatchRequest request)
    {
        var c = new TARoadWorkRequest();
        c.request = request;
        c.sectionName = "ta_road_work_request";
        c.description = "road_work";
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.detours.Add(new TARoadWorkDetourLeg
        {
            routeTag = "suggested-detour",
            detourGoalWorld = c.goalWorld,
            ignorable = ParseIgnorable(request?.notes)
        });
        return c;
    }

    static bool ParseIgnorable(string notes)
    {
        if (string.IsNullOrEmpty(notes)) return true;
        if (notes.IndexOf("ignorable=false", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (notes.IndexOf("ignorable=0", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    public void RegisterWithTrafficAvoid(TrafficWarden warden)
    {
        if (warden == null) return;
        for (int i = 0; i < detours.Count; i++)
        {
            var d = detours[i];
            if (d == null || d.ignorable) continue;
            warden.SendMessage("OnSuggestedDetour", d.detourGoalWorld, SendMessageOptions.DontRequireReceiver);
        }
    }

    public bool ShouldPlannerIgnoreDetour(int index)
    {
        if (index < 0 || index >= detours.Count || detours[index] == null) return true;
        return detours[index].ignorable;
    }
}



// ---- AssetDB compile hosts (VehicleRagdoll / TravelAgentCard / Dispatch / TrafficWarden) ----

[Serializable]
public sealed class VehicleInventoryItem
{
    public string itemId;
    public string label;
    public int count = 1;
}

[Serializable]
public sealed class VehicleInventorySection
{
    public string sectionName = "cabin";
    public float capacity = 20f;
    public List<VehicleInventoryItem> items = new List<VehicleInventoryItem>();
}

[DisallowMultipleComponent]
public class VehicleRagdoll : MonoBehaviour
{
    public string vehicleId;
    public string displayName;
    [Range(0f, 1f)] public float integrity01 = 1f;
    public List<VehicleInventorySection> interiors = new List<VehicleInventorySection>();
    public bool available = true;
    public float totalInteriorSize;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(vehicleId)) vehicleId = gameObject.name;
        if (string.IsNullOrEmpty(displayName)) displayName = vehicleId;
    }

    public float ComputeInteriorSizeSum()
    {
        float sum = 0f;
        if (interiors != null)
            for (int i = 0; i < interiors.Count; i++)
                if (interiors[i] != null) sum += interiors[i].capacity;
        return sum;
    }

    public void RecalculateTotalInteriorSize()
    {
        float sum = ComputeInteriorSizeSum();
        if (totalInteriorSize <= 0f) totalInteriorSize = sum;
        else totalInteriorSize = Mathf.Max(totalInteriorSize, sum);
    }

    public Dictionary<string, object> ToDto()
    {
        RecalculateTotalInteriorSize();
        return new Dictionary<string, object>
        {
            ["vehicleId"] = vehicleId ?? "",
            ["displayName"] = displayName ?? "",
            ["integrity01"] = integrity01,
            ["totalSize"] = totalInteriorSize,
            ["available"] = available
        };
    }
}

[System.Serializable]
public class TravelAgentCard : GoodSection
{
    [Header("TravelAgent")]
    public JusticeCard justice;
    public Vector3 goalWorld;
    public GameObject goalTarget;
    public string waypointGroup;
    public bool preferFlee;
    public bool useSocialDeescalate;
    [Header("Lane policy")]
    public TravelLanePolicy lanePolicy = TravelLanePolicy.StayInLanes;
    [Range(0f, 1f)] public float stayInLanes01 = 1f;
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0f)] public float gridCarLengths = 1f;

    public TravelAgentCard()
    {
        isTravelAgentGoal = true;
        physicalPathingTag = "travel_agent";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "travel";
    }

    public static TravelAgentCard GenerateDefault(GameObject target)
    {
        return new TravelAgentCard
        {
            sectionName = "travel_agent_default",
            description = "TravelAgent + Justice",
            isTravelAgentGoal = true,
            goalTarget = target,
            justice = JusticeCard.Generate(JusticeAction.SecureArea, target),
            preferFlee = true,
            physicalPathingTag = "travel_agent"
        };
    }

    public static TravelAgentCard GeneratePatrol(Vector3 goal, JusticeAction action = JusticeAction.SecureArea)
    {
        return new TravelAgentCard
        {
            sectionName = "travel_agent_patrol",
            description = "Patrol",
            isTravelAgentGoal = true,
            goalWorld = goal,
            justice = JusticeCard.Generate(action, null),
            preferFlee = false,
            physicalPathingTag = "travel_agent_patrol"
        };
    }

    /// <summary>Apply to actor: flee or justice path via TravelAgent.</summary>
    public virtual void ApplyToActor(GameObject actor, float threat01, SocialSkills social = null)
    {
        if (actor == null) return;
        var ta = actor.GetComponent<TravelAgent>();
        Vector3 goal = goalTarget != null ? goalTarget.transform.position : goalWorld;
        bool flee = preferFlee;
        if (justice != null)
            flee = !justice.ShouldRespondPhysically(actor, threat01);

        if (flee)
        {
            Vector3 away = actor.transform.position + (actor.transform.position - goal).normalized * 8f;
            if (ta != null)
            {
                ApplyLanePolicy(ta);
                ta.previewGoalWorld = away;
                ta.RebuildCachedPlan();
            }
            var sched = actor.GetComponent<PersonalSchedule>();
            sched?.ForceFlee();
            return;
        }

        if (useSocialDeescalate && social != null)
        {
            var r = social.Interpret(SocialRequestChannel.Local, "calm down", goal);
            social.Apply(r);
        }

        if (ta != null)
        {
            ApplyLanePolicy(ta);
            ta.previewGoalWorld = goal;
            ta.RebuildCachedPlan();
        }
    }

    public void ApplyLanePolicy(TravelAgent ta)
    {
        if (ta == null) return;
        ta.lanePolicy = lanePolicy;
        ta.stayInLanes01 = lanePolicy == TravelLanePolicy.StayInLanes ? stayInLanes01 : 0f;
        ta.followTimeSec = followTimeSec;
        ta.gridCarLengths = gridCarLengths;
    }
}

[Serializable]
public sealed class DispatchRequest
{
    public string requestId;
    public string fromServiceId;
    public string toServiceId;
    public string kind = "route";
    public Vector3 worldTarget;
    public string personaKey;
    public string notes;
    public float priority01 = 0.5f;
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Warden")]
public sealed class TrafficWarden : MonoBehaviour
{
    public static TrafficWarden Instance { get; private set; }

    [Tooltip("Optional CityPixelGrid — when bakedCaches exist for the active frame, prefer bake for enqueue backbone.")]
    public CityPixelGrid cityGrid;

    [Tooltip("When set with cityGrid, prefer baked MST over live sampling when available.")]
    public bool preferCityGridBake = true;

    public CityPixelGridRuntime cityGridRuntime;

    public CentralDispatchHub hub;
    public TrafficDispatchBioRhythm trafficBio;
    public HierarchicalPathingSolver pathingSolver;
    public float rebuildIntervalSec = 2f;
    public float corridorCellSize = 4f;
    public float congestionDemandThreshold = 8f;
    public bool narrativeLeaseActive;

    public readonly TrafficCorridorGraph corridorGraph = new TrafficCorridorGraph();
    public readonly TrafficCarEnqueue carEnqueue = new TrafficCarEnqueue();
    public readonly TrafficWardenStateMachine stateMachine = new TrafficWardenStateMachine();
    public readonly List<TrafficLightController> lights = new List<TrafficLightController>();
    public readonly List<Transform> avoidSources = new List<Transform>();
    public List<TrafficCorridorEdge> backboneEdges = new List<TrafficCorridorEdge>();

    float _rebuildT;
    public float MaxEdgeDemand { get; private set; }

    void Awake()
    {
        Instance = this;
        stateMachine.Bind(this);
        stateMachine.congestedDemandThreshold = congestionDemandThreshold;
        if (hub == null)
            hub = CentralDispatchHub.Instance ?? FindFirstObjectByType<CentralDispatchHub>();
        if (trafficBio == null)
            trafficBio = GetComponent<TrafficDispatchBioRhythm>()
                         ?? gameObject.AddComponent<TrafficDispatchBioRhythm>();
        trafficBio.warden = this;
        if (pathingSolver == null)
            SceneServiceLookup.TryResolve("pathing.hierarchical", out pathingSolver);
        RefreshLights();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        _rebuildT += dt;
        if (_rebuildT >= rebuildIntervalSec)
        {
            _rebuildT = 0f;
            RebuildCorridorMst();
        }

        stateMachine.Tick(dt, MaxEdgeDemand, narrativeLeaseActive);
        ApplyLightPolicy(dt);
        carEnqueue.ReleaseAlongBackbone(backboneEdges, corridorGraph, lights);
    }

    public void RefreshLights()
    {
        lights.Clear();
        lights.AddRange(FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None));
    }

    public void RebuildCorridorMst()
    {
        if (preferCityGridBake && TryApplyCityGridBake())
        {
            MaxEdgeDemand = 0f;
            for (int i = 0; i < backboneEdges.Count; i++)
                MaxEdgeDemand = Mathf.Max(MaxEdgeDemand, backboneEdges[i].demand);
            // Merge live demand for dirty tracking.
            corridorGraph.IngestTravelAgentPlans(TravelAgentRegistry.All, driveLegsPreferred: true);
            return;
        }

        corridorGraph.Clear();
        corridorGraph.cellSize = corridorCellSize > 0.01f
            ? corridorCellSize
            : (pathingSolver != null ? pathingSolver.cellSize : 4f);
        corridorGraph.IngestTravelAgentPlans(TravelAgentRegistry.All, driveLegsPreferred: true);
        backboneEdges = TrafficMstBuilder.Build(corridorGraph);
        MaxEdgeDemand = 0f;
        for (int i = 0; i < corridorGraph.edges.Count; i++)
            MaxEdgeDemand = Mathf.Max(MaxEdgeDemand, corridorGraph.edges[i].demand);
    }

    bool TryApplyCityGridBake()
    {
        if (cityGrid == null) return false;
        int frame = 0;
        if (cityGridRuntime == null)
            cityGridRuntime = FindFirstObjectByType<CityPixelGridRuntime>();
        if (cityGridRuntime != null && cityGridRuntime.grid == cityGrid)
            frame = cityGridRuntime.ActiveFrameIndex;
        var bake = cityGrid.FindBake(frame);
        if (bake == null || bake.mstEdges == null || bake.mstEdges.Count == 0)
            return false;
        CityPixelGridBaker.ApplyBakeToWarden(cityGrid, frame, this);
        return backboneEdges != null && backboneEdges.Count > 0;
    }

    public void EnqueueCar(TravelAgent agent) => carEnqueue.Enqueue(agent);

    public void SeedFromParkingLots(float radiusM = 40f)
    {
        var lots = FindObjectsByType<ParkingLot>(FindObjectsSortMode.None);
        for (int i = 0; i < lots.Length; i++)
        {
            var lot = lots[i];
            if (lot == null) continue;
            lot.SeedTravelAgents(radiusM);
            var agents = FindObjectsByType<TravelAgent>(FindObjectsSortMode.None);
            Vector3 goal = lot.ArrivalWorld;
            for (int a = 0; a < agents.Length; a++)
            {
                var ta = agents[a];
                if (ta == null) continue;
                if ((ta.transform.position - goal).sqrMagnitude > radiusM * radiusM) continue;
                EnqueueCar(ta);
            }
        }
    }

    public Vector3 SuggestFlowGoal(Vector3 from)
    {
        if (backboneEdges == null || backboneEdges.Count == 0 || corridorGraph.nodes.Count == 0)
            return from + Vector3.forward * 8f;
        float best = float.PositiveInfinity;
        Vector3 goal = from;
        for (int i = 0; i < backboneEdges.Count; i++)
        {
            var e = backboneEdges[i];
            if (!corridorGraph.nodes.TryGetValue(e.b, out var nb)) continue;
            float d = (nb.world - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                goal = nb.world;
            }
        }
        return goal;
    }

    /// <summary>Non-ignorable suggested-detour from TARoadWorkRequest — register as avoid point.</summary>
    public void OnSuggestedDetour(Vector3 world)
    {
        var go = new GameObject("suggested_detour_" + avoidSources.Count);
        go.transform.position = world;
        RegisterAvoidSource(go.transform);
    }

    public void RegisterAvoidSource(Transform t)
    {
        if (t == null || avoidSources.Contains(t)) return;
        avoidSources.Add(t);
    }

    public void UnregisterAvoidSource(Transform t)
    {
        if (t == null) return;
        avoidSources.Remove(t);
    }

    public void RegisterAvoidSource(PoliceCarVehicleRagdoll cruiser)
    {
        if (cruiser == null) return;
        RegisterAvoidSource(cruiser.transform);
    }

    public void ClearAvoidSources() => avoidSources.Clear();

    public void CopyAvoidPoints(List<Vector3> into)
    {
        into.Clear();
        for (int i = 0; i < avoidSources.Count; i++)
        {
            if (avoidSources[i] != null)
                into.Add(avoidSources[i].position);
        }
    }

    public void OnStateEntered(TrafficWardenMode mode)
    {
        if (mode == TrafficWardenMode.PoliceDetailActive || mode == TrafficWardenMode.EmergencyPreempt)
            RequestPoliceTrafficDetail(stateMachine.detailTargetWorld);
    }

    public void BeginPoliceDetail(Vector3 worldTarget)
    {
        stateMachine.BeginPoliceDetail(worldTarget);
    }

    public bool RequestPoliceTrafficDetail(Vector3 worldTarget)
    {
        var request = new DispatchRequest
        {
            kind = "traffic_detail",
            worldTarget = worldTarget,
            notes = "traffic_detail",
            priority01 = 0.75f
        };
        if (hub == null)
            hub = CentralDispatchHub.Instance;
        return hub != null && hub.RequestCrossDispatch("traffic_warden", "police", request);
    }

    void ApplyLightPolicy(float dt)
    {
        if (lights.Count == 0) return;
        switch (stateMachine.Mode)
        {
            case TrafficWardenMode.CongestedHold:
                for (int i = 0; i < lights.Count; i++)
                {
                    var l = lights[i];
                    if (l == null) continue;
                    if (l.Phase != TrafficSignalPhase.AllRed)
                        l.Enter(TrafficSignalPhase.AllRed);
                }
                break;
            case TrafficWardenMode.EmergencyPreempt:
            case TrafficWardenMode.PoliceDetailActive:
                PreemptToward(stateMachine.detailTargetWorld);
                break;
            case TrafficWardenMode.NarrativeLease:
                // Soft hold — same as congested for MVP.
                goto case TrafficWardenMode.CongestedHold;
        }
    }

    void PreemptToward(Vector3 target)
    {
        TrafficLightController closest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < lights.Count; i++)
        {
            var l = lights[i];
            if (l == null) continue;
            float d = (l.transform.position - target).sqrMagnitude;
            if (d < best)
            {
                best = d;
                closest = l;
            }
        }

        if (closest != null && !closest.MainProceed)
            closest.Enter(TrafficSignalPhase.MainGreen);
    }

    public void OnTrafficLightPhase(TrafficLightController ctrl)
    {
        // Hook for sensors / tests; lights already registered.
    }
}

// TrainVehicleRagdoll stub (fields needed by CompositeMultiModalPathNode / TravelMultibodyPathAdjuster)
public enum TrainDriveKind { Wheels = 0, Maglev = 1 }

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Vehicle Ragdoll")]
public sealed class TrainVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string craftName = "Train";
    public string callsign = "CUU-T1";
    public string consistId = "consist_1";
    public string formationGroupId = "train_snake";

    [Header("Consist (this unit is head when cars populated)")]
    public List<TrainVehicleRagdoll> cars = new List<TrainVehicleRagdoll>();
    public bool linkedSegmentMultibody = true;
    public float nominalCouplerSpacingM = 1.2f;
    public int carIndexInConsist;
    public TrainVehicleRagdoll headTrain;

    [Header("Train type")]
    public TrainDriveKind driveKind = TrainDriveKind.Wheels;
    public Transform wheelBarAnchor;
    public float gaugeM = 1.435f;
    public float enginePowerKw = 4000f;
    public float brakePowerKw = 5000f;
    public float startupSec = 8f;
    public float shutdownSec = 6f;
    public bool engineRunning;

    [Header("Coupling")]
    public TrainCouplingRuntime coupling;
    public Transform frontCoupler;
    public Transform rearCoupler;

    [Header("Composition")]
    public List<TrainCarAmbulationLimb> limbs = new List<TrainCarAmbulationLimb>();
    public List<TrainCarContainmentBay> containmentBays = new List<TrainCarContainmentBay>();
    public CargoLashRuntime lashRuntime;
    public CargoStabilityBakeAsset defaultBake;
    public CargoStabilityMode defaultStabilityMode = CargoStabilityMode.Nominal;

    [Header("Cabin / pathing")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public string doorOpenCloseTopologyId = "train_door";
    public BehaviorTree doorOpenCloseBt;
    public PlanarSplinePathLocomotion aislePath;
    public PlanarSplinePathLocomotion doorBridgePath;
    public PlanarSplinePathLocomotion caboosePorchPath;
    public PlanarSplinePathLocomotion engineCabinPath;
    public List<VehicleGrabHold> grabHolds = new List<VehicleGrabHold>();
    public List<VehicleStrapHold> strapHolds = new List<VehicleStrapHold>();

    [Header("Telecom")]
    public Component engineerTelecomBridge;
    public bool engineerWebtopMapEnabled = true;
    public Component attendantIntercom;
    public Component passengerWalkie;
    public string cabinMusicTrackId;
    public bool cabinMusicPlaying;

    [Header("Travel")]
    public string railSegmentId;
    public float speedLimitMs = 40f;
    public float currentSpeedMs;

    [Header("Fuel")]
    [Range(0f, 1f)] public float fuel01 = 1f;
    public Transform fuelPort;
    public string fuelPortTopologyId = "fuel01";

    [Header("Seat ticket")]
    public TrainSeatTicketConfig seatTicket;

    TrainCarResultantApi _resultants;
    public TrainCarResultantApi Resultants => _resultants ??= new TrainCarResultantApi(this);

    public float LastLashStable01 => lashRuntime != null ? lashRuntime.LashStable01 : 1f;
    public bool LastFoldFailed { get; set; }
    public TrainVehicleRagdoll Head => cars != null && cars.Count > 0 ? cars[0] : this;
    public TrainVehicleRagdoll Tail => cars != null && cars.Count > 0 ? cars[cars.Count - 1] : this;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "cargo") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "cargo", capacity = 120f });
        if (interiors.Find(s => s != null && s.sectionName == "baggage") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "baggage", capacity = 80f });
        if (coupling == null)
            coupling = GetComponent<TrainCouplingRuntime>() ?? gameObject.AddComponent<TrainCouplingRuntime>();
        coupling.car = this;
        if (lashRuntime == null)
            lashRuntime = GetComponent<CargoLashRuntime>() ?? gameObject.AddComponent<CargoLashRuntime>();
        lashRuntime.bake = defaultBake;
        lashRuntime.mode = defaultStabilityMode;
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (engineerTelecomBridge == null)
            engineerTelecomBridge = GetComponent("TelecomUnityBridge");
        if (string.IsNullOrEmpty(consistId))
            consistId = gameObject.name;
        EnsureDefaultLimb();
        EnsureDefaultBay();
        EnsureSharedHolds();
        if (cars.Count == 0)
            RebuildCarsFromChildren();
        IndexCars();
        seatTicket?.ApplyTo(this);
    }

    public void EnsureSharedHolds()
    {
        if (grabHolds == null) grabHolds = new List<VehicleGrabHold>();
        if (strapHolds == null) strapHolds = new List<VehicleStrapHold>();
        grabHolds.Clear();
        strapHolds.Clear();
        grabHolds.AddRange(GetComponentsInChildren<VehicleGrabHold>(true));
        strapHolds.AddRange(GetComponentsInChildren<VehicleStrapHold>(true));
        for (int i = 0; i < grabHolds.Count; i++)
            grabHolds[i]?.EnsureCollider();
        for (int i = 0; i < strapHolds.Count; i++)
            strapHolds[i]?.EnsureRope();
    }

    void EnsureDefaultLimb()
    {
        if (limbs.Count > 0) return;
        limbs.Add(new TrainCarAmbulationLimb
        {
            limbId = "main_crane",
            role = TrainCarLimbRole.Crane,
            openCloseTopologyId = "train_limb_crane"
        });
    }

    void EnsureDefaultBay()
    {
        if (containmentBays.Count > 0) return;
        containmentBays.Add(new TrainCarContainmentBay
        {
            bayId = "deck",
            kind = TrainCarBayKind.Vehicle,
            capacity = 2,
            parkAnchor = transform,
            deckRoot = transform
        });
    }

    public void RebuildCarsFromChildren()
    {
        cars.Clear();
        cars.Add(this);
        var found = GetComponentsInChildren<TrainVehicleRagdoll>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i] != this && !cars.Contains(found[i]))
                cars.Add(found[i]);
        }
        IndexCars();
    }

    public void RebuildCarsFromCouplers() => RebuildFromCouplers(this);

    /// <summary>Rebuild this host's car list by walking couplers from <paramref name="seed"/>.</summary>
    public void RebuildFromCouplers(TrainVehicleRagdoll seed)
    {
        cars.Clear();
        var head = WalkToHead(seed != null ? seed : this);
        var cur = head;
        var guard = 0;
        while (cur != null && guard++ < 256)
        {
            if (!cars.Contains(cur)) cars.Add(cur);
            cur = cur.coupling != null && cur.coupling.rearConnected != null
                ? cur.coupling.rearConnected.car
                : null;
        }
        IndexCars();
    }

    static TrainVehicleRagdoll WalkToHead(TrainVehicleRagdoll seed)
    {
        var cur = seed;
        var guard = 0;
        while (cur?.coupling?.frontConnected?.car != null && guard++ < 256)
            cur = cur.coupling.frontConnected.car;
        return cur;
    }

    public void IndexCars()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i] == null) continue;
            cars[i].carIndexInConsist = i;
            cars[i].consistId = consistId;
            cars[i].headTrain = this;
        }
    }

    public void AddCar(TrainVehicleRagdoll car)
    {
        if (car == null || cars.Contains(car)) return;
        cars.Add(car);
        IndexCars();
    }

    public bool RemoveCar(TrainVehicleRagdoll car)
    {
        if (car == null || car == this) return false;
        bool ok = cars.Remove(car);
        if (ok)
        {
            car.coupling?.DecoupleFront();
            car.coupling?.DecoupleRear();
            car.headTrain = null;
            IndexCars();
        }
        return ok;
    }

    public bool ReplaceCar(int index, TrainVehicleRagdoll replacement)
    {
        if (replacement == null || index < 0 || index >= cars.Count) return false;
        var old = cars[index];
        cars[index] = replacement;
        if (old != null && old != replacement)
        {
            old.coupling?.DecoupleFront();
            old.coupling?.DecoupleRear();
            old.headTrain = null;
        }
        IndexCars();
        return true;
    }

    public void InsertCar(int index, TrainVehicleRagdoll car)
    {
        if (car == null) return;
        index = Mathf.Clamp(index, 0, cars.Count);
        if (!cars.Contains(car))
            cars.Insert(index, car);
        IndexCars();
    }

    public void CopySnakeWorldPositions(IReadOnlyList<Vector3> samples)
    {
        if (samples == null || cars == null) return;
        int n = Mathf.Min(cars.Count, samples.Count);
        for (int i = 0; i < n; i++)
        {
            if (cars[i] == null) continue;
            var p = samples[i];
            cars[i].transform.position = new Vector3(p.x, cars[i].transform.position.y, p.z);
            if (i + 1 < n)
            {
                Vector3 dir = samples[i + 1] - samples[i];
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-4f)
                    cars[i].transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
    }

    public TrainCarAmbulationLimb FindLimb(string limbId)
    {
        for (int i = 0; i < limbs.Count; i++)
            if (limbs[i] != null && limbs[i].limbId == limbId)
                return limbs[i];
        return null;
    }

    public TrainCarContainmentBay FindBay(string bayId)
    {
        for (int i = 0; i < containmentBays.Count; i++)
            if (containmentBays[i] != null && containmentBays[i].bayId == bayId)
                return containmentBays[i];
        return containmentBays.Count > 0 ? containmentBays[0] : null;
    }

    public bool TryUnfoldLimb(string limbId)
    {
        var limb = FindLimb(limbId) ?? (limbs.Count > 0 ? limbs[0] : null);
        if (limb == null) return false;
        limb.state = TrainCarLimbState.Unfolded;
        LastFoldFailed = false;
        Notify(TrainCarNarrativeActionIds.UnfoldLimb);
        ApplyLimbLash(limb);
        return true;
    }

    public bool TryRefoldLimb(string limbId)
    {
        var limb = FindLimb(limbId) ?? (limbs.Count > 0 ? limbs[0] : null);
        if (limb == null) return false;
        limb.state = TrainCarLimbState.Folded;
        LastFoldFailed = false;
        Notify(TrainCarNarrativeActionIds.RefoldLimb);
        ApplyLimbLash(limb);
        return true;
    }

    public void MarkFoldFailed(string limbOrBayId)
    {
        LastFoldFailed = true;
        var limb = FindLimb(limbOrBayId);
        if (limb != null) limb.state = TrainCarLimbState.Failed;
        Notify(TrainCarNarrativeActionIds.FoldFailed);
    }

    public bool TryParkVehicle(VehicleRagdoll vehicle, string bayId = null)
    {
        var bay = FindBay(bayId);
        if (bay == null || vehicle == null || !bay.HasRoom) return false;
        if (!bay.containedVehicles.Contains(vehicle))
            bay.containedVehicles.Add(vehicle);
        if (bay.parkAnchor != null)
        {
            vehicle.transform.SetParent(bay.parkAnchor, true);
            vehicle.transform.localPosition = Vector3.zero;
            vehicle.transform.localRotation = Quaternion.identity;
        }
        var rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        ApplyBayLash(bay, vehicle);
        Notify(TrainCarNarrativeActionIds.ParkVehicle);
        return true;
    }

    public bool TryUnloadVehicle(VehicleRagdoll vehicle, string bayId = null)
    {
        var bay = FindBay(bayId);
        if (bay == null || vehicle == null) return false;
        bay.containedVehicles.Remove(vehicle);
        bay.rampUnfolded = true;
        vehicle.transform.SetParent(null, true);
        var rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        Notify(TrainCarNarrativeActionIds.UnloadBay);
        return true;
    }

    public void SetBayRampUnfolded(string bayId, bool unfolded)
    {
        var bay = FindBay(bayId);
        if (bay != null) bay.rampUnfolded = unfolded;
    }

    public void SetEngineRunning(bool running)
    {
        engineRunning = running;
        Notify(running ? TrainDispatchNarrativeIds.EngineStart : TrainDispatchNarrativeIds.EngineStop);
    }

    public void SetCabinMusic(string trackId, bool play)
    {
        cabinMusicTrackId = trackId;
        cabinMusicPlaying = play;
    }

    public void SetCabinLocked(bool locked)
    {
        Notify(locked ? TrainDispatchNarrativeIds.CabinLock : TrainDispatchNarrativeIds.CabinUnlock);
    }

    public void RebuildPlanarPaths()
    {
        aislePath?.Rebuild();
        doorBridgePath?.Rebuild();
        caboosePorchPath?.Rebuild();
        engineCabinPath?.Rebuild();
    }

    void ApplyLimbLash(TrainCarAmbulationLimb limb)
    {
        if (lashRuntime == null || limb == null) return;
        lashRuntime.deckRoot = limb.limbRoot != null ? limb.limbRoot : transform;
        lashRuntime.mode = limb.stabilityMode;
        lashRuntime.ApplyProfile(limb.lashProfile, limb.stabilityMode);
        lashRuntime.TickEvaluate(Vector3.zero);
    }

    void ApplyBayLash(TrainCarContainmentBay bay, VehicleRagdoll vehicle)
    {
        if (lashRuntime == null || bay == null) return;
        lashRuntime.deckRoot = bay.deckRoot != null ? bay.deckRoot : transform;
        lashRuntime.cargoBody = vehicle != null ? vehicle.GetComponent<Rigidbody>() : null;
        lashRuntime.mode = bay.stabilityMode;
        lashRuntime.bake = defaultBake;
        lashRuntime.ApplyProfile(bay.lashProfile, bay.stabilityMode);
        lashRuntime.TickEvaluate(Vector3.zero);
    }

    void Notify(string id) =>
        SendMessage("OnNarrativeSchedulerAction", id ?? "", SendMessageOptions.DontRequireReceiver);

    public Dictionary<string, object> LemmaSnapshot()
    {
        var limb = limbs.Count > 0 ? limbs[0] : null;
        var bay = containmentBays.Count > 0 ? containmentBays[0] : null;
        return new Dictionary<string, object>
        {
            [TrainCarLemmaPropertyKeys.ConsistId] = consistId ?? "",
            [TrainCarLemmaPropertyKeys.LimbState] = limb != null ? limb.state.ToString() : "",
            [TrainCarLemmaPropertyKeys.LimbRole] = limb != null ? limb.role.ToString() : "",
            [TrainCarLemmaPropertyKeys.BayId] = bay != null ? bay.bayId : "",
            [TrainCarLemmaPropertyKeys.ContainedVehicle] =
                bay != null && bay.containedVehicles.Count > 0 && bay.containedVehicles[0] != null
                    ? bay.containedVehicles[0].vehicleId
                    : "",
            [TrainCarLemmaPropertyKeys.LashStable01] = LastLashStable01,
            [TrainCarLemmaPropertyKeys.ImpossibleKeepStable] =
                defaultStabilityMode == CargoStabilityMode.ImpossibleKeepStable,
            [TrainCarLemmaPropertyKeys.StabilityMode] = defaultStabilityMode.ToString(),
            [TrainCarLemmaPropertyKeys.FoldFailed] = LastFoldFailed
        };
    }
}

public static class TrainDispatchNarrativeIds
{
    public const string EngineStart = "train_engine_start";
    public const string EngineStop = "train_engine_stop";
    public const string CabinLock = "train_cabin_lock";
    public const string CabinUnlock = "train_cabin_unlock";
    public const string SpeedAdjust = "train_speed_adjust";
    public const string Plow = "train_plow";
    public const string FollowTrain = "train_follow";
    public const string Turnstile = "train_turnstile";
}

// ---- more travel/road orphan compile hosts ----
public static class AmbulationPathCache
{
    public const float HumanLikelihood01 = 0.35f;
    public const float NonHumanLikelihood01 = 0.85f;

    struct Entry
    {
        public Vector3 start;
        public Vector3 goal;
        public int fingerprint;
        public GenericMultiModalPathPlan plan;
        public float toleranceM;
    }

    static readonly Dictionary<string, Entry> s_byKey = new Dictionary<string, Entry>();

    public static void Clear() => s_byKey.Clear();

    public static float DefaultLikelihood01(BaseAmbulatingActor actor)
    {
        if (actor is VehicleActor || actor is AnimalAmbulatingActor)
            return NonHumanLikelihood01;
        return HumanLikelihood01;
    }

    public static bool TryReuse(TravelAgent agent, out GenericMultiModalPathPlan plan)
    {
        plan = null;
        if (agent == null) return false;
        float tol = agent.cacheToleranceM > 0f ? agent.cacheToleranceM : 1.5f;
        if (!s_byKey.TryGetValue(agent.ambulationCacheKey ?? "", out var e)) return false;
        if ((e.start - agent.previewStartWorld).sqrMagnitude > tol * tol) return false;
        if ((e.goal - agent.previewGoalWorld).sqrMagnitude > tol * tol) return false;
        plan = e.plan != null ? e.plan.Clone() : null;
        return plan != null && !plan.IsEmpty;
    }

    public static void Remember(TravelAgent agent, GenericMultiModalPathPlan plan)
    {
        if (agent == null || plan == null || plan.IsEmpty) return;
        float tol = agent.cacheToleranceM > 0f ? agent.cacheToleranceM : 1.5f;
        s_byKey[agent.ambulationCacheKey ?? ""] = new Entry
        {
            start = agent.previewStartWorld,
            goal = agent.previewGoalWorld,
            plan = plan.Clone(),
            toleranceM = tol
        };
    }

    public static void Put(string key, Vector3 start, Vector3 goal, int fingerprint, GenericMultiModalPathPlan plan, float toleranceM)
    {
        s_byKey[key] = new Entry
        {
            start = start,
            goal = goal,
            fingerprint = fingerprint,
            plan = plan,
            toleranceM = toleranceM
        };
    }

    public static bool TryGet(string key, Vector3 start, Vector3 goal, int fingerprint, float toleranceM, out GenericMultiModalPathPlan plan)
    {
        plan = null;
        if (!s_byKey.TryGetValue(key, out var e)) return false;
        float t = Mathf.Max(0.01f, toleranceM);
        if (e.fingerprint != fingerprint) return false;
        if ((e.start - start).sqrMagnitude > t * t) return false;
        if ((e.goal - goal).sqrMagnitude > t * t) return false;
        plan = e.plan;
        return plan != null;
    }
}

public static class RoadLaneSnap
{
    public delegate void SampleAt(float distanceAlong, out Vector3 position, out Vector3 binormal);

    public static float SnapS(float distanceAlong, float cellLengthM)
    {
        float cell = Mathf.Max(0.25f, cellLengthM);
        return Mathf.Round(distanceAlong / cell) * cell;
    }

    public static Vector3 ApplyPolicy(
        Vector3 world,
        float distanceAlong,
        float lateralOffset,
        TravelLanePolicy policy,
        float stayInLanes01,
        RoadLaneLayout layout,
        float cellLengthM,
        SampleAt sample)
    {
        if (sample == null) return world;
        float s = distanceAlong;
        if (policy != TravelLanePolicy.IgnoreLaneGrid)
            s = SnapS(distanceAlong, cellLengthM);
        sample(s, out Vector3 pos, out Vector3 bin);
        if (policy == TravelLanePolicy.IgnoreLaneGrid)
            return pos;
        if (policy == TravelLanePolicy.AlignGridIgnoreLanes)
            return pos + bin * lateralOffset;
        float laneCenter = layout != null ? layout.LaneCenterOffset(layout.LaneFromLateral(lateralOffset)) : 0f;
        float lat = Mathf.Lerp(lateralOffset, laneCenter, Mathf.Clamp01(stayInLanes01));
        return pos + bin * lat;
    }

    public static List<Vector3> SnapList(
        List<Vector3> waypoints, List<float> distances, List<float> laterals,
        TravelLanePolicy policy, float stayInLanes01, RoadLaneLayout layout, float cell, SampleAt sampleAt)
    {
        return waypoints ?? new List<Vector3>();
    }
}

public sealed class RoadLaneSplineBinding : MonoBehaviour
{
    public RoadLaneConfigAsset config;
    public RoadLaneLayout layout = new RoadLaneLayout();
    public RoadLaneGridSettings grid = new RoadLaneGridSettings();
    public RoadLaneLayout ResolveLayout() => layout ?? new RoadLaneLayout();
    public RoadLaneGridSettings ResolveGrid() => grid ?? new RoadLaneGridSettings();
}

public static class PlayerVehicleTravelSlowOverride
{
    public static bool ShouldApplyTravelSlow(TravelAgent agent)
    {
        if (agent == null) return true;
        var vehicle = agent.GetComponent<VehicleRagdoll>()
                      ?? agent.GetComponentInParent<VehicleRagdoll>()
                      ?? agent.GetComponentInChildren<VehicleRagdoll>();
        return ShouldApplyTravelSlow(vehicle);
    }

    public static bool ShouldApplyTravelSlow(VehicleRagdoll vehicle)
    {
        if (vehicle == null) return true;
        var buf = vehicle.GetComponent<RagdollPlayerInputBuffer>()
                  ?? vehicle.GetComponentInChildren<RagdollPlayerInputBuffer>();
        if (buf == null || buf.options == null || !buf.options.overrideTravelAgentSlow)
            return true;
        return buf.State.selfDriving || buf.State.brake01 > 0.01f;
    }
}

public enum RoadLotGradeMode
{
    Flat = 0,
    GradedHeightMap = 1,
    TerrainConform = 2
}

[Serializable]
public sealed class RoadLotOutlet
{
    public string roadSegmentId;
    [Range(0f, 1f)] public float distanceAlong01 = 0.5f;
    public float distanceAlongMeters = -1f;
    public float lateralSide = 1f;
    public float curbWidth = 2f;
}

public enum RoadLotKind
{
    Pad = 0,
    Driveway = 1,
    Garage = 2,
    Intersection = 3
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Road Lot")]
public sealed class RoadLot : MonoBehaviour
{
    public string lotId;
    public string displayName;
    public RoadLotKind lotKind = RoadLotKind.Pad;
    public RoadLotGradeMode gradeMode = RoadLotGradeMode.Flat;
    public Vector3 padSize = new Vector3(40f, 2f, 40f);
    public Texture2D heightMap;
    public float heightMapAmplitude = 4f;
    public Terrain terrainRef;
    public List<RoadLotOutlet> roadOutlets = new List<RoadLotOutlet>();
    public RoadLotBoundarySpline boundary;
    public LotGrassGrowthController grass;
    public bool registerCorridorOnAwake = true;

    [Header("Walk path ribbons")]
    public List<PlanarSplinePathLocomotion> pathRibbons = new List<PlanarSplinePathLocomotion>();
    public bool autoBuildOutletRibbons = true;

    static readonly List<RoadLot> Registry = new List<RoadLot>();

    public static IReadOnlyList<RoadLot> All => Registry;

    void Awake()
    {
        if (string.IsNullOrEmpty(lotId))
            lotId = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = lotId;
        if (boundary == null)
            boundary = GetComponent<RoadLotBoundarySpline>() ?? gameObject.AddComponent<RoadLotBoundarySpline>();
        boundary.EnsureClosedLoopDefault();
        if (pathRibbons.Count == 0)
            pathRibbons.AddRange(GetComponentsInChildren<PlanarSplinePathLocomotion>(true));
        if (autoBuildOutletRibbons)
            EnsureOutletPathRibbons();
        if (!Registry.Contains(this))
            Registry.Add(this);
    }

    /// <summary>Ensure a planar spline ribbon from arrival pad toward each road outlet.</summary>
    public void EnsureOutletPathRibbons()
    {
        if (roadOutlets == null) return;
        for (int i = 0; i < roadOutlets.Count; i++)
        {
            var outlet = roadOutlets[i];
            if (outlet == null || string.IsNullOrEmpty(outlet.roadSegmentId)) continue;
            string ribbonName = "path_ribbon_" + outlet.roadSegmentId;
            PlanarSplinePathLocomotion ribbon = null;
            for (int r = 0; r < pathRibbons.Count; r++)
            {
                if (pathRibbons[r] != null && pathRibbons[r].name == ribbonName)
                {
                    ribbon = pathRibbons[r];
                    break;
                }
            }
            if (ribbon == null)
            {
                var go = new GameObject(ribbonName);
                go.transform.SetParent(transform, false);
                ribbon = go.AddComponent<PlanarSplinePathLocomotion>();
                pathRibbons.Add(ribbon);
            }
            Vector3 pad = transform.InverseTransformPoint(ArrivalWorld);
            Vector3 outLocal = pad + new Vector3(outlet.lateralSide * outlet.curbWidth, 0f, 8f + i * 2f);
            if (ribbon.controlPoints == null || ribbon.controlPoints.Count < 2)
            {
                ribbon.controlPoints = new List<Vector3> { pad, outLocal };
                ribbon.Rebuild();
            }
        }
    }

    void OnDestroy() => Registry.Remove(this);

    public Bounds GetWorldBounds()
    {
        Vector3 size = Vector3.Scale(padSize, transform.lossyScale);
        return new Bounds(transform.position, size);
    }

    public float SampleHeight(Vector3 world)
    {
        switch (gradeMode)
        {
            case RoadLotGradeMode.TerrainConform:
                if (terrainRef != null)
                    return terrainRef.SampleHeight(world) + terrainRef.transform.position.y;
                break;
            case RoadLotGradeMode.GradedHeightMap:
                if (heightMap != null)
                {
                    Bounds b = GetWorldBounds();
                    float u = Mathf.InverseLerp(b.min.x, b.max.x, world.x);
                    float v = Mathf.InverseLerp(b.min.z, b.max.z, world.z);
                    Color c = heightMap.GetPixelBilinear(u, v);
                    return transform.position.y + c.r * heightMapAmplitude;
                }
                break;
        }
        return transform.position.y;
    }

    public bool ContainsXZ(Vector3 world)
    {
        Bounds b = GetWorldBounds();
        return world.x >= b.min.x && world.x <= b.max.x && world.z >= b.min.z && world.z <= b.max.z;
    }

    public bool HasOutletTo(string roadSegmentId)
    {
        if (string.IsNullOrEmpty(roadSegmentId) || roadOutlets == null) return false;
        for (int i = 0; i < roadOutlets.Count; i++)
            if (roadOutlets[i] != null && roadOutlets[i].roadSegmentId == roadSegmentId)
                return true;
        return false;
    }

    public Vector3 ArrivalWorld
    {
        get
        {
            if (boundary != null && boundary.controlPoints != null && boundary.controlPoints.Count > 0)
                return transform.TransformPoint(boundary.CentroidLocal());
            return transform.position;
        }
    }

    public static RoadLot FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].lotId == id)
                return Registry[i];
        return null;
    }

    public static RoadLot FindNearest(Vector3 world, float maxDist = 200f)
    {
        RoadLot best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null) continue;
            float sq = (lot.ArrivalWorld - world).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = lot; }
        }
        return best;
    }

    public static RoadLot FindConnectedToRoad(string roadSegmentId, Vector3 near)
    {
        RoadLot best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null || !lot.HasOutletTo(roadSegmentId)) continue;
            float sq = (lot.ArrivalWorld - near).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = lot; }
        }
        return best;
    }
}

/// <summary>Stub until PlanarSplinePathLocomotion.cs is AssetDB-imported.</summary>
public enum PlanarSplineGranularityMode
{
    Division = 0,
    PerLength = 1
}

[Serializable]
public sealed class PlanarSplinePathPlane
{
    public float tStart;
    public float tEnd;
    public float halfWidth = 0.5f;
    public string hierarchicalPlaneId;
    public Vector3 center;
    public Vector3 normal = Vector3.up;
    public Vector3 tangent = Vector3.forward;
    public Vector3 binormal = Vector3.right;

    public float MidT01 => (tStart + tEnd) * 0.5f;
    public float Length01 => Mathf.Max(0f, tEnd - tStart);
}

[Serializable]
public sealed class PlanarSplineCustomSection
{
    [Range(0f, 1f)] public float startT01;
    [Range(0f, 1f)] public float endT01 = 0.1f;
    public float width = 1f;
    public string hierarchicalPlaneId;
    public Vector3 gizmoLocalPosition;
    public Vector3 gizmoLocalEuler;
    public Vector3 gizmoLocalScale = Vector3.one;
    [NonSerialized] public Transform gizmoTransform;
}

/// <summary>Walkable ribbon of planes along a Catmull-Rom spline — aisles, branches, ledges.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Pathing/Planar Spline Path Locomotion")]
public sealed class PlanarSplinePathLocomotion : MonoBehaviour
{
    public const string RebuildNarrativeAction = "planar_spline_rebuild";

    public List<Vector3> controlPoints = new List<Vector3>();
    public float defaultWidth = 1.2f;
    public PlanarSplineGranularityMode granularity = PlanarSplineGranularityMode.Division;
    [Min(1)] public int divisionStopCount = 8;
    [Min(0.1f)] public float perLengthMeters = 2f;
    public List<PlanarSplineCustomSection> customSections = new List<PlanarSplineCustomSection>();
    public bool blockFallUnlessJump;
    public float jumpWallHeight;
    public float wallThickness = 0.08f;
    public List<PlanarSplinePathPlane> planes = new List<PlanarSplinePathPlane>();

    readonly List<BoxCollider> _ledgeWalls = new List<BoxCollider>();
    float[] _cumulativeLengths;
    float _totalLength;

    void Awake() => Rebuild();

    public void Rebuild()
    {
        RebuildLengthTable();
        if (planes == null) planes = new List<PlanarSplinePathPlane>();
        planes.Clear();
        if (controlPoints == null || controlPoints.Count < 2 || _totalLength <= 1e-4f)
        {
            SyncLedgeWalls();
            return;
        }

        planes.AddRange(MergeCustomOverAuto(BuildAutoPlanes()));
        SyncLedgeWalls();
    }

    List<PlanarSplinePathPlane> BuildAutoPlanes()
    {
        var list = new List<PlanarSplinePathPlane>();
        int count = granularity == PlanarSplineGranularityMode.Division
            ? Mathf.Max(1, divisionStopCount)
            : Mathf.Max(1, Mathf.CeilToInt(_totalLength / perLengthMeters));
        for (int i = 0; i < count; i++)
        {
            float t0 = i / (float)count;
            float t1 = (i + 1) / (float)count;
            list.Add(MakePlane(t0, t1, defaultWidth, "auto_" + i));
        }
        return list;
    }

    List<PlanarSplinePathPlane> MergeCustomOverAuto(List<PlanarSplinePathPlane> auto)
    {
        if (customSections == null || customSections.Count == 0)
            return auto;

        var result = new List<PlanarSplinePathPlane>();
        for (int i = 0; i < auto.Count; i++)
        {
            var a = auto[i];
            bool covered = false;
            for (int c = 0; c < customSections.Count; c++)
            {
                var cs = customSections[c];
                if (cs == null) continue;
                float s = Mathf.Min(cs.startT01, cs.endT01);
                float e = Mathf.Max(cs.startT01, cs.endT01);
                if (a.MidT01 >= s && a.MidT01 <= e)
                {
                    covered = true;
                    break;
                }
            }
            if (!covered)
                result.Add(a);
        }

        for (int c = 0; c < customSections.Count; c++)
        {
            var cs = customSections[c];
            if (cs == null) continue;
            float s = Mathf.Clamp01(Mathf.Min(cs.startT01, cs.endT01));
            float e = Mathf.Clamp01(Mathf.Max(cs.startT01, cs.endT01));
            if (e - s < 1e-4f) e = Mathf.Min(1f, s + 0.01f);
            float width = cs.width > 1e-3f ? cs.width : defaultWidth;
            if (Mathf.Abs(cs.gizmoLocalScale.x - 1f) > 1e-3f && cs.gizmoLocalScale.x > 1e-3f)
                width = Mathf.Abs(cs.gizmoLocalScale.x) * defaultWidth;
            var plane = MakePlane(s, e, width, string.IsNullOrEmpty(cs.hierarchicalPlaneId) ? "custom_" + c : cs.hierarchicalPlaneId);
            if (cs.gizmoLocalEuler.sqrMagnitude > 1e-6f)
            {
                Quaternion q = Quaternion.Euler(cs.gizmoLocalEuler);
                plane.normal = q * plane.normal;
                plane.tangent = q * plane.tangent;
                plane.binormal = Vector3.Cross(plane.normal, plane.tangent).normalized;
            }
            if (cs.gizmoLocalPosition.sqrMagnitude > 1e-8f)
                plane.center += transform.TransformVector(cs.gizmoLocalPosition);
            result.Add(plane);
        }

        result.Sort((a, b) => a.tStart.CompareTo(b.tStart));
        return result;
    }

    PlanarSplinePathPlane MakePlane(float t0, float t1, float width, string id)
    {
        float mid = (t0 + t1) * 0.5f;
        Vector3 tan = EvaluateTangent(mid);
        Vector3 bin = Vector3.Cross(Vector3.up, tan);
        if (bin.sqrMagnitude < 1e-6f) bin = Vector3.right;
        else bin.Normalize();
        Vector3 nrm = Vector3.Cross(tan, bin).normalized;
        return new PlanarSplinePathPlane
        {
            tStart = t0,
            tEnd = t1,
            halfWidth = Mathf.Max(0.05f, width * 0.5f),
            hierarchicalPlaneId = id,
            center = Evaluate(mid),
            normal = nrm,
            tangent = tan,
            binormal = bin
        };
    }

    public bool TryProject(Vector3 worldPoint, out Vector3 onPlane, out PlanarSplinePathPlane plane)
    {
        onPlane = worldPoint;
        plane = null;
        if (planes == null || planes.Count == 0) return false;
        float best = float.MaxValue;
        for (int i = 0; i < planes.Count; i++)
        {
            var p = planes[i];
            if (p == null) continue;
            Vector3 local = worldPoint - p.center;
            float along = Vector3.Dot(local, p.tangent);
            float side = Vector3.Dot(local, p.binormal);
            float halfLen = Mathf.Max(0.05f, p.Length01 * GetTotalLength() * 0.5f);
            along = Mathf.Clamp(along, -halfLen, halfLen);
            side = Mathf.Clamp(side, -p.halfWidth, p.halfWidth);
            Vector3 candidate = p.center + p.tangent * along + p.binormal * side;
            float d = (candidate - worldPoint).sqrMagnitude;
            if (d < best)
            {
                best = d;
                onPlane = candidate;
                plane = p;
            }
        }
        return plane != null;
    }

    public Vector3 ClampToPath(Vector3 worldPoint) =>
        TryProject(worldPoint, out var on, out _) ? on : worldPoint;

    public float GetTotalLength()
    {
        if (_cumulativeLengths == null) RebuildLengthTable();
        return _totalLength;
    }

    void SyncLedgeWalls()
    {
        for (int i = 0; i < _ledgeWalls.Count; i++)
            if (_ledgeWalls[i] != null)
            {
                if (Application.isPlaying) Destroy(_ledgeWalls[i].gameObject);
                else DestroyImmediate(_ledgeWalls[i].gameObject);
            }
        _ledgeWalls.Clear();
        if (!blockFallUnlessJump || jumpWallHeight <= 1e-4f || planes == null) return;
        for (int i = 0; i < planes.Count; i++)
        {
            var p = planes[i];
            if (p == null) continue;
            float len = Mathf.Max(0.2f, p.Length01 * GetTotalLength());
            SpawnWall(p, 1, len);
            SpawnWall(p, -1, len);
        }
    }

    void SpawnWall(PlanarSplinePathPlane p, int sideSign, float length)
    {
        var go = new GameObject("LedgeWall_" + p.hierarchicalPlaneId + "_" + (sideSign > 0 ? "R" : "L"));
        go.transform.SetParent(transform, false);
        go.transform.position = p.center + p.binormal * (p.halfWidth * sideSign) + p.normal * (jumpWallHeight * 0.5f);
        go.transform.rotation = Quaternion.LookRotation(p.tangent, p.normal);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(wallThickness, jumpWallHeight, length);
        _ledgeWalls.Add(box);
    }

    void RebuildLengthTable()
    {
        if (controlPoints == null || controlPoints.Count < 2)
        {
            _cumulativeLengths = null;
            _totalLength = 0f;
            return;
        }
        int sampleCount = Mathf.Max(8, controlPoints.Count * 8);
        _cumulativeLengths = new float[sampleCount];
        Vector3 prev = EvaluateCatmull(0f);
        for (int i = 1; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            Vector3 p = EvaluateCatmull(t);
            _cumulativeLengths[i] = _cumulativeLengths[i - 1] + Vector3.Distance(prev, p);
            prev = p;
        }
        _totalLength = _cumulativeLengths[sampleCount - 1];
    }

    public Vector3 Evaluate(float normalizedT) => EvaluateCatmull(Mathf.Clamp01(normalizedT));

    public Vector3 EvaluateTangent(float normalizedT)
    {
        const float dt = 0.001f;
        Vector3 a = EvaluateCatmull(Mathf.Clamp01(normalizedT - dt));
        Vector3 b = EvaluateCatmull(Mathf.Clamp01(normalizedT + dt));
        Vector3 t = b - a;
        return t.sqrMagnitude > 1e-8f ? t.normalized : transform.forward;
    }

    Vector3 EvaluateCatmull(float normalizedT)
    {
        if (controlPoints == null || controlPoints.Count == 0)
            return transform.position;
        if (controlPoints.Count == 1)
            return transform.TransformPoint(controlPoints[0]);
        float t = Mathf.Clamp01(normalizedT);
        int segmentCount = controlPoints.Count - 1;
        float scaled = t * segmentCount;
        int seg = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
        float localT = scaled - seg;
        Vector3 p0 = controlPoints[Mathf.Max(seg - 1, 0)];
        Vector3 p1 = controlPoints[seg];
        Vector3 p2 = controlPoints[Mathf.Min(seg + 1, controlPoints.Count - 1)];
        Vector3 p3 = controlPoints[Mathf.Min(seg + 2, controlPoints.Count - 1)];
        float t2 = localT * localT;
        float t3 = t2 * localT;
        Vector3 local = 0.5f * (
            (2f * p1) +
            (-p0 + p2) * localT +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        return transform.TransformPoint(local);
    }

    public void ApplyGizmoSave(int sectionIndex)
    {
        if (customSections == null || sectionIndex < 0 || sectionIndex >= customSections.Count) return;
        var cs = customSections[sectionIndex];
        if (cs?.gizmoTransform == null) return;
        cs.gizmoLocalPosition = cs.gizmoTransform.localPosition;
        cs.gizmoLocalEuler = cs.gizmoTransform.localEulerAngles;
        cs.gizmoLocalScale = cs.gizmoTransform.localScale;
        Rebuild();
    }

    public void ApplyGizmoRevert(int sectionIndex, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        if (customSections == null || sectionIndex < 0 || sectionIndex >= customSections.Count) return;
        var cs = customSections[sectionIndex];
        if (cs == null) return;
        cs.gizmoLocalPosition = pos;
        cs.gizmoLocalEuler = euler;
        cs.gizmoLocalScale = scale;
        if (cs.gizmoTransform != null)
        {
            cs.gizmoTransform.localPosition = pos;
            cs.gizmoTransform.localEulerAngles = euler;
            cs.gizmoTransform.localScale = scale;
        }
        Rebuild();
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(RoadLot))]
[AddComponentMenu("Locomotion/Civil/Roads/Intersection Lot")]
[Serializable]
public sealed class IntersectionLotLeg
{
    public string roadSegmentId;
    public string approachId = "main";
    public float headingYaw;
    public RoadLaneLayout laneLayout;
    public RoadLotOutlet outlet;
}

public sealed class IntersectionLot : MonoBehaviour
{
    public string lotId;
    public RoadLot pad;
    public TAIntersectionCard intersectionCard;
    public TrafficLightController lights;
    public List<IntersectionLotLeg> legs = new List<IntersectionLotLeg>();
    public List<StreetWireEnd> wireEnds = new List<StreetWireEnd>();

    static readonly List<IntersectionLot> Registry = new List<IntersectionLot>();
    public static IReadOnlyList<IntersectionLot> All => Registry;

    void Awake()
    {
        if (pad == null)
            pad = GetComponent<RoadLot>() ?? gameObject.AddComponent<RoadLot>();
        pad.lotKind = RoadLotKind.Intersection;
        if (string.IsNullOrEmpty(lotId))
            lotId = string.IsNullOrEmpty(pad.lotId) ? gameObject.name : pad.lotId;
        pad.lotId = lotId;
        if (!Registry.Contains(this))
            Registry.Add(this);
        if (intersectionCard == null)
            intersectionCard = TAIntersectionCard.Generate(transform.position);
        intersectionCard.BindLot(this);
    }

    void OnDestroy() => Registry.Remove(this);

    public bool ContainsWaypoint(Vector3 world) => pad != null && pad.ContainsXZ(world);

    public bool TrySnapDriveOutlet(string roadSegmentId, Vector3 from, out Vector3 world)
    {
        world = pad != null ? pad.ArrivalWorld : transform.position;
        IntersectionLotLeg match = null;
        for (int i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            if (!string.IsNullOrEmpty(roadSegmentId) && leg.roadSegmentId == roadSegmentId)
            {
                match = leg;
                break;
            }
        }
        if (match == null && legs.Count > 0)
            match = NearestLeg(from);
        if (match?.outlet == null) return pad != null;
        world = OutletWorld(match);
        if (pad != null)
            world.y = pad.SampleHeight(world);
        return true;
    }

    public Vector3 OutletWorld(IntersectionLotLeg leg)
    {
        if (leg?.outlet == null)
            return pad != null ? pad.ArrivalWorld : transform.position;
        Vector3 padPos = pad != null ? pad.ArrivalWorld : transform.position;
        Vector3 dir = Quaternion.Euler(0f, leg.headingYaw, 0f) * Vector3.forward;
        float along = leg.outlet.distanceAlongMeters > 0f ? leg.outlet.distanceAlongMeters : 8f;
        return padPos + dir * along + Vector3.right * (leg.outlet.lateralSide * leg.outlet.curbWidth);
    }

    IntersectionLotLeg NearestLeg(Vector3 from)
    {
        IntersectionLotLeg best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            float sq = (OutletWorld(leg) - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = leg;
            }
        }
        return best;
    }

    public static IntersectionLot FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].lotId == id)
                return Registry[i];
        return null;
    }

    public static IntersectionLot FindNearest(Vector3 world, float maxDist = 40f)
    {
        IntersectionLot best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null) continue;
            Vector3 p = lot.pad != null ? lot.pad.ArrivalWorld : lot.transform.position;
            float sq = (p - world).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = lot;
            }
        }
        return best;
    }

    public void EnsureFourLegs(string[] segmentIds)
    {
        legs.Clear();
        float[] yaws = { 0f, 90f, 180f, 270f };
        int n = segmentIds != null ? Mathf.Min(4, segmentIds.Length) : 4;
        for (int i = 0; i < n; i++)
        {
            string id = segmentIds != null && i < segmentIds.Length ? segmentIds[i] : "leg_" + i;
            legs.Add(new IntersectionLotLeg
            {
                roadSegmentId = id,
                approachId = i % 2 == 0 ? "main" : "side",
                headingYaw = yaws[i],
                outlet = new RoadLotOutlet { roadSegmentId = id, curbWidth = 3f, lateralSide = 1f, distanceAlongMeters = 8f }
            });
        }
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Sidewalk Ribbon")]
public sealed class SidewalkRibbon : MonoBehaviour
{
    public string roadLotId;
    public float widthM = 1.8f;
    public float paddingM = 0.2f;
    [Range(0f, 1f)] public float mattingWidth01;
    public bool walkOpen = true;
    public Vector3 along = Vector3.forward;

    static readonly System.Collections.Generic.List<SidewalkRibbon> Registry = new System.Collections.Generic.List<SidewalkRibbon>();
    public static System.Collections.Generic.IReadOnlyList<SidewalkRibbon> All => Registry;

    public float WalkableWidthM => Mathf.Max(0.1f, widthM - 2f * paddingM);
    public bool HasMatting => mattingWidth01 > 1e-4f;

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
    }

    void OnDisable() => Registry.Remove(this);

    public bool TrySampleWalk(Vector3 near, out Vector3 world)
    {
        world = transform.position;
        if (!walkOpen) return false;
        Vector3 a = Vector3.ProjectOnPlane(near - transform.position, Vector3.up);
        Vector3 dir = Vector3.ProjectOnPlane(along, Vector3.up);
        if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
        dir.Normalize();
        float t = Vector3.Dot(a, dir);
        world = transform.position + dir * t;
        world.y = transform.position.y;
        return true;
    }

    public static SidewalkRibbon FindNearest(Vector3 world, float maxDist)
    {
        SidewalkRibbon best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var r = Registry[i];
            if (r == null || !r.walkOpen) continue;
            float sq = (r.transform.position - world).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = r;
            }
        }
        return best;
    }
}

public static class SidewalkRibbonUtil
{
    public static bool TryProject(Vector3 world, out Vector3 projected)
    {
        projected = world;
        return false;
    }
}

