using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Open.Topology
{
    /// <summary>World-space loop forming an opening into a concave volume.</summary>
    [System.Serializable]
    public sealed class OpeningLoop
    {
        public List<Vector3> vertices = new List<Vector3>();
        public Vector3 normal = Vector3.up;
        public Vector3 center = Vector3.zero;
        public float area;
        public bool isVertical;

        public void CalculateProperties()
        {
            if (vertices == null || vertices.Count < 3)
            {
                center = Vector3.zero;
                area = 0f;
                return;
            }

            center = Vector3.zero;
            foreach (var v in vertices)
                center += v;
            center /= vertices.Count;

            area = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                area += Vector3.Cross(vertices[i] - center, vertices[next] - center).magnitude * 0.5f;
            }

            normal = Vector3.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                normal += Vector3.Cross(vertices[i] - center, vertices[next] - center).normalized;
            }
            if (normal.sqrMagnitude > 1e-6f)
                normal.Normalize();
            isVertical = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.7f;
        }
    }
}
