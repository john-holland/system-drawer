using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

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
    [Range(0f, 1f)] public float alignmentOffsetCoefficient = 0.5f;
    
    [Header("Generation Bounds")]
    public Vector3 generationSize = Vector3.one * 10f;
    
    [Header("Empty Space Markers")]
    public bool useEmptySpaceMarkers = true;
    public List<SGBehaviorTreeEmptySpace> emptySpaceMarkers = new List<SGBehaviorTreeEmptySpace>();
    
    [Header("Output Organization")]
    public Transform behaviorTreeParent;
    public Transform sceneTreeParent;
    public Transform sceneGraphParent;
    
    [Header("Test Results")]
    [SerializeField, TextArea(10, 20)] private string testResults = "";
    
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
            // Skip destroyed objects
            if (marker == null)
            {
                continue;
            }
            
            // Only include markers within generation bounds (or all if bounds checking is disabled)
            Bounds markerBounds = marker.GetBounds();
            if (generationBounds.Intersects(markerBounds) || !useEmptySpaceMarkers)
            {
                emptySpaceMarkers.Add(marker);
            }
        }
        
        // Clean up any null references that might have been added previously
        emptySpaceMarkers.RemoveAll(marker => marker == null);
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
        
        // Apply alignment (this may change the position)
        ApplyAlignment(node, instance, placementBounds);
        
        // Get final bounds after alignment
        // Note: We insert with final bounds since the object hasn't been inserted yet
        Bounds instanceBounds = GetGameObjectBounds(instance);
        
        // Add to spatial tree with final bounds
        treeSolver.Insert(instanceBounds, node, instance);
    }
    
    private void ApplyAlignment(SGBehaviorTreeNode node, GameObject obj, Bounds bounds)
    {
        Vector3 position = obj.transform.position;
        Vector3 size = GetGameObjectBounds(obj).size;
        
        // Calculate offset coefficient
        // 0.0 = flush against edge, 1.0 = maximum offset from edge
        float offsetCoeff = alignmentOffsetCoefficient;
        
        bool isFlush = node.placeFlush;
        
        switch (node.alignPreference)
        {
            case SGBehaviorTreeNode.AlignmentPreference.Up:
                if (isFlush)
                {
                    // Object's top edge at bounds.max.y
                    position.y = bounds.max.y - size.y * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at top, 1 = offset downward
                    position.y = bounds.max.y - size.y * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Down:
                if (isFlush)
                {
                    // Object's bottom edge at bounds.min.y
                    position.y = bounds.min.y + size.y * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at bottom, 1 = offset upward
                    position.y = bounds.min.y + size.y * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Left:
                if (isFlush)
                {
                    // Object's left edge at bounds.min.x
                    position.x = bounds.min.x + size.x * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at left, 1 = offset rightward
                    position.x = bounds.min.x + size.x * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Right:
                if (isFlush)
                {
                    // Object's right edge at bounds.max.x
                    position.x = bounds.max.x - size.x * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at right, 1 = offset leftward
                    position.x = bounds.max.x - size.x * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Forward:
                if (isFlush)
                {
                    // Object's front edge at bounds.max.z
                    position.z = bounds.max.z - size.z * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at front, 1 = offset backward
                    position.z = bounds.max.z - size.z * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Backward:
                if (isFlush)
                {
                    // Object's back edge at bounds.min.z
                    position.z = bounds.min.z + size.z * 0.5f;
                }
                else
                {
                    // offsetCoeff 0 = flush at back, 1 = offset forward
                    position.z = bounds.min.z + size.z * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case SGBehaviorTreeNode.AlignmentPreference.Center:
                // Center alignment (offset doesn't apply)
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
    
    public void RunTest()
    {
        System.Text.StringBuilder results = new System.Text.StringBuilder();
        results.AppendLine("=== SpatialGenerator Test Run ===");
        results.AppendLine($"Mode: {mode}");
        results.AppendLine($"Seed: {seed}");
        results.AppendLine($"Generation Bounds: {generationSize}");
        results.AppendLine();
        
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        
        try
        {
            // Check behavior tree structure
            results.AppendLine("--- Behavior Tree Analysis ---");
            
            if (behaviorTreeParent == null)
            {
                errors.Add("BehaviorTree parent is null");
                results.AppendLine("ERROR: BehaviorTree parent is null");
            }
            else
            {
                SGTreeNodeContainer container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
                if (container == null)
                {
                    errors.Add("No SGTreeNodeContainer found in BehaviorTree parent");
                    results.AppendLine("ERROR: No SGTreeNodeContainer found in BehaviorTree parent");
                }
                else
                {
                    SGBehaviorTreeNode rootNode = container.GetRootNode();
                    if (rootNode == null)
                    {
                        errors.Add("No root behavior tree node found");
                        results.AppendLine("ERROR: No root behavior tree node found");
                    }
                    else
                    {
                        results.AppendLine($"Root node found: {rootNode.name}");
                        results.AppendLine($"Root enabled: {rootNode.isEnabled}");
                        results.AppendLine($"Root prefabs: {rootNode.gameObjectPrefabs?.Count ?? 0}");
                        
                        // Analyze tree structure
                        int nodeCount = AnalyzeBehaviorTree(rootNode, results, errors, warnings, 1);
                        results.AppendLine($"Total nodes in tree: {nodeCount}");
                        results.AppendLine();
                    }
                }
            }
            
            // Check sizing issues
            results.AppendLine("--- Size Validation ---");
            ValidateSizing(behaviorTreeParent, results, warnings);
            
            // Check if nodes fit within generation bounds
            ValidateBoundsCompatibility(behaviorTreeParent, results, errors, warnings);
            results.AppendLine();
            
            // Run generation
            results.AppendLine("--- Generation Test ---");
            if (errors.Count == 0)
            {
                // Clear previous generation
                ClearGeneration();
                
                // Initialize if needed
                if (!isInitialized)
                {
                    Initialize();
                }
                
                // Run generation
                Generate();
                
                // Count generated objects
                int generatedCount = 0;
                if (sceneTreeParent != null)
                {
                    generatedCount = sceneTreeParent.childCount;
                }
                
                results.AppendLine($"Generated objects: {generatedCount}");
                
                if (generatedCount == 0)
                {
                    warnings.Add("No objects were generated");
                    results.AppendLine("WARNING: No objects were generated");
                }
                else if (generatedCount < 3)
                {
                    warnings.Add($"Only {generatedCount} object(s) generated (expected at least 3)");
                    results.AppendLine($"WARNING: Only {generatedCount} object(s) generated");
                }
                else
                {
                    results.AppendLine($"✓ Generated {generatedCount} objects (minimum threshold met)");
                }
                
                // Show structure
                results.AppendLine();
                results.AppendLine("--- Generated Object Structure ---");
                if (sceneTreeParent != null)
                {
                    ShowObjectStructure(sceneTreeParent, results, 0);
                }
            }
            else
            {
                results.AppendLine("SKIPPED: Generation not run due to errors");
            }
            
            // Summary
            results.AppendLine();
            results.AppendLine("=== Test Summary ===");
            results.AppendLine($"Errors: {errors.Count}");
            results.AppendLine($"Warnings: {warnings.Count}");
            
            if (errors.Count > 0)
            {
                results.AppendLine();
                results.AppendLine("Errors:");
                foreach (string error in errors)
                {
                    results.AppendLine($"  - {error}");
                }
            }
            
            if (warnings.Count > 0)
            {
                results.AppendLine();
                results.AppendLine("Warnings:");
                foreach (string warning in warnings)
                {
                    results.AppendLine($"  - {warning}");
                }
            }
            
            if (errors.Count == 0 && warnings.Count == 0)
            {
                results.AppendLine();
                results.AppendLine("✓ All tests passed!");
            }
        }
        catch (System.Exception e)
        {
            results.AppendLine();
            results.AppendLine($"FATAL ERROR: {e.Message}");
            results.AppendLine($"Stack trace: {e.StackTrace}");
        }
        
        testResults = results.ToString();
        Debug.Log(testResults);
    }
    
    private int AnalyzeBehaviorTree(SGBehaviorTreeNode node, System.Text.StringBuilder results, List<string> errors, List<string> warnings, int depth)
    {
        if (node == null) return 0;
        
        string indent = new string(' ', depth * 2);
        results.AppendLine($"{indent}- {node.name} (depth {depth})");
        results.AppendLine($"{indent}  Enabled: {node.isEnabled}");
        results.AppendLine($"{indent}  Prefabs: {node.gameObjectPrefabs?.Count ?? 0}");
        results.AppendLine($"{indent}  Min Space: {node.minSpace}");
        results.AppendLine($"{indent}  Max Space: {node.maxSpace}");
        results.AppendLine($"{indent}  Optimal Space: {node.optimalSpace}");
        results.AppendLine($"{indent}  Placement Limit: {node.placementLimit}");
        results.AppendLine($"{indent}  Child Nodes: {node.childNodes?.Count ?? 0}");
        
        if (node.gameObjectPrefabs == null || node.gameObjectPrefabs.Count == 0)
        {
            warnings.Add($"Node '{node.name}' has no prefabs assigned");
            results.AppendLine($"{indent}  WARNING: No prefabs assigned");
        }
        
        int count = 1;
        if (node.childNodes != null)
        {
            foreach (var child in node.childNodes)
            {
                count += AnalyzeBehaviorTree(child, results, errors, warnings, depth + 1);
            }
        }
        
        return count;
    }
    
    private void ValidateSizing(Transform behaviorTreeParent, System.Text.StringBuilder results, List<string> warnings)
    {
        if (behaviorTreeParent == null) return;
        
        SGTreeNodeContainer container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
        if (container == null) return;
        
        SGBehaviorTreeNode rootNode = container.GetRootNode();
        if (rootNode == null) return;
        
        ValidateNodeSizing(rootNode, results, warnings);
    }
    
    private void ValidateNodeSizing(SGBehaviorTreeNode node, System.Text.StringBuilder results, List<string> warnings)
    {
        if (node == null) return;
        
        // Check if min > max
        if (node.minSpace.x > node.maxSpace.x || node.minSpace.y > node.maxSpace.y || node.minSpace.z > node.maxSpace.z)
        {
            warnings.Add($"Node '{node.name}' has min space larger than max space");
            results.AppendLine($"WARNING: {node.name} - min > max: min={node.minSpace}, max={node.maxSpace}");
        }
        
        // Check if optimal is outside min/max range
        if (node.optimalSpace.x < node.minSpace.x || node.optimalSpace.x > node.maxSpace.x ||
            node.optimalSpace.y < node.minSpace.y || node.optimalSpace.y > node.maxSpace.y ||
            node.optimalSpace.z < node.minSpace.z || node.optimalSpace.z > node.maxSpace.z)
        {
            warnings.Add($"Node '{node.name}' has optimal space outside min/max range");
            results.AppendLine($"WARNING: {node.name} - optimal outside range: min={node.minSpace}, optimal={node.optimalSpace}, max={node.maxSpace}");
        }
        
        // Check for zero or negative sizes
        if (node.minSpace.x <= 0 || node.minSpace.y <= 0 || node.minSpace.z <= 0)
        {
            warnings.Add($"Node '{node.name}' has zero or negative min space");
            results.AppendLine($"WARNING: {node.name} - zero/negative min space: {node.minSpace}");
        }
        
        if (node.maxSpace.x <= 0 || node.maxSpace.y <= 0 || node.maxSpace.z <= 0)
        {
            warnings.Add($"Node '{node.name}' has zero or negative max space");
            results.AppendLine($"WARNING: {node.name} - zero/negative max space: {node.maxSpace}");
        }
        
        // Recurse to children
        if (node.childNodes != null)
        {
            foreach (var child in node.childNodes)
            {
                ValidateNodeSizing(child, results, warnings);
            }
        }
    }
    
    private void ValidateBoundsCompatibility(Transform behaviorTreeParent, System.Text.StringBuilder results, List<string> errors, List<string> warnings)
    {
        if (behaviorTreeParent == null) return;
        
        SGTreeNodeContainer container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
        if (container == null) return;
        
        SGBehaviorTreeNode rootNode = container.GetRootNode();
        if (rootNode == null) return;
        
        results.AppendLine("--- Bounds Compatibility Check ---");
        results.AppendLine($"Generation Bounds: {generationSize}");
        
        // Check root node against generation bounds (errors if it doesn't fit)
        ValidateNodeBounds(rootNode, generationSize, results, errors, warnings, true);
        
        // Check all child nodes (warnings if they don't fit)
        if (rootNode.childNodes != null)
        {
            foreach (var child in rootNode.childNodes)
            {
                ValidateNodeBounds(child, generationSize, results, errors, warnings, false);
            }
        }
    }
    
    private void ValidateNodeBounds(SGBehaviorTreeNode node, Vector3 genBounds, System.Text.StringBuilder results, List<string> errors, List<string> warnings, bool rootOnly)
    {
        if (node == null) return;
        
        // Check if node's min space fits within generation bounds
        bool minFits = node.minSpace.x <= genBounds.x && node.minSpace.y <= genBounds.y && node.minSpace.z <= genBounds.z;
        bool maxFits = node.maxSpace.x <= genBounds.x && node.maxSpace.y <= genBounds.y && node.maxSpace.z <= genBounds.z;
        bool optimalFits = node.optimalSpace.x <= genBounds.x && node.optimalSpace.y <= genBounds.y && node.optimalSpace.z <= genBounds.z;
        
        if (!minFits)
        {
            string msg = $"Node '{node.name}' min space ({node.minSpace}) exceeds generation bounds ({genBounds})";
            if (rootOnly)
            {
                errors.Add(msg);
                results.AppendLine($"ERROR: {msg}");
            }
            else
            {
                warnings.Add(msg);
                results.AppendLine($"WARNING: {msg}");
            }
        }
        
        if (!maxFits)
        {
            string msg = $"Node '{node.name}' max space ({node.maxSpace}) exceeds generation bounds ({genBounds})";
            if (rootOnly)
            {
                errors.Add(msg);
                results.AppendLine($"ERROR: {msg}");
            }
            else
            {
                warnings.Add(msg);
                results.AppendLine($"WARNING: {msg}");
            }
        }
        
        if (!optimalFits && optimalFits != minFits) // Only warn if optimal doesn't fit but min does
        {
            warnings.Add($"Node '{node.name}' optimal space ({node.optimalSpace}) exceeds generation bounds ({genBounds})");
            results.AppendLine($"WARNING: {node.name} - optimal space exceeds bounds: optimal={node.optimalSpace}, bounds={genBounds}");
        }
        
        if (minFits && maxFits && optimalFits && rootOnly)
        {
            results.AppendLine($"✓ Root node '{node.name}' space requirements fit within generation bounds");
        }
        
        // Recurse to children if not root-only check
        if (!rootOnly && node.childNodes != null)
        {
            foreach (var child in node.childNodes)
            {
                ValidateNodeBounds(child, genBounds, results, errors, warnings, false);
            }
        }
    }
    
    private void ShowObjectStructure(Transform parent, System.Text.StringBuilder results, int depth)
    {
        if (parent == null) return;
        
        string indent = new string(' ', depth * 2);
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Bounds bounds = GetGameObjectBounds(child.gameObject);
            
            results.AppendLine($"{indent}- {child.name}");
            results.AppendLine($"{indent}  Position: {child.position}");
            results.AppendLine($"{indent}  Rotation: {child.rotation.eulerAngles}");
            results.AppendLine($"{indent}  Scale: {child.localScale}");
            results.AppendLine($"{indent}  Bounds: center={bounds.center}, size={bounds.size}");
            
            if (child.childCount > 0)
            {
                ShowObjectStructure(child, results, depth + 1);
            }
        }
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
