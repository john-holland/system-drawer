using System.Collections.Generic;
using UnityEngine;
using System;

// Solver for 3D spatial generation using OctTree
// Facilitates interaction between SGBehaviorTreeNode's and SGOctTree
public class SGOctTreeSolver : MonoBehaviour, SGTreeSolverInterface
{
    private SGOctTree octTree;
    private Bounds treeBounds;
    private System.Random rng;
    
    // Store behavior tree choices for update comparison
    private Dictionary<GameObject, object> objectProperties = new Dictionary<GameObject, object>();
    
    // Prune buffer for objects being relocated during updates
    private List<GameObject> pruneBuffer = new List<GameObject>();
    private Dictionary<GameObject, object> pruneBufferProperties = new Dictionary<GameObject, object>();
    
    void Awake()
    {
        // Initialize with bounds from transform
        Bounds bounds = new Bounds(transform.position, transform.localScale);
        octTree = new SGOctTree(bounds);
        treeBounds = bounds;
    }
    
    public void Initialize(Bounds bounds, int seed)
    {
        treeBounds = bounds;
        octTree = new SGOctTree(bounds);
        rng = new System.Random(seed);
        objectProperties.Clear();
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
    
    // Find available space for placement, considering empty space markers
    public Bounds? FindAvailableSpace(Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace, List<SGBehaviorTreeEmptySpace> emptySpaces)
    {
        // If we have empty space markers, search within them first
        if (emptySpaces != null && emptySpaces.Count > 0)
        {
            foreach (var emptySpace in emptySpaces)
            {
                Bounds spaceBounds = emptySpace.GetBounds();
                Bounds? availableSpace = FindSpaceInBounds(spaceBounds, minSpace, maxSpace, optimalSpace);
                if (availableSpace.HasValue)
                {
                    return availableSpace.Value;
                }
            }
        }
        
        // Otherwise search in tree bounds
        return FindSpaceInBounds(treeBounds, minSpace, maxSpace, optimalSpace);
    }
    
    private Bounds? FindSpaceInBounds(Bounds searchBounds, Vector3 minSpace, Vector3 maxSpace, Vector3 optimalSpace)
    {
        // Simple implementation: try to find a space that fits
        // More sophisticated algorithms can be added later
        
        // Try optimal space first
        if (searchBounds.size.x >= optimalSpace.x && searchBounds.size.y >= optimalSpace.y && searchBounds.size.z >= optimalSpace.z)
        {
            // Check if this space is clear
            Bounds testBounds = new Bounds(searchBounds.center, optimalSpace);
            List<GameObject> overlapping = Search(testBounds);
            if (overlapping.Count == 0)
            {
                return testBounds;
            }
        }
        
        // Try min space
        if (searchBounds.size.x >= minSpace.x && searchBounds.size.y >= minSpace.y && searchBounds.size.z >= minSpace.z)
        {
            Bounds testBounds = new Bounds(searchBounds.center, minSpace);
            List<GameObject> overlapping = Search(testBounds);
            if (overlapping.Count == 0)
            {
                return testBounds;
            }
        }
        
        // TODO: More sophisticated space finding algorithm
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
        if (!showTreeStructure || octTree == null)
        {
            return;
        }
        
        DrawTreeRecursive(octTree.GetRoot(), 0, 8);
    }
    
    private void DrawTreeRecursive(SGOctTree.OctTreeNode node, int depth, int maxDepth)
    {
        if (node == null || depth > maxDepth)
        {
            return;
        }
        
        // Color based on depth
        float normalizedDepth = (float)depth / maxDepth;
        Gizmos.color = Color.Lerp(Color.green, Color.red, normalizedDepth);
        
        // Draw wireframe for node bounds
        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
        
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
