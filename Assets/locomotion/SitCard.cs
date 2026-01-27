using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized card for sitting behavior.
/// Validates that hip vertex (upper leg vertex) is on a ledge,
/// torso is positioned over seated vertices, and torso is not hanging over the ledge edge.
/// </summary>
[System.Serializable]
public class SitCard : GoodSection
{
    [Header("Sit-Specific Properties")]
    [Tooltip("Position where hip vertex should be placed (on ledge)")]
    public Vector3 hipVertexPosition;

    [Tooltip("Normal of the ledge surface")]
    public Vector3 ledgeNormal;

    [Tooltip("Seated vertices positions (torso contact points)")]
    public List<Vector3> seatedVertices = new List<Vector3>();

    [Tooltip("Ledge edge positions (to check if torso hangs over)")]
    public List<Vector3> ledgeEdges = new List<Vector3>();

    [Tooltip("Maximum distance torso can hang over ledge edge")]
    public float maxHangOverDistance = 0.1f;

    [Tooltip("Required distance from ledge edge for safe sitting")]
    public float safeEdgeDistance = 0.2f;

    /// <summary>
    /// Check if this sit card is feasible for the given ragdoll state and surface geometry.
    /// </summary>
    public bool IsSitFeasible(RagdollState currentState, Vector3 surfacePosition, Vector3 surfaceNormal, List<Vector3> surfaceEdges)
    {
        // First check base feasibility
        if (!IsFeasible(currentState))
        {
            return false;
        }

        // Check if hip vertex can be placed on ledge
        if (!CanPlaceHipOnLedge(surfacePosition, surfaceNormal))
        {
            return false;
        }

        // Check if torso can be positioned over seated vertices
        if (!CanPositionTorsoOverSeatedVertices())
        {
            return false;
        }

        // Check if torso would hang over ledge edge
        if (WouldTorsoHangOverEdge(surfaceEdges))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if hip vertex can be placed on the ledge.
    /// </summary>
    private bool CanPlaceHipOnLedge(Vector3 surfacePosition, Vector3 surfaceNormal)
    {
        // Calculate distance from hip vertex position to surface
        Vector3 toHip = hipVertexPosition - surfacePosition;
        float distanceToSurface = Vector3.Dot(toHip, surfaceNormal);

        // Hip should be close to surface (within tolerance)
        float tolerance = 0.1f;
        if (Mathf.Abs(distanceToSurface) > tolerance)
        {
            return false;
        }

        // Check if hip position is on the surface plane
        Vector3 projectedHip = hipVertexPosition - surfaceNormal * distanceToSurface;
        float distanceFromProjected = Vector3.Distance(projectedHip, surfacePosition);
        
        // For now, assume surface is large enough (could add surface bounds check)
        return true;
    }

    /// <summary>
    /// Check if torso can be positioned over seated vertices.
    /// </summary>
    private bool CanPositionTorsoOverSeatedVertices()
    {
        if (seatedVertices == null || seatedVertices.Count == 0)
        {
            // No seated vertices specified - assume it's feasible
            return true;
        }

        // Check if all seated vertices are above the hip vertex position
        foreach (var vertex in seatedVertices)
        {
            // Seated vertices should be at or above hip level
            if (vertex.y < hipVertexPosition.y - 0.1f)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if torso would hang over the ledge edge.
    /// </summary>
    private bool WouldTorsoHangOverEdge(List<Vector3> surfaceEdges)
    {
        if (surfaceEdges == null || surfaceEdges.Count == 0)
        {
            // No edges specified - assume safe
            return false;
        }

        if (seatedVertices == null || seatedVertices.Count == 0)
        {
            // No seated vertices - can't check hangover
            return false;
        }

        // Check each seated vertex against each edge
        foreach (var vertex in seatedVertices)
        {
            foreach (var edge in surfaceEdges)
            {
                // Calculate distance from vertex to edge
                float distanceToEdge = Vector3.Distance(vertex, edge);

                // If vertex is too close to edge, it might hang over
                if (distanceToEdge < safeEdgeDistance)
                {
                    // Check if vertex is actually over the edge (project onto edge plane)
                    Vector3 toVertex = vertex - edge;
                    // Simplified check: if vertex is close to edge and below hip level, it might hang
                    if (vertex.y < hipVertexPosition.y + maxHangOverDistance)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Generate a sit card for a given surface position and normal.
    /// </summary>
    public static SitCard GenerateSitCard(Vector3 surfacePosition, Vector3 surfaceNormal, RagdollState currentState, GameObject ragdollActor)
    {
        SitCard card = new SitCard
        {
            sectionName = "Sit_" + System.Guid.NewGuid().ToString("N").Substring(0, 8),
            description = "Sit on ledge with hip vertex on surface and torso over seated vertices"
        };

        // Estimate hip vertex position (would need to get from ragdoll system)
        // For now, use a position relative to the ragdoll root
        if (ragdollActor != null)
        {
            var ragdollSystem = ragdollActor.GetComponent<RagdollSystem>();
            if (ragdollSystem != null && ragdollSystem.pelvisComponent != null)
            {
                Transform pelvisTransform = ragdollSystem.pelvisComponent.PrimaryBoneTransform;
                if (pelvisTransform != null)
                {
                    // Hip vertex is approximately at pelvis position
                    card.hipVertexPosition = pelvisTransform.position;
                }
            }
        }

        // If we couldn't get pelvis, estimate from current state
        if (card.hipVertexPosition == Vector3.zero && currentState != null)
        {
            card.hipVertexPosition = currentState.rootPosition + Vector3.down * 0.5f; // Rough estimate
        }

        // Set ledge properties
        card.ledgeNormal = surfaceNormal;

        // Estimate seated vertices (torso contact points)
        // These would typically be calculated based on ragdoll geometry
        card.seatedVertices = new List<Vector3>
        {
            surfacePosition + Vector3.up * 0.3f, // Approximate torso position
            surfacePosition + Vector3.up * 0.3f + Vector3.forward * 0.2f,
            surfacePosition + Vector3.up * 0.3f + Vector3.back * 0.2f
        };

        // Set limits for sitting
        card.limits = new SectionLimits
        {
            maxForce = 500f,
            maxTorque = 100f,
            maxVelocityChange = 2f
        };

        // Set required and target states
        card.requiredState = currentState?.CopyState();
        card.targetState = currentState?.CopyState();
        if (card.targetState != null)
        {
            // Adjust target state for sitting position
            card.targetState.rootPosition = surfacePosition + Vector3.up * 0.2f;
        }

        return card;
    }

    /// <summary>
    /// Validate that the sit card meets all requirements.
    /// </summary>
    public bool ValidateSitCard()
    {
        if (hipVertexPosition == Vector3.zero)
        {
            Debug.LogWarning($"[SitCard] {sectionName}: Hip vertex position is not set");
            return false;
        }

        if (seatedVertices == null || seatedVertices.Count == 0)
        {
            Debug.LogWarning($"[SitCard] {sectionName}: No seated vertices specified");
            return false;
        }

        return true;
    }
}
