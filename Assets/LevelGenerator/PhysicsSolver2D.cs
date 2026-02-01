using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Physics solver wrapper for 2D AABB level generation.
/// Supports Unity 2D physics or custom AABB solver with stability detection.
/// </summary>
public class PhysicsSolver2D : MonoBehaviour
{
    [Header("Solver Configuration")]
    [Tooltip("Use Unity 2D Physics (Rigidbody2D/Collider2D)")]
    public bool useUnity2DPhysics = true;

    [Tooltip("Stability threshold (movement/velocity below this = stable)")]
    public float stabilityThreshold = 0.01f;

    [Tooltip("Stability check method")]
    public StabilityCheckMethod stabilityCheckMethod = StabilityCheckMethod.VelocityAndPosition;

    [Header("Physics Settings")]
    [Tooltip("Fixed timestep for physics simulation")]
    public float fixedTimeStep = 0.02f;

    [Tooltip("Physics iterations per step")]
    public int physicsIterations = 1;

    // Internal state
    private List<RoomAsset> rooms = new List<RoomAsset>();
    private CustomAABBSolver customSolver;
    private List<Vector2> previousPositions = new List<Vector2>();
    private int stabilityCheckCount = 0;
    private int requiredStableFrames = 3; // Require 3 consecutive stable frames

    /// <summary>
    /// Initialize physics solver with rooms.
    /// </summary>
    public void Initialize(List<RoomAsset> roomList)
    {
        rooms = new List<RoomAsset>(roomList);
        previousPositions.Clear();
        stabilityCheckCount = 0;

        // Initialize custom solver if not using Unity 2D physics
        if (!useUnity2DPhysics)
        {
            if (customSolver == null)
            {
                customSolver = gameObject.AddComponent<CustomAABBSolver>();
            }
            customSolver.Initialize(rooms);
        }
        else
        {
            // Ensure all rooms have Rigidbody2D and Collider2D
            foreach (var room in rooms)
            {
                if (room == null)
                    continue;

                // Ensure Rigidbody2D exists
                Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = room.gameObject.AddComponent<Rigidbody2D>();
                }
                rb.gravityScale = 0f; // No gravity
                rb.linearDamping = 0.5f; // Some drag for stability
                rb.angularDamping = 0.5f;

                // Ensure Collider2D exists
                Collider2D collider = room.GetComponent<Collider2D>();
                if (collider == null)
                {
                    BoxCollider2D boxCollider = room.gameObject.AddComponent<BoxCollider2D>();
                    Rect bounds = room.GetBounds();
                    boxCollider.size = new Vector2(bounds.width, bounds.height);
                }
            }

            // Store initial positions
            StorePositions();
        }
    }

    /// <summary>
    /// Step physics simulation forward.
    /// </summary>
    public void Step()
    {
        if (useUnity2DPhysics)
        {
            // Use Unity's physics simulation
            Physics2D.Simulate(fixedTimeStep);
        }
        else
        {
            // Use custom AABB solver
            if (customSolver != null)
            {
                customSolver.Step();
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

        bool stable = false;

        if (useUnity2DPhysics)
        {
            stable = CheckUnity2DStability();
        }
        else
        {
            if (customSolver != null)
            {
                stable = customSolver.IsStable();
            }
        }

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
        if (useUnity2DPhysics)
        {
            StorePositions();
        }

        return false;
    }

    /// <summary>
    /// Check stability using Unity 2D physics.
    /// </summary>
    private bool CheckUnity2DStability()
    {
        switch (stabilityCheckMethod)
        {
            case StabilityCheckMethod.Velocity:
                return CheckVelocityStability();

            case StabilityCheckMethod.Position:
                return CheckPositionStability();

            case StabilityCheckMethod.VelocityAndPosition:
                return CheckVelocityStability() && CheckPositionStability();

            case StabilityCheckMethod.Collision:
                return CheckCollisionStability();

            default:
                return CheckVelocityStability();
        }
    }

    /// <summary>
    /// Check stability based on velocities (Method 1).
    /// </summary>
    private bool CheckVelocityStability()
    {
        foreach (var room in rooms)
        {
            if (room == null)
                continue;

            Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Check linear velocity
                if (rb.linearVelocity.magnitude > stabilityThreshold)
                {
                    return false;
                }

                // Check angular velocity
                if (Mathf.Abs(rb.angularVelocity) > stabilityThreshold)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Check stability based on position changes (Method 2).
    /// </summary>
    private bool CheckPositionStability()
    {
        if (previousPositions == null || previousPositions.Count != rooms.Count)
        {
            StorePositions();
            return false; // Need at least one frame of history
        }

        float maxMovement = 0f;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i] == null)
                continue;

            Vector2 currentPos = rooms[i].GetCenter();
            Vector2 previousPos = previousPositions[i];

            float movement = Vector2.Distance(currentPos, previousPos);
            maxMovement = Mathf.Max(maxMovement, movement);
        }

        return maxMovement < stabilityThreshold;
    }

    /// <summary>
    /// Check stability based on active collisions (Method 3).
    /// </summary>
    private bool CheckCollisionStability()
    {
        // Count active collisions
        int activeCollisions = 0;

        foreach (var room in rooms)
        {
            if (room == null)
                continue;

            // Check for overlapping colliders
            Collider2D collider = room.GetComponent<Collider2D>();
            if (collider != null)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.NoFilter();
                List<Collider2D> results = new List<Collider2D>();

                int count = collider.Overlap(filter, results);
                // Subtract 1 because it includes itself
                activeCollisions += Mathf.Max(0, count - 1);
            }
        }

        // Stable when collision count is minimal (rooms are separated)
        return activeCollisions < rooms.Count * 0.1f; // Less than 10% of rooms colliding
    }

    /// <summary>
    /// Store current room positions for position-based stability checking.
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
    /// Get current room positions.
    /// </summary>
    public List<Vector2> GetRoomPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                positions.Add(room.GetCenter());
            }
        }
        return positions;
    }

    /// <summary>
    /// Cleanup physics solver.
    /// </summary>
    public void Cleanup()
    {
        rooms.Clear();
        previousPositions.Clear();
        stabilityCheckCount = 0;

        if (customSolver != null)
        {
            customSolver.Cleanup();
        }
    }
}

/// <summary>
/// Stability check methods for physics solver.
/// </summary>
public enum StabilityCheckMethod
{
    Velocity,              // Check velocities only
    Position,              // Check position changes only
    VelocityAndPosition,   // Check both velocity and position
    Collision              // Check active collisions
}
