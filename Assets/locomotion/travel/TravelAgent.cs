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
}

[Serializable]
public class TravelAgentCard : GoodSection
{
    public Vector3 goalWorld;
    public GameObject goalTarget;
    public string waypointGroup;
    public bool preferFlee;
    public bool useSocialDeescalate;
    [Range(0f, 1f)] public float stayInLanes01 = 1f;
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0f)] public float gridCarLengths = 1f;

    public TravelAgentCard()
    {
        isTravelAgentGoal = true;
        physicalPathingTag = "travel_agent";
    }

    public static TravelAgentCard GenerateDefault(GameObject target)
    {
        return new TravelAgentCard
        {
            sectionName = "travel_agent_default",
            description = "TravelAgent",
            isTravelAgentGoal = true,
            goalTarget = target,
            preferFlee = true,
            physicalPathingTag = "travel_agent"
        };
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
public sealed class TrafficWarden : MonoBehaviour
{
    public static TrafficWarden Instance { get; private set; }
    public readonly List<Transform> avoidSources = new List<Transform>();

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnSuggestedDetour(Vector3 worldPos)
    {
        // Stub: full TrafficWarden lives in _PendingAssetDbImport until AssetDB import recovers.
    }
}

// TrainVehicleRagdoll stub (fields needed by CompositeMultiModalPathNode / TravelMultibodyPathAdjuster)
public enum TrainDriveKind { Wheels = 0, Maglev = 1 }

public sealed class TrainVehicleRagdoll : VehicleRagdoll
{
    public string craftName = "Train";
    public string callsign = "CUU-T1";
    public string consistId = "consist_1";
    public string formationGroupId = "train_snake";
    public List<TrainVehicleRagdoll> cars = new List<TrainVehicleRagdoll>();
    public bool linkedSegmentMultibody = true;
    public float nominalCouplerSpacingM = 1.2f;
    public int carIndexInConsist;
    public TrainVehicleRagdoll headTrain;
    public TrainDriveKind driveKind = TrainDriveKind.Wheels;
    public string railSegmentId;
}


// ---- more travel/road orphan compile hosts ----
public static class AmbulationPathCache
{
    public static bool TryReuse(TravelAgent agent, out GenericMultiModalPathPlan plan)
    {
        plan = null;
        return false;
    }

    public static void Remember(TravelAgent agent, GenericMultiModalPathPlan plan) { }
}

public static class RoadLaneSnap
{
    public delegate void SampleAt(float distanceAlong, out Vector3 position, out Vector3 binormal);

    public static List<Vector3> SnapList(
        List<Vector3> waypoints, List<float> distances, List<float> laterals,
        TravelLanePolicy policy, float stayInLanes01, RoadLaneLayout layout, float cell, SampleAt sampleAt)
    {
        return waypoints ?? new List<Vector3>();
    }
}

public sealed class RoadLaneSplineBinding : MonoBehaviour
{
    public RoadLaneLayout ResolveLayout() => new RoadLaneLayout();
    public RoadLaneGridSettings ResolveGrid() => new RoadLaneGridSettings();
}

public static class PlayerVehicleTravelSlowOverride
{
    public static bool ShouldApplyTravelSlow(TravelAgent agent) => false;
}

public class RoadLot : MonoBehaviour
{
    public string lotId;
    public RoadLotBoundarySpline boundary;
    public List<PlanarSplinePathLocomotion> pathRibbons = new List<PlanarSplinePathLocomotion>();
    static readonly List<RoadLot> Registry = new List<RoadLot>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() { Registry.Remove(this); }
    public Vector3 ArrivalWorld => transform.position;
    public float SampleHeight(Vector3 world) => world.y;
    public bool ContainsXZ(Vector3 world)
    {
        Vector3 d = world - transform.position;
        d.y = 0f;
        return d.sqrMagnitude < 100f;
    }
    public static RoadLot FindNearest(Vector3 world, float maxDist = 200f)
    {
        RoadLot best = null; float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null) continue;
            float sq = (lot.ArrivalWorld - world).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = lot; }
        }
        return best;
    }
    public static RoadLot FindConnectedToRoad(string roadSegmentId, Vector3 near) => FindNearest(near, 200f);
}

/// <summary>Stub until PlanarSplinePathLocomotion.cs is AssetDB-imported.</summary>
public sealed class PlanarSplinePathLocomotion : MonoBehaviour
{
    public List<Vector3> controlPoints = new List<Vector3>();
}

public class IntersectionLot : MonoBehaviour
{
    public string lotId;
    static readonly List<IntersectionLot> Registry = new List<IntersectionLot>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() { Registry.Remove(this); }
    public bool ContainsWaypoint(Vector3 world) => false;
    public bool TrySnapDriveOutlet(string roadSegmentId, Vector3 end, out Vector3 outlet)
    {
        outlet = end;
        return false;
    }
    public static IntersectionLot FindNearest(Vector3 world, float maxDist = 200f) => null;
}

public class SidewalkRibbon : MonoBehaviour
{
    public string roadLotId;
    static readonly List<SidewalkRibbon> Registry = new List<SidewalkRibbon>();
    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() { Registry.Remove(this); }

    public bool TrySampleWalk(Vector3 world, out Vector3 walkPt)
    {
        walkPt = world;
        return false;
    }

    public static SidewalkRibbon FindNearest(Vector3 world, float maxDist = 200f) => null;
}

public static class SidewalkRibbonUtil
{
    public static bool TryProject(Vector3 world, out Vector3 projected)
    {
        projected = world;
        return false;
    }
}

