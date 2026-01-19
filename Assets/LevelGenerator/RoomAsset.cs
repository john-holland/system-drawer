using UnityEngine;

/// <summary>
/// Room component with 2D AABB bounds for level generation.
/// Attached to room prefabs to define their size and properties.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomAsset : MonoBehaviour
{
    [Header("Room Properties")]
    [Tooltip("Type/category of room")]
    public string roomType = "Standard";

    [Tooltip("Name/identifier for this room")]
    public string roomName = "Room";

    [Tooltip("Is this room static (won't move during physics)")]
    public bool isStatic = false;

    [Header("Room Bounds")]
    [Tooltip("Manual room bounds (if not set, will use collider bounds)")]
    public Rect manualBounds;

    [Tooltip("Use manual bounds instead of collider bounds")]
    public bool useManualBounds = false;

    // Cached bounds
    private Rect cachedBounds;
    private bool boundsCached = false;

    private void Awake()
    {
        // Ensure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(10f, 10f); // Default size
        }
    }

    /// <summary>
    /// Get 2D AABB bounds for this room.
    /// </summary>
    public Rect GetBounds()
    {
        if (useManualBounds && manualBounds.size != Vector2.zero)
        {
            return manualBounds;
        }

        // Use collider bounds
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Bounds worldBounds = collider.bounds;
            Rect bounds = new Rect(
                worldBounds.center.x - worldBounds.extents.x,
                worldBounds.center.y - worldBounds.extents.y,
                worldBounds.size.x,
                worldBounds.size.y
            );
            return bounds;
        }

        // Fallback: use transform bounds
        return new Rect(
            transform.position.x - 5f,
            transform.position.y - 5f,
            10f,
            10f
        );
    }

    /// <summary>
    /// Get center point of room (2D).
    /// </summary>
    public Vector2 GetCenter()
    {
        Rect bounds = GetBounds();
        return new Vector2(bounds.center.x, bounds.center.y);
    }

    /// <summary>
    /// Set room position (2D, z stays the same).
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        boundsCached = false; // Invalidate cache
    }

    /// <summary>
    /// Check if this room intersects with another room.
    /// </summary>
    public bool Intersects(RoomAsset other)
    {
        if (other == null)
            return false;

        Rect bounds1 = GetBounds();
        Rect bounds2 = other.GetBounds();

        return bounds1.Overlaps(bounds2);
    }

    /// <summary>
    /// Get distance to another room (center to center).
    /// </summary>
    public float GetDistanceTo(RoomAsset other)
    {
        if (other == null)
            return float.MaxValue;

        Vector2 center1 = GetCenter();
        Vector2 center2 = other.GetCenter();

        return Vector2.Distance(center1, center2);
    }

    private void OnDrawGizmos()
    {
        // Draw room bounds in editor
        Rect bounds = GetBounds();
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3(bounds.center.x, bounds.center.y, transform.position.z),
            new Vector3(bounds.width, bounds.height, 0f)
        );
    }
}
