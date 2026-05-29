using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    public RagdollAnimationSetManager ragdollAnimationSetManager;
    public VehicleActor hintVehicle;

    [Header("Preview / solver inputs")]
    public HierarchicalPathingSolver pathingSolverForPreview;
    public Vector3 previewStartWorld;
    public Vector3 previewGoalWorld;
    [Range(0f, 1f)] public float requireAsset01 = 0.5f;
    [Range(0f, 1f)] public float requireType01 = 0.5f;
    public List<GoodSection> toolSectionsForPreview = new List<GoodSection>();
    public List<GoodSection> acrobaticsSectionsForPreview = new List<GoodSection>();
    public TravelCoordinateMode coordinateMode = TravelCoordinateMode.World;

    [Header("Spatial authoring (Bedoga / Continuum-friendly)")]
    [Tooltip("Wide slot for SpatialGenerator / SpatialGenerator4D when assigned from editor.")]
    public UnityEngine.Object spatialGeneratorSlot;

    [Tooltip("When true and a spatial generator is assigned, raw world fields are treated as overridden by the generator workflow.")]
    public bool disableRawLocationWhenSpatialGeneratorAssigned;

    [Tooltip("When true with static seed mode, location + asset slot enabled per authoring flow.")]
    public bool staticGeneratorSeedMode;

    [Header("Preview navigation (editor)")]
    public TravelPreviewFitMode previewFitMode = TravelPreviewFitMode.EntirePath;
    public int previewSegmentIndex;

    [Header("Travel script (editor authoring)")]
    public List<TravelAuthoringRow> authoringRows = new List<TravelAuthoringRow>();

    [Header("Gizmos")]
    public bool drawTravelGizmos = true;
    [Tooltip("When multibody runs, also draw the pre-adjustment plan (magenta in scene handles).")]
    public bool drawMultibodyBasePlan = true;

    [Header("Multibody travel")]
    public TravelAgentMultibodySettings multibody = new TravelAgentMultibodySettings();

    [Header("Multibody formation (optional)")]
    [Tooltip("Agents with the same non-empty id share a formation cohort for slot assignment and optional peer filtering.")]
    public string multibodyFormationGroupId = "";

    [Tooltip("Slot index within cohort when >= 0; -1 = order by stable instance id sort within group.")]
    public int formationSlotIndex = -1;

    [Header("Timeline planner (optional)")]
    public PlannerTimelineOptions plannerTimelineOptions = PlannerTimelineOptions.DefaultLegacy();

    [Tooltip("Optional extra landmark positions merged into the timeline chord graph (world space).")]
    public List<Vector3> timelineExtraLandmarks = new List<Vector3>();

    [SerializeField] GenericMultiModalPathPlan cachedPlan = new GenericMultiModalPathPlan();

    [SerializeField] GenericMultiModalPathPlan cachedPlanBeforeMultibody;

    [SerializeField] List<TravelDiscoveredNodeInfo> discoveredNodes = new List<TravelDiscoveredNodeInfo>();

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

    /// <summary>
    /// Rebuild cached plan using preview positions and configured sections (editor preview / runtime tooling).
    /// </summary>
    public void RebuildCachedPlan(GameObject goalTarget = null)
    {
        cachedPlanBeforeMultibody = null;
        cachedPlan = new GenericMultiModalPathPlan();
        HierarchicalPathingSolver solver = pathingSolverForPreview != null
            ? pathingSolverForPreview
            : FindAnyObjectByType<HierarchicalPathingSolver>();

        if (solver == null)
            return;

        Vector3 queryPos = previewStartWorld;
        var hints = new GenericTraversibilityPlannerSolver.PlannerHints
        {
            requireAsset01 = requireAsset01,
            requireType01 = requireType01,
            preferredVehicle = hintVehicle
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
            PhysicalPathingMedium.Air,
            in tl);

        if (built == null || built.IsEmpty)
        {
            cachedPlan = built ?? new GenericMultiModalPathPlan();
            return;
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
