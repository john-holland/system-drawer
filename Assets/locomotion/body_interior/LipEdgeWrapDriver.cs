using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks capsules (finger/brush/tool) and binds lip edge wrap material parameters.
/// </summary>
[AddComponentMenu("Locomotion/Body Interior/Lip Edge Wrap Driver")]
public sealed class LipEdgeWrapDriver : MonoBehaviour
{
    [System.Serializable]
    public sealed class CapsuleTrack
    {
        public Transform transform;
        public float radius = 0.01f;
        public float height = 0.04f;
        public Vector3 localCenter;
    }

    public Renderer lipRenderer;
    public List<CapsuleTrack> tracks = new List<CapsuleTrack>();
    public int maxBoundCapsules = 4;

    static readonly int Capsule0Id = Shader.PropertyToID("_Capsule0");
    static readonly int Capsule1Id = Shader.PropertyToID("_Capsule1");
    static readonly int Capsule2Id = Shader.PropertyToID("_Capsule2");
    static readonly int Capsule3Id = Shader.PropertyToID("_Capsule3");
    static readonly int CapsuleCountId = Shader.PropertyToID("_CapsuleCount");

    public void ClearTracks() => tracks.Clear();

    public void UpsertTrack(Transform t, float radius, float height)
    {
        if (t == null) return;
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && tracks[i].transform == t)
            {
                tracks[i].radius = radius;
                tracks[i].height = height;
                return;
            }
        }
        tracks.Add(new CapsuleTrack { transform = t, radius = radius, height = height });
    }

    void LateUpdate()
    {
        if (lipRenderer == null) return;
        var mat = lipRenderer.material;
        if (mat == null) return;
        int n = Mathf.Min(maxBoundCapsules, tracks.Count);
        mat.SetFloat(CapsuleCountId, n);
        for (int i = 0; i < 4; i++)
        {
            Vector4 c = Vector4.zero;
            if (i < n && tracks[i]?.transform != null)
            {
                Vector3 p = tracks[i].transform.TransformPoint(tracks[i].localCenter);
                c = new Vector4(p.x, p.y, p.z, tracks[i].radius);
            }
            mat.SetVector(i switch
            {
                0 => Capsule0Id,
                1 => Capsule1Id,
                2 => Capsule2Id,
                _ => Capsule3Id
            }, c);
        }
    }
}
