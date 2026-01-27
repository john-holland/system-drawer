using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects grabbable surfaces using edge detection.
/// Finds edges with 90° or less angle that can be grabbed for climbing.
/// </summary>
public class SurfaceGrabDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Maximum angle (in degrees) for an edge to be considered grabbable")]
    [Range(0f, 90f)]
    public float maxGrabAngle = 90f;

    [Tooltip("Minimum edge length to be considered grabbable")]
    public float minEdgeLength = 0.1f;

    [Tooltip("Raycast distance for surface detection")]
    public float raycastDistance = 5f;

    [Tooltip("Layer mask for surface detection")]
    public LayerMask surfaceLayerMask = ~0;

    [Tooltip("Number of samples per edge for angle calculation")]
    [Range(2, 20)]
    public int samplesPerEdge = 5;

    [Header("Grab Hold Settings")]
    [Tooltip("Preferred hand reach distance for grab holds")]
    public float handReachDistance = 0.6f;

    [Tooltip("Minimum distance between grab holds")]
    public float minGrabHoldSpacing = 0.3f;

    /// <summary>
    /// Information about a grabbable edge/hold.
    /// </summary>
    [System.Serializable]
    public class GrabHold
    {
        public Vector3 position;
        public Vector3 normal;
        public float edgeAngle; // Angle in degrees
        public Vector3 edgeDirection;
        public GameObject surfaceObject;
        public Collider surfaceCollider;
        public float edgeLength;
        public bool isGrabbable;

        public GrabHold(Vector3 pos, Vector3 norm, float angle, Vector3 dir, GameObject obj, Collider col, float length)
        {
            position = pos;
            normal = norm;
            edgeAngle = angle;
            edgeDirection = dir;
            surfaceObject = obj;
            surfaceCollider = col;
            edgeLength = length;
            isGrabbable = angle <= 90f;
        }
    }

    /// <summary>
    /// Detect grabbable edges in a given area.
    /// </summary>
    public List<GrabHold> DetectGrabbableEdges(Vector3 center, float radius)
    {
        List<GrabHold> grabHolds = new List<GrabHold>();

        // Sample points in a sphere around center
        int sampleCount = 32;
        for (int i = 0; i < sampleCount; i++)
        {
            // Generate random direction
            Vector3 direction = Random.onUnitSphere;
            Vector3 samplePoint = center + direction * radius;

            // Raycast to find surfaces
            RaycastHit hit;
            if (Physics.Raycast(samplePoint, -direction, out hit, raycastDistance * 2f, surfaceLayerMask))
            {
                // Check if this hit point is on an edge
                GrabHold hold = DetectEdgeAtPoint(hit.point, hit.normal, hit.collider, hit.transform.gameObject);
                if (hold != null && hold.isGrabbable)
                {
                    // Check if this hold is far enough from existing holds
                    bool tooClose = false;
                    foreach (var existing in grabHolds)
                    {
                        if (Vector3.Distance(hold.position, existing.position) < minGrabHoldSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        grabHolds.Add(hold);
                    }
                }
            }
        }

        return grabHolds;
    }

    /// <summary>
    /// Detect edge at a specific point on a surface.
    /// </summary>
    private GrabHold DetectEdgeAtPoint(Vector3 point, Vector3 normal, Collider collider, GameObject surfaceObject)
    {
        // Sample points around the given point to detect edges
        List<Vector3> samplePoints = new List<Vector3>();
        List<Vector3> sampleNormals = new List<Vector3>();

        // Generate sample points in a circle around the point
        float sampleRadius = 0.1f;
        for (int i = 0; i < samplesPerEdge; i++)
        {
            float angle = (i / (float)samplesPerEdge) * 360f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * sampleRadius;
            Vector3 samplePos = point + offset;

            // Raycast down to find surface
            RaycastHit hit;
            if (Physics.Raycast(samplePos + Vector3.up * 0.5f, Vector3.down, out hit, 1f, surfaceLayerMask))
            {
                if (hit.collider == collider)
                {
                    samplePoints.Add(hit.point);
                    sampleNormals.Add(hit.normal);
                }
            }
        }

        if (samplePoints.Count < 3)
        {
            return null; // Not enough samples
        }

        // Calculate edge angle by comparing normals
        float maxAngleDifference = 0f;
        Vector3 edgeDirection = Vector3.zero;

        for (int i = 0; i < sampleNormals.Count; i++)
        {
            for (int j = i + 1; j < sampleNormals.Count; j++)
            {
                float angle = Vector3.Angle(sampleNormals[i], sampleNormals[j]);
                if (angle > maxAngleDifference)
                {
                    maxAngleDifference = angle;
                    edgeDirection = (samplePoints[j] - samplePoints[i]).normalized;
                }
            }
        }

        // Calculate average normal
        Vector3 avgNormal = Vector3.zero;
        foreach (var n in sampleNormals)
        {
            avgNormal += n;
        }
        avgNormal /= sampleNormals.Count;
        avgNormal.Normalize();

        // Calculate edge length (approximate)
        float edgeLength = 0f;
        for (int i = 0; i < samplePoints.Count - 1; i++)
        {
            edgeLength += Vector3.Distance(samplePoints[i], samplePoints[i + 1]);
        }

        // Check if edge meets minimum length requirement
        if (edgeLength < minEdgeLength)
        {
            return null;
        }

        // The edge angle is the maximum angle difference between normals
        // If this is <= 90°, it's grabbable
        float edgeAngle = maxAngleDifference;

        return new GrabHold(point, avgNormal, edgeAngle, edgeDirection, surfaceObject, collider, edgeLength);
    }

    /// <summary>
    /// Detect grabbable edges along a raycast path.
    /// </summary>
    public List<GrabHold> DetectGrabbableEdgesAlongRay(Vector3 origin, Vector3 direction, float maxDistance)
    {
        List<GrabHold> grabHolds = new List<GrabHold>();

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, surfaceLayerMask);
        foreach (var hit in hits)
        {
            GrabHold hold = DetectEdgeAtPoint(hit.point, hit.normal, hit.collider, hit.transform.gameObject);
            if (hold != null && hold.isGrabbable && hold.edgeAngle <= maxGrabAngle)
            {
                grabHolds.Add(hold);
            }
        }

        return grabHolds;
    }

    /// <summary>
    /// Find the best grab hold near a target position.
    /// </summary>
    public GrabHold FindBestGrabHold(Vector3 targetPosition, float searchRadius)
    {
        List<GrabHold> holds = DetectGrabbableEdges(targetPosition, searchRadius);

        if (holds.Count == 0)
            return null;

        // Find hold closest to target that is within reach
        GrabHold bestHold = null;
        float bestScore = float.MaxValue;

        foreach (var hold in holds)
        {
            float distance = Vector3.Distance(hold.position, targetPosition);
            
            // Prefer holds that are:
            // 1. Close to target
            // 2. Have good edge angle (closer to 90° is better for gripping)
            // 3. Have sufficient edge length
            float distanceScore = distance;
            float angleScore = Mathf.Abs(hold.edgeAngle - 90f); // Prefer 90° edges
            float lengthScore = 1f / (hold.edgeLength + 0.1f); // Prefer longer edges

            float totalScore = distanceScore * 0.5f + angleScore * 0.2f + lengthScore * 0.3f;

            if (distance <= handReachDistance && totalScore < bestScore)
            {
                bestScore = totalScore;
                bestHold = hold;
            }
        }

        return bestHold;
    }

    /// <summary>
    /// Check if a position has a grabbable edge nearby.
    /// </summary>
    public bool HasGrabbableEdge(Vector3 position, float radius)
    {
        List<GrabHold> holds = DetectGrabbableEdges(position, radius);
        return holds.Count > 0;
    }

    /// <summary>
    /// Get all grab holds within a bounds.
    /// </summary>
    public List<GrabHold> GetGrabHoldsInBounds(Bounds bounds)
    {
        List<GrabHold> grabHolds = new List<GrabHold>();

        // Sample the bounds volume
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        float maxRadius = Mathf.Max(size.x, size.y, size.z) * 0.5f;

        return DetectGrabbableEdges(center, maxRadius);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raycastDistance);
    }
#endif
}
