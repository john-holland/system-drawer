using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Solver for 3D spatial generation using OctTree
// Facilitates interaction between SGBehaviorTreeNode's and SGOctTree
public class SGOctTreeSolver : MonoBehaviour, SGTreeSolverInterface
{
    private SGOctTree octTree;
    private Bounds treeBounds;
    private System.Random rng;
    private SpatialGenerator spatialGenerator;
    
    // Store behavior tree choices for update comparison
    private Dictionary<GameObject, object> objectProperties = new Dictionary<GameObject, object>();
    
    // Prune buffer for objects being relocated during updates
    private List<GameObject> pruneBuffer = new List<GameObject>();
    private Dictionary<GameObject, object> pruneBufferProperties = new Dictionary<GameObject, object>();
    
    [Header("Partitioning (OctTree bucket / cell size)")]
    [Tooltip("Max objects per leaf before subdividing. Respected during partition.")]
    public int maxObjectsPerNode = 8;
    [Tooltip("Max subdivision depth.")]
    public int maxDepth = 8;
    [Tooltip("Minimum cell size (per axis). Don't subdivide if child half-size would be smaller. 0 = no limit.")]
    public float minCellSize = 0f;
    
    [Header("Height Maps / Terrain")]
    [Tooltip("Terrains to check; only buckets above the height map are valid. Placements whose floor is at or below the terrain surface are rejected.")]
    public List<Terrain> heightMapTerrains = new List<Terrain>();
    [Tooltip("When true, only generate in buckets above the height map. Disable for maps with portals so placements can be above and below terrain.")]
    public bool onlyGenerateAboveTerrain = true;
    [Tooltip("Use raycasts (from top of bounds downward) for terrain conflict check; otherwise use Terrain.SampleHeight at sample points.")]
    public bool useRaycastForTerrain = true;
    [Tooltip("Number of sample points per axis (XZ) for terrain check. More = more accurate but slower. 3–9 recommended.")]
    [Range(3, 9)]
    public int terrainCheckSampleCount = 5;
    
    void Awake()
    {
        // Fallback; proper initialization happens via Initialize() from SpatialGenerator
        Bounds bounds = new Bounds(Vector3.zero, transform.localScale);
        octTree = new SGOctTree(bounds, maxObjectsPerNode, maxDepth, minCellSize);
        treeBounds = bounds;
    }
    
    public void Initialize(Bounds bounds, int seed)
    {
        treeBounds = bounds;
        octTree = new SGOctTree(bounds, maxObjectsPerNode, maxDepth, minCellSize);
        rng = new System.Random(seed);
        objectProperties.Clear();
        
        // Cache SpatialGenerator reference for visualization
        if (spatialGenerator == null)
        {
            spatialGenerator = FindAnyObjectByType<SpatialGenerator>();
        }
    }
    
    public bool Insert(Bounds bounds, object behaviorTreeProperties, GameObject gameObject)
    {
        if (octTree == null)
        {
            Initialize(bounds, 0);
        }
        
        octTree.Insert(bounds, gameObject, behaviorTreeProperties);
        objectProperties[gameObject] = behaviorTreeProperties;
        return true;
    }
    
    public List<GameObject> Search(Bounds searchBounds)
    {
        if (octTree == null)
        {
            return new List<GameObject>();
        }
        
        return octTree.Search(searchBounds);
    }
    
    public bool Intersects(Bounds bounds)
    {
        if (octTree == null)
        {
            return false;
        }
        
        return octTree.Intersects(bounds);
    }
    
    public void Clear()
    {
        if (octTree != null)
        {
            octTree.Clear();
        }
        objectProperties.Clear();
    }
    
    public List<GameObject> GetAllObjects()
    {
        if (octTree == null)
        {
            return new List<GameObject>();
        }
        
        return octTree.GetAllObjects();
    }
    
    public void UpdateTree(Bounds newBounds)
    {
        treeBounds = newBounds;
        
        // Collect existing objects and properties
        List<GameObject> existingObjects = GetAllObjects();
        Dictionary<GameObject, object> existingProperties = new Dictionary<GameObject, object>(objectProperties);
        
        // Move objects that don't fit into prune buffer
        pruneBuffer.Clear();
        pruneBufferProperties.Clear();
        
        foreach (var kvp in existingProperties)
        {
            Bounds objBounds = GetGameObjectBounds(kvp.Key);
            if (!BoundsContains(newBounds, objBounds))
            {
                pruneBuffer.Add(kvp.Key);
                pruneBufferProperties[kvp.Key] = kvp.Value;
            }
        }
        
        // Clear and reinitialize tree
        Clear();
        Initialize(newBounds, rng != null ? rng.GetHashCode() : 0);
        
        // Re-insert objects that still fit
        foreach (var kvp in existingProperties)
        {
            if (!pruneBuffer.Contains(kvp.Key))
            {
                Bounds objBounds = GetGameObjectBounds(kvp.Key);
                if (BoundsContains(newBounds, objBounds))
                {
                    Insert(objBounds, kvp.Value, kvp.Key);
                }
            }
        }
        
        // Try to re-insert pruned objects if space becomes available
        ReinsertPrunedObjects();
    }
    
    private void ReinsertPrunedObjects()
    {
        // Try to reinsert objects from prune buffer
        List<GameObject> toRemove = new List<GameObject>();
        foreach (GameObject obj in pruneBuffer)
        {
            Bounds objBounds = GetGameObjectBounds(obj);
            if (BoundsContains(treeBounds, objBounds))
            {
                List<GameObject> overlapping = Search(objBounds);
                if (overlapping.Count == 0)
                {
                    // Space is available, reinsert
                    Insert(objBounds, pruneBufferProperties[obj], obj);
                    toRemove.Add(obj);
                }
            }
        }
        
        // Remove reinserted objects from prune buffer
        foreach (GameObject obj in toRemove)
        {
            pruneBuffer.Remove(obj);
            pruneBufferProperties.Remove(obj);
        }
    }
    
    public List<GameObject> GetPruneBuffer()
    {
        return new List<GameObject>(pruneBuffer);
    }
    
    public bool CompareTree(SGTreeSolverInterface otherTree)
    {
        // Compare tree structures
        // For now, simple comparison - can be enhanced in Phase 7
        List<GameObject> thisObjects = GetAllObjects();
        List<GameObject> otherObjects = otherTree.GetAllObjects();
        
        if (thisObjects.Count != otherObjects.Count)
        {
            return false;
        }
        
        // TODO: More sophisticated comparison
        return true;
    }
    
    public System.Random GetRNG()
    {
        return rng;
    }
    
    public SGOctTree GetOctTree()
    {
        return octTree;
    }
    
    public Bounds GetBounds()
    {
        return treeBounds;
    }
    
    // Find available space for placement. placementIndex: 0 = center then grid; >0 = use the placementIndex-th slot so multiple placements get different positions.
    // When empty spaces exist, placementIndex selects which empty space (placementIndex % count) and which slot within it (placementIndex / count) so rooms spread across markers.
    // When placementConfig is set, uses fit/stack/wrap from config; otherwise legacy row-major from min.
    public Bounds? FindAvailableSpace(Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, List<SGBehaviorTreeEmptySpace> emptySpaces, int placementIndex = 0, PlacementSlotConfig? placementConfig = null)
    {
        if (emptySpaces != null && emptySpaces.Count > 0)
        {
            int spaceIndex = placementIndex % emptySpaces.Count;
            int slotInSpace = placementIndex / emptySpaces.Count;
            for (int k = 0; k < emptySpaces.Count; k++)
            {
                int idx = (spaceIndex + k) % emptySpaces.Count;
                var emptySpace = emptySpaces[idx];
                if (emptySpace == null) continue;
                Bounds worldSpaceBounds = emptySpace.GetBounds();
                Vector3 localCenter = transform.InverseTransformPoint(worldSpaceBounds.center);
                Vector3 localSize = new Vector3(
                    worldSpaceBounds.size.x / transform.lossyScale.x,
                    worldSpaceBounds.size.y / transform.lossyScale.y,
                    worldSpaceBounds.size.z / transform.lossyScale.z
                );
                Bounds localSpaceBounds = new Bounds(localCenter, localSize);
                Bounds? availableSpace = FindSpaceInBounds(localSpaceBounds, minSpace, maxSpace, optimalSpace, k == 0 ? slotInSpace : 0, placementConfig);
                if (availableSpace.HasValue)
                    return availableSpace.Value;
            }
        }
        return FindSpaceInBounds(treeBounds, minSpace, maxSpace, optimalSpace, placementIndex, placementConfig);
    }
    
    /// <summary>Returns placement slot bounds (center + size) using the same step/grid logic as the solver, for gizmo visualization. Bounds are in local space. Does not check occupancy.</summary>
    public List<Bounds> GetPlacementSlotsForGizmo(Bounds searchBounds, Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, int maxSlots = 64, PlacementSlotConfig? placementConfig = null)
    {
        var list = new List<Bounds>();
        float stepX = Mathf.Max(optimalSpace.x, minSpace.x * 0.5f);
        float stepY = Mathf.Max(optimalSpace.y, minSpace.y * 0.5f);
        float stepZ = Mathf.Max(optimalSpace.z, minSpace.z * 0.5f);
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        if (stepZ <= 0f) stepZ = 1f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int numZ = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.z - optimalSpace.z) / stepZ) + 1);
        int gridCount = numX * numY * numZ;
        int totalSlots = 1 + gridCount;
        int count = Mathf.Min(maxSlots, totalSlots);
        for (int slot = 0; slot < count; slot++)
        {
            if (PlacementSlotConfig.ComputeSlotCenter3D(searchBounds, optimalSpace, minSpace, slot, placementConfig, out Vector3 slotCenter))
                list.Add(new Bounds(slotCenter, optimalSpace));
            else
                list.Add(new Bounds(searchBounds.center, optimalSpace));
        }
        return list;
    }
    
    private Bounds? FindSpaceInBounds(Bounds searchBounds, Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, int placementIndex = 0, PlacementSlotConfig? placementConfig = null)
    {
        float stepX = Mathf.Max(optimalSpace.x, minSpace.x * 0.5f);
        float stepY = Mathf.Max(optimalSpace.y, minSpace.y * 0.5f);
        float stepZ = Mathf.Max(optimalSpace.z, minSpace.z * 0.5f);
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        if (stepZ <= 0f) stepZ = 1f;
        Vector3 halfOpt = optimalSpace * 0.5f;
        Vector3 halfMin = minSpace * 0.5f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int numZ = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.z - optimalSpace.z) / stepZ) + 1);
        int gridCount = numX * numY * numZ;
        int totalSlots = 1 + gridCount;
        int slot = placementIndex % Mathf.Max(1, totalSlots);

        if (!PlacementSlotConfig.ComputeSlotCenter3D(searchBounds, optimalSpace, minSpace, slot, placementConfig, out Vector3 slotCenter))
            slotCenter = searchBounds.center;
        
        if (searchBounds.size.x >= optimalSpace.x && searchBounds.size.y >= optimalSpace.y && searchBounds.size.z >= optimalSpace.z)
        {
            Bounds testBounds = new Bounds(slotCenter, optimalSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        if (searchBounds.size.x >= minSpace.x && searchBounds.size.y >= minSpace.y && searchBounds.size.z >= minSpace.z)
        {
            Bounds testBounds = new Bounds(slotCenter, minSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        
        if (searchBounds.size.x >= optimalSpace.x && searchBounds.size.y >= optimalSpace.y && searchBounds.size.z >= optimalSpace.z)
        {
            Bounds testBounds = new Bounds(searchBounds.center, optimalSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        if (searchBounds.size.x >= minSpace.x && searchBounds.size.y >= minSpace.y && searchBounds.size.z >= minSpace.z)
        {
            Bounds testBounds = new Bounds(searchBounds.center, minSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        // Vary X in inner loop so multiple root placements spread along X first (consistent with linear-X fallback).
        for (float z = searchBounds.min.z + halfOpt.z; z <= searchBounds.max.z - halfOpt.z; z += stepZ)
        for (float y = searchBounds.min.y + halfOpt.y; y <= searchBounds.max.y - halfOpt.y; y += stepY)
        for (float x = searchBounds.min.x + halfOpt.x; x <= searchBounds.max.x - halfOpt.x; x += stepX)
        {
            Bounds testBounds = new Bounds(new Vector3(x, y, z), optimalSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        for (float z = searchBounds.min.z + halfMin.z; z <= searchBounds.max.z - halfMin.z; z += stepZ)
        for (float y = searchBounds.min.y + halfMin.y; y <= searchBounds.max.y - halfMin.y; y += stepY)
        for (float x = searchBounds.min.x + halfMin.x; x <= searchBounds.max.x - halfMin.x; x += stepX)
        {
            Bounds testBounds = new Bounds(new Vector3(x, y, z), minSpace);
            if (Search(testBounds).Count == 0 && !BoundsConflictsWithHeightMaps(testBounds)) return testBounds;
        }
        return null;
    }
    
    /// <summary>True if the given local-space bounds conflict with any registered height map (terrain). We only generate in buckets above the height map: conflict if terrain surface is at or above the placement floor (worldMinY). Disabled when onlyGenerateAboveTerrain is false (e.g. maps with portals).</summary>
    private bool BoundsConflictsWithHeightMaps(Bounds localBounds)
    {
        if (!onlyGenerateAboveTerrain)
            return false;
        if (heightMapTerrains == null || heightMapTerrains.Count == 0)
            return false;
        
        Bounds worldBounds = LocalBoundsToWorld(localBounds);
        int n = Mathf.Clamp(terrainCheckSampleCount, 3, 9);
        
        if (useRaycastForTerrain)
            return BoundsConflictsWithHeightMapsRaycast(worldBounds, n);
        return BoundsConflictsWithHeightMapsSample(worldBounds, n);
    }
    
    private bool BoundsConflictsWithHeightMapsRaycast(Bounds worldBounds, int sampleCount)
    {
        float worldMinX = worldBounds.min.x;
        float worldMaxX = worldBounds.max.x;
        float worldMinZ = worldBounds.min.z;
        float worldMaxZ = worldBounds.max.z;
        float worldMinY = worldBounds.min.y;
        float worldMaxY = worldBounds.max.y;
        const float raycastMargin = 1f;
        Vector3 rayOrigin = new Vector3(0f, worldMaxY + raycastMargin, 0f);
        Vector3 rayDir = Vector3.down;
        float rayLength = (worldMaxY - worldMinY) + 2f * raycastMargin;
        
        HashSet<Collider> terrainColliders = new HashSet<Collider>();
        for (int t = 0; t < heightMapTerrains.Count; t++)
        {
            if (heightMapTerrains[t] == null) continue;
            Collider col = heightMapTerrains[t].GetComponent<Collider>();
            if (col != null)
                terrainColliders.Add(col);
        }
        if (terrainColliders.Count == 0)
            return BoundsConflictsWithHeightMapsSample(worldBounds, sampleCount);
        
        for (int ix = 0; ix < sampleCount; ix++)
        {
            float x = sampleCount <= 1 ? worldBounds.center.x : Mathf.Lerp(worldMinX, worldMaxX, ix / (float)(sampleCount - 1));
            for (int iz = 0; iz < sampleCount; iz++)
            {
                float z = sampleCount <= 1 ? worldBounds.center.z : Mathf.Lerp(worldMinZ, worldMaxZ, iz / (float)(sampleCount - 1));
                rayOrigin.x = x;
                rayOrigin.z = z;
                rayOrigin.y = worldMaxY + raycastMargin;
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, rayLength);
                for (int h = 0; h < hits.Length; h++)
                {
                    if (!terrainColliders.Contains(hits[h].collider)) continue;
                    float hitY = hits[h].point.y;
                    if (hitY >= worldMinY)
                        return true;
                }
            }
        }
        return false;
    }
    
    private bool BoundsConflictsWithHeightMapsSample(Bounds worldBounds, int sampleCount)
    {
        float worldMinX = worldBounds.min.x;
        float worldMaxX = worldBounds.max.x;
        float worldMinZ = worldBounds.min.z;
        float worldMaxZ = worldBounds.max.z;
        float worldMinY = worldBounds.min.y;
        float worldMaxY = worldBounds.max.y;
        
        for (int t = 0; t < heightMapTerrains.Count; t++)
        {
            Terrain terrain = heightMapTerrains[t];
            if (terrain == null || terrain.terrainData == null)
                continue;
            
            Bounds terrainWorldBounds = terrain.terrainData.bounds;
            terrainWorldBounds.center += terrain.transform.position;
            if (!BoundsIntersectXZ(worldBounds, terrainWorldBounds))
                continue;
            
            for (int ix = 0; ix < sampleCount; ix++)
            {
                float x = sampleCount <= 1 ? worldBounds.center.x : Mathf.Lerp(worldMinX, worldMaxX, ix / (float)(sampleCount - 1));
                for (int iz = 0; iz < sampleCount; iz++)
                {
                    float z = sampleCount <= 1 ? worldBounds.center.z : Mathf.Lerp(worldMinZ, worldMaxZ, iz / (float)(sampleCount - 1));
                    float terrainY = terrain.SampleHeight(new Vector3(x, 0f, z));
                    if (terrainY >= worldMinY)
                        return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>Public API for tests: returns true if the given local bounds would conflict with any registered terrain.</summary>
    public bool WouldBoundsConflictWithTerrain(Bounds localBounds)
    {
        return BoundsConflictsWithHeightMaps(localBounds);
    }
    
    /// <summary>Convert solver local bounds to world bounds (for placement and terrain check).</summary>
    public Bounds LocalBoundsToWorld(Bounds localBounds)
    {
        return LocalBoundsToWorldInternal(localBounds);
    }
    
    /// <summary>Convert world bounds to solver local bounds. Used by tests to build local bounds from terrain world bounds.</summary>
    public Bounds WorldBoundsToLocal(Bounds worldBounds)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 worldSize = worldBounds.size;
        Vector3 localSize = new Vector3(
            worldSize.x / Mathf.Abs(transform.lossyScale.x),
            worldSize.y / Mathf.Abs(transform.lossyScale.y),
            worldSize.z / Mathf.Abs(transform.lossyScale.z));
        return new Bounds(localCenter, localSize);
    }
    
    private Bounds LocalBoundsToWorldInternal(Bounds localBounds)
    {
        Vector3 worldCenter = transform.TransformPoint(localBounds.center);
        Vector3 worldSize = new Vector3(
            transform.lossyScale.x * localBounds.size.x,
            transform.lossyScale.y * localBounds.size.y,
            transform.lossyScale.z * localBounds.size.z);
        worldSize.x = Mathf.Abs(worldSize.x);
        worldSize.y = Mathf.Abs(worldSize.y);
        worldSize.z = Mathf.Abs(worldSize.z);
        return new Bounds(worldCenter, worldSize);
    }
    
    private static bool BoundsIntersectXZ(Bounds a, Bounds b)
    {
        if (a.max.x < b.min.x || b.max.x < a.min.x) return false;
        if (a.max.z < b.min.z || b.max.z < a.min.z) return false;
        return true;
    }
    
    private Bounds GetGameObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }
        
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(corners[i]);
            }
            return bounds;
        }
        
        return new Bounds(obj.transform.position, Vector3.one);
    }
    
    // Helper method to check if one bounds fully contains another
    private bool BoundsContains(Bounds outer, Bounds inner)
    {
        return outer.Contains(inner.min) && outer.Contains(inner.max);
    }
    
    #if UNITY_EDITOR
    [Header("Editor Visualization")]
    public bool showTreeStructure = false;
    
    void OnDrawGizmos()
    {
        // Only draw when Unity is paused and visualization is enabled
        if (!EditorApplication.isPaused)
        {
            return;
        }
        
        // Check SpatialGenerator toggle
        if (spatialGenerator == null)
        {
            spatialGenerator = FindAnyObjectByType<SpatialGenerator>();
        }
        
        if (spatialGenerator == null || !spatialGenerator.showTreeVisualization)
        {
            return;
        }
        
        if (octTree == null)
        {
            return;
        }
        
        DrawTreeRecursive(octTree.GetRoot(), 0, 8);
    }
    
    private Color GetNodeColor(SGOctTree.OctTreeNode node, int maxDepth, int depth)
    {
        const int maxObjectsPerNode = 8;
        
        // Empty nodes: light gray with low alpha
        if (node.objects == null || node.objects.Count == 0)
        {
            Color gray = Color.gray;
            gray.a = 0.25f;
            return gray;
        }
        
        // Occupied nodes: color gradient based on occupancy
        float occupancyRatio = Mathf.Clamp01((float)node.objects.Count / maxObjectsPerNode);
        
        if (occupancyRatio < 0.5f)
        {
            // Low occupancy: green to yellow
            return Color.Lerp(Color.green, Color.yellow, occupancyRatio * 2f);
        }
        else
        {
            // High occupancy: yellow to red
            return Color.Lerp(Color.yellow, Color.red, (occupancyRatio - 0.5f) * 2f);
        }
    }
    
    private void DrawTreeRecursive(SGOctTree.OctTreeNode node, int depth, int maxDepth)
    {
        if (node == null || depth > maxDepth)
        {
            return;
        }
        
        // Convert node bounds from local space to world space for visualization
        // (solver is a component on SpatialGenerator, so transform is SpatialGenerator transform)
        Vector3 worldCenter = transform.TransformPoint(node.bounds.center);
        Vector3 worldSize = Vector3.Scale(node.bounds.size, transform.lossyScale);
        
        // Get color based on occupancy
        Color nodeColor = GetNodeColor(node, maxDepth, depth);
        Gizmos.color = nodeColor;
        
        // Draw wireframe for node bounds (in world space)
        Gizmos.DrawWireCube(worldCenter, worldSize);
        
        // Draw spheres for objects in this node
        if (node.objects != null && node.objects.Count > 0)
        {
            // Use a slightly different color for spheres (brighter)
            Color sphereColor = nodeColor;
            sphereColor.a = Mathf.Min(1f, sphereColor.a + 0.3f);
            Gizmos.color = sphereColor;
            
            // Calculate sphere size based on node size (smaller nodes = smaller spheres)
            // Use world size for sphere calculation
            float sphereSize = Mathf.Min(worldSize.x, Mathf.Min(worldSize.y, worldSize.z)) * 0.1f;
            sphereSize = Mathf.Max(0.05f, Mathf.Min(0.2f, sphereSize)); // Clamp between 0.05 and 0.2
            
            foreach (GameObject obj in node.objects)
            {
                if (obj != null)
                {
                    // Draw sphere at object position
                    Gizmos.DrawSphere(obj.transform.position, sphereSize);
                }
            }
        }
        
        // Draw child nodes
        if (!node.isLeaf && node.children != null)
        {
            for (int i = 0; i < node.children.Length; i++)
            {
                DrawTreeRecursive(node.children[i], depth + 1, maxDepth);
            }
        }
    }
    #endif
}
