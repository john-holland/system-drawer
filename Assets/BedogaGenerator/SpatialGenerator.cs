using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Main component for procedural spatial generation
// Manages generation lifecycle, coordinates with behavior tree and spatial trees
public class SpatialGenerator : MonoBehaviour
{
    public enum GenerationMode
    {
        TwoDimensional,  // Use QuadTree
        ThreeDimensional // Use OctTree
    }
    
    [Header("Generation Settings")]
    public GenerationMode mode = GenerationMode.TwoDimensional;
    public int seed = 0;
    public bool autoGenerateOnStart = false;
    public bool autoRegenerateOnResize = true;
    
    [Header("Generation Bounds")]
    public Vector3 generationSize = Vector3.one * 10f;
    
    [Header("Empty Space Markers")]
    public bool useEmptySpaceMarkers = true;
    public List<SGBehaviorTreeEmptySpace> emptySpaceMarkers = new List<SGBehaviorTreeEmptySpace>();
    
    [Header("Output Organization")]
    public Transform behaviorTreeParent;
    public Transform sceneTreeParent;
    public Transform sceneGraphParent;
    
    private SGTreeSolverInterface treeSolver;
    private System.Random rng;
    private Bounds generationBounds;
    private Vector3 lastScale;
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
        if (autoGenerateOnStart)
        {
            Generate();
        }
    }
    
    void Update()
    {
        if (autoRegenerateOnResize)
        {
            // Check if transform scale has changed
            if (transform.localScale != lastScale)
            {
                OnResize();
                lastScale = transform.localScale;
            }
        }
    }
    
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }
        
        rng = new System.Random(seed);
        generationBounds = new Bounds(transform.position, generationSize);
        lastScale = transform.localScale;
        
        // Setup solver based on mode
        SetupSolver();
        
        // Setup output organization
        SetupOutputOrganization();
        
        // Collect empty space markers
        if (useEmptySpaceMarkers)
        {
            CollectEmptySpaceMarkers();
        }
        
        isInitialized = true;
    }
    
    private void SetupSolver()
    {
        // Remove existing solver if any
        SGQuadTreeSolver quadSolver = GetComponent<SGQuadTreeSolver>();
        SGOctTreeSolver octSolver = GetComponent<SGOctTreeSolver>();
        
        if (mode == GenerationMode.TwoDimensional)
        {
            if (octSolver != null)
            {
                DestroyImmediate(octSolver);
            }
            
            if (quadSolver == null)
            {
                quadSolver = gameObject.AddComponent<SGQuadTreeSolver>();
            }
            
            quadSolver.Initialize(generationBounds, seed);
            treeSolver = quadSolver;
        }
        else // ThreeDimensional
        {
            if (quadSolver != null)
            {
                DestroyImmediate(quadSolver);
            }
            
            if (octSolver == null)
            {
                octSolver = gameObject.AddComponent<SGOctTreeSolver>();
            }
            
            octSolver.Initialize(generationBounds, seed);
            treeSolver = octSolver;
        }
    }
    
    private void SetupOutputOrganization()
    {
        // Create or find BehaviorTree parent
        if (behaviorTreeParent == null)
        {
            GameObject behaviorTreeObj = transform.Find("BehaviorTree")?.gameObject;
            if (behaviorTreeObj == null)
            {
                behaviorTreeObj = new GameObject("BehaviorTree");
                behaviorTreeObj.transform.SetParent(transform);
            }
            behaviorTreeParent = behaviorTreeObj.transform;
        }
        
        // Create or find SceneTree parent
        if (sceneTreeParent == null)
        {
            GameObject sceneTreeObj = transform.Find("SceneTree")?.gameObject;
            if (sceneTreeObj == null)
            {
                sceneTreeObj = new GameObject("SceneTree");
                sceneTreeObj.transform.SetParent(transform);
            }
            sceneTreeParent = sceneTreeObj.transform;
        }
        
        // Create or find SceneGraph parent
        if (sceneGraphParent == null)
        {
            GameObject sceneGraphObj = transform.Find("SceneGraph")?.gameObject;
            if (sceneGraphObj == null)
            {
                sceneGraphObj = new GameObject("SceneGraph");
                sceneGraphObj.transform.SetParent(transform);
            }
            sceneGraphParent = sceneGraphObj.transform;
        }
    }
    
    public void CollectEmptySpaceMarkers()
    {
        emptySpaceMarkers.Clear();
        SGBehaviorTreeEmptySpace[] markers = FindObjectsOfType<SGBehaviorTreeEmptySpace>();
        foreach (var marker in markers)
        {
            // Only include markers within generation bounds (or all if bounds checking is disabled)
            Bounds markerBounds = marker.GetBounds();
            if (generationBounds.Intersects(markerBounds) || !useEmptySpaceMarkers)
            {
                emptySpaceMarkers.Add(marker);
            }
        }
    }
    
    public void Generate()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        // Clear existing generation
        ClearGeneration();
        
        // Get behavior tree root
        SGTreeNodeContainer container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
        if (container == null)
        {
            Debug.LogWarning("SpatialGenerator: No SGTreeNodeContainer found in BehaviorTree parent");
            return;
        }
        
        SGBehaviorTreeNode rootNode = container.GetRootNode();
        if (rootNode == null)
        {
            Debug.LogWarning("SpatialGenerator: No root behavior tree node found");
            return;
        }
        
        // Traverse behavior tree and generate
        TraverseBehaviorTree(rootNode);
    }
    
    private void TraverseBehaviorTree(SGBehaviorTreeNode node)
    {
        if (node == null || !node.isEnabled)
        {
            return;
        }
        
        // Check if node can place
        if (!node.CanPlace())
        {
            return;
        }
        
        // Find available space
        Bounds? availableSpace = FindAvailableSpaceForNode(node);
        if (!availableSpace.HasValue)
        {
            // No space found - disable node
            return;
        }
        
        // Place objects based on node configuration
        PlaceNodeObjects(node, availableSpace.Value);
        
        // Increment placement count
        node.IncrementPlacementCount();
        
        // Traverse child nodes
        foreach (var childNode in node.childNodes)
        {
            TraverseBehaviorTree(childNode);
        }
    }
    
    private Bounds? FindAvailableSpaceForNode(SGBehaviorTreeNode node)
    {
        if (treeSolver == null)
        {
            return null;
        }
        
        // Use appropriate solver method
        if (mode == GenerationMode.TwoDimensional)
        {
            SGQuadTreeSolver quadSolver = treeSolver as SGQuadTreeSolver;
            if (quadSolver != null)
            {
                return quadSolver.FindAvailableSpace(node.minSpace, node.maxSpace, node.optimalSpace, emptySpaceMarkers);
            }
        }
        else
        {
            SGOctTreeSolver octSolver = treeSolver as SGOctTreeSolver;
            if (octSolver != null)
            {
                return octSolver.FindAvailableSpace(node.minSpace, node.maxSpace, node.optimalSpace, emptySpaceMarkers);
            }
        }
        
        return null;
    }
    
    private void PlaceNodeObjects(SGBehaviorTreeNode node, Bounds placementBounds)
    {
        if (node.gameObjectPrefabs == null || node.gameObjectPrefabs.Count == 0)
        {
            return;
        }
        
        // Select a random prefab
        int prefabIndex = rng.Next(node.gameObjectPrefabs.Count);
        GameObject prefab = node.gameObjectPrefabs[prefabIndex];
        if (prefab == null)
        {
            return;
        }
        
        // Instantiate object
        GameObject instance = Instantiate(prefab, sceneTreeParent);
        instance.transform.position = placementBounds.center;
        instance.transform.rotation = Quaternion.Euler(node.rotationPreference);
        
        // Apply alignment
        ApplyAlignment(node, instance, placementBounds);
        
        // Add to spatial tree
        Bounds instanceBounds = GetGameObjectBounds(instance);
        treeSolver.Insert(instanceBounds, node, instance);
    }
    
    private void ApplyAlignment(SGBehaviorTreeNode node, GameObject obj, Bounds bounds)
    {
        Vector3 position = obj.transform.position;
        Vector3 size = GetGameObjectBounds(obj).size;
        
        switch (node.alignPreference)
        {
            // todo: review: per 0.5f coeficient, we might want to add a "align flush" option
            
            case SGBehaviorTreeNode.AlignmentPreference.Up:
                position.y = bounds.max.y - size.y * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Down:
                position.y = bounds.min.y + size.y * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Left:
                position.x = bounds.min.x + size.x * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Right:
                position.x = bounds.max.x - size.x * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Forward:
                position.z = bounds.max.z - size.z * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Backward:
                position.z = bounds.min.z + size.z * 0.5f;
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Center:
                position = bounds.center;
                break;
        }
        
        obj.transform.position = position;
    }
    
    private void ClearGeneration()
    {
        if (treeSolver != null)
        {
            treeSolver.Clear();
        }
        
        // Clear scene tree
        if (sceneTreeParent != null)
        {
            for (int i = sceneTreeParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(sceneTreeParent.GetChild(i).gameObject);
            }
        }
    }
    
    private void OnResize()
    {
        if (!isInitialized)
        {
            return;
        }
        
        // Update generation bounds
        Bounds newBounds = new Bounds(transform.position, transform.localScale);
        
        // Update tree solver if bounds changed significantly
        if (treeSolver != null)
        {
            // For now, always update tree (can be enhanced with comparison logic)
            if (treeSolver is SGQuadTreeSolver quadSolver)
            {
                quadSolver.UpdateTree(newBounds);
            }
            else if (treeSolver is SGOctTreeSolver octSolver)
            {
                octSolver.UpdateTree(newBounds);
            }
        }
        
        generationBounds = newBounds;
        
        // Re-evaluate empty space markers
        if (useEmptySpaceMarkers)
        {
            CollectEmptySpaceMarkers();
        }
        
        // Regenerate with same seed
        Generate();
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
    
    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        rng = new System.Random(seed);
        if (treeSolver != null)
        {
            // Reinitialize solver with new seed
            SetupSolver();
        }
    }
    
    public void Regenerate()
    {
        Generate();
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw bounding box for generation area
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
        Gizmos.DrawWireCube(transform.position, generationSize);
        
        // Show mode indicator
        if (mode == GenerationMode.TwoDimensional)
        {
            // Draw 2D indicator
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position - Vector3.right * generationSize.x * 0.5f, 
                           transform.position + Vector3.right * generationSize.x * 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw more prominently when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, generationSize);
    }
    #endif
}
