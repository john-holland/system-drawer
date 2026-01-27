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
    
    [Header("Node Configuration")]
    public bool isEnabled = true;
    public PlacementLimitType placementLimitType = PlacementLimitType.Specific;
    public int placementLimit = 1;
    public int placementMin = 0;
    public int placementMax = 10;
    
    [Header("Space Requirements")]
    public Vector3 minSpace = Vector3.one;
    public Vector3 maxSpace = Vector3.one * 2f;
    public Vector3 optimalSpace = Vector3.one * 1.5f;
    
    [Header("Alignment")]
    public AlignmentPreference alignPreference = AlignmentPreference.Center;
    public AlignmentPreference placeSearchMode = AlignmentPreference.Center; // Opposite to align
    public bool placeFlush = false; // Place object flush against bounds edge
    
    [Header("Rotation")]
    public bool allowRotation = true;
    public Vector3 rotationPreference = Vector3.zero;
    public Dictionary<AlignmentPreference, Vector3> rotationByDirection = new Dictionary<AlignmentPreference, Vector3>();
    
    [Header("GameObjects")]
    public List<GameObject> gameObjectPrefabs = new List<GameObject>();
    
    [Header("Stretch Objects")]
    public List<StretchObject> stretchPieces = new List<StretchObject>();
    
    [Header("Adjacency Rules")]
    public List<GameObject> requiredAdjacentObjects = new List<GameObject>();
    public List<GameObject> bannedAdjacentObjects = new List<GameObject>();
    public int maxBannedAdjacentCount = 0; // If exceeded, object won't spawn
    
    [Header("Child Nodes")]
    public List<SGBehaviorTreeNode> childNodes = new List<SGBehaviorTreeNode>();
    
    // Runtime tracking
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
    /// Draw a filled yellow square showing where the object will align on parent bounds.
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
        
        switch (alignPreference)
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
    /// Calculate where the object would be placed based on alignment preference and flush setting.
    /// Uses the same logic as SpatialGenerator.ApplyAlignment.
    /// </summary>
    private Vector3 CalculatePlacementPosition(Bounds parentBounds, Vector3 objectSize, float alignmentOffsetCoefficient = 0.5f)
    {
        Vector3 position = parentBounds.center;
        float offsetCoeff = alignmentOffsetCoefficient;
        bool isFlush = placeFlush;
        
        switch (alignPreference)
        {
            case AlignmentPreference.Up:
                if (isFlush)
                {
                    position.y = parentBounds.max.y - objectSize.y * 0.5f;
                }
                else
                {
                    position.y = parentBounds.max.y - objectSize.y * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Down:
                if (isFlush)
                {
                    position.y = parentBounds.min.y + objectSize.y * 0.5f;
                }
                else
                {
                    position.y = parentBounds.min.y + objectSize.y * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Left:
                if (isFlush)
                {
                    position.x = parentBounds.min.x + objectSize.x * 0.5f;
                }
                else
                {
                    position.x = parentBounds.min.x + objectSize.x * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Right:
                if (isFlush)
                {
                    position.x = parentBounds.max.x - objectSize.x * 0.5f;
                }
                else
                {
                    position.x = parentBounds.max.x - objectSize.x * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Forward:
                if (isFlush)
                {
                    position.z = parentBounds.max.z - objectSize.z * 0.5f;
                }
                else
                {
                    position.z = parentBounds.max.z - objectSize.z * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Backward:
                if (isFlush)
                {
                    position.z = parentBounds.min.z + objectSize.z * 0.5f;
                }
                else
                {
                    position.z = parentBounds.min.z + objectSize.z * (0.5f + offsetCoeff * 0.5f);
                }
                break;
            case AlignmentPreference.Center:
                position = parentBounds.center;
                break;
        }
        
        return position;
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
