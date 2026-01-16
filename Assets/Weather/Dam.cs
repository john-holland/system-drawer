using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Dam system that blocks water with a configurable plane/mesh.
    /// Calculates overflow rates and manages water level above/below dam.
    /// </summary>
    public class Dam : MonoBehaviour
    {
        [Header("Dam Parameters")]
        [Tooltip("Dam height in meters")]
        public float height = 10f;

        [Tooltip("Water level in meters (above/below dam)")]
        public float waterLevel = 0f;

        [Tooltip("Flow rate in m³/s (if water overflows)")]
        public float flowRate = 0f;

        [Header("Blocking Geometry")]
        [Tooltip("Use attached Collider as blocking plane")]
        public bool useCollider = true;

        [Tooltip("Custom blocking mesh (optional)")]
        public Mesh customMesh;

        [Tooltip("Blocking plane (if not using collider)")]
        public Plane blockingPlane;

        [Header("Configuration")]
        [Tooltip("Auto-create blocking plane from transform")]
        public bool autoCreatePlane = true;

        [Tooltip("Overflow coefficient (affects flow rate calculation)")]
        [Range(0f, 1f)]
        public float overflowCoefficient = 0.6f;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        private void Awake()
        {
            if (autoCreatePlane && !useCollider)
            {
                CreateBlockingPlane();
            }
        }

        /// <summary>
        /// Service update called by Water system
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Check for overflow
            CheckOverflow();

            // Calculate overflow rate if overflowing
            if (waterLevel > height)
            {
                CalculateOverflowRate();
            }
            else
            {
                flowRate = 0f;
            }
        }

        /// <summary>
        /// Check if water exceeds dam height (overflow)
        /// </summary>
        public bool CheckOverflow()
        {
            return waterLevel > height;
        }

        /// <summary>
        /// Calculate overflow rate using weir flow equation
        /// Q = C * L * H^(3/2)
        /// where Q = flow rate, C = coefficient, L = length, H = head (water level - dam height)
        /// </summary>
        public void CalculateOverflowRate()
        {
            if (waterLevel <= height)
            {
                flowRate = 0f;
                return;
            }

            float head = waterLevel - height; // Head above dam
            float length = GetDamLength(); // Effective length of dam

            // Weir flow equation (simplified)
            // Q = C * L * H^(3/2)
            // C ≈ 1.84 for rectangular weir (SI units)
            float coefficient = 1.84f * overflowCoefficient;
            flowRate = coefficient * length * Mathf.Pow(head, 1.5f);
        }

        /// <summary>
        /// Get water level difference (above/below dam)
        /// </summary>
        public float GetWaterLevelDifference()
        {
            return waterLevel - height;
        }

        /// <summary>
        /// Get effective dam length for flow calculations
        /// </summary>
        private float GetDamLength()
        {
            if (useCollider)
            {
                Collider collider = GetComponent<Collider>();
                if (collider != null)
                {
                    Bounds bounds = collider.bounds;
                    // Use the larger of width or depth as length
                    return Mathf.Max(bounds.size.x, bounds.size.z);
                }
            }

            // Fallback: use transform scale
            Vector3 scale = transform.lossyScale;
            return Mathf.Max(scale.x, scale.z);
        }

        /// <summary>
        /// Create blocking plane from transform
        /// </summary>
        private void CreateBlockingPlane()
        {
            Vector3 normal = transform.forward;
            Vector3 point = transform.position;
            blockingPlane = new Plane(normal, point);
        }

        /// <summary>
        /// Check if a point is blocked by the dam
        /// </summary>
        public bool IsBlocked(Vector3 point)
        {
            if (useCollider)
            {
                Collider collider = GetComponent<Collider>();
                if (collider != null)
                {
                    return collider.bounds.Contains(point);
                }
            }

            // Use plane for blocking check
            float distance = blockingPlane.GetDistanceToPoint(point);
            return distance < 0f; // Point is on the "blocked" side of the plane
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;
            Color damColor = Color.yellow;
            damColor.a = 0.7f;

            // Blocking plane visualization
            if (useCollider)
            {
                Collider collider = GetComponent<Collider>();
                if (collider != null)
                {
                    Gizmos.color = damColor;
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
                }
            }
            else
            {
                // Draw plane as a quad
                Vector3 normal = blockingPlane.normal;
                Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
                if (right.magnitude < 0.01f)
                    right = Vector3.Cross(normal, Vector3.forward).normalized;
                Vector3 up = Vector3.Cross(right, normal).normalized;

                float size = GetDamLength();
                Vector3[] corners = new Vector3[4];
                corners[0] = position + right * size * 0.5f + up * height * 0.5f;
                corners[1] = position - right * size * 0.5f + up * height * 0.5f;
                corners[2] = position - right * size * 0.5f - up * height * 0.5f;
                corners[3] = position + right * size * 0.5f - up * height * 0.5f;

                Gizmos.color = damColor;
                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);
            }

            // Height indicator
            Gizmos.color = new Color(damColor.r, damColor.g, damColor.b, 0.5f);
            Gizmos.DrawLine(position, position + Vector3.up * height);

            // Overflow visualization (if overflowing)
            if (waterLevel > height)
            {
                Color overflowColor = Color.red;
                overflowColor.a = 0.6f;
                Gizmos.color = overflowColor;
                float overflowHeight = waterLevel - height;
                Gizmos.DrawWireCube(
                    position + Vector3.up * (height + overflowHeight * 0.5f),
                    new Vector3(GetDamLength(), overflowHeight, 1f)
                );
            }
        }
    }
}
