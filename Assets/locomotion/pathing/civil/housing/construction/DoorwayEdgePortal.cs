using System.Collections.Generic;
using UnityEngine;

/// <summary>Doorway / garage-door edge portal: clip shader + convex refit of transiting child rigidbodies.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Doorway Edge Portal")]
public sealed class DoorwayEdgePortal : MonoBehaviour
{
    public const string OverlayName = "doorway_portal";

    public List<Vector3> edgeLoopLocal = new List<Vector3>();
    public Vector3 loopNormal = Vector3.forward;
    [Range(0f, 1f)] public float open01 = 1f;
    public MeshRenderer overlayRenderer;
    public PathingAperture aperture;
    public string jointId = "door";
    public bool restoreOnExit = true;

    readonly Dictionary<int, Mesh> _prebakedMeshes = new Dictionary<int, Mesh>();
    readonly HashSet<int> _inside = new HashSet<int>();

    public bool IsOverlay(GameObject go) =>
        overlayRenderer != null && go != null && overlayRenderer.gameObject == go;

    public void EnsureOverlay()
    {
        if (overlayRenderer != null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = OverlayName;
        go.transform.SetParent(transform, false);
        var col = go.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);
        overlayRenderer = go.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Locomotion/DoorwayPortalOcclusion") ?? Shader.Find("Unlit/Color");
        if (overlayRenderer != null && shader != null)
            overlayRenderer.sharedMaterial = new Material(shader);
        ApplyOpen();
    }

    public void ApplyOpen()
    {
        if (overlayRenderer == null) return;
        var mat = overlayRenderer.material;
        if (mat.HasProperty("_Open01"))
            mat.SetFloat("_Open01", open01);
        overlayRenderer.enabled = open01 > 0.01f;
    }

    public bool PointInsideOpening(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        if (Vector3.Dot(local, loopNormal) < -0.05f || Vector3.Dot(local, loopNormal) > 0.05f + 0.5f)
            return false;
        return open01 > 0.05f;
    }

    public int RefitTransiting(Transform root)
    {
        if (root == null || open01 < 0.05f) return 0;
        EnsureOverlay();
        var groups = RigidbodyPhysicsWalk.Collect(root, true);
        int n = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            if (g.body == null) continue;
            bool crossing = PointInsideOpening(g.body.worldCenterOfMass);
            int id = g.body.GetInstanceID();
            if (!crossing)
            {
                if (_inside.Remove(id) && restoreOnExit)
                    RestoreGroup(g);
                continue;
            }
            _inside.Add(id);
            for (int c = 0; c < g.meshColliders.Count; c++)
            {
                var mc = g.meshColliders[c];
                if (mc == null || mc.sharedMesh == null) continue;
                if (!_prebakedMeshes.ContainsKey(mc.GetInstanceID()))
                    _prebakedMeshes[mc.GetInstanceID()] = mc.sharedMesh;
                mc.convex = true;
                ConvexTreeMeshColliderService.Invalidate(mc);
                ConvexTreeMeshColliderService.EnsureBuilt(mc);
                n++;
            }
        }
        return n;
    }

    void RestoreGroup(RigidbodyPhysicsWalk.BodyMeshGroup g)
    {
        for (int c = 0; c < g.meshColliders.Count; c++)
        {
            var mc = g.meshColliders[c];
            if (mc == null) continue;
            if (_prebakedMeshes.TryGetValue(mc.GetInstanceID(), out var mesh) && mesh != null)
                mc.sharedMesh = mesh;
            ConvexTreeMeshColliderService.Invalidate(mc);
            ConvexTreeMeshColliderService.EnsureBuilt(mc);
        }
    }
}
