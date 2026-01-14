using UnityEngine;

// Component that denotes a GameObject as empty space available for behavior tree generation
// Can be placed on empty GameObjects or prefabs
public class SGBehaviorTreeEmptySpace : MonoBehaviour
{
    public enum MeshType
    {
        Base,  // Uses GameObject's transform bounds (BoxCollider, MeshRenderer bounds, or default box)
        Box    // Explicit box shape defined by size/scale
    }
    
    [Header("Search Space Configuration")]
    public MeshType meshType = MeshType.Base;
    
    [Header("Box Parameters (for Box mesh type)")]
    public Vector3 boxSize = Vector3.one;
    
    [Header("Editor Visualization")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f); // Green with transparency
    
    /// <summary>
    /// Get the bounds of this empty space marker
    /// </summary>
    public Bounds GetBounds()
    {
        if (meshType == MeshType.Base)
        {
            // Try to get bounds from colliders or renderers (these are already in world space)
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                return boxCollider.bounds; // Already world space
            }
            
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer.bounds; // Already world space
            }
            
            // Default: use transform scale
            // Use lossyScale to account for parent transforms (world space scale)
            Vector3 size = transform.lossyScale;
            return new Bounds(transform.position, size);
        }
        else // Box
        {
            // Use explicit box size, transformed to world space
            // Multiply by lossyScale to account for parent transforms
            Vector3 worldSize = new Vector3(
                boxSize.x * transform.lossyScale.x,
                boxSize.y * transform.lossyScale.y,
                boxSize.z * transform.lossyScale.z
            );
            return new Bounds(transform.position, worldSize);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }
        
        Gizmos.color = gizmoColor;
        Bounds bounds = GetBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmo)
        {
            return;
        }
        
        // Draw more prominently when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Bounds bounds = GetBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
