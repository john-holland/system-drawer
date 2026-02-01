using System.Collections.Generic;
using UnityEngine;

// OctTree data structure for 3D spatial partitioning
// Each leaf has 8 sections (octants)
public class SGOctTree
{
    public enum Octant
    {
        FrontBottomLeft,   // z < 0.5 && y < 0.5 && x < 0.5
        FrontLowerRight,   // z < 0.5 && y < 0.5 && x > 0.5
        FrontUpperRight,   // z < 0.5 && y > 0.5 && x > 0.5
        FrontTopLeft,      // z < 0.5 && y > 0.5 && x < 0.5
        BackBottomLeft,    // z > 0.5 && y < 0.5 && x < 0.5
        BackLowerRight,    // z > 0.5 && y < 0.5 && x > 0.5
        BackUpperRight,    // z > 0.5 && y > 0.5 && x > 0.5
        BackTopLeft        // z > 0.5 && y > 0.5 && x < 0.5
    }
    
    public class OctTreeNode
    {
        public Bounds bounds;
        public List<GameObject> objects = new List<GameObject>();
        /// <summary>Bounds stored at Insert (same coordinate space as tree). Used for Search so overlap checks match Insert space.</summary>
        public List<Bounds> objectBounds = new List<Bounds>();
        public List<object> objectBehaviorTreeProperties = new List<object>();
        public OctTreeNode[] children;
        public bool isLeaf = true;
        
        public OctTreeNode(Bounds bounds)
        {
            this.bounds = bounds;
        }
    }
    
    private OctTreeNode root;
    private int maxObjectsPerNode;
    private int maxDepth;
    private float minCellSize;
    
    public SGOctTree(Bounds bounds, int maxObjectsPerNode = 8, int maxDepth = 8, float minCellSize = 0f)
    {
        root = new OctTreeNode(bounds);
        this.maxObjectsPerNode = Mathf.Max(1, maxObjectsPerNode);
        this.maxDepth = Mathf.Max(0, maxDepth);
        this.minCellSize = Mathf.Max(0f, minCellSize);
    }
    
    public void Insert(Bounds objectBounds, GameObject obj, object behaviorTreeProperties)
    {
        InsertRecursive(root, objectBounds, obj, behaviorTreeProperties, 0);
    }
    
    private void InsertRecursive(OctTreeNode node, Bounds objectBounds, GameObject obj, object behaviorTreeProperties, int depth)
    {
        if (!node.bounds.Intersects(objectBounds))
        {
            return;
        }
        
        if (node.isLeaf)
        {
            node.objects.Add(obj);
            node.objectBounds.Add(objectBounds);
            node.objectBehaviorTreeProperties.Add(behaviorTreeProperties);
            
            // Subdivide only if over bucket size, under max depth, and child size respects minCellSize
            if (node.objects.Count > maxObjectsPerNode && depth < maxDepth && ShouldSubdivide(node))
            {
                Subdivide(node, depth);
            }
        }
        else
        {
            // Insert into children
            for (int i = 0; i < node.children.Length; i++)
            {
                InsertRecursive(node.children[i], objectBounds, obj, behaviorTreeProperties, depth + 1);
            }
        }
    }
    
    /// <summary>True if child half-size meets minCellSize (when set) so we respect partition/bucket size.</summary>
    private bool ShouldSubdivide(OctTreeNode node)
    {
        if (minCellSize <= 0f)
            return true;
        Vector3 halfSize = node.bounds.size * 0.5f;
        return halfSize.x >= minCellSize && halfSize.y >= minCellSize && halfSize.z >= minCellSize;
    }
    
    private void Subdivide(OctTreeNode node, int depth)
    {
        if (node.isLeaf && node.objects.Count > maxObjectsPerNode && depth < maxDepth && ShouldSubdivide(node))
        {
            node.isLeaf = false;
            node.children = new OctTreeNode[8];
            
            Vector3 center = node.bounds.center;
            Vector3 size = node.bounds.size * 0.5f;
            Vector3 quarterSize = size * 0.5f;
            
            // Create 8 children (octants)
            node.children[(int)Octant.FrontBottomLeft] = new OctTreeNode(new Bounds(
                new Vector3(center.x - quarterSize.x, center.y - quarterSize.y, center.z - quarterSize.z),
                size));
            
            node.children[(int)Octant.FrontLowerRight] = new OctTreeNode(new Bounds(
                new Vector3(center.x + quarterSize.x, center.y - quarterSize.y, center.z - quarterSize.z),
                size));
            
            node.children[(int)Octant.FrontUpperRight] = new OctTreeNode(new Bounds(
                new Vector3(center.x + quarterSize.x, center.y + quarterSize.y, center.z - quarterSize.z),
                size));
            
            node.children[(int)Octant.FrontTopLeft] = new OctTreeNode(new Bounds(
                new Vector3(center.x - quarterSize.x, center.y + quarterSize.y, center.z - quarterSize.z),
                size));
            
            node.children[(int)Octant.BackBottomLeft] = new OctTreeNode(new Bounds(
                new Vector3(center.x - quarterSize.x, center.y - quarterSize.y, center.z + quarterSize.z),
                size));
            
            node.children[(int)Octant.BackLowerRight] = new OctTreeNode(new Bounds(
                new Vector3(center.x + quarterSize.x, center.y - quarterSize.y, center.z + quarterSize.z),
                size));
            
            node.children[(int)Octant.BackUpperRight] = new OctTreeNode(new Bounds(
                new Vector3(center.x + quarterSize.x, center.y + quarterSize.y, center.z + quarterSize.z),
                size));
            
            node.children[(int)Octant.BackTopLeft] = new OctTreeNode(new Bounds(
                new Vector3(center.x - quarterSize.x, center.y + quarterSize.y, center.z + quarterSize.z),
                size));
            
            // Redistribute objects using stored bounds (same coordinate space as Insert/Search)
            List<GameObject> objectsToRedistribute = new List<GameObject>(node.objects);
            List<Bounds> boundsToRedistribute = new List<Bounds>(node.objectBounds);
            List<object> propertiesToRedistribute = new List<object>(node.objectBehaviorTreeProperties);
            
            node.objects.Clear();
            node.objectBounds.Clear();
            node.objectBehaviorTreeProperties.Clear();
            
            for (int i = 0; i < objectsToRedistribute.Count; i++)
            {
                InsertRecursive(node, boundsToRedistribute[i], objectsToRedistribute[i], propertiesToRedistribute[i], depth);
            }
        }
    }
    
    public List<GameObject> Search(Bounds searchBounds)
    {
        List<GameObject> results = new List<GameObject>();
        SearchRecursive(root, searchBounds, results);
        return results;
    }
    
    private void SearchRecursive(OctTreeNode node, Bounds searchBounds, List<GameObject> results)
    {
        if (!node.bounds.Intersects(searchBounds))
        {
            return;
        }
        
        if (node.isLeaf)
        {
            for (int i = 0; i < node.objects.Count; i++)
            {
                if (node.objectBounds[i].Intersects(searchBounds))
                {
                    results.Add(node.objects[i]);
                }
            }
        }
        else
        {
            for (int i = 0; i < node.children.Length; i++)
            {
                SearchRecursive(node.children[i], searchBounds, results);
            }
        }
    }
    
    public bool Intersects(Bounds bounds)
    {
        return root.bounds.Intersects(bounds);
    }
    
    public void Clear()
    {
        root = new OctTreeNode(root.bounds);
    }
    
    public List<GameObject> GetAllObjects()
    {
        List<GameObject> allObjects = new List<GameObject>();
        GetAllObjectsRecursive(root, allObjects);
        return allObjects;
    }
    
    private void GetAllObjectsRecursive(OctTreeNode node, List<GameObject> results)
    {
        if (node.isLeaf)
        {
            results.AddRange(node.objects);
        }
        else
        {
            for (int i = 0; i < node.children.Length; i++)
            {
                GetAllObjectsRecursive(node.children[i], results);
            }
        }
    }
    
    public OctTreeNode GetRoot()
    {
        return root;
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
}// we could use a farey nested interval set?
