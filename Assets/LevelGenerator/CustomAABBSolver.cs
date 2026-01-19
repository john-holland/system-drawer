using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom AABB collision solver as fallback when Unity 2D physics stability detection is unreliable.
/// Implements AABB collision detection and resolution.
/// </summary>
public class CustomAABBSolver : MonoBehaviour
{
    [Header("Solver Settings")]
    [Tooltip("Separation force strength")]
    public float separationForce = 10f;

    [Tooltip("Damping factor for velocity")]
    public float damping = 0.9f;

    [Tooltip("Stability threshold")]
    public float stabilityThreshold = 0.01f;

    // Internal state
    private List<RoomAsset> rooms = new List<RoomAsset>();
    private Dictionary<RoomAsset, Vector2> velocities = new Dictionary<RoomAsset, Vector2>();
    private List<Vector2> previousPositions = new List<Vector2>();
    private int stabilityCheckCount = 0;
    private int requiredStableFrames = 3;

    /// <summary>
    /// Initialize custom solver with rooms.
    /// </summary>
    public void Initialize(List<RoomAsset> roomList)
    {
        rooms = new List<RoomAsset>(roomList);
        velocities.Clear();
        previousPositions.Clear();
        stabilityCheckCount = 0;

        // Initialize velocities
        foreach (var room in rooms)
        {
            if (room != null)
            {
                velocities[room] = Vector2.zero;
                previousPositions.Add(room.GetCenter());
            }
        }
    }

    /// <summary>
    /// Step physics simulation forward.
    /// </summary>
    public void Step()
    {
        // Apply separation forces for collisions
        ResolveCollisions();

        // Update positions based on velocities
        foreach (var room in rooms)
        {
            if (room == null || room.isStatic)
                continue;

            if (velocities.TryGetValue(room, out Vector2 velocity))
            {
                // Apply damping
                velocity *= damping;

                // Update position
                Vector2 currentPos = room.GetCenter();
                Vector2 newPos = currentPos + velocity * Time.fixedDeltaTime;
                room.SetPosition(newPos);

                // Store updated velocity
                velocities[room] = velocity;
            }
        }
    }

    /// <summary>
    /// Resolve AABB collisions by applying separation forces.
    /// </summary>
    private void ResolveCollisions()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomAsset room1 = rooms[i];
            if (room1 == null || room1.isStatic)
                continue;

            Rect bounds1 = room1.GetBounds();

            for (int j = i + 1; j < rooms.Count; j++)
            {
                RoomAsset room2 = rooms[j];
                if (room2 == null || room2.isStatic)
                    continue;

                Rect bounds2 = room2.GetBounds();

                // Check AABB intersection
                if (bounds1.Overlaps(bounds2))
                {
                    // Calculate separation direction and distance
                    Vector2 center1 = bounds1.center;
                    Vector2 center2 = bounds2.center;
                    Vector2 direction = (center1 - center2).normalized;

                    if (direction.magnitude < 0.001f)
                    {
                        // Random direction if centers overlap
                        direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    }

                    // Calculate overlap distance
                    float overlapX = Mathf.Min(
                        bounds1.xMax - bounds2.xMin,
                        bounds2.xMax - bounds1.xMin
                    );
                    float overlapY = Mathf.Min(
                        bounds1.yMax - bounds2.yMin,
                        bounds2.yMax - bounds1.yMin
                    );

                    float minOverlap = Mathf.Min(overlapX, overlapY);
                    float separationDistance = minOverlap * 0.5f; // Split separation between both rooms

                    // Apply separation force
                    Vector2 force = direction * separationForce * separationDistance;

                    // Update velocities
                    if (velocities.TryGetValue(room1, out Vector2 vel1))
                    {
                        velocities[room1] = vel1 + force;
                    }
                    else
                    {
                        velocities[room1] = force;
                    }

                    if (velocities.TryGetValue(room2, out Vector2 vel2))
                    {
                        velocities[room2] = vel2 - force;
                    }
                    else
                    {
                        velocities[room2] = -force;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if physics has reached stable state.
    /// </summary>
    public bool IsStable()
    {
        if (rooms == null || rooms.Count == 0)
            return true;

        // Check velocities
        bool velocitiesStable = true;
        foreach (var kvp in velocities)
        {
            if (kvp.Value.magnitude > stabilityThreshold)
            {
                velocitiesStable = false;
                break;
            }
        }

        // Check position changes
        bool positionsStable = true;
        if (previousPositions.Count == rooms.Count)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] == null)
                    continue;

                Vector2 currentPos = rooms[i].GetCenter();
                Vector2 previousPos = previousPositions[i];

                if (Vector2.Distance(currentPos, previousPos) > stabilityThreshold)
                {
                    positionsStable = false;
                    break;
                }
            }
        }
        else
        {
            positionsStable = false;
        }

        bool stable = velocitiesStable && positionsStable;

        // Require consecutive stable frames
        if (stable)
        {
            stabilityCheckCount++;
            if (stabilityCheckCount >= requiredStableFrames)
            {
                return true;
            }
        }
        else
        {
            stabilityCheckCount = 0;
        }

        // Store positions for next check
        StorePositions();

        return false;
    }

    /// <summary>
    /// Store current positions for stability checking.
    /// </summary>
    private void StorePositions()
    {
        previousPositions.Clear();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                previousPositions.Add(room.GetCenter());
            }
            else
            {
                previousPositions.Add(Vector2.zero);
            }
        }
    }

    /// <summary>
    /// Cleanup solver.
    /// </summary>
    public void Cleanup()
    {
        rooms.Clear();
        velocities.Clear();
        previousPositions.Clear();
        stabilityCheckCount = 0;
    }
}
