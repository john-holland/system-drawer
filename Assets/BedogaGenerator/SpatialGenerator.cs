using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

// Main component for procedural spatial generation
// Manages generation lifecycle, coordinates with behavior tree and spatial trees
public class SpatialGenerator : SpatialGeneratorBase
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
    
    /// <summary>Immediate = depth-first, place as soon as space is found. UniformQueue = collect all placement requests in BFS order, then process in order so placement is uniform (e.g. root then children, round-robin when placementLimit > 1).</summary>
    public enum PlacementStrategy { Immediate, UniformQueue }
    [Tooltip("UniformQueue processes placements in breadth-first order for more even distribution.")]
    public PlacementStrategy placementStrategy = PlacementStrategy.UniformQueue;
    
    [Header("Tree Visualization")]
    public bool showTreeVisualization = false;
    [Tooltip("Draw placement slot positions (solver grid) within generation bounds. Uses root node min/max/optimal if available, else Gizmo Placement Slot Size.")]
    public bool showPlacementSlots = true;
    [Tooltip("Slot size used for placement gizmo when root node is not available (min/max/optimal).")]
    public Vector3 gizmoPlacementSlotSize = new Vector3(10f, 6f, 10f);
    
    [Header("Generation Bounds")]
    public Vector3 generationSize = Vector3.one * 10f;
    
    [Header("Empty Space Markers")]
    public bool useEmptySpaceMarkers = true;
    public List<SGBehaviorTreeEmptySpace> emptySpaceMarkers = new List<SGBehaviorTreeEmptySpace>();

    [Header("Debug")]
    [Tooltip("When enabled, log every FindAvailableSpaceForNode and ApplyAlignment (noisy in tests).")]
    public bool verbosePlacementLogging = false;
    
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
    
    /// <summary>Maps each behavior tree node to all scene instances placed for that node (order = placement order), so child nodes can parent under the correct instance when placementLimit > 1.</summary>
    private Dictionary<SGBehaviorTreeNode, List<GameObject>> behaviorNodeToSceneInstance;
    
    /// <summary>Placement count per (child node, parent instance index) so each parent placement gets its own slot index (e.g. first spotlight in room 0 and first in room 1 both use slot 0).</summary>
    private Dictionary<(SGBehaviorTreeNode node, int parentInstanceIndex), int> placementCountPerParentInstance;
    
    /// <summary>Global index for root-level placements so different nodes (e.g. room vs spotlight) don't get the same slot when falling back to root solver.</summary>
    private int nextRootPlacementIndex;
    
    /// <summary>Next slot index per parent instance so different nodes placing in the same parent get distinct slots (avoids e.g. two spotlight nodes stacking in the same corner).</summary>
    private Dictionary<int, int> nextSlotPerParentInstance;
    
    /// <summary>Minimum size per axis when node/object has zero size (e.g. spotlight with no Renderer/Collider and zero optimalSpace). Avoids "doesn't fit in parent bounds" and degenerate bounds.</summary>
    private const float MinSizeEpsilon = 0.01f;

    /// <inheritdoc />
    public override Bounds GetSpatialBounds()
    {
        Bounds local = isInitialized ? generationBounds : new Bounds(Vector3.zero, generationSize);
        return LocalToWorldBounds(local);
    }
    
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
        // Use local space bounds (center at origin, size is generationSize)
        generationBounds = new Bounds(Vector3.zero, generationSize);
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
        SGBehaviorTreeEmptySpace[] markers = FindObjectsByType<SGBehaviorTreeEmptySpace>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            // Skip destroyed objects
            if (marker == null)
            {
                continue;
            }
            
            // Only include markers within generation bounds (or all if bounds checking is disabled)
            // Convert marker bounds from world space to local space relative to SpatialGenerator
            Bounds worldMarkerBounds = marker.GetBounds();
            Bounds localMarkerBounds = WorldToLocalBounds(worldMarkerBounds);
            
            if (generationBounds.Intersects(localMarkerBounds) || !useEmptySpaceMarkers)
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
        
        // Generate using chosen strategy
        if (placementStrategy == PlacementStrategy.UniformQueue)
        {
            GenerateUniformPlacement(rootNode);
        }
        else
        {
            TraverseBehaviorTree(rootNode);
        }
    }
    
    /// <summary>Collect placement requests in BFS order, then process in two phases: (1) place all root instances first, (2) place children so each parent instance gets its full set (walls, table, sign, spotlights per room).</summary>
    private void GenerateUniformPlacement(SGBehaviorTreeNode rootNode)
    {
        // Build queue: (node, depth) in BFS order. Include nodes that have prefabs, and container nodes (no prefabs but placementLimit > 0 and have children that need a parent).
        var queue = new List<(SGBehaviorTreeNode node, int depth)>();
        var containerNodes = new List<(SGBehaviorTreeNode node, int depth)>();
        var bfsQueue = new Queue<(SGBehaviorTreeNode node, int depth)>();
        bfsQueue.Enqueue((rootNode, 0));
        while (bfsQueue.Count > 0)
        {
            var (node, depth) = bfsQueue.Dequeue();
            if (node == null || !node.isEnabled)
                continue;
            bool hasPrefabs = node.gameObjectPrefabs != null && node.gameObjectPrefabs.Count > 0;
            if (hasPrefabs)
                queue.Add((node, depth));
            else if (node.placementLimit > 0 && node.childNodes != null && node.childNodes.Count > 0)
                containerNodes.Add((node, depth));
            if (node.childNodes != null)
            {
                foreach (var child in node.childNodes)
                    bfsQueue.Enqueue((child, depth + 1));
            }
        }
        
        // Phase 1: Place all root nodes first (parentCount == 0). Place container roots as empty GOs so children have parent instances; place prefab roots normally.
        var rootIndices = new List<int>();
        for (int i = 0; i < queue.Count; i++)
        {
            if (GetParentInstanceCount(queue[i].node) == 0)
                rootIndices.Add(i);
        }
        // Place container roots first (no prefabs): create empty GameObjects at root positions so their children have instances to parent under.
        // When any descendant has perParentPlacementLimits = true, place at least that many parent instances so each clone gets its per-parent children.
        foreach (var (node, depth) in containerNodes)
        {
            if (GetParentInstanceCount(node) != 0)
                continue;
            int effectiveLimit = Mathf.Max(node.placementLimit, GetRequiredParentInstanceCount(node));
            while (node.GetPlacementCount() < effectiveLimit)
            {
                Bounds? availableSpace = FindAvailableSpaceForNode(node);
                if (!availableSpace.HasValue)
                    break;
                PlaceContainerNode(node, availableSpace.Value);
                node.IncrementPlacementCount();
            }
        }
        // Prefab roots: place up to effectiveLimit (max of node limit and required parent instances when a descendant has perParentPlacementLimits).
        int phase1Safety = 0;
        const int maxPhase1Iterations = 50000;
        while (phase1Safety < maxPhase1Iterations)
        {
            bool placedAny = false;
            foreach (int i in rootIndices)
            {
                var (node, depth) = queue[i];
                int effectiveLimit = Mathf.Max(node.placementLimit, GetRequiredParentInstanceCount(node));
                if (node.GetPlacementCount() >= effectiveLimit)
                    continue;
                Bounds? availableSpace = FindAvailableSpaceForNode(node);
                if (!availableSpace.HasValue)
                    continue;
                PlaceNodeObjects(node, availableSpace.Value);
                node.IncrementPlacementCount();
                placedAny = true;
            }
            if (!placedAny)
                break;
            phase1Safety++;
        }
        
        // Phase 2: Place children. When parent has multiple instances, place one per parent so every bounds gets walls, table, sign, spotlights.
        for (int i = 0; i < queue.Count; i++)
        {
            var (node, depth) = queue[i];
            if (GetParentInstanceCount(node) == 0)
                continue; // root already placed in phase 1
            if (!node.CanPlace())
                continue;
            int parentCount = GetParentInstanceCount(node);
            if (parentCount > 1)
            {
                int limitPerParent = node.perParentPlacementLimits ? node.GetPlacementLimitValue() : -1;
                for (int pi = 0; pi < parentCount; pi++)
                {
                    while (limitPerParent >= 0
                        ? GetPlacementCountInParent(node, pi) < limitPerParent
                        : node.CanPlace())
                    {
                        int slotInParent = GetNextSlotInParent(pi);
                        Bounds? slotBounds = FindAvailableSpaceForNode(node, pi, slotInParent);
                        if (!slotBounds.HasValue)
                            break;
                        PlaceNodeObjects(node, slotBounds.Value, pi);
                        node.IncrementPlacementCount();
                        IncrementPlacementCountInParent(node, pi);
                        IncrementSlotInParent(pi);
                    }
                }
                continue;
            }
            // Single parent: use global slot index for this parent (pi=0) so different nodes don't get the same slot
            int slot0 = GetNextSlotInParent(0);
            Bounds? availableSpace = FindAvailableSpaceForNode(node, 0, slot0);
            if (!availableSpace.HasValue)
            {
                Debug.LogWarning($"[SpatialGenerator] UniformQueue - skipped '{node.name}' (no space found)");
                continue;
            }
            PlaceNodeObjects(node, availableSpace.Value, 0);
            node.IncrementPlacementCount();
            IncrementSlotInParent(0);
        }
    }
    
    private void TraverseBehaviorTree(SGBehaviorTreeNode node)
    {
        if (node == null)
        {
            if (verbosePlacementLogging)
                Debug.Log("[SpatialGenerator] TraverseBehaviorTree - skipped null node");
            return;
        }
        if (!node.isEnabled)
        {
            if (verbosePlacementLogging)
                Debug.Log($"[SpatialGenerator] TraverseBehaviorTree - skipped '{node.name}' (disabled)");
            return;
        }
        
        if (!node.CanPlace())
        {
            Debug.Log($"[SpatialGenerator] TraverseBehaviorTree - skipped '{node.name}' (CanPlace=false, placement limit reached or node disabled)");
            return;
        }
        
        Bounds? availableSpace = FindAvailableSpaceForNode(node);
        if (!availableSpace.HasValue)
        {
            Debug.LogWarning($"[SpatialGenerator] TraverseBehaviorTree - skipped '{node.name}' (no space found)");
            return;
        }
        
        PlaceNodeObjects(node, availableSpace.Value);
        node.IncrementPlacementCount();
        if (verbosePlacementLogging)
            Debug.Log($"[SpatialGenerator] TraverseBehaviorTree - placed '{node.name}'");
        
        foreach (var childNode in node.childNodes)
        {
            TraverseBehaviorTree(childNode);
        }
    }
    
    /// <summary>When any descendant has perParentPlacementLimits = true, returns the number of parent instances needed so each can get its per-parent placements. Used to "clone" the parent (place it multiple times) when a child needs per-parent.</summary>
    private int GetRequiredParentInstanceCount(SGBehaviorTreeNode node)
    {
        if (node == null) return 1;
        int required = 1;
        if (node.perParentPlacementLimits)
            required = Mathf.Max(required, node.GetPlacementLimitValue());
        if (node.childNodes != null)
        {
            foreach (var child in node.childNodes)
            {
                required = Mathf.Max(required, GetRequiredParentInstanceCount(child));
            }
        }
        return required;
    }

    /// <summary>Number of placed instances for this node's parent (0 if no parent or parent has none). Used to "place one per parent" so rigid bodies etc. go to every bounds.</summary>
    private int GetParentInstanceCount(SGBehaviorTreeNode node)
    {
        Transform parent = node.transform.parent;
        if (parent == null || behaviorNodeToSceneInstance == null)
            return 0;
        SGBehaviorTreeNode parentNode = parent.GetComponent<SGBehaviorTreeNode>();
        if (parentNode == null || !behaviorNodeToSceneInstance.TryGetValue(parentNode, out List<GameObject> list) || list == null)
            return 0;
        return list.Count;
    }
    
    /// <summary>Get the parent scene instance to use when placing this child node. When parentInstanceIndex is set, use that instance; else distribute by child placement count (round-robin).</summary>
    private GameObject GetParentInstanceForChild(SGBehaviorTreeNode node, int? parentInstanceIndex = null)
    {
        Transform parent = node.transform.parent;
        if (parent == null || behaviorNodeToSceneInstance == null)
            return null;
        SGBehaviorTreeNode parentNode = parent.GetComponent<SGBehaviorTreeNode>();
        if (parentNode == null || !behaviorNodeToSceneInstance.TryGetValue(parentNode, out List<GameObject> list) || list == null || list.Count == 0)
            return null;
        int index = parentInstanceIndex.HasValue ? Mathf.Clamp(parentInstanceIndex.Value, 0, list.Count - 1) : (node.GetPlacementCount() % list.Count);
        return list[index];
    }
    
    /// <summary>Placement count for this node within the given parent instance index (so each parent gets its own slot 0, 1, 2...).</summary>
    private int GetPlacementCountInParent(SGBehaviorTreeNode node, int parentInstanceIndex)
    {
        if (placementCountPerParentInstance == null) return 0;
        var key = (node, parentInstanceIndex);
        return placementCountPerParentInstance.TryGetValue(key, out int count) ? count : 0;
    }
    
    private void IncrementPlacementCountInParent(SGBehaviorTreeNode node, int parentInstanceIndex)
    {
        if (placementCountPerParentInstance == null)
            placementCountPerParentInstance = new Dictionary<(SGBehaviorTreeNode node, int parentInstanceIndex), int>();
        var key = (node, parentInstanceIndex);
        placementCountPerParentInstance[key] = GetPlacementCountInParent(node, parentInstanceIndex) + 1;
    }
    
    private int GetNextSlotInParent(int parentInstanceIndex)
    {
        if (nextSlotPerParentInstance == null) return 0;
        return nextSlotPerParentInstance.TryGetValue(parentInstanceIndex, out int v) ? v : 0;
    }
    
    private void IncrementSlotInParent(int parentInstanceIndex)
    {
        if (nextSlotPerParentInstance == null)
            nextSlotPerParentInstance = new Dictionary<int, int>();
        nextSlotPerParentInstance[parentInstanceIndex] = GetNextSlotInParent(parentInstanceIndex) + 1;
    }
    
    private Bounds? FindAvailableSpaceForNode(SGBehaviorTreeNode node, int? parentInstanceIndex = null, int? placementIndexInParent = null)
    {
        if (treeSolver == null)
            return null;
        
        Bounds? result = null;
        Transform parent = node.transform.parent;
        SGBehaviorTreeNode parentNode = parent != null ? parent.GetComponent<SGBehaviorTreeNode>() : null;
        GameObject parentSceneObj = GetParentInstanceForChild(node, parentInstanceIndex);
        if (parentSceneObj != null && parentNode != null)
        {
            // Use logical placement bounds (reserved space) so parent bounds are always node.optimalSpace in local;
            // actual instance bounds (e.g. thin plane) can be smaller and cause "optimal size doesn't fit" for rooms 2+
            Bounds parentWorldBounds = GetLogicalPlacementBounds(parentSceneObj, parentNode);
            Bounds parentLocalBounds = WorldToLocalBounds(parentWorldBounds);
            
            int slotInParent = placementIndexInParent.HasValue ? placementIndexInParent.Value : node.GetPlacementCount();
            if (mode == GenerationMode.TwoDimensional)
            {
                SGQuadTreeSolver quadSolver = treeSolver as SGQuadTreeSolver;
                if (quadSolver != null)
                    result = FindSpaceInParentBounds(quadSolver, parentLocalBounds, node, emptySpaceMarkers, parentNode, excludeFromOverlap: parentSceneObj, placementIndexInParent: slotInParent);
            }
            else
            {
                SGOctTreeSolver octSolver = treeSolver as SGOctTreeSolver;
                if (octSolver != null)
                    result = FindSpaceInParentBounds(octSolver, parentLocalBounds, node, emptySpaceMarkers, parentNode, excludeFromOverlap: parentSceneObj, placementIndexInParent: slotInParent);
            }
        }
        else if (parentNode != null)
        {
            Debug.LogWarning($"[SpatialGenerator] FindAvailableSpaceForNode - Node: {node.name} - parent '{parentNode.name}' has no placed instance; falling back to root solver");
        }
        
        // If no parent or no space found in parent, use root solver with a global placement index so each placement (across all nodes) gets a different slot (avoids different nodes e.g. room vs spotlight getting the same slot).
        if (!result.HasValue)
        {
            int placementIndex = nextRootPlacementIndex;
            if (mode == GenerationMode.TwoDimensional)
            {
                SGQuadTreeSolver quadSolver = treeSolver as SGQuadTreeSolver;
                if (quadSolver != null)
                    result = quadSolver.FindAvailableSpace(node.minSpace, node.maxSpace, node.optimalSpace, emptySpaceMarkers, placementIndex, PlacementSlotConfig.FromNode(node));
            }
            else
            {
                SGOctTreeSolver octSolver = treeSolver as SGOctTreeSolver;
                if (octSolver != null)
                    result = octSolver.FindAvailableSpace(node.minSpace, node.maxSpace, node.optimalSpace, emptySpaceMarkers, placementIndex, PlacementSlotConfig.FromNode(node));
            }
            if (result.HasValue)
                nextRootPlacementIndex++;
        }
        
        // Debug: log what the solver returned
        if (result.HasValue)
        {
            Debug.Log($"[SpatialGenerator] FindAvailableSpaceForNode - Node: {node.name}\n" +
                      $"  Solver returned (local space): center={result.Value.center}, size={result.Value.size}\n" +
                      $"  Node requirements: min={node.minSpace}, max={node.maxSpace}, optimal={node.optimalSpace}\n" +
                      $"  Alignment from fit: {node.GetAlignmentFromFit()}, Place flush: {node.placeFlush}");
        }
        else
        {
            Debug.LogWarning($"[SpatialGenerator] FindAvailableSpaceForNode - Node: {node.name} - No space found");
        }
        
        return result;
    }
    
    /// <summary>
    /// Find space within parent bounds. Uses parent node's placement mode (In = no translate, Left/Right/Forward/Down/Under/Up = that edge) to place children; falls back to child's fit-derived alignment when parent is null.
    /// </summary>
    /// <param name="parentNode">Parent behavior node; its placementMode defines where this child is placed (On=center, Left/Right/Forward/Down/Under=that edge).</param>
    /// <param name="excludeFromOverlap">If set, this object is ignored when checking for overlapping placed objects (e.g. the parent's own instance when placing a child inside it).</param>
    /// <param name="placementIndexInParent">Index of this placement within the parent (0 = first). Used to offset slot so multiple children (e.g. spotlights) get distinct positions and don't fall back to root and overlap.</param>
    private Bounds? FindSpaceInParentBounds(SGTreeSolverInterface solver, Bounds parentBounds, SGBehaviorTreeNode node, List<SGBehaviorTreeEmptySpace> emptySpaces, SGBehaviorTreeNode parentNode, GameObject excludeFromOverlap = null, int placementIndexInParent = 0)
    {
        // Clamp to minimum size so zero-size nodes (e.g. spotlight with no Renderer/Collider and 0 optimalSpace) get a valid test bounds and pass the fit check
        Vector3 optimalSize = new Vector3(
            Mathf.Max(node.optimalSpace.x, MinSizeEpsilon),
            Mathf.Max(node.optimalSpace.y, MinSizeEpsilon),
            Mathf.Max(node.optimalSpace.z, MinSizeEpsilon));
        Vector3 minSize = new Vector3(
            Mathf.Max(node.minSpace.x, MinSizeEpsilon),
            Mathf.Max(node.minSpace.y, MinSizeEpsilon),
            Mathf.Max(node.minSpace.z, MinSizeEpsilon));
        
        // Position based on parent's placement mode (where to place this node's children) or child's fit-derived alignment
        Vector3 boundsCenter = parentBounds.center;
        Vector3 boundsSize = optimalSize;
        
        bool placeOutside = false;
        if (parentNode != null)
        {
            switch (parentNode.placementMode)
            {
                case SGBehaviorTreeNode.PlacementMode.In:
                    break;
                case SGBehaviorTreeNode.PlacementMode.Left:
                    boundsCenter.x = parentBounds.min.x - optimalSize.x * 0.5f;
                    placeOutside = true;
                    break;
                case SGBehaviorTreeNode.PlacementMode.Right:
                    boundsCenter.x = parentBounds.max.x + optimalSize.x * 0.5f;
                    placeOutside = true;
                    break;
                case SGBehaviorTreeNode.PlacementMode.Forward:
                    boundsCenter.z = parentBounds.max.z + optimalSize.z * 0.5f;
                    placeOutside = true;
                    break;
                case SGBehaviorTreeNode.PlacementMode.Down:
                case SGBehaviorTreeNode.PlacementMode.Under:
                    boundsCenter.y = parentBounds.min.y - optimalSize.y * 0.5f;
                    placeOutside = true;
                    break;
                case SGBehaviorTreeNode.PlacementMode.Up:
                    boundsCenter.y = parentBounds.max.y + optimalSize.y * 0.5f;
                    placeOutside = true;
                    break;
            }
        }
        else
        {
            SGBehaviorTreeNode.AlignmentPreference align = node.GetAlignmentFromFit();
            switch (align)
            {
                case SGBehaviorTreeNode.AlignmentPreference.Left:
                    boundsCenter.x = parentBounds.min.x + optimalSize.x * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Right:
                    boundsCenter.x = parentBounds.max.x - optimalSize.x * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Up:
                    boundsCenter.y = parentBounds.max.y - optimalSize.y * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Down:
                    boundsCenter.y = parentBounds.min.y + optimalSize.y * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Forward:
                    boundsCenter.z = parentBounds.max.z - optimalSize.z * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Backward:
                    boundsCenter.z = parentBounds.min.z + optimalSize.z * 0.5f;
                    break;
                case SGBehaviorTreeNode.AlignmentPreference.Center:
                    break;
            }
        }
        
        // When placing multiple children of same type (e.g. 2nd spotlight), offset slot so we don't get same position and fall back to root (causing overlap).
        if (placementIndexInParent > 0)
        {
            float step = Mathf.Max(optimalSize.x, optimalSize.z, MinSizeEpsilon);
            float offset = placementIndexInParent * step * 0.5f;
            float sign = (placementIndexInParent % 2 == 0) ? -1f : 1f;
            if (placeOutside)
                boundsCenter.x += sign * offset;
            else
                boundsCenter.x = Mathf.Clamp(boundsCenter.x + sign * offset, parentBounds.min.x + boundsSize.x * 0.5f, parentBounds.max.x - boundsSize.x * 0.5f);
        }
        
        // Check if this space is clear (solver uses local space).
        // When placeOutside, slot is outside parent bounds so we only check overlap, not containment.
        const float containTolerance = 1e-4f;
        Bounds testBounds = new Bounds(boundsCenter, boundsSize);
        bool containsOptimal = placeOutside || BoundsContainsWithTolerance(parentBounds, testBounds, containTolerance);
        
        if (containsOptimal)
        {
            List<GameObject> overlapping = solver.Search(testBounds);
            if (excludeFromOverlap != null && overlapping != null)
            {
                overlapping = new List<GameObject>(overlapping);
                overlapping.RemoveAll(o => o == excludeFromOverlap);
            }
            if (overlapping == null || overlapping.Count == 0)
            {
                return testBounds;
            }
            Debug.Log($"[SpatialGenerator] FindSpaceInParentBounds - '{node.name}' optimal size overlaps {overlapping.Count} object(s); trying min size");
        }
        else
        {
            if (verbosePlacementLogging)
                Debug.Log($"[SpatialGenerator] FindSpaceInParentBounds - '{node.name}' optimal size doesn't fit in parent bounds");
        }
        
        // If it doesn't fit or overlaps, try with min size
        boundsSize = minSize;
        testBounds = new Bounds(boundsCenter, boundsSize);
        bool containsMin = placeOutside || BoundsContainsWithTolerance(parentBounds, testBounds, containTolerance);
        if (containsMin)
        {
            List<GameObject> overlapping = solver.Search(testBounds);
            if (excludeFromOverlap != null && overlapping != null)
            {
                overlapping = new List<GameObject>(overlapping);
                overlapping.RemoveAll(o => o == excludeFromOverlap);
            }
            if (overlapping == null || overlapping.Count == 0)
            {
                return testBounds;
            }
            Debug.LogWarning($"[SpatialGenerator] FindSpaceInParentBounds - '{node.name}' min size also overlaps {overlapping.Count} object(s); no space");
        }
        else
        {
            Debug.LogWarning($"[SpatialGenerator] FindSpaceInParentBounds - '{node.name}' min size doesn't fit in parent bounds");
        }
        
        return null;
    }
    
    /// <summary>Place a container node (no prefabs) as an empty GameObject so children have a parent instance to use. Records instance and inserts logical bounds into the tree.</summary>
    private void PlaceContainerNode(SGBehaviorTreeNode node, Bounds placementBounds, int? parentInstanceIndex = null)
    {
        Bounds worldBounds = LocalToWorldBounds(placementBounds);
        Transform parentTransform = sceneTreeParent;
        GameObject parentSceneObj = GetParentInstanceForChild(node, parentInstanceIndex);
        if (parentSceneObj != null)
            parentTransform = parentSceneObj.transform;
        GameObject instance = new GameObject(node.name + "_container");
        instance.transform.SetParent(parentTransform, false);
        instance.transform.position = worldBounds.center;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        if (behaviorNodeToSceneInstance != null)
        {
            if (!behaviorNodeToSceneInstance.TryGetValue(node, out List<GameObject> list) || list == null)
            {
                list = new List<GameObject>();
                behaviorNodeToSceneInstance[node] = list;
            }
            list.Add(instance);
        }
        Bounds worldInstanceBounds = GetLogicalPlacementBounds(instance, node);
        Bounds localInstanceBounds = WorldToLocalBounds(worldInstanceBounds);
        treeSolver.Insert(localInstanceBounds, node, instance);
    }
    
    private void PlaceNodeObjects(SGBehaviorTreeNode node, Bounds placementBounds, int? parentInstanceIndex = null)
    {
        if (node.gameObjectPrefabs == null || node.gameObjectPrefabs.Count == 0)
        {
            Debug.LogWarning($"[SpatialGenerator] PlaceNodeObjects - Node '{node.name}' has no prefabs assigned; skipping placement. Assign at least one prefab in the Inspector.");
            return;
        }
        
        Bounds worldBounds = LocalToWorldBounds(placementBounds);
        int prefabIndex = rng.Next(node.gameObjectPrefabs.Count);
        GameObject prefab = node.gameObjectPrefabs[prefabIndex];
        if (prefab == null)
        {
            Debug.LogWarning($"[SpatialGenerator] PlaceNodeObjects - Node '{node.name}' prefab at index {prefabIndex} is null (missing reference); skipping placement.");
            return;
        }
        
        // Parent under the correct scene instance (use parentInstanceIndex when placing one per parent)
        Transform parentTransform = sceneTreeParent;
        GameObject parentSceneObj = GetParentInstanceForChild(node, parentInstanceIndex);
        if (parentSceneObj != null)
            parentTransform = parentSceneObj.transform;
        
        // Instantiate object under the correct parent
        GameObject instance = Instantiate(prefab, parentTransform);
        instance.transform.position = worldBounds.center;
        
        // Only touch scale when stretch mode is specified (stretchPieces); otherwise leave prefab scale untouched
        bool hasStretchMode = node.stretchPieces != null && node.stretchPieces.Count > 0;
        if (hasStretchMode)
        {
            Vector3 parentLossy = parentTransform.lossyScale;
            Vector3 prefabScale = prefab.transform.localScale;
            instance.transform.localScale = new Vector3(
                prefabScale.x / (parentLossy.x != 0f ? parentLossy.x : 1f),
                prefabScale.y / (parentLossy.y != 0f ? parentLossy.y : 1f),
                prefabScale.z / (parentLossy.z != 0f ? parentLossy.z : 1f)
            );
        }
        
        // Record this instance so child nodes can parent under the correct instance (list order = placement order)
        if (behaviorNodeToSceneInstance != null)
        {
            if (!behaviorNodeToSceneInstance.TryGetValue(node, out List<GameObject> list) || list == null)
            {
                list = new List<GameObject>();
                behaviorNodeToSceneInstance[node] = list;
            }
            list.Add(instance);
        }
        
        // Get rotation based on alignment direction, or use default rotation preference
        // Combine with prefab's original rotation to preserve baked rotations
        Vector3 rotationEuler = node.GetRotationForDirection(node.GetAlignmentFromFit());
        instance.transform.rotation = Quaternion.Euler(rotationEuler) * prefab.transform.rotation;
        
        // Debug: Draw gizmo showing placement bounds before alignment
        #if UNITY_EDITOR
        Debug.DrawLine(worldBounds.center - Vector3.up * 0.5f, worldBounds.center + Vector3.up * 0.5f, Color.cyan, 5f);
        Debug.DrawLine(worldBounds.center - Vector3.right * 0.5f, worldBounds.center + Vector3.right * 0.5f, Color.cyan, 5f);
        Debug.DrawLine(worldBounds.center - Vector3.forward * 0.5f, worldBounds.center + Vector3.forward * 0.5f, Color.cyan, 5f);
        #endif
        
        // Apply alignment (this may change the position)
        ApplyAlignment(node, instance, worldBounds);
        
        // Debug: Draw line from bounds center to final position
        #if UNITY_EDITOR
        Debug.DrawLine(worldBounds.center, instance.transform.position, Color.magenta, 5f);
        #endif
        
        // Insert the placement bounds (slot we reserved), not the instance's actual bounds, so the spatial tree
        // doesn't mark the whole area occupied and the next root placement gets a distinct slot (avoids 10 rooms stacking).
        Bounds localPlacementBounds = WorldToLocalBounds(worldBounds);
        treeSolver.Insert(localPlacementBounds, node, instance);
    }
    
    private void ApplyAlignment(SGBehaviorTreeNode node, GameObject obj, Bounds bounds)
    {
        Vector3 position = obj.transform.position;
        Bounds objBounds = GetGameObjectBounds(obj);
        Vector3 size = objBounds.size;
        
        // If object size is default (1,1,1), it might not have a renderer/collider
        // Try to use the node's optimal space as fallback (clamp so zero optimalSpace doesn't break alignment)
        if (size == Vector3.one && (obj.GetComponent<Renderer>() == null && obj.GetComponent<Collider>() == null))
        {
            Debug.LogWarning($"[SpatialGenerator] ApplyAlignment - Object '{obj.name}' has no Renderer or Collider, using node optimalSpace as size estimate");
            size = Vector3.Scale(node.optimalSpace, obj.transform.lossyScale);
            size.x = Mathf.Max(size.x, MinSizeEpsilon);
            size.y = Mathf.Max(size.y, MinSizeEpsilon);
            size.z = Mathf.Max(size.z, MinSizeEpsilon);
        }

        if (verbosePlacementLogging)
            Debug.Log($"[SpatialGenerator] ApplyAlignment - Node: {node.name}, Object: {obj.name}\n" +
                      $"  Placement bounds center: {bounds.center}\n" +
                      $"  Placement bounds min: {bounds.min}\n" +
                      $"  Placement bounds max: {bounds.max}\n" +
                      $"  Placement bounds size: {bounds.size}\n" +
                      $"  Object bounds center: {objBounds.center}\n" +
                      $"  Object size (used): {size}\n" +
                      $"  Object transform position (before): {position}\n" +
                      $"  Object transform lossyScale: {obj.transform.lossyScale}\n" +
                      $"  SpatialGenerator world position: {transform.position}\n" +
                      $"  SpatialGenerator lossy scale: {transform.lossyScale}\n" +
                      $"  Alignment from fit: {node.GetAlignmentFromFit()}, Place flush: {node.placeFlush}");
        
        // Calculate offset coefficient
        // 0.0 = flush against edge, 1.0 = maximum offset from edge
        float offsetCoeff = alignmentOffsetCoefficient;
        
        bool isFlush = node.placeFlush;
        SGBehaviorTreeNode.AlignmentPreference align = node.GetAlignmentFromFit();
        switch (align)
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
                    // Object's back edge at bounds.max.z so wall sits just outside parent with back touching parent front
                    position.z = bounds.max.z + size.z * 0.5f;
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
        
        if (verbosePlacementLogging)
            Debug.Log($"[SpatialGenerator] ApplyAlignment - Final position after alignment: {position}");
        obj.transform.position = position;
    }
    
    /// <summary>Resets placement count on every behavior tree node so the next Generate() run starts with clean state.</summary>
    private void ResetAllNodePlacementCounts()
    {
        if (behaviorTreeParent == null) return;
        SGTreeNodeContainer container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
        if (container == null) return;
        SGBehaviorTreeNode root = container.GetRootNode();
        if (root == null) return;
        var queue = new Queue<SGBehaviorTreeNode>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            SGBehaviorTreeNode node = queue.Dequeue();
            if (node != null)
            {
                node.ResetPlacementCount();
                if (node.childNodes != null)
                {
                    foreach (var child in node.childNodes)
                        queue.Enqueue(child);
                }
            }
        }
    }
    
    private void ClearGeneration()
    {
        if (treeSolver != null)
        {
            treeSolver.Clear();
        }
        
        behaviorNodeToSceneInstance?.Clear();
        if (behaviorNodeToSceneInstance == null)
        {
            behaviorNodeToSceneInstance = new Dictionary<SGBehaviorTreeNode, List<GameObject>>();
        }
        
        placementCountPerParentInstance?.Clear();
        if (placementCountPerParentInstance == null)
        {
            placementCountPerParentInstance = new Dictionary<(SGBehaviorTreeNode node, int parentInstanceIndex), int>();
        }
        
        nextRootPlacementIndex = 0;
        nextSlotPerParentInstance?.Clear();
        if (nextSlotPerParentInstance == null)
            nextSlotPerParentInstance = new Dictionary<int, int>();
        
        // Reset placement counts on all behavior tree nodes so the next Generate() starts clean
        ResetAllNodePlacementCounts();
        
        // Clear scene tree (regeneration always replaces it)
        if (sceneTreeParent != null)
        {
            int childCount = sceneTreeParent.childCount;
            if (childCount > 0)
            {
                Debug.LogWarning($"[SpatialGenerator] Clearing {childCount} existing object(s) from SceneTree for regeneration. SceneTree is repopulated each time Generate() runs; do not rely on keeping objects under SceneTree.");
            }
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
        
        // Update generation bounds (use local space). Generate() will ClearGeneration() and repopulate, so no need to call UpdateTree() here.
        generationBounds = new Bounds(Vector3.zero, transform.localScale);
        
        // Re-evaluate empty space markers
        if (useEmptySpaceMarkers)
        {
            CollectEmptySpaceMarkers();
        }
        
        // Regenerate with same seed (clears tree and SceneTree, then refills)
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
    
    /// <summary>
    /// Returns world-space bounds for the logical placement of a parent (center from instance, size from node.optimalSpace in generator scale).
    /// Use this when finding space for children so parent bounds are always the reserved space, not the instance's actual Renderer/Collider bounds
    /// (which can be smaller and cause "optimal size doesn't fit" for rooms 2+).
    /// </summary>
    private Bounds GetLogicalPlacementBounds(GameObject obj, SGBehaviorTreeNode node)
    {
        Vector3 worldSize = Vector3.Scale(node.optimalSpace, transform.lossyScale);
        worldSize.x = Mathf.Max(worldSize.x, MinSizeEpsilon);
        worldSize.y = Mathf.Max(worldSize.y, MinSizeEpsilon);
        worldSize.z = Mathf.Max(worldSize.z, MinSizeEpsilon);
        return new Bounds(obj.transform.position, worldSize);
    }

    /// <summary>
    /// Returns world-space bounds for a placed instance. When the object has no Renderer/Collider,
    /// uses the node's optimalSpace scaled by the instance's lossyScale so parent bounds and
    /// tree insertion use the correct logical size (avoids 1x1x1 causing "min size doesn't fit"
    /// and all children falling back to the same root center).
    /// </summary>
    private Bounds GetPlacedInstanceBounds(GameObject obj, SGBehaviorTreeNode node)
    {
        Bounds b = GetGameObjectBounds(obj);
        if (b.size == Vector3.one && obj.GetComponent<Renderer>() == null && obj.GetComponent<Collider>() == null && obj.GetComponent<RectTransform>() == null)
        {
            Vector3 worldSize = Vector3.Scale(node.optimalSpace, obj.transform.lossyScale);
            // Avoid zero size (e.g. spotlight with 0 optimalSpace) so parent bounds and tree insertion use a valid footprint
            worldSize.x = Mathf.Max(worldSize.x, MinSizeEpsilon);
            worldSize.y = Mathf.Max(worldSize.y, MinSizeEpsilon);
            worldSize.z = Mathf.Max(worldSize.z, MinSizeEpsilon);
            b = new Bounds(obj.transform.position, worldSize);
        }
        return b;
    }
    
    /// <summary>Returns true if outer contains inner, with a small tolerance to avoid floating-point false negatives.</summary>
    private static bool BoundsContainsWithTolerance(Bounds outer, Bounds inner, float tolerance)
    {
        Vector3 innerMin = inner.min;
        Vector3 innerMax = inner.max;
        return innerMin.x >= outer.min.x - tolerance && innerMax.x <= outer.max.x + tolerance
            && innerMin.y >= outer.min.y - tolerance && innerMax.y <= outer.max.y + tolerance
            && innerMin.z >= outer.min.z - tolerance && innerMax.z <= outer.max.z + tolerance;
    }
    
    /// <summary>
    /// Convert world space bounds to local space relative to SpatialGenerator transform
    /// </summary>
    private Bounds WorldToLocalBounds(Bounds worldBounds)
    {
        // Convert center from world to local space
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        
        // Convert size from world to local (divide by lossyScale)
        Vector3 localSize = new Vector3(
            worldBounds.size.x / transform.lossyScale.x,
            worldBounds.size.y / transform.lossyScale.y,
            worldBounds.size.z / transform.lossyScale.z
        );
        
        return new Bounds(localCenter, localSize);
    }
    
    /// <summary>
    /// Convert local space bounds to world space relative to SpatialGenerator transform
    /// </summary>
    private Bounds LocalToWorldBounds(Bounds localBounds)
    {
        // Convert center from local to world space
        Vector3 worldCenter = transform.TransformPoint(localBounds.center);
        
        // Convert size from local to world (multiply by lossyScale)
        Vector3 worldSize = Vector3.Scale(localBounds.size, transform.lossyScale);
        
        return new Bounds(worldCenter, worldSize);
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
                        
                        // Root must have at least one non-null prefab or generation will produce 0 objects
                        bool rootHasValidPrefab = rootNode.gameObjectPrefabs != null && rootNode.gameObjectPrefabs.Count > 0;
                        if (rootHasValidPrefab)
                        {
                            rootHasValidPrefab = false;
                            for (int i = 0; i < rootNode.gameObjectPrefabs.Count; i++)
                            {
                                if (rootNode.gameObjectPrefabs[i] != null) { rootHasValidPrefab = true; break; }
                            }
                        }
                        if (!rootHasValidPrefab)
                        {
                            errors.Add("Root node has no prefabs assigned (or all prefab references are null). Assign at least one prefab to the root node so objects can be generated.");
                            results.AppendLine("ERROR: Root node has no valid prefabs; generation would produce 0 objects.");
                        }
                        
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
            
            // Terrain / height map check (OctTreeSolver)
            results.AppendLine("--- Terrain / Height Map Check ---");
            if (mode == GenerationMode.ThreeDimensional)
            {
                SGOctTreeSolver octSolver = GetComponent<SGOctTreeSolver>();
                if (octSolver != null)
                {
                    int terrainCount = octSolver.heightMapTerrains != null ? octSolver.heightMapTerrains.Count : 0;
                    int validTerrainCount = 0;
                    if (octSolver.heightMapTerrains != null)
                    {
                        foreach (var t in octSolver.heightMapTerrains)
                            if (t != null && t.terrainData != null) validTerrainCount++;
                    }
                    results.AppendLine($"OctTreeSolver height map terrains: {terrainCount} (valid: {validTerrainCount})");
                    results.AppendLine($"Use raycast for terrain: {octSolver.useRaycastForTerrain}, sample count: {octSolver.terrainCheckSampleCount}");
                    if (validTerrainCount > 0)
                    {
                        Terrain firstTerrain = null;
                        for (int ti = 0; ti < octSolver.heightMapTerrains.Count; ti++)
                        {
                            if (octSolver.heightMapTerrains[ti] != null && octSolver.heightMapTerrains[ti].terrainData != null)
                            { firstTerrain = octSolver.heightMapTerrains[ti]; break; }
                        }
                        Bounds terrainWorldBounds = firstTerrain.terrainData.bounds;
                        terrainWorldBounds.center = firstTerrain.transform.position + terrainWorldBounds.center;
                        Bounds localAtTerrain = octSolver.WorldBoundsToLocal(terrainWorldBounds);
                        bool conflictsAtTerrain = octSolver.WouldBoundsConflictWithTerrain(localAtTerrain);
                        Bounds worldAboveTerrain = new Bounds(terrainWorldBounds.center + Vector3.up * 1000f, terrainWorldBounds.size);
                        Bounds localAbove = octSolver.WorldBoundsToLocal(worldAboveTerrain);
                        bool conflictsAbove = octSolver.WouldBoundsConflictWithTerrain(localAbove);
                        if (conflictsAtTerrain && !conflictsAbove)
                        {
                            results.AppendLine("✓ Terrain conflict check: bounds at terrain height conflict; bounds above terrain do not.");
                        }
                        else if (!conflictsAtTerrain)
                        {
                            warnings.Add("Terrain conflict test: bounds at terrain height did not report conflict (check solver transform vs terrain position).");
                            results.AppendLine("WARNING: Bounds at terrain height did not report conflict.");
                        }
                        else if (conflictsAbove)
                        {
                            warnings.Add("Terrain conflict test: bounds far above terrain reported conflict (sample/raycast may be too broad).");
                            results.AppendLine("WARNING: Bounds above terrain reported conflict.");
                        }
                    }
                    else if (terrainCount > 0)
                    {
                        results.AppendLine("WARNING: Height map list has entries but no valid terrain (null or missing terrainData).");
                    }
                }
            }
            else
            {
                results.AppendLine("(Terrain check applies to ThreeDimensional mode only.)");
            }
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
        
        DrawPlacementSlotsGizmo();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw more prominently when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, generationSize);
        DrawPlacementSlotsGizmo();
    }
    
    /// <summary>Draws placement slot positions using the solver's step/grid logic (respects min/max/optimal limits).</summary>
    private void DrawPlacementSlotsGizmo()
    {
        if (!showPlacementSlots) return;
        
        Bounds localBounds = new Bounds(Vector3.zero, generationSize);
        Vector3 minSpace = gizmoPlacementSlotSize;
        Vector3 maxSpace = gizmoPlacementSlotSize;
        Vector3 optimalSpace = gizmoPlacementSlotSize;
        
        if (behaviorTreeParent != null)
        {
            var container = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
            if (container != null)
            {
                var root = container.GetRootNode();
                if (root != null)
                {
                    minSpace = root.minSpace;
                    maxSpace = root.maxSpace;
                    optimalSpace = root.optimalSpace;
                }
            }
        }
        SGBehaviorTreeNode rootForGizmo = null;
        if (behaviorTreeParent != null)
        {
            var containerForGizmo = behaviorTreeParent.GetComponent<SGTreeNodeContainer>();
            if (containerForGizmo != null)
                rootForGizmo = containerForGizmo.GetRootNode();
        }
        PlacementSlotConfig? gizmoConfig = rootForGizmo != null ? PlacementSlotConfig.FromNode(rootForGizmo) : (PlacementSlotConfig?)null;
        List<Bounds> slots = null;
        if (mode == GenerationMode.ThreeDimensional)
        {
            var octSolver = GetComponent<SGOctTreeSolver>();
            if (octSolver != null)
                slots = octSolver.GetPlacementSlotsForGizmo(localBounds, minSpace, maxSpace, optimalSpace, 64, gizmoConfig);
        }
        else
        {
            var quadSolver = GetComponent<SGQuadTreeSolver>();
            if (quadSolver != null)
                slots = quadSolver.GetPlacementSlotsForGizmo(localBounds, minSpace, maxSpace, optimalSpace, 64, gizmoConfig);
        }
        
        if (slots == null || slots.Count == 0) return;
        
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f); // Cyan-green
        foreach (Bounds b in slots)
        {
            Vector3 worldCenter = transform.TransformPoint(b.center);
            Vector3 worldSize = Vector3.Scale(b.size, transform.lossyScale);
            Gizmos.DrawWireCube(worldCenter, worldSize);
        }
    }
    #endif
}
