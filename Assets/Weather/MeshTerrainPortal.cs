using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// GameObject component created for each detected opening/entrance to an enclosed space.
    /// Contains vertex loop information for easier portal creation.
    /// </summary>
    public class MeshTerrainPortal : MonoBehaviour
    {
        [Header("Portal Data")]
        [Tooltip("The vertex loop data for this opening")]
        public VertexLoop vertexLoop;

        [Tooltip("Reference to the enclosed space this portal connects to")]
        public EnclosedSpace enclosedSpace;

        [Tooltip("Type of portal (Horizontal, Vertical, or Mixed)")]
        public PortalType portalType = PortalType.Horizontal;

        [Header("Gizmo Settings")]
        [Tooltip("Show gizmo in scene view")]
        public bool showGizmo = true;

        [Tooltip("Gizmo color")]
        public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange

        [Tooltip("Gizmo line width")]
        public float gizmoLineWidth = 0.1f;

        /// <summary>
        /// Get the vertex loop data.
        /// </summary>
        public VertexLoop GetVertexLoop()
        {
            return vertexLoop;
        }

        /// <summary>
        /// Get UV coordinates for the vertex loop.
        /// </summary>
        public List<Vector2> GetUVCoordinates()
        {
            return vertexLoop != null ? vertexLoop.uvs : new List<Vector2>();
        }

        /// <summary>
        /// Check if this is a vertical entrance.
        /// </summary>
        public bool IsVerticalEntrance()
        {
            return vertexLoop != null && vertexLoop.isVertical;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo || vertexLoop == null || vertexLoop.vertices == null || vertexLoop.vertices.Count < 3)
                return;

            Gizmos.color = gizmoColor;

            // Draw vertex loop
            for (int i = 0; i < vertexLoop.vertices.Count; i++)
            {
                int next = (i + 1) % vertexLoop.vertices.Count;
                Gizmos.DrawLine(vertexLoop.vertices[i], vertexLoop.vertices[next]);
            }

            // Draw center point
            Gizmos.color = gizmoColor * 0.5f;
            Gizmos.DrawSphere(vertexLoop.center, gizmoLineWidth * 2f);

            // Draw normal direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(vertexLoop.center, vertexLoop.normal * gizmoLineWidth * 5f);

            // Draw connection to enclosed space center
            if (enclosedSpace != null)
            {
                Gizmos.color = gizmoColor * 0.3f;
                Gizmos.DrawLine(vertexLoop.center, enclosedSpace.center);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo || vertexLoop == null || vertexLoop.vertices == null || vertexLoop.vertices.Count < 3)
                return;

            // Draw a subtle outline when not selected
            Gizmos.color = gizmoColor * 0.3f;
            for (int i = 0; i < vertexLoop.vertices.Count; i++)
            {
                int next = (i + 1) % vertexLoop.vertices.Count;
                Gizmos.DrawLine(vertexLoop.vertices[i], vertexLoop.vertices[next]);
            }
        }
    }
}
