using System.Collections.Generic;
using UnityEngine;

// QuadTree data structure for 2D spatial partitioning
// Each leaf has 4 sections: upper-right, top-left, bottom-left, lower-right
public class SGQuadTree
{
    public enum Quadrant
    {
        UpperRight,  // y > 0.5 && x > 0.5
        TopLeft,     // y > 0.5 && x < 0.5
        BottomLeft,  // y < 0.5 && x < 0.5
        LowerRight   // y < 0.5 && x > 0.5
    }
    
    public class QuadTreeNode
    {
        public Bounds bounds;
        public List<GameObject> objects = new List<GameObject>();
        public List<object> objectBehaviorTreeProperties = new List<object>();
        public QuadTreeNode[] children;
        public bool isLeaf = true;
        
        public QuadTreeNode(Bounds bounds)
        {
            this.bounds = bounds;
        }
    }
    
    private QuadTreeNode root;
    private int maxObjectsPerNode = 4;
    private int maxDepth = 8;
    
    public SGQuadTree(Bounds bounds)
    {
        root = new QuadTreeNode(bounds);
    }
    
    public void Insert(Bounds objectBounds, GameObject obj, object behaviorTreeProperties)
    {
        InsertRecursive(root, objectBounds, obj, behaviorTreeProperties, 0);
    }
    
    private void InsertRecursive(QuadTreeNode node, Bounds objectBounds, GameObject obj, object behaviorTreeProperties, int depth)
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
    
    private void Subdivide(QuadTreeNode node, int depth)
    {
        if (node.isLeaf && node.objects.Count > maxObjectsPerNode && depth < maxDepth)
        {
            node.isLeaf = false;
            node.children = new QuadTreeNode[4];
            
            Vector3 center = node.bounds.center;
            Vector3 size = node.bounds.size * 0.5f;
            
            // Create 4 children
            node.children[(int)Quadrant.UpperRight] = new QuadTreeNode(new Bounds(
                new Vector3(center.x + size.x * 0.5f, center.y + size.y * 0.5f, center.z),
                new Vector3(size.x, size.y, node.bounds.size.z)));
            
            node.children[(int)Quadrant.TopLeft] = new QuadTreeNode(new Bounds(
                new Vector3(center.x - size.x * 0.5f, center.y + size.y * 0.5f, center.z),
                new Vector3(size.x, size.y, node.bounds.size.z)));
            
            node.children[(int)Quadrant.BottomLeft] = new QuadTreeNode(new Bounds(
                new Vector3(center.x - size.x * 0.5f, center.y - size.y * 0.5f, center.z),
                new Vector3(size.x, size.y, node.bounds.size.z)));
            
            node.children[(int)Quadrant.LowerRight] = new QuadTreeNode(new Bounds(
                new Vector3(center.x + size.x * 0.5f, center.y - size.y * 0.5f, center.z),
                new Vector3(size.x, size.y, node.bounds.size.z)));
            
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
    
    private void SearchRecursive(QuadTreeNode node, Bounds searchBounds, List<GameObject> results)
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
        root = new QuadTreeNode(root.bounds);
    }
    
    public List<GameObject> GetAllObjects()
    {
        List<GameObject> allObjects = new List<GameObject>();
        GetAllObjectsRecursive(root, allObjects);
        return allObjects;
    }
    
    private void GetAllObjectsRecursive(QuadTreeNode node, List<GameObject> results)
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
    
    public QuadTreeNode GetRoot()
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
}
