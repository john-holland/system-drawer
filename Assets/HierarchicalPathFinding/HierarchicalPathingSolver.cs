using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Pathing mode: Walk = ground/slope/terrain; Fly = 2D grid with Y interpolated along path; Drive = vehicle route preferring road corridor markers.
/// </summary>
public enum PathingMode
{
    Walk,
    Fly,
    Drive
}

/// <summary>
/// Spatial backend used by <see cref="HierarchicalPathingSolver"/>.
/// </summary>
public enum HierarchicalPathingBackend
{
    Grid2DVolumeXZ,
    UniformVolume3D,
    OctreeLeaves
}

/// <summary>
/// MVP hierarchical pathing coordinator.
/// Right now this primarily:
/// - tracks NoPathing + OffLimitsSpace markers
/// - provides a central dirty/rebuild loop that other systems can subscribe to
///
/// Future: oct/quad tree prebakes, capsule math, AABB balancing, traversable-space graph, etc.
/// </summary>
public class HierarchicalPathingSolver : MonoBehaviour, IHierarchicalPathingTree
{
    [Header("Discovery")]
    public bool autoFindMarkers = true;

    [Header("Spatial volume providers (SDF max / mesh convex tree)")]
    [Tooltip("Optional SDF max or mesh convex tree volumes treated as off-limits during grid rebuild.")]
    public List<SpatialVolumes.SpatialVolumeProvider> volumeProviders = new List<SpatialVolumes.SpatialVolumeProvider>();

    [Header("Pathing Mode")]
    [Tooltip("Walk = ground, slope, terrain. Fly = no slope blocking, Y interpolated between start and goal along path. Drive = prefer RoadCorridorMarker cells.")]
    public PathingMode pathingMode = PathingMode.Walk;

    [Header("Drive / Roads")]
    [Tooltip("When Drive mode, block cells not on a road corridor unless no corridor markers exist.")]
    public bool restrictDriveToRoadCorridors = true;

    [Tooltip("Edge cost multiplier for off-road cells in Drive mode (>1 penalizes off-road).")]
    public float driveOffRoadCostMultiplier = 4f;

    [Header("Spatial Backend")]
    [Tooltip("Grid2DVolumeXZ = legacy XZ occupancy; UniformVolume3D = full 3D cells; OctreeLeaves = adaptive octree leaf graph.")]
    public HierarchicalPathingBackend pathingBackend = HierarchicalPathingBackend.Grid2DVolumeXZ;

    [Tooltip("Cell size for UniformVolume3D backend.")]
    public float volumeCellSize = 1f;

    [Tooltip("Maximum subdivision depth for OctreeLeaves backend.")]
    [Range(1, 12)]
    public int octreeMaxDepth = 6;

    [Tooltip("Stop subdividing octree when a leaf is smaller than this extent on every axis.")]
    public float octreeMinLeafExtent = 0.75f;

    [Header("Grid (2D XZ)")]
    public Bounds worldBounds = new Bounds(Vector3.zero, new Vector3(50f, 10f, 50f));

    [Tooltip("XZ cell size in meters. Smaller = more accurate but slower to rebuild.")]
    public float cellSize = 1.0f;

    [Tooltip("Agent capsule radius for blockage checks.")]
    public float agentRadius = 0.35f;

    [Tooltip("Agent capsule height for blockage checks.")]
    public float agentHeight = 1.7f;

    [Tooltip("Physics layers treated as obstacles for traversal.")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("If enabled, allow diagonal movement (8-connected grid).")]
    public bool allowDiagonals = true;

    [Tooltip("Max nodes to expand during a single path query (0 = unlimited).")]
    public int maxExpandedNodes = 20000;

    [Header("Soft avoid (traffic / cops)")]
    public Vector3[] softAvoidPoints;
    public float softAvoidRadius = 12f;
    public float softAvoidCostMultiplier = 4f;
    public bool softAvoidEnabled = true;

    [Header("Fit to Terrain")]
    [Tooltip("When enabled, sample terrain height at each cell so paths and occupancy follow the terrain surface.")]
    public bool fitToTerrain = false;

    [Tooltip("Terrains to sample for height. Uses first terrain that contains the XZ point. Leave empty to disable.")]
    public List<Terrain> fitToTerrains = new List<Terrain>();

    [Tooltip("Max walkable slope in degrees (0 = flat only). Cells with slope to any neighbor above this are blocked. Ignored when 0 or fit-to-terrain off.")]
    [Range(0f, 90f)]
    public float maxWalkableSlopeDegrees = 45f;

    [Header("Rebuild")]
    [Tooltip("Debounce interval (seconds) for rebuilds after changes.")]
    public float rebuildDebounceSeconds = 0.1f;

    public event Action Rebuilt;

    private readonly List<OffLimitsSpace> offLimitsSpaces = new List<OffLimitsSpace>(64);
    private readonly List<NoPathing> noPathingMarkers = new List<NoPathing>(64);

    private bool dirty;
    private float lastRebuildRequestTime = -999f;
    private HierarchicalPathingGrid2D grid2D;
    private HierarchicalPathingVolumeGrid3D grid3D;
    private HierarchicalPathingOctTree octTreeBuilt;
    private readonly List<DoNotPathRegion> doNotPathMarkers = new List<DoNotPathRegion>(64);
    private int gridVersion = 0;
    private bool[] _capsuleBlocked;
    private int _physicsOccupancyKey;
    private bool _forcePhysicsRefresh;

    private int _capsuleQueriesThisRebuild;
    private double _capsuleMsThisRebuild;
    private bool _reusedPhysicsThisRebuild;

    private int _frameRebuilds;
    private int _frameDeferred;
    private int _frameCapsuleQueries;
    private double _frameCapsuleMs;
    private bool _frameReusedPhysics = true;
    private string _lastRebuildVia = "-";
    private int _dirtyMarksThisFrame;
    private int _ignoredVolumeFlips;
    private int _pendingFlipped;
    private int _pendingBoneFlips;
    private int _pendingBoneMoves;
    private float _nextQuietLogTime;
    private readonly List<string> _reasonLabels = new List<string>(8);
    private readonly List<int> _reasonCounts = new List<int>(8);

    public bool IsDirty => dirty;
    public int GridVersion => gridVersion;
    public int LastRebuildCapsuleQueries { get; private set; }
    public bool LastRebuildReusedPhysicsCache { get; private set; }
    public int IgnoredVolumeFlips => _ignoredVolumeFlips;

    private void OnEnable()
    {
        NoPathing.Changed += HandleNoPathingChanged;
        OffLimitsSpace.Changed += HandleOffLimitsChanged;
        DoNotPathRegion.Changed += HandleDoNotPathChanged;
        PhysicsPathingZone.Changed += HandlePhysicsZoneChanged;
        PhysicalMediumVolume.Changed += HandlePhysicalMediumVolumeChanged;
        SpatialVolumes.SpatialVolumeProvider.Changed += HandleSpatialVolumeProviderChanged;

        if (autoFindMarkers)
        {
            RefreshMarkers();
        }

        MarkDirty("OnEnable");
    }

    private void OnDisable()
    {
        NoPathing.Changed -= HandleNoPathingChanged;
        OffLimitsSpace.Changed -= HandleOffLimitsChanged;
        DoNotPathRegion.Changed -= HandleDoNotPathChanged;
        PhysicsPathingZone.Changed -= HandlePhysicsZoneChanged;
        PhysicalMediumVolume.Changed -= HandlePhysicalMediumVolumeChanged;
        SpatialVolumes.SpatialVolumeProvider.Changed -= HandleSpatialVolumeProviderChanged;
    }

    void HandleSpatialVolumeProviderChanged(SpatialVolumes.SpatialVolumeProvider provider)
    {
        if (provider == null || !provider.SyncSDFTreeShape)
            return;
        // Bone and shape sync broadcast every provider. This grid only samples volumeProviders,
        // so an unrelated skinned volume must not throw away the capsule cache.
        if (volumeProviders == null || !volumeProviders.Contains(provider))
        {
            _ignoredVolumeFlips++;
            return;
        }
        MarkDirty("SpatialVolume");
    }

    private void HandleDoNotPathChanged(DoNotPathRegion _)
    {
        MarkDirty("DoNotPath");
    }

    private void HandlePhysicsZoneChanged(PhysicsPathingZone _)
    {
        MarkDirty("PhysicsZone");
    }

    private void HandlePhysicalMediumVolumeChanged(PhysicalMediumVolume _)
    {
        MarkDirty("PhysicalMedium");
    }

    private void Update()
    {
        if (!dirty)
            return;

        if (Time.time - lastRebuildRequestTime < rebuildDebounceSeconds)
            return;

        RebuildNow();
    }

    void LateUpdate()
    {
        _pendingFlipped += PathingFrameTrace.TakeFlippedHasChanged();
        _pendingBoneFlips += SdfMax.SdfMaxShapeSyncStats.FlippedHasChanged;
        _pendingBoneMoves += SdfMax.SdfMaxShapeSyncStats.ShapeChanges;
        SdfMax.SdfMaxShapeSyncStats.FlippedHasChanged = 0;
        SdfMax.SdfMaxShapeSyncStats.ShapeChanges = 0;

        bool rebuilt = _frameRebuilds > 0 || _frameDeferred > 0;
        bool quiet = _dirtyMarksThisFrame > 0 || _ignoredVolumeFlips > 0 || _pendingFlipped > 0 || _pendingBoneFlips > 0 || _pendingBoneMoves > 0;
        if (!rebuilt && !quiet)
            return;
        if (!rebuilt && Time.unscaledTime < _nextQuietLogTime)
            return;

        float dtMs = Time.unscaledDeltaTime * 1000f;
        float fps = Time.unscaledDeltaTime > 1e-6f ? 1f / Time.unscaledDeltaTime : 0f;
        int gw = grid2D != null ? grid2D.width : 0;
        int gh = grid2D != null ? grid2D.height : 0;
        string reused = _frameRebuilds == 0 ? "-" : _frameReusedPhysics.ToString();
        PathingFrameTrace.Write(
            "frame=" + Time.frameCount +
            " dtMs=" + dtMs.ToString("F1") +
            " fps=" + fps.ToString("F2") +
            " solver=" + name +
            " rebuilds=" + _frameRebuilds +
            " via=" + _lastRebuildVia +
            " cells=" + gw + "x" + gh +
            " bounds=" + worldBounds.size.x.ToString("F0") + "x" + worldBounds.size.z.ToString("F0") +
            " cellSize=" + cellSize.ToString("F3") +
            " capsules=" + _frameCapsuleQueries +
            " capsuleMs=" + _frameCapsuleMs.ToString("F1") +
            " reusedPhysics=" + reused +
            " deferred=" + _frameDeferred +
            " dirty=" + _dirtyMarksThisFrame + "(" + FormatReasons() + ")" +
            " ignoredVolume=" + _ignoredVolumeFlips +
            " flippedHasChanged=" + _pendingFlipped +
            " boneFlips=" + _pendingBoneFlips +
            " boneMoves=" + _pendingBoneMoves);

        if (!rebuilt)
            _nextQuietLogTime = Time.unscaledTime + 1f;

        _frameRebuilds = 0;
        _frameDeferred = 0;
        _frameCapsuleQueries = 0;
        _frameCapsuleMs = 0;
        _frameReusedPhysics = true;
        _dirtyMarksThisFrame = 0;
        _ignoredVolumeFlips = 0;
        _pendingFlipped = 0;
        _pendingBoneFlips = 0;
        _pendingBoneMoves = 0;
        _reasonLabels.Clear();
        _reasonCounts.Clear();
    }

    public void MarkDirty() => MarkDirty(null);

    public void MarkDirty(string reason)
    {
        dirty = true;
        lastRebuildRequestTime = Time.time;
        NoteDirtyReason(string.IsNullOrEmpty(reason) ? "MarkDirty" : reason);
    }

    void NoteDirtyReason(string reason)
    {
        _dirtyMarksThisFrame++;
        for (int i = 0; i < _reasonLabels.Count; i++)
        {
            if (_reasonLabels[i] == reason)
            {
                _reasonCounts[i]++;
                return;
            }
        }
        if (_reasonLabels.Count >= 8)
            return;
        _reasonLabels.Add(reason);
        _reasonCounts.Add(1);
    }

    string FormatReasons()
    {
        if (_reasonLabels.Count == 0)
            return "-";
        var sb = new StringBuilder();
        for (int i = 0; i < _reasonLabels.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(_reasonLabels[i]);
            if (_reasonCounts[i] > 1)
            {
                sb.Append('x');
                sb.Append(_reasonCounts[i]);
            }
        }
        return sb.ToString();
    }

    /// <summary>Set worldBounds to the union of all fit-to-terrain terrains' world bounds. No-op if fitToTerrains is null or empty.</summary>
    public void SetWorldBoundsFromTerrains()
    {
        if (fitToTerrains == null || fitToTerrains.Count == 0)
            return;
        Bounds? union = null;
        for (int i = 0; i < fitToTerrains.Count; i++)
        {
            Terrain t = fitToTerrains[i];
            if (t == null || t.terrainData == null)
                continue;
            Bounds local = t.terrainData.bounds;
            Vector3 worldCenter = t.transform.TransformPoint(local.center);
            Vector3 worldSize = Vector3.Scale(local.size, t.transform.lossyScale);
            Bounds world = new Bounds(worldCenter, worldSize);
            if (!union.HasValue)
                union = world;
            else
            {
                var u = union.Value;
                u.Encapsulate(world);
                union = u;
            }
        }
        if (union.HasValue)
            worldBounds = union.Value;
    }

    /// <summary>Force an immediate rebuild of the occupancy grid (markers + terrain). Call from editor button or at runtime.</summary>
    public void RebuildGrid()
    {
        if (autoFindMarkers)
            RefreshMarkers();
        _forcePhysicsRefresh = true;
        RebuildPathBackendData("RebuildGrid");
        _forcePhysicsRefresh = false;
        gridVersion++;
        dirty = false;
        Rebuilt?.Invoke();
    }

    public IReadOnlyList<OffLimitsSpace> GetOffLimitsSpaces() => offLimitsSpaces;
    public IReadOnlyList<NoPathing> GetNoPathingMarkers() => noPathingMarkers;

    private void HandleNoPathingChanged(NoPathing np)
    {
        MarkDirty("NoPathing");
    }

    private void HandleOffLimitsChanged(OffLimitsSpace ol)
    {
        MarkDirty("OffLimits");
    }

    private void RefreshMarkers()
    {
        offLimitsSpaces.Clear();
        offLimitsSpaces.AddRange(FindObjectsByType<OffLimitsSpace>(FindObjectsSortMode.None));

        noPathingMarkers.Clear();
        noPathingMarkers.AddRange(FindObjectsByType<NoPathing>(FindObjectsSortMode.None));

        doNotPathMarkers.Clear();
        doNotPathMarkers.AddRange(FindObjectsByType<DoNotPathRegion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    private void RebuildNow()
    {
        if (autoFindMarkers)
            RefreshMarkers();

        RebuildPathBackendData("Update");
        gridVersion++;

        dirty = false;
        Rebuilt?.Invoke();
    }

    void RebuildPathBackendData(string via)
    {
        _capsuleQueriesThisRebuild = 0;
        _capsuleMsThisRebuild = 0;
        _reusedPhysicsThisRebuild = false;

        switch (pathingBackend)
        {
            case HierarchicalPathingBackend.Grid2DVolumeXZ:
                grid3D = null;
                octTreeBuilt = null;
                RebuildOccupancyGrid2D();
                break;
            case HierarchicalPathingBackend.UniformVolume3D:
                grid2D = null;
                octTreeBuilt = null;
                {
                    var sw = Stopwatch.StartNew();
                    RebuildOccupancyVolume3D();
                    _capsuleMsThisRebuild = sw.Elapsed.TotalMilliseconds;
                }
                break;
            case HierarchicalPathingBackend.OctreeLeaves:
                grid2D = null;
                grid3D = null;
                {
                    var sw = Stopwatch.StartNew();
                    RebuildOctreeLeaves();
                    _capsuleMsThisRebuild = sw.Elapsed.TotalMilliseconds;
                }
                break;
        }

        AccountRebuild(via);
    }

    void AccountRebuild(string via)
    {
        LastRebuildCapsuleQueries = _capsuleQueriesThisRebuild;
        LastRebuildReusedPhysicsCache = _reusedPhysicsThisRebuild;
        _frameRebuilds++;
        _lastRebuildVia = via;
        _frameCapsuleQueries += _capsuleQueriesThisRebuild;
        _frameCapsuleMs += _capsuleMsThisRebuild;
        _frameReusedPhysics = _frameRebuilds == 1
            ? _reusedPhysicsThisRebuild
            : _frameReusedPhysics && _reusedPhysicsThisRebuild;
    }

    bool DoNotPathContains(Vector3 worldCenter)
    {
        for (int i = 0; i < doNotPathMarkers.Count; i++)
        {
            DoNotPathRegion r = doNotPathMarkers[i];
            if (r != null && r.isActiveAndEnabled && r.ContainsWorldPosition(worldCenter))
                return true;
        }

        return false;
    }

    float PhysicsZoneEdgeCostMultiplier(Vector3 a, Vector3 b)
    {
        PhysicsPathingZone.SampleAt((a + b) * 0.5f, out float pathCostMul, out _);
        if (pathingMode == PathingMode.Drive)
            pathCostMul *= DriveCorridorEdgeCostMultiplier((a + b) * 0.5f);
        pathCostMul *= SoftAvoidEdgeCostMultiplier((a + b) * 0.5f);
        return pathCostMul;
    }

    float SoftAvoidEdgeCostMultiplier(Vector3 midpoint)
    {
        if (!softAvoidEnabled || softAvoidPoints == null || softAvoidPoints.Length == 0)
            return 1f;
        float r = Mathf.Max(0.01f, softAvoidRadius);
        float r2 = r * r;
        float mul = Mathf.Max(1f, softAvoidCostMultiplier);
        for (int i = 0; i < softAvoidPoints.Length; i++)
        {
            Vector3 d = midpoint - softAvoidPoints[i];
            d.y = 0f;
            if (d.sqrMagnitude <= r2)
                return mul;
        }
        return 1f;
    }

    public void SetSoftAvoid(Vector3[] points, float radius, float costMultiplier, bool enabled)
    {
        softAvoidPoints = points;
        softAvoidRadius = radius;
        softAvoidCostMultiplier = costMultiplier;
        softAvoidEnabled = enabled;
    }

    /// <summary>Test / tooling: soft-avoid multiplier at a world point.</summary>
    public float EvaluateSoftAvoidMultiplier(Vector3 world) => SoftAvoidEdgeCostMultiplier(world);

    float DriveCorridorEdgeCostMultiplier(Vector3 midpoint)
    {
        bool onRoad = RoadCorridorMarker.IsOnRoadCorridor(midpoint);
        if (restrictDriveToRoadCorridors && !onRoad)
            return driveOffRoadCostMultiplier;
        return onRoad ? 1f : driveOffRoadCostMultiplier;
    }

    bool EvaluateDriveCorridorBlocked(Vector3 center)
    {
        if (pathingMode != PathingMode.Drive || !restrictDriveToRoadCorridors)
            return false;
        var markers = FindObjectsByType<RoadCorridorMarker>(FindObjectsSortMode.None);
        if (markers == null || markers.Length == 0)
            return false;
        return !RoadCorridorMarker.IsOnRoadCorridor(center);
    }

    bool EvaluateCapsuleBlocked(Vector3 center)
    {
        float halfH = Mathf.Max(0.01f, agentHeight * 0.5f);
        float capsuleBottomOffset = Mathf.Max(agentRadius, 0.01f);
        Vector3 p1 = center + Vector3.up * (halfH - capsuleBottomOffset);
        Vector3 p2 = center - Vector3.up * (halfH - capsuleBottomOffset);
        _capsuleQueriesThisRebuild++;
        return Physics.CheckCapsule(p1, p2, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    bool EvaluateMarkersBlocked(Vector3 center)
    {
        for (int i = 0; i < offLimitsSpaces.Count; i++)
        {
            OffLimitsSpace ol = offLimitsSpaces[i];
            if (ol != null && ol.GetWorldBounds().Contains(center))
                return true;
        }

        for (int i = 0; i < noPathingMarkers.Count; i++)
        {
            NoPathing np = noPathingMarkers[i];
            if (np != null && np.GetWorldBounds().Contains(center))
                return true;
        }

        if (DoNotPathContains(center))
            return true;

        if (EvaluateSpatialVolumesBlocked(center))
            return true;

        return false;
    }

    bool EvaluateSpatialVolumesBlocked(Vector3 center)
    {
        if (volumeProviders == null || volumeProviders.Count == 0)
            return false;

        for (int i = 0; i < volumeProviders.Count; i++)
        {
            var provider = volumeProviders[i];
            if (provider == null || !provider.isActiveAndEnabled)
                continue;
            if (provider.TrySample(center, 0f, out _, out bool inside) && inside)
                return true;
        }

        return false;
    }

    void RebuildOccupancyVolume3D()
    {
        float cs = Mathf.Max(0.05f, volumeCellSize > 0f ? volumeCellSize : cellSize);
        grid3D = new HierarchicalPathingVolumeGrid3D(worldBounds, cs);

        for (int iz = 0; iz < grid3D.depth; iz++)
        {
            for (int iy = 0; iy < grid3D.height; iy++)
            {
                for (int ix = 0; ix < grid3D.width; ix++)
                {
                    Vector3 center = grid3D.CellCenterWorld(ix, iy, iz);
                    bool blocked = EvaluateCapsuleBlocked(center) || EvaluateMarkersBlocked(center) || EvaluateDriveCorridorBlocked(center);
                    grid3D.SetBlocked(ix, iy, iz, blocked);
                }
            }
        }
    }

    void RebuildOctreeLeaves()
    {
        octTreeBuilt = HierarchicalPathingOctTree.Build(
            worldBounds,
            octreeMaxDepth,
            octreeMinLeafExtent,
            center => EvaluateCapsuleBlocked(center) || EvaluateMarkersBlocked(center) || EvaluateDriveCorridorBlocked(center));
    }

    /// <summary>
    /// Query a path on the current grid. Returns an empty list if no grid or no path.
    /// When pathingMode is Fly, Y is interpolated between start and goal along the path.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld, bool returnBestEffortPathWhenNoPath = false)
    {
        EnsureGridBuiltForQuery();

        List<Vector3> path;

        switch (pathingBackend)
        {
            case HierarchicalPathingBackend.UniformVolume3D:
                path = HierarchicalPathingAStar3D.FindPath(
                    grid3D,
                    startWorld,
                    goalWorld,
                    new HierarchicalPathingAStar3D.Settings
                    {
                        allowDiagonalSteps = allowDiagonals,
                        maxExpandedNodes = maxExpandedNodes,
                        returnBestEffortPathWhenNoPath = returnBestEffortPathWhenNoPath,
                        EdgeCost = (a, b) => PhysicsZoneEdgeCostMultiplier(a, b)
                    });
                return path ?? new List<Vector3>();

            case HierarchicalPathingBackend.OctreeLeaves:
                path = HierarchicalPathingOctTree.FindPathThroughLeaves(
                    octTreeBuilt != null ? octTreeBuilt.Leaves : null,
                    startWorld,
                    goalWorld,
                    maxExpandedNodes,
                    SoftAvoidEdgeCostMultiplier);
                return path ?? new List<Vector3>();

            default:
                PathingMode gridMode = pathingMode == PathingMode.Drive ? PathingMode.Walk : pathingMode;
                float sampleY = gridMode == PathingMode.Fly ? Mathf.Lerp(startWorld.y, goalWorld.y, 0.5f) : startWorld.y;
                path = HierarchicalPathingAStar2D.FindPath(
                    grid2D,
                    startWorld,
                    goalWorld,
                    sampleY,
                    new HierarchicalPathingAStar2D.Settings
                    {
                        allowDiagonals = allowDiagonals,
                        maxExpandedNodes = maxExpandedNodes,
                        returnBestEffortPathWhenNoPath = returnBestEffortPathWhenNoPath,
                        EdgeCostMultiplier = PhysicsZoneEdgeCostMultiplier
                    });

                if (path != null && path.Count > 0 && gridMode == PathingMode.Fly)
                    ApplyFlyingYInterpolation(path, startWorld.y, goalWorld.y);

                return path ?? new List<Vector3>();
        }
    }

    /// <summary>
    /// Interpolate Y along path from startY to goalY (Fly mode).
    /// </summary>
    private static void ApplyFlyingYInterpolation(List<Vector3> path, float startY, float goalY)
    {
        int n = path.Count;
        if (n <= 0) return;
        for (int i = 0; i < n; i++)
        {
            float t = n > 1 ? (float)i / (n - 1) : 1f;
            Vector3 p = path[i];
            path[i] = new Vector3(p.x, Mathf.Lerp(startY, goalY, t), p.z);
        }
    }

    /// <summary>
    /// Find a walkable position at approximately distanceFromGoal meters from the goal along the path from start to goal.
    /// Uses FindPath then PathDistanceUtility. Useful for "stand here to throw" (e.g. desiredThrowDistance from target).
    /// </summary>
    /// <param name="startWorld">Agent/start position.</param>
    /// <param name="goalWorld">Target position (e.g. throw target).</param>
    /// <param name="distanceFromGoal">Desired distance from goal (meters). Position returned is at pathLength - distanceFromGoal from start.</param>
    /// <param name="returnBestEffortPathWhenNoPath">If true, use best-effort path when no full path found.</param>
    /// <returns>World position at that distance from goal along the path, or goal if path is empty or too short.</returns>
    public Vector3 FindPositionAtDistanceFromGoal(Vector3 startWorld, Vector3 goalWorld, float distanceFromGoal, bool returnBestEffortPathWhenNoPath = false)
    {
        List<Vector3> path = FindPath(startWorld, goalWorld, returnBestEffortPathWhenNoPath);
        if (path == null || path.Count == 0)
            return goalWorld;
        float totalLength = PathDistanceUtility.GetPathLength(path);
        float distanceFromStart = Mathf.Max(0f, totalLength - distanceFromGoal);
        return PathDistanceUtility.GetPositionAtDistanceAlongPath(path, distanceFromStart, fromStart: true);
    }

    /// <summary>
    /// Quick occupancy query for obstacle-aware systems (audio/smell) without running A*.
    /// Returns true if the point is outside bounds or inside a blocked cell.
    /// </summary>
    public bool IsBlockedAtWorld(Vector3 worldPos)
    {
        EnsureGridBuiltForQuery();

        switch (pathingBackend)
        {
            case HierarchicalPathingBackend.UniformVolume3D:
                if (grid3D == null)
                    return true;
                if (!grid3D.TryWorldToCell(worldPos, out int vx, out int vy, out int vz))
                    return true;
                return grid3D.IsBlocked(vx, vy, vz);

            case HierarchicalPathingBackend.OctreeLeaves:
                if (octTreeBuilt == null || octTreeBuilt.Leaves == null)
                    return true;
                foreach (var leaf in octTreeBuilt.Leaves)
                {
                    if (leaf == null || !leaf.bounds.Contains(worldPos))
                        continue;
                    return leaf.blocked;
                }

                return true;

            default:
                if (grid2D == null)
                    return true;

                if (!grid2D.TryWorldToCell(worldPos, out int x, out int z))
                    return true;

                return grid2D.IsBlocked(x, z);
        }
    }

    bool BackendHasValidData()
    {
        switch (pathingBackend)
        {
            case HierarchicalPathingBackend.Grid2DVolumeXZ:
                return grid2D != null;
            case HierarchicalPathingBackend.UniformVolume3D:
                return grid3D != null;
            case HierarchicalPathingBackend.OctreeLeaves:
                return octTreeBuilt != null && octTreeBuilt.Leaves != null && octTreeBuilt.Leaves.Count > 0;
            default:
                return false;
        }
    }

    private void EnsureGridBuiltForQuery()
    {
        if (!dirty && BackendHasValidData())
            return;

        // A query in the same frame as a fresh dirty mark used to rebuild immediately,
        // ignoring debounce. That second sweep is the frame that dropped under 1 fps.
        if (BackendHasValidData() && Time.time - lastRebuildRequestTime < rebuildDebounceSeconds)
        {
            _frameDeferred++;
            return;
        }

        if (autoFindMarkers)
            RefreshMarkers();

        RebuildPathBackendData("Query");
        gridVersion++;
        dirty = false;
    }

    int ComputePhysicsOccupancyKey()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + worldBounds.center.GetHashCode();
            h = h * 31 + worldBounds.size.GetHashCode();
            h = h * 31 + Mathf.RoundToInt(cellSize * 1000f);
            h = h * 31 + Mathf.RoundToInt(agentRadius * 1000f);
            h = h * 31 + Mathf.RoundToInt(agentHeight * 1000f);
            h = h * 31 + obstacleMask.value;
            h = h * 31 + (fitToTerrain ? 1 : 0);
            h = h * 31 + (int)pathingBackend;
            h = h * 31 + (int)pathingMode;
            if (fitToTerrains != null)
            {
                for (int i = 0; i < fitToTerrains.Count; i++)
                {
                    Terrain t = fitToTerrains[i];
                    h = h * 31 + (t != null ? t.GetInstanceID() : 0);
                }
            }
            return h;
        }
    }

    bool SlopeBlocksCell(int x, int z, float defaultY)
    {
        float myH = grid2D.GetCellHeight(x, z, defaultY);
        float maxRise = cellSize * Mathf.Tan(maxWalkableSlopeDegrees * Mathf.Deg2Rad);
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0)
                    continue;
                int nx = x + dx;
                int nz = z + dz;
                if (!grid2D.IsInBounds(nx, nz))
                    continue;
                float nh = grid2D.GetCellHeight(nx, nz, defaultY);
                if (Mathf.Abs(nh - myH) > maxRise)
                    return true;
            }
        }
        return false;
    }

    private void RebuildOccupancyGrid2D()
    {
        bool useTerrainHeights = fitToTerrain && fitToTerrains != null && fitToTerrains.Count > 0;
        float defaultY = worldBounds.center.y;
        bool flying = pathingMode == PathingMode.Fly;
        int key = ComputePhysicsOccupancyKey();
        bool reuse = !_forcePhysicsRefresh
            && grid2D != null
            && _capsuleBlocked != null
            && _physicsOccupancyKey == key
            && _capsuleBlocked.Length == grid2D.width * grid2D.height;

        if (!reuse)
        {
            grid2D = new HierarchicalPathingGrid2D(worldBounds, cellSize, storeHeights: useTerrainHeights);
            _capsuleBlocked = new bool[grid2D.width * grid2D.height];
            _physicsOccupancyKey = key;

            if (useTerrainHeights)
            {
                Profiler.BeginSample("Pathing.TerrainHeights");
                for (int z = 0; z < grid2D.height; z++)
                {
                    for (int x = 0; x < grid2D.width; x++)
                    {
                        Vector3 center = grid2D.CellCenterWorld(x, z, defaultY);
                        float terrainY = SampleTerrainHeightAt(center.x, center.z, defaultY);
                        grid2D.SetCellHeight(x, z, terrainY);
                    }
                }
                Profiler.EndSample();
            }

            Profiler.BeginSample("Pathing.CheckCapsuleSweep");
            var sw = Stopwatch.StartNew();
            for (int z = 0; z < grid2D.height; z++)
            {
                for (int x = 0; x < grid2D.width; x++)
                {
                    Vector3 center = grid2D.CellCenterWorld(x, z, defaultY);
                    _capsuleBlocked[grid2D.Index(x, z)] = EvaluateCapsuleBlocked(center);
                }
            }
            sw.Stop();
            Profiler.EndSample();
            _capsuleMsThisRebuild = sw.Elapsed.TotalMilliseconds;
        }

        _reusedPhysicsThisRebuild = reuse;

        Profiler.BeginSample("Pathing.MarkerSweep");
        for (int z = 0; z < grid2D.height; z++)
        {
            for (int x = 0; x < grid2D.width; x++)
            {
                Vector3 center = grid2D.CellCenterWorld(x, z, defaultY);
                bool blocked = _capsuleBlocked[grid2D.Index(x, z)] || EvaluateMarkersBlocked(center);
                if (!blocked && !flying && useTerrainHeights && maxWalkableSlopeDegrees > 0f)
                    blocked = SlopeBlocksCell(x, z, defaultY);
                grid2D.SetBlocked(x, z, blocked);
            }
        }
        Profiler.EndSample();
    }

    /// <summary>Sample terrain height at world XZ from registered fit-to-terrain terrains. Returns defaultY if no terrain contains the point.</summary>
    private float SampleTerrainHeightAt(float worldX, float worldZ, float defaultY)
    {
        if (fitToTerrains == null) return defaultY;
        for (int i = 0; i < fitToTerrains.Count; i++)
        {
            Terrain t = fitToTerrains[i];
            if (t == null || t.terrainData == null) continue;
            Bounds tb = t.terrainData.bounds;
            Vector3 tMin = t.transform.position + tb.min;
            Vector3 tMax = t.transform.position + tb.max;
            if (worldX >= tMin.x && worldX <= tMax.x && worldZ >= tMin.z && worldZ <= tMax.z)
                return t.SampleHeight(new Vector3(worldX, 0f, worldZ)) + t.transform.position.y;
        }
        return defaultY;
    }

    [Header("Gizmos")]
    public bool showGizmos = true;
    public bool showBlockedCells = true;
    public bool showFreeCells = false;
    public int gizmoCellStride = 2;
    public Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.25f);
    public Color freeColor = new Color(0.2f, 1f, 0.2f, 0.12f);

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

        if (pathingBackend != HierarchicalPathingBackend.Grid2DVolumeXZ || grid2D == null)
            return;

        int stride = Mathf.Max(1, gizmoCellStride);
        float y = worldBounds.center.y;
        Vector3 size = new Vector3(grid2D.cellSize, 0.1f, grid2D.cellSize);

        for (int z = 0; z < grid2D.height; z += stride)
        {
            for (int x = 0; x < grid2D.width; x += stride)
            {
                bool blocked = grid2D.IsBlocked(x, z);
                if (blocked && !showBlockedCells) continue;
                if (!blocked && !showFreeCells) continue;

                Gizmos.color = blocked ? blockedColor : freeColor;
                Gizmos.DrawCube(grid2D.CellCenterWorld(x, z, y), size);
            }
        }
    }
}

/// <summary>
/// RTS-style gate: behavior subtree / causality depth must complete before path replacement or layout.
/// </summary>
public static class PathReplacementGate
{
    static bool _globallyLocked;
    static int _requiredCausalityDepth;
    static int _currentCausalityDepth;
    static readonly HashSet<string> _pendingSubtreeIds = new HashSet<string>();
    static readonly HashSet<string> _completedSubtreeIds = new HashSet<string>();

    public static bool IsLocked => _globallyLocked;

    public static void LockUntilCausalityDepth(int minDepth)
    {
        _globallyLocked = true;
        _requiredCausalityDepth = Mathf.Max(0, minDepth);
    }

    public static void LockUntilSubtreeSuccess(string subtreeId)
    {
        if (string.IsNullOrEmpty(subtreeId))
            return;
        _globallyLocked = true;
        _pendingSubtreeIds.Add(subtreeId);
    }

    public static void NotifySubtreeSuccess(string subtreeId)
    {
        if (string.IsNullOrEmpty(subtreeId))
            return;
        _completedSubtreeIds.Add(subtreeId);
        _pendingSubtreeIds.Remove(subtreeId);
        TryUnlock();
    }

    public static void SetCausalityDepth(int depth)
    {
        _currentCausalityDepth = Mathf.Max(0, depth);
        TryUnlock();
    }

    public static void Unlock()
    {
        _globallyLocked = false;
        _requiredCausalityDepth = 0;
        _pendingSubtreeIds.Clear();
    }

    static void TryUnlock()
    {
        if (_currentCausalityDepth < _requiredCausalityDepth)
            return;
        if (_pendingSubtreeIds.Count > 0)
            return;
        _globallyLocked = false;
    }

    public static bool CanReplacePath(bool behaviorTreeSucceeded = false)
    {
        if (!_globallyLocked)
            return true;
        return behaviorTreeSucceeded;
    }
}

/// <summary>Marks baked road corridor bounds for Drive pathing preference (set by Roads bake).</summary>
public class RoadCorridorMarker : MonoBehaviour
{
    public string roadSegmentId;
    public List<Bounds> corridorBounds = new List<Bounds>();

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        for (int i = 0; i < corridorBounds.Count; i++)
        {
            if (corridorBounds[i].Contains(worldPoint))
                return true;
        }
        return false;
    }

    public static bool IsOnRoadCorridor(Vector3 worldPoint)
    {
        var markers = FindObjectsByType<RoadCorridorMarker>(FindObjectsSortMode.None);
        foreach (var m in markers)
        {
            if (m != null && m.isActiveAndEnabled && m.ContainsWorldPoint(worldPoint))
                return true;
        }
        return false;
    }
}

