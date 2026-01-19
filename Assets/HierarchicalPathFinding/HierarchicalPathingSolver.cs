using System;
using System.Collections.Generic;
using UnityEngine;

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
        offLimitsSpaces.AddRange(FindObjectsOfType<OffLimitsSpace>());

        noPathingMarkers.Clear();
        noPathingMarkers.AddRange(FindObjectsOfType<NoPathing>());
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
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld, bool returnBestEffortPathWhenNoPath = false)
    {
        EnsureGridBuiltForQuery();

        float sampleY = startWorld.y;
        return HierarchicalPathingAStar2D.FindPath(
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
        grid2D = new HierarchicalPathingGrid2D(worldBounds, cellSize);

        float halfH = Mathf.Max(0.01f, agentHeight * 0.5f);
        float capsuleBottomOffset = Mathf.Max(agentRadius, 0.01f);

        // Sample at mid-height within the worldBounds
        float sampleY = worldBounds.center.y;

        // Mark blocked cells based on:
        // - physics obstacles (capsule overlap)
        // - OffLimitsSpace / NoPathing bounds
        for (int z = 0; z < grid2D.height; z++)
        {
            for (int x = 0; x < grid2D.width; x++)
            {
                Vector3 center = grid2D.CellCenterWorld(x, z, sampleY);

                // Capsule endpoints
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

                grid2D.SetBlocked(x, z, blocked);
            }
        }
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

