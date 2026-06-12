using System;
using System.Collections.Generic;
using UnityEngine;

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

    public bool IsDirty => dirty;
    public int GridVersion => gridVersion;

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

        MarkDirty();
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
        if (provider != null && provider.SyncSDFTreeShape)
            MarkDirty();
    }

    private void HandleDoNotPathChanged(DoNotPathRegion _)
    {
        MarkDirty();
    }

    private void HandlePhysicsZoneChanged(PhysicsPathingZone _)
    {
        MarkDirty();
    }

    private void HandlePhysicalMediumVolumeChanged(PhysicalMediumVolume _)
    {
        MarkDirty();
    }

    private void Update()
    {
        if (!dirty)
            return;

        if (Time.time - lastRebuildRequestTime < rebuildDebounceSeconds)
            return;

        RebuildNow();
    }

    public void MarkDirty()
    {
        dirty = true;
        lastRebuildRequestTime = Time.time;
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
        RebuildPathBackendData();
        gridVersion++;
        dirty = false;
        Rebuilt?.Invoke();
    }

    public IReadOnlyList<OffLimitsSpace> GetOffLimitsSpaces() => offLimitsSpaces;
    public IReadOnlyList<NoPathing> GetNoPathingMarkers() => noPathingMarkers;

    private void HandleNoPathingChanged(NoPathing np)
    {
        MarkDirty();
    }

    private void HandleOffLimitsChanged(OffLimitsSpace ol)
    {
        MarkDirty();
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

        RebuildPathBackendData();
        gridVersion++;

        dirty = false;
        Rebuilt?.Invoke();
    }

    void RebuildPathBackendData()
    {
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
                RebuildOccupancyVolume3D();
                break;
            case HierarchicalPathingBackend.OctreeLeaves:
                grid2D = null;
                grid3D = null;
                RebuildOctreeLeaves();
                break;
        }
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
        return pathCostMul;
    }

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
                path = HierarchicalPathingOctTree.FindPathThroughLeaves(octTreeBuilt != null ? octTreeBuilt.Leaves : null, startWorld, goalWorld, maxExpandedNodes);
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

        if (autoFindMarkers)
            RefreshMarkers();

        RebuildPathBackendData();
        gridVersion++;
        dirty = false;
    }

    private void RebuildOccupancyGrid2D()
    {
        bool useTerrainHeights = fitToTerrain && fitToTerrains != null && fitToTerrains.Count > 0;
        grid2D = new HierarchicalPathingGrid2D(worldBounds, cellSize, storeHeights: useTerrainHeights);

        float halfH = Mathf.Max(0.01f, agentHeight * 0.5f);
        float capsuleBottomOffset = Mathf.Max(agentRadius, 0.01f);
        float defaultY = worldBounds.center.y;

        // First pass: set cell heights when fit-to-terrain is enabled
        if (useTerrainHeights)
        {
            for (int z = 0; z < grid2D.height; z++)
            {
                for (int x = 0; x < grid2D.width; x++)
                {
                    Vector3 center = grid2D.CellCenterWorld(x, z, defaultY);
                    float terrainY = SampleTerrainHeightAt(center.x, center.z, defaultY);
                    grid2D.SetCellHeight(x, z, terrainY);
                }
            }
        }

        bool flying = pathingMode == PathingMode.Fly;
        // Mark blocked cells based on:
        // - physics obstacles (capsule overlap) at cell height
        // - OffLimitsSpace / NoPathing bounds (unless flying: still respect no-fly zones)
        // - slope above maxWalkableSlope when fit-to-terrain is on (Walk only; Fly skips slope)
        for (int z = 0; z < grid2D.height; z++)
        {
            for (int x = 0; x < grid2D.width; x++)
            {
                float cellY = grid2D.GetCellHeight(x, z, defaultY);
                Vector3 center = grid2D.CellCenterWorld(x, z, defaultY);

                // Capsule endpoints at cell height (agent stands on terrain)
                Vector3 p1 = center + Vector3.up * (halfH - capsuleBottomOffset);
                Vector3 p2 = center - Vector3.up * (halfH - capsuleBottomOffset);

                bool blocked = Physics.CheckCapsule(p1, p2, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore);

                if (!blocked)
                {
                    // OffLimitsSpace blocks cells in its bounds
                    for (int i = 0; i < offLimitsSpaces.Count; i++)
                    {
                        OffLimitsSpace ol = offLimitsSpaces[i];
                        if (ol == null) continue;
                        if (ol.GetWorldBounds().Contains(center))
                        {
                            blocked = true;
                            break;
                        }
                    }
                }

                if (!blocked)
                {
                    for (int i = 0; i < noPathingMarkers.Count; i++)
                    {
                        NoPathing np = noPathingMarkers[i];
                        if (np == null) continue;
                        if (np.GetWorldBounds().Contains(center))
                        {
                            blocked = true;
                            break;
                        }
                    }
                }

                if (!blocked && DoNotPathContains(center))
                    blocked = true;

                // Slope check (Walk only): block cell if rise to any neighbor exceeds max walkable slope
                if (!blocked && !flying && useTerrainHeights && maxWalkableSlopeDegrees > 0f)
                {
                    float myH = grid2D.GetCellHeight(x, z, defaultY);
                    float maxRise = cellSize * Mathf.Tan(maxWalkableSlopeDegrees * Mathf.Deg2Rad);
                    for (int dz = -1; dz <= 1 && !blocked; dz++)
                    {
                        for (int dx = -1; dx <= 1 && !blocked; dx++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = x + dx, nz = z + dz;
                            if (!grid2D.IsInBounds(nx, nz)) continue;
                            float nh = grid2D.GetCellHeight(nx, nz, defaultY);
                            if (Mathf.Abs(nh - myH) > maxRise)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }
                }

                grid2D.SetBlocked(x, z, blocked);
            }
        }
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

