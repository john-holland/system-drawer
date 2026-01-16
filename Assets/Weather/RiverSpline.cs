using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// River spline that defines river path geometry.
    /// Supports spline painting for shader fitting and segmentation via nested GameObjects.
    /// </summary>
    public class RiverSpline : MonoBehaviour
    {
        [Header("Spline Points")]
        [Tooltip("List of spline points (world positions)")]
        public List<Vector3> splinePoints = new List<Vector3>();

        [Header("River Properties")]
        [Tooltip("River width in meters")]
        public float width = 5f;

        [Tooltip("River depth in meters")]
        public float depth = 2f;

        [Header("Segmentation")]
        [Tooltip("Use nested GameObjects for segmentation (for manual tweaking)")]
        public bool useNestedSegments = false;

        [Tooltip("Segment GameObjects (auto-populated if useNestedSegments is true)")]
        public List<Transform> segmentTransforms = new List<Transform>();

        [Header("Configuration")]
        [Tooltip("Spline resolution (points per meter)")]
        public float splineResolution = 0.1f;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Arrow spacing along spline (meters)")]
        public float arrowSpacing = 5f;

        private void Awake()
        {
            if (useNestedSegments)
            {
                CollectSegmentTransforms();
            }

            if (splinePoints.Count == 0)
            {
                GenerateSplineFromSegments();
            }
        }

        /// <summary>
        /// Get world position at distance along spline
        /// </summary>
        public Vector3 GetPositionAtDistance(float distance)
        {
            if (splinePoints.Count < 2)
                return transform.position;

            // Clamp distance to spline length
            float totalLength = GetTotalLength();
            distance = Mathf.Clamp(distance, 0f, totalLength);

            // Find segment and interpolate
            float currentDistance = 0f;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
                if (currentDistance + segmentLength >= distance)
                {
                    float t = (distance - currentDistance) / segmentLength;
                    return Vector3.Lerp(splinePoints[i], splinePoints[i + 1], t);
                }
                currentDistance += segmentLength;
            }

            return splinePoints[splinePoints.Count - 1];
        }

        /// <summary>
        /// Get flow direction at distance along spline
        /// </summary>
        public Vector3 GetFlowDirectionAtDistance(float distance)
        {
            if (splinePoints.Count < 2)
                return transform.forward; // Fallback to transform forward if no spline points

            float totalLength = GetTotalLength();
            distance = Mathf.Clamp(distance, 0f, totalLength);

            // Find segment
            float currentDistance = 0f;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
                if (currentDistance + segmentLength >= distance)
                {
                    Vector3 direction = (splinePoints[i + 1] - splinePoints[i]).normalized;
                    return direction;
                }
                currentDistance += segmentLength;
            }

            // If distance exceeds all segments, return direction of last segment
            if (splinePoints.Count >= 2)
            {
                return (splinePoints[splinePoints.Count - 1] - splinePoints[splinePoints.Count - 2]).normalized;
            }

            return transform.forward; // Final fallback
        }

        /// <summary>
        /// Get width at distance along spline
        /// </summary>
        public float GetWidthAtDistance(float distance)
        {
            // For now, return constant width
            // Could be extended to vary along spline
            return width;
        }

        /// <summary>
        /// Get depth at distance along spline
        /// </summary>
        public float GetDepthAtDistance(float distance)
        {
            // For now, return constant depth
            // Could be extended to vary along spline
            return depth;
        }

        /// <summary>
        /// Get total length of spline
        /// </summary>
        public float GetTotalLength()
        {
            if (splinePoints.Count < 2)
                return 0f;

            float length = 0f;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                length += Vector3.Distance(splinePoints[i], splinePoints[i + 1]);
            }

            return length;
        }

        /// <summary>
        /// Collect segment transforms from nested GameObjects
        /// </summary>
        private void CollectSegmentTransforms()
        {
            segmentTransforms.Clear();
            foreach (Transform child in transform)
            {
                segmentTransforms.Add(child);
            }
        }

        /// <summary>
        /// Generate spline points from segment transforms
        /// </summary>
        private void GenerateSplineFromSegments()
        {
            if (segmentTransforms.Count == 0)
                return;

            splinePoints.Clear();
            foreach (var segment in segmentTransforms)
            {
                if (segment != null)
                {
                    splinePoints.Add(segment.position);
                }
            }
        }

        /// <summary>
        /// Draw flow direction arrow (reusing WeatherEvent arrow style)
        /// </summary>
        private void DrawFlowArrow(Vector3 position, Vector3 direction, float width, Color color)
        {
            if (direction.magnitude < 0.01f)
                return;

            direction = direction.normalized;
            float arrowLength = width * 0.5f;
            Vector3 endPos = position + direction * arrowLength;

            // Arrow shaft
            Gizmos.color = color;
            Gizmos.DrawLine(position, endPos);

            // Arrow head
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;

            Vector3 arrowHeadSize = direction * -0.3f + right * 0.2f;
            Gizmos.DrawLine(endPos, endPos + arrowHeadSize);
            Gizmos.DrawLine(endPos, endPos + direction * -0.3f - right * 0.2f);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            if (splinePoints.Count < 2)
                return;

            Color riverColor = new Color(0f, 0.5f, 1f, 0.8f); // Water blue (reusing WeatherEvent color system)

            // Spline path visualization (line through points)
            Gizmos.color = riverColor;
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
            }

            // Flow direction arrows along spline (reusing WeatherEvent arrow style)
            float totalLength = GetTotalLength();
            float currentDistance = 0f;
            int segmentIndex = 0;

            while (currentDistance < totalLength && segmentIndex < splinePoints.Count - 1)
            {
                Vector3 pos = GetPositionAtDistance(currentDistance);
                Vector3 flowDir = GetFlowDirectionAtDistance(currentDistance);
                float currentWidth = GetWidthAtDistance(currentDistance);

                // Draw flow arrow
                DrawFlowArrow(pos, flowDir, currentWidth, riverColor);

                // Width visualization (perpendicular lines)
                Vector3 perpendicular = Vector3.Cross(flowDir, Vector3.up).normalized;
                if (perpendicular.magnitude > 0.01f)
                {
                    Gizmos.color = new Color(riverColor.r, riverColor.g, riverColor.b, 0.5f);
                    Gizmos.DrawLine(
                        pos + perpendicular * currentWidth * 0.5f,
                        pos - perpendicular * currentWidth * 0.5f
                    );
                }

                // Depth indicator (vertical line)
                float currentDepth = GetDepthAtDistance(currentDistance);
                Gizmos.color = new Color(riverColor.r, riverColor.g, riverColor.b, 0.7f);
                Gizmos.DrawLine(pos, pos + Vector3.down * currentDepth);

                currentDistance += arrowSpacing;
            }
        }
    }
}
