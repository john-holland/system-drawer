using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pathing mode: Walk = ground/slope/terrain; Fly = 3D movement, no slope blocking, Y interpolated along path.
/// </summary>
public enum PathingMode
{
    Walk,
    Fly
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

    [Header("Pathing Mode")]
    [Tooltip("Walk = ground, slope, terrain. Fly = no slope blocking, Y interpolated between start and goal along path.")]
    public PathingMode pathingMode = PathingMode.Walk;

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
    private int gridVersion = 0;

    public bool IsDirty => dirty;
    public int GridVersion => gridVersion;

    private void OnEnable()
    {
        NoPathing.Changed += HandleNoPathingChanged;
        OffLimitsSpace.Changed += HandleOffLimitsChanged;

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
        RebuildOccupancyGrid2D();
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
    }

    private void RebuildNow()
    {
        // Rebuild marker lists + occupancy grid.
        if (autoFindMarkers)
        {
            RefreshMarkers();
        }

        RebuildOccupancyGrid2D();
        gridVersion++;

        dirty = false;
        Rebuilt?.Invoke();
    }

    /// <summary>
    /// Query a path on the current grid. Returns an empty list if no grid or no path.
    /// When pathingMode is Fly, Y is interpolated between start and goal along the path.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld, bool returnBestEffortPathWhenNoPath = false)
    {
        EnsureGridBuiltForQuery();

        float sampleY = pathingMode == PathingMode.Fly ? Mathf.Lerp(startWorld.y, goalWorld.y, 0.5f) : startWorld.y;
        List<Vector3> path = HierarchicalPathingAStar2D.FindPath(
            grid2D,
            startWorld,
            goalWorld,
            sampleY,
            new HierarchicalPathingAStar2D.Settings
            {
                allowDiagonals = allowDiagonals,
                maxExpandedNodes = maxExpandedNodes,
                returnBestEffortPathWhenNoPath = returnBestEffortPathWhenNoPath
            });

        if (path != null && path.Count > 0 && pathingMode == PathingMode.Fly)
            ApplyFlyingYInterpolation(path, startWorld.y, goalWorld.y);

        return path ?? new List<Vector3>();
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

        if (grid2D == null)
            return true;

        if (!grid2D.TryWorldToCell(worldPos, out int x, out int z))
            return true;

        return grid2D.IsBlocked(x, z);
    }

    private void EnsureGridBuiltForQuery()
    {
        // If we don't have a grid or it's marked dirty, rebuild immediately for query correctness.
        // (Debounce is for Update-driven rebuilds; query callers generally want a correct answer now.)
        if (grid2D == null || dirty)
        {
            if (autoFindMarkers)
            {
                RefreshMarkers();
            }

            RebuildOccupancyGrid2D();
            gridVersion++;
            dirty = false;
        }
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

        if (grid2D == null)
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

