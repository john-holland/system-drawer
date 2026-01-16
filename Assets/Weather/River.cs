using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// River system that manages river geometry via RiverSplines and calculates flow rates.
    /// Supports procedural or placed splines and uses Finite Volume Method for flow calculations.
    /// </summary>
    public class River : MonoBehaviour
    {
        [Header("River Parameters")]
        [Tooltip("Flow rate in m³/s")]
        public float flowRate = 10f;

        [Tooltip("Velocity in m/s")]
        public float velocity = 2f;

        [Tooltip("Width in meters")]
        public float width = 5f;

        [Tooltip("Depth in meters")]
        public float depth = 2f;

        [Header("River Splines")]
        [Tooltip("List of RiverSpline components")]
        public List<RiverSpline> riverSplines = new List<RiverSpline>();

        [Header("Materials")]
        [Tooltip("Materials for rendering")]
        public Material[] materials;

        [Header("Wind Effects")]
        [Tooltip("Optional wind effect override for this river")]
        public Wind windEffectOverride;

        [Header("Configuration")]
        [Tooltip("Auto-find river splines on start")]
        public bool autoFindSplines = true;

        [Tooltip("Upstream rivers (feeds into this river)")]
        public List<River> upstreamRivers = new List<River>();

        [Tooltip("Downstream rivers/ponds (this river feeds into)")]
        public List<MonoBehaviour> downstreamBodies = new List<MonoBehaviour>(); // Can be River or Pond

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        private void Start()
        {
            if (autoFindSplines)
            {
                FindRiverSplines();
            }
        }

        /// <summary>
        /// Service update called by Water system
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Calculate flow rate from current parameters
            CalculateFlowRate();

            // Update velocity based on flow rate
            UpdateVelocity();
        }

        /// <summary>
        /// Add water from precipitation or upstream rivers
        /// </summary>
        public void AddWater(float volumeM3)
        {
            // Increase flow rate based on added volume
            // Flow rate = Volume / Time, so we approximate by adding to current flow
            // This is simplified - real flow would depend on cross-sectional area and velocity
            float additionalFlow = volumeM3 / 1f; // Assume 1 second time scale
            flowRate += additionalFlow;
        }

        /// <summary>
        /// Calculate flow rate using Finite Volume Method
        /// Flow rate (Q) = Cross-sectional area (A) × Velocity (v)
        /// </summary>
        public void CalculateFlowRate()
        {
            // Calculate cross-sectional area
            float crossSectionalArea = width * depth; // m²

            // Flow rate = area × velocity
            flowRate = crossSectionalArea * velocity;

            // Add flow from upstream rivers
            foreach (var upstream in upstreamRivers)
            {
                if (upstream != null)
                {
                    flowRate += upstream.flowRate * 0.5f; // Simplified: 50% of upstream flow
                }
            }
        }

        /// <summary>
        /// Update velocity based on flow rate and cross-sectional area
        /// </summary>
        private void UpdateVelocity()
        {
            float crossSectionalArea = width * depth;
            if (crossSectionalArea > 0f)
            {
                velocity = flowRate / crossSectionalArea;
            }
        }

        /// <summary>
        /// Get spline data at distance along river
        /// </summary>
        public RiverSplineData GetSplineAtDistance(float distance)
        {
            // Find which spline contains this distance
            float currentDistance = 0f;
            foreach (var spline in riverSplines)
            {
                if (spline == null)
                    continue;

                float splineLength = spline.GetTotalLength();
                if (distance <= currentDistance + splineLength)
                {
                    float localDistance = distance - currentDistance;
                    return new RiverSplineData
                    {
                        position = spline.GetPositionAtDistance(localDistance),
                        flowDirection = spline.GetFlowDirectionAtDistance(localDistance),
                        width = spline.GetWidthAtDistance(localDistance),
                        depth = spline.GetDepthAtDistance(localDistance)
                    };
                }
                currentDistance += splineLength;
            }

            // Return default if distance exceeds all splines
            if (riverSplines.Count > 0 && riverSplines[0] != null)
            {
                return new RiverSplineData
                {
                    position = riverSplines[0].GetPositionAtDistance(0f),
                    flowDirection = riverSplines[0].GetFlowDirectionAtDistance(0f),
                    width = width,
                    depth = depth
                };
            }

            return new RiverSplineData
            {
                position = transform.position,
                flowDirection = Vector3.forward,
                width = width,
                depth = depth
            };
        }

        /// <summary>
        /// Find all RiverSpline components
        /// </summary>
        private void FindRiverSplines()
        {
            riverSplines.Clear();
            riverSplines.AddRange(GetComponentsInChildren<RiverSpline>());
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Color riverColor = new Color(0f, 0.5f, 1f, 0.6f);

            // Spline connections visualization
            Gizmos.color = riverColor;
            foreach (var spline in riverSplines)
            {
                if (spline == null)
                    continue;

                // Draw connection from river to spline
                Gizmos.DrawLine(transform.position, spline.transform.position);
            }

            // Flow direction indicators (at river position)
            if (riverSplines.Count > 0 && riverSplines[0] != null)
            {
                var splineData = GetSplineAtDistance(0f);
                Vector3 flowDir = splineData.flowDirection;
                if (flowDir.magnitude > 0.01f)
                {
                    Gizmos.color = new Color(riverColor.r, riverColor.g, riverColor.b, 0.8f);
                    float arrowLength = velocity * 2f;
                    Vector3 endPos = transform.position + flowDir * arrowLength;
                    Gizmos.DrawLine(transform.position, endPos);

                    // Arrow head
                    Vector3 right = Vector3.Cross(flowDir, Vector3.up).normalized;
                    if (right.magnitude < 0.01f)
                        right = Vector3.Cross(flowDir, Vector3.forward).normalized;

                    Vector3 arrowHead = flowDir * -0.3f + right * 0.2f;
                    Gizmos.DrawLine(endPos, endPos + arrowHead);
                    Gizmos.DrawLine(endPos, endPos + flowDir * -0.3f - right * 0.2f);
                }
            }
        }
    }

    /// <summary>
    /// River spline data structure
    /// </summary>
    public struct RiverSplineData
    {
        public Vector3 position;
        public Vector3 flowDirection;
        public float width;
        public float depth;
    }
}
