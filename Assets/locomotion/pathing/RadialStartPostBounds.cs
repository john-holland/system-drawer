using System.Collections.Generic;
using UnityEngine;

/// <summary>Unit-cube start-joint grabber under CenterPost. Facing gizmo aims slot 0.</summary>
[AddComponentMenu("Locomotion/Build/Radial Start Post Bounds")]
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RadialStartPostBounds : MonoBehaviour
{
    public SkinnedMeshLoopSectionAsset sectionAsset;
    public string loopId = "";
    public List<int> bespokeVertexIndices = new List<int>();

    public Vector3 FacingVector() => transform.forward;

    public Vector3 SelectedCentroid()
    {
        if (sectionAsset != null && !string.IsNullOrEmpty(loopId))
        {
            var loop = sectionAsset.GetLoop(loopId);
            Mesh mesh = sectionAsset.originalMesh != null ? sectionAsset.originalMesh : sectionAsset.savedCacheMesh;
            if (loop != null && mesh != null)
            {
                var idx = loop.CombinedVertexIndices(mesh.vertices, transform.localToWorldMatrix);
                if (idx.Count > 0)
                {
                    Vector3 c = Vector3.zero;
                    var v = mesh.vertices;
                    for (int i = 0; i < idx.Count; i++)
                    {
                        int vi = idx[i];
                        if (vi >= 0 && vi < v.Length)
                            c += transform.TransformPoint(v[vi]);
                    }
                    return c / idx.Count;
                }
            }
        }
        return transform.position;
    }

    public bool ContainsWorldPoint(Vector3 world)
    {
        Vector3 p = transform.InverseTransformPoint(world);
        return Mathf.Abs(p.x) <= 0.5f && Mathf.Abs(p.y) <= 0.5f && Mathf.Abs(p.z) <= 0.5f;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.45f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.95f);
        Vector3 tip = Vector3.forward * 0.85f;
        Gizmos.DrawLine(Vector3.zero, tip);
        Gizmos.DrawLine(tip, tip + new Vector3(-0.12f, 0f, -0.18f));
        Gizmos.DrawLine(tip, tip + new Vector3(0.12f, 0f, -0.18f));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.95f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
