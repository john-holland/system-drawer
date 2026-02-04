using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Behavior tree node for spatial generation grammar
// Defines placement rules for objects in the generated scene
public class SGBehaviorTreeNode : MonoBehaviour
{
    public enum AlignmentPreference
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Backward,
        Center
    }
    
    public enum PlacementLimitType
    {
        Min,
        Max,
        Specific
    }

    public enum FitX { Center, Left, Right }
    public enum FitY { Center, Down, Up }
    public enum FitZ { Center, Backward, Forward }
    public enum AxisDirection { PosX, NegX, PosY, NegY, PosZ, NegZ }
    public enum PlaceSearchMode { CenterFirst = 0, GridFromMin = 1, GridFromMax = 2, FromFit = 3, Random = 4, SlotIndexOnly = 5 }
    public enum PlacementMode { In, Left, Right, Forward, Down, Under, Up }
    
    [Header("Node Configuration")]
    [Tooltip("When true, placement limit is applied per parent instance (e.g. 1 wall of each type per room). When false, limit is global across all parents.")]
    public bool perParentPlacementLimits = false;
    public bool isEnabled = true;
    public PlacementLimitType placementLimitType = PlacementLimitType.Specific;
    [Tooltip("Max instances to place. For multiple rooms/containers, set > 1 (e.g. 8). Default 1 = only one instance.")]
    public int placementLimit = 1;
    public int placementMin = 0;
    public int placementMax = 10;
    
    [Header("Space Requirements")]
    public Vector3 minSpace = Vector3.one;
    public Vector3 maxSpace = Vector3.one * 2f;
    public Vector3 optimalSpace = Vector3.one * 1.5f;
    
    [Header("Fit / Stack")]
    [Tooltip("Where to anchor the placement grid on the X axis (center, left/min, right/max).")]
    public FitX fitX = FitX.Center;
    [Tooltip("Where to anchor the placement grid on the Y axis (center, down/min, up/max).")]
    public FitY fitY = FitY.Center;
    [Tooltip("Where to anchor the placement grid on the Z axis (center, backward/min, forward/max).")]
    public FitZ fitZ = FitZ.Center;
    [Tooltip("Axis and sign incremented first when filling slots (e.g. PosX = fill along +X first).")]
    public AxisDirection stackDirection = AxisDirection.PosX;
    [Tooltip("Axis and sign incremented when stack direction is exhausted (e.g. PosZ = next row along +Z).")]
    public AxisDirection wrapDirection = AxisDirection.PosZ;
    [Tooltip("Place object flush against bounds edge (no offset). When false, alignmentOffsetCoefficient on SpatialGenerator is used.")]
    public bool placeFlush = false;
    [Tooltip("How to search for a placement slot: CenterFirst = try center then grid; GridFromMin/Max = scan from min/max; FromFit = use fit anchor then stack/wrap; Random = random slot; SlotIndexOnly = use placement index only.")]
    public PlaceSearchMode placeSearchMode = PlaceSearchMode.CenterFirst;
    [Tooltip("Where to place this node's child nodes: In = no translate (center). Left/Right/Forward/Down/Under/Up = outside that face of this node's bounds (child center past min/max).")]
    public PlacementMode placementMode = PlacementMode.In;
    
    [Header("Rotation")]
    public bool allowRotation = true;
    public Vector3 rotationPreference = Vector3.zero;
    public Dictionary<AlignmentPreference, Vector3> rotationByDirection = new Dictionary<AlignmentPreference, Vector3>();
    
    [Header("GameObjects")]
    [Tooltip("Optional key for stylesheet lookup. When set, stylesheet entries match this instead of node name.")]
    public string skinKey = "";
    public List<GameObject> gameObjectPrefabs = new List<GameObject>();
    
    [Header("Stretch Objects")]
    public List<StretchObject> stretchPieces = new List<StretchObject>();
    
    [Header("Adjacency Rules")]
    public List<GameObject> requiredAdjacentObjects = new List<GameObject>();
    public List<GameObject> bannedAdjacentObjects = new List<GameObject>();
    public int maxBannedAdjacentCount = 0; // If exceeded, object won't spawn
    
    [Header("Child Nodes")]
    public List<SGBehaviorTreeNode> childNodes = new List<SGBehaviorTreeNode>();
    
    // Runtime tracking (exposed read-only in custom editor as "Placed (runtime)")
    private int currentPlacementCount = 0;
    
    void Start()
    {
        // Initialize child nodes from children
        if (childNodes.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                SGBehaviorTreeNode child = transform.GetChild(i).GetComponent<SGBehaviorTreeNode>();
                if (child != null)
                {
                    childNodes.Add(child);
                }
            }
        }
    }
    
    public bool CanPlace()
    {
        if (!isEnabled)
        {
            return false;
        }
        
        // Check placement limits
        switch (placementLimitType)
        {
            case PlacementLimitType.Specific:
                if (currentPlacementCount >= placementLimit)
                {
                    return false;
                }
                break;
            case PlacementLimitType.Min:
                // Can place if under min
                if (currentPlacementCount >= placementMax)
                {
                    return false;
                }
                break;
            case PlacementLimitType.Max:
                if (currentPlacementCount >= placementMax)
                {
                    return false;
                }
                break;
        }
        
        return true;
    }
    
    public void IncrementPlacementCount()
    {
        currentPlacementCount++;
    }
    
    /// <summary>Number of times this node has been placed so far (used to distribute children across multiple parent instances).</summary>
    public int GetPlacementCount()
    {
        return currentPlacementCount;
    }
    
    public void ResetPlacementCount()
    {
        currentPlacementCount = 0;
    }
    
    public bool HasReachedLimit()
    {
        switch (placementLimitType)
        {
            case PlacementLimitType.Specific:
                return currentPlacementCount >= placementLimit;
            case PlacementLimitType.Max:
                return currentPlacementCount >= placementMax;
            case PlacementLimitType.Min:
                return currentPlacementCount >= placementMax;
            default:
                return false;
        }
    }
    
    /// <summary>Effective placement limit value (max count) for the current limit type. Used when perParentPlacementLimits is true to cap per-parent count.</summary>
    public int GetPlacementLimitValue()
    {
        switch (placementLimitType)
        {
            case PlacementLimitType.Specific:
                return placementLimit;
            case PlacementLimitType.Max:
            case PlacementLimitType.Min:
                return placementMax;
            default:
                return placementLimit;
        }
    }
    
    /// <summary>Derives alignment direction from fit X/Y/Z (first non-center wins: X then Y then Z). Used for placement position and rotation.</summary>
    public AlignmentPreference GetAlignmentFromFit()
    {
        if (fitX != FitX.Center) return fitX == FitX.Left ? AlignmentPreference.Left : AlignmentPreference.Right;
        if (fitY != FitY.Center) return fitY == FitY.Down ? AlignmentPreference.Down : AlignmentPreference.Up;
        if (fitZ != FitZ.Center) return fitZ == FitZ.Backward ? AlignmentPreference.Backward : AlignmentPreference.Forward;
        return AlignmentPreference.Center;
    }

    public Vector3 GetRotationForDirection(AlignmentPreference direction)
    {
        if (rotationByDirection.ContainsKey(direction))
        {
            return rotationByDirection[direction];
        }
        return rotationPreference;
    }
    
    #if UNITY_EDITOR
    [Header("Editor Visualization")]
    public bool showGizmos = true;
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !isEnabled)
        {
            return;
        }
        
        Vector3 position = transform.position;
        
        // Draw wireframe boxes for space requirements
        // Min space: red
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireCube(position, minSpace);
        
        // Optimal space: yellow
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(position, optimalSpace);
        
        // Max space: green
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(position, maxSpace);
        
        // Draw alignment indicator
        DrawAlignmentIndicator(position);
        
        // Draw placement highlight (lighter when not selected)
        #if UNITY_EDITOR
        Bounds parentBounds = GetParentBounds();
        Vector3 objectSize = optimalSpace;
        if (objectSize == Vector3.zero)
        {
            objectSize = (minSpace + maxSpace) * 0.5f;
        }
        SpatialGenerator spatialGen = GetComponentInParent<SpatialGenerator>();
        float offsetCoeff = spatialGen != null ? spatialGen.alignmentOffsetCoefficient : 0.5f;
        Vector3 placementPos = CalculatePlacementPosition(parentBounds, objectSize, offsetCoeff);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Lighter yellow when not selected
        Gizmos.DrawCube(placementPos, objectSize);
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireCube(placementPos, objectSize);
        #endif
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos || !isEnabled)
        {
            return;
        }
        
        Vector3 position = transform.position;
        
        // Draw more prominently when selected
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(position, minSpace);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(position, optimalSpace);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(position, maxSpace);
        
        // Draw alignment indicator
        DrawAlignmentIndicator(position);
        
        // Draw placement highlight gizmo
        DrawPlacementHighlight();
    }
    
    /// <summary>
    /// Draw a filled yellow square showing where the object will be placed on parent bounds using fit X/Y/Z and place-flush.
    /// </summary>
    private void DrawPlacementHighlight()
    {
        #if UNITY_EDITOR
        // Get parent bounds
        Bounds parentBounds = GetParentBounds();
        
        // Use optimal space as object size (or average of min/max)
        // Convert to world space if needed
        Vector3 objectSize = optimalSpace;
        if (objectSize == Vector3.zero)
        {
            objectSize = (minSpace + maxSpace) * 0.5f;
        }
        
        // Convert object size to world space (account for scale)
        objectSize = Vector3.Scale(objectSize, transform.lossyScale);
        
        // Calculate placement position
        SpatialGenerator spatialGen = GetComponentInParent<SpatialGenerator>();
        float offsetCoeff = spatialGen != null ? spatialGen.alignmentOffsetCoefficient : 0.5f;
        Vector3 placementPos = CalculatePlacementPosition(parentBounds, objectSize, offsetCoeff);
        
        // Draw parent bounds outline for debugging (cyan wireframe)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan with transparency
        Gizmos.DrawWireCube(parentBounds.center, parentBounds.size);
        
        // Draw filled yellow square at placement position
        // Use Gizmos.DrawCube for filled shape
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow with transparency
        Gizmos.DrawCube(placementPos, objectSize);
        
        // Draw outline in brighter yellow
        Gizmos.color = new Color(1f, 1f, 0f, 1f); // Solid yellow
        Gizmos.DrawWireCube(placementPos, objectSize);
        
        // Draw a line from parent bounds center to placement position for clarity
        Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
        Gizmos.DrawLine(parentBounds.center, placementPos);
        #endif
    }
    
    private void DrawAlignmentIndicator(Vector3 position)
    {
        Gizmos.color = Color.white;
        Vector3 direction = Vector3.zero;
        float arrowLength = 0.5f;
        AlignmentPreference align = GetAlignmentFromFit();
        switch (align)
        {
            case AlignmentPreference.Up:
                direction = Vector3.up;
                break;
            case AlignmentPreference.Down:
                direction = Vector3.down;
                break;
            case AlignmentPreference.Left:
                direction = Vector3.left;
                break;
            case AlignmentPreference.Right:
                direction = Vector3.right;
                break;
            case AlignmentPreference.Forward:
                direction = Vector3.forward;
                break;
            case AlignmentPreference.Backward:
                direction = Vector3.back;
                break;
            case AlignmentPreference.Center:
                // Draw center indicator
                Gizmos.DrawSphere(position, 0.1f);
                return;
        }
        
        if (direction != Vector3.zero)
        {
            // Draw arrow
            Gizmos.DrawLine(position, position + direction * arrowLength);
            // Draw arrow head (simple triangle)
            Gizmos.DrawLine(position + direction * arrowLength, position + direction * arrowLength * 0.8f + Vector3.Cross(direction, Vector3.up) * 0.1f);
            Gizmos.DrawLine(position + direction * arrowLength, position + direction * arrowLength * 0.8f - Vector3.Cross(direction, Vector3.up) * 0.1f);
        }
    }
    
    /// <summary>
    /// Calculate where the object would be placed based on fit X/Y/Z and place-flush setting.
    /// Matches PlacementSlotConfig slot-zero anchor and SpatialGenerator.ApplyAlignment per-axis.
    /// </summary>
    private Vector3 CalculatePlacementPosition(Bounds parentBounds, Vector3 objectSize, float alignmentOffsetCoefficient = 0.5f)
    {
        float offsetCoeff = alignmentOffsetCoefficient;
        bool isFlush = placeFlush;
        float halfX = objectSize.x * 0.5f;
        float halfY = objectSize.y * 0.5f;
        float halfZ = objectSize.z * 0.5f;
        float offX = isFlush ? halfX : objectSize.x * (0.5f + offsetCoeff * 0.5f);
        float offY = isFlush ? halfY : objectSize.y * (0.5f + offsetCoeff * 0.5f);
        float offZ = isFlush ? halfZ : objectSize.z * (0.5f + offsetCoeff * 0.5f);
        
        float x = parentBounds.center.x;
        switch (fitX)
        {
            case FitX.Left:  x = parentBounds.min.x + offX; break;
            case FitX.Right: x = parentBounds.max.x - offX; break;
        }
        float y = parentBounds.center.y;
        switch (fitY)
        {
            case FitY.Down: y = parentBounds.min.y + offY; break;
            case FitY.Up:   y = parentBounds.max.y - offY; break;
        }
        float z = parentBounds.center.z;
        switch (fitZ)
        {
            case FitZ.Backward: z = parentBounds.min.z + offZ; break;
            case FitZ.Forward:   z = parentBounds.max.z - offZ; break;
        }
        
        return new Vector3(x, y, z);
    }
    
    /// <summary>
    /// Get parent bounds for alignment calculation.
    /// Tries to simulate what the solver would return, or uses optimalSpace as fallback.
    /// </summary>
    private Bounds GetParentBounds()
    {
        // Try to get from SpatialGenerator first (most accurate)
        SpatialGenerator spatialGen = GetComponentInParent<SpatialGenerator>();
        if (spatialGen != null)
        {
            // The solver works in local space relative to SpatialGenerator
            // For gizmo visualization, we'll use the generation bounds as the parent bounds
            // This simulates what would be returned for a root-level node
            Vector3 worldSize = Vector3.Scale(spatialGen.generationSize, spatialGen.transform.lossyScale);
            Bounds genBounds = new Bounds(spatialGen.transform.position, worldSize);
            
            // For child nodes, try to get from parent node
            Transform parent = transform.parent;
            if (parent != null)
            {
                SGBehaviorTreeNode parentNode = parent.GetComponent<SGBehaviorTreeNode>();
                if (parentNode != null)
                {
                    // Use parent's optimal space as bounds, centered at parent position
                    Vector3 parentWorldSize = Vector3.Scale(parentNode.optimalSpace, parent.lossyScale);
                    return new Bounds(parent.position, parentWorldSize);
                }
            }
            
            return genBounds;
        }
        
        // Try to get bounds from parent SGBehaviorTreeNode
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            SGBehaviorTreeNode parentNode = parentTransform.GetComponent<SGBehaviorTreeNode>();
            if (parentNode != null)
            {
                // Use parent's optimal space as bounds
                Vector3 parentWorldSize = Vector3.Scale(parentNode.optimalSpace, parentTransform.lossyScale);
                return new Bounds(parentTransform.position, parentWorldSize);
            }
        }
        
        // Fallback: use optimalSpace centered at this position
        Vector3 worldSizeFallback = Vector3.Scale(optimalSpace, transform.lossyScale);
        return new Bounds(transform.position, worldSizeFallback);
    }
    #endif
}
