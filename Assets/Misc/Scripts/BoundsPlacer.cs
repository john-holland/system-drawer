using UnityEngine;

/// <summary>
/// Component for placing an object on one side of a Bounds
/// </summary>
public class BoundsPlacer : MonoBehaviour
{
    public enum BoundsSide
    {
        Left,    // min.x
        Right,   // max.x
        Bottom,  // min.y
        Top,     // max.y
        Back,    // min.z
        Front    // max.z
    }
    
    [Header("Target Bounds")]
    [Tooltip("The bounds to align against. If null, will use bounds from a Bounds component or Renderer/Collider on this GameObject.")]
    public Bounds? targetBounds;
    
    [Tooltip("GameObject whose bounds to use as target (if targetBounds is not set)")]
    public GameObject targetBoundsObject;
    
    [Header("Placement Settings")]
    [Tooltip("Which side of the bounds to place on")]
    public BoundsSide side = BoundsSide.Right;
    
    [Tooltip("Offset from the edge (positive = away from bounds, negative = into bounds)")]
    public float offset = 0f;
    
    [Tooltip("If true, object's edge aligns flush with bounds edge (offset ignored)")]
    public bool flush = false;
    
    [Header("Auto Update")]
    [Tooltip("Automatically update position when values change in editor")]
    public bool autoUpdate = true;
    
    [Tooltip("Update position on Start")]
    public bool updateOnStart = true;
    
    void Start()
    {
        if (updateOnStart)
        {
            PlaceOnBounds();
        }
    }
    
    /// <summary>
    /// Place this object on the specified side of the target bounds
    /// </summary>
    public void PlaceOnBounds()
    {
        Bounds? bounds = GetTargetBounds();
        if (!bounds.HasValue)
        {
            Debug.LogWarning("BoundsPlacer: No target bounds available", this);
            return;
        }
        
        PlaceOnBoundsSide(gameObject, bounds.Value, side, offset, flush);
    }
    
    /// <summary>
    /// Static method to place an object on one side of bounds
    /// </summary>
    public static void PlaceOnBoundsSide(GameObject obj, Bounds bounds, BoundsSide side, float offset = 0f, bool flush = false)
    {
        if (obj == null) return;
        
        Bounds objBounds = GetObjectBounds(obj);
        Vector3 objSize = objBounds.size;
        Vector3 position = obj.transform.position;
        
        if (flush) offset = 0f;
        
        switch (side)
        {
            case BoundsSide.Left:
                position.x = bounds.min.x - (flush ? objSize.x * 0.5f : objSize.x * 0.5f + offset);
                break;
            case BoundsSide.Right:
                position.x = bounds.max.x + (flush ? objSize.x * 0.5f : objSize.x * 0.5f + offset);
                break;
            case BoundsSide.Bottom:
                position.y = bounds.min.y - (flush ? objSize.y * 0.5f : objSize.y * 0.5f + offset);
                break;
            case BoundsSide.Top:
                position.y = bounds.max.y + (flush ? objSize.y * 0.5f : objSize.y * 0.5f + offset);
                break;
            case BoundsSide.Back:
                position.z = bounds.min.z - (flush ? objSize.z * 0.5f : objSize.z * 0.5f + offset);
                break;
            case BoundsSide.Front:
                position.z = bounds.max.z + (flush ? objSize.z * 0.5f : objSize.z * 0.5f + offset);
                break;
        }
        
        obj.transform.position = position;
    }
    
    /// <summary>
    /// Get the target bounds from various sources
    /// </summary>
    private Bounds? GetTargetBounds()
    {
        // Use explicitly set bounds if available
        if (targetBounds.HasValue)
        {
            return targetBounds.Value;
        }
        
        // Try to get bounds from target object
        if (targetBoundsObject != null)
        {
            Bounds? bounds = GetObjectBoundsNullable(targetBoundsObject);
            if (bounds.HasValue)
            {
                return bounds.Value;
            }
        }
        
        // Try to get bounds from this object
        return GetObjectBoundsNullable(gameObject);
    }
    
    /// <summary>
    /// Helper to get object bounds (from Renderer, Collider, or default)
    /// </summary>
    private static Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds;
        
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) return collider.bounds;
        
        // Default: use transform scale
        return new Bounds(obj.transform.position, obj.transform.lossyScale);
    }
    
    /// <summary>
    /// Helper to get object bounds, returning null if not available
    /// </summary>
    private static Bounds? GetObjectBoundsNullable(GameObject obj)
    {
        if (obj == null) return null;
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds;
        
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) return collider.bounds;
        
        // If no renderer/collider, return null (don't use transform scale as it might be misleading)
        return null;
    }
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying)
        {
            PlaceOnBounds();
        }
    }
    #endif
}
