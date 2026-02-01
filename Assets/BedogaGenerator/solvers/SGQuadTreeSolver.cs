using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Solver for 2D spatial generation using QuadTree
// Facilitates interaction between SGBehaviorTreeNode's and SGQuadTree
public class SGQuadTreeSolver : MonoBehaviour, SGTreeSolverInterface
{
    private SGQuadTree quadTree;
    private Bounds treeBounds;
    private System.Random rng;
    
    public SpatialGenerator spatialGenerator;

    // Store behavior tree choices for update comparison
    private Dictionary<GameObject, object> objectProperties = new Dictionary<GameObject, object>();
    
    // Prune buffer for objects being relocated during updates
    private List<GameObject> pruneBuffer = new List<GameObject>();
    private Dictionary<GameObject, object> pruneBufferProperties = new Dictionary<GameObject, object>();
    
    [Header("Partitioning (QuadTree bucket / cell size)")]
    [Tooltip("Max objects per leaf before subdividing. Respected during partition.")]
    public int maxObjectsPerNode = 4;
    [Tooltip("Max subdivision depth.")]
    public int maxDepth = 8;
    [Tooltip("Minimum cell size (x/y). Don't subdivide if child half-size would be smaller. 0 = no limit.")]
    public float minCellSize = 0f;
    
    void Awake()
    {
        // Fallback; proper initialization happens via Initialize() from SpatialGenerator
        Bounds bounds = new Bounds(Vector3.zero, transform.localScale);
        quadTree = new SGQuadTree(bounds, maxObjectsPerNode, maxDepth, minCellSize);
        treeBounds = bounds;
    }
    
    public void Initialize(Bounds bounds, int seed)
    {
        treeBounds = bounds;
        quadTree = new SGQuadTree(bounds, maxObjectsPerNode, maxDepth, minCellSize);
        rng = new System.Random(seed);
        objectProperties.Clear();
        
        // Cache SpatialGenerator reference for visualization
        if (spatialGenerator == null)
        {
            //todo: review: we may want to search up the object hierarchy to find the spatial generator
            //  that is most closely related to this one
            spatialGenerator = FindAnyObjectByType<SpatialGenerator>();
        }
    }
    
    public bool Insert(Bounds bounds, object behaviorTreeProperties, GameObject gameObject)
    {
        if (quadTree == null)
        {
            Initialize(bounds, 0);
        }
        
        quadTree.Insert(bounds, gameObject, behaviorTreeProperties);
        objectProperties[gameObject] = behaviorTreeProperties;
        return true;
    }
    
    public List<GameObject> Search(Bounds searchBounds)
    {
        if (quadTree == null)
        {
            return new List<GameObject>();
        }
        
        return quadTree.Search(searchBounds);
    }
    
    public bool Intersects(Bounds bounds)
    {
        if (quadTree == null)
        {
            return false;
        }
        
        return quadTree.Intersects(bounds);
    }
    
    public void Clear()
    {
        if (quadTree != null)
        {
            quadTree.Clear();
        }
        objectProperties.Clear();
    }
    
    public List<GameObject> GetAllObjects()
    {
        if (quadTree == null)
        {
            return new List<GameObject>();
        }
        
        return quadTree.GetAllObjects();
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
    
    public SGQuadTree GetQuadTree()
    {
        return quadTree;
    }
    
    public Bounds GetBounds()
    {
        return treeBounds;
    }
    
    // Find available space for placement. placementIndex: 0 = try center then grid; >0 = use the placementIndex-th slot so multiple placements get different positions (avoids relying on Search overlap when coords differ).
    // When placementConfig is set, uses fit/stack/wrap from config (X,Y only for 2D); otherwise legacy row-major from min.
    public Bounds? FindAvailableSpace(Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, List<SGBehaviorTreeEmptySpace> emptySpaces, int placementIndex = 0, PlacementSlotConfig? placementConfig = null)
    {
        // If we have empty space markers, search within them first
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
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int gridCount = numX * numY;
        int totalSlots = 1 + gridCount;
        int count = Mathf.Min(maxSlots, totalSlots);
        Vector3 size = new Vector3(optimalSpace.x, optimalSpace.y, optimalSpace.z);
        for (int slot = 0; slot < count; slot++)
        {
            if (PlacementSlotConfig.ComputeSlotCenter2D(searchBounds, optimalSpace, minSpace, slot, placementConfig, out Vector3 slotCenter))
                list.Add(new Bounds(slotCenter, size));
            else
                list.Add(new Bounds(new Vector3(searchBounds.center.x, searchBounds.center.y, searchBounds.center.z), size));
        }
        return list;
    }
    
    private Bounds? FindSpaceInBounds(Bounds searchBounds, Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, int placementIndex = 0, PlacementSlotConfig? placementConfig = null)
    {
        float stepX = Mathf.Max(optimalSpace.x, minSpace.x * 0.5f);
        float stepY = Mathf.Max(optimalSpace.y, minSpace.y * 0.5f);
        if (stepX <= 0f) stepX = 1f;
        if (stepY <= 0f) stepY = 1f;
        float halfXOpt = optimalSpace.x * 0.5f;
        float halfYOpt = optimalSpace.y * 0.5f;
        float halfXMin = minSpace.x * 0.5f;
        float halfYMin = minSpace.y * 0.5f;
        int numX = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.x - optimalSpace.x) / stepX) + 1);
        int numY = Mathf.Max(0, Mathf.FloorToInt((searchBounds.size.y - optimalSpace.y) / stepY) + 1);
        int gridCount = numX * numY;
        int totalSlots = 1 + gridCount;
        int slot = placementIndex % Mathf.Max(1, totalSlots);

        if (!PlacementSlotConfig.ComputeSlotCenter2D(searchBounds, optimalSpace, minSpace, slot, placementConfig, out Vector3 slotCenter))
            slotCenter = searchBounds.center;
        
        if (searchBounds.size.x >= optimalSpace.x && searchBounds.size.y >= optimalSpace.y)
        {
            Bounds testBounds = new Bounds(slotCenter, optimalSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        if (searchBounds.size.x >= minSpace.x && searchBounds.size.y >= minSpace.y)
        {
            Bounds testBounds = new Bounds(slotCenter, minSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        
        // Fallback: try center then full grid
        if (searchBounds.size.x >= optimalSpace.x && searchBounds.size.y >= optimalSpace.y)
        {
            Bounds testBounds = new Bounds(searchBounds.center, optimalSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        if (searchBounds.size.x >= minSpace.x && searchBounds.size.y >= minSpace.y)
        {
            Bounds testBounds = new Bounds(searchBounds.center, minSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        for (float x = searchBounds.min.x + halfXOpt; x <= searchBounds.max.x - halfXOpt; x += stepX)
        for (float y = searchBounds.min.y + halfYOpt; y <= searchBounds.max.y - halfYOpt; y += stepY)
        {
            Bounds testBounds = new Bounds(new Vector3(x, y, searchBounds.center.z), optimalSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        for (float x = searchBounds.min.x + halfXMin; x <= searchBounds.max.x - halfXMin; x += stepX)
        for (float y = searchBounds.min.y + halfYMin; y <= searchBounds.max.y - halfYMin; y += stepY)
        {
            Bounds testBounds = new Bounds(new Vector3(x, y, searchBounds.center.z), minSpace);
            if (Search(testBounds).Count == 0) return testBounds;
        }
        return null;
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
        
        if (quadTree == null)
        {
            return;
        }
        
        DrawTreeRecursive(quadTree.GetRoot(), 0, 8);
    }
    
    private Color GetNodeColor(SGQuadTree.QuadTreeNode node, int maxDepth, int depth)
    {
        const int maxObjectsPerNode = 4;
        
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
    
    private void DrawTreeRecursive(SGQuadTree.QuadTreeNode node, int depth, int maxDepth)
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
            float sphereSize = Mathf.Min(worldSize.x, worldSize.y) * 0.1f;
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
