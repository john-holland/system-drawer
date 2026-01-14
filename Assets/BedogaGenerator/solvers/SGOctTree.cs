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
        public List<object> objectBehaviorTreeProperties = new List<object>();
        public OctTreeNode[] children;
        public bool isLeaf = true;
        
        public OctTreeNode(Bounds bounds)
        {
            this.bounds = bounds;
        }
    }
    
    private OctTreeNode root;
    private int maxObjectsPerNode = 8;
    private int maxDepth = 8;
    
    public SGOctTree(Bounds bounds)
    {
        root = new OctTreeNode(bounds);
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
            node.objectBehaviorTreeProperties.Add(behaviorTreeProperties);
            
            // Subdivide if necessary
            if (node.objects.Count > maxObjectsPerNode && depth < maxDepth)
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
    
    private void Subdivide(OctTreeNode node, int depth)
    {
        if (node.isLeaf && node.objects.Count > maxObjectsPerNode && depth < maxDepth)
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
            
            // Redistribute objects
            List<GameObject> objectsToRedistribute = new List<GameObject>(node.objects);
            List<object> propertiesToRedistribute = new List<object>(node.objectBehaviorTreeProperties);
            
            node.objects.Clear();
            node.objectBehaviorTreeProperties.Clear();
            
            for (int i = 0; i < objectsToRedistribute.Count; i++)
            {
                Bounds objBounds = GetGameObjectBounds(objectsToRedistribute[i]);
                InsertRecursive(node, objBounds, objectsToRedistribute[i], propertiesToRedistribute[i], depth);
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
            foreach (GameObject obj in node.objects)
            {
                Bounds objBounds = GetGameObjectBounds(obj);
                if (objBounds.Intersects(searchBounds))
                {
                    results.Add(obj);
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
