using System.Collections.Generic;
using UnityEngine;

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
    #endif
}
