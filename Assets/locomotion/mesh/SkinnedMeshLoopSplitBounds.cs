using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Movable unit-cube bounds parented to a loop-section mesh. Overlap tests use the transform
/// (move / rotate / scale). Local AABB is centered at origin with size (1,1,1).
/// Holds the picker asset / mesh prefab and the associated loop name.
/// </summary>
[AddComponentMenu("Locomotion/Mesh/Skinned Mesh Loop Split Bounds")]
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SkinnedMeshLoopSplitBounds : MonoBehaviour
{
    public SkinnedMeshLoopSectionAsset sectionAsset;
    public GameObject meshPrefab;
    public string loopId;
    public string loopName;
    public string displayName;

    public void Associate(string id, string name, SkinnedMeshLoopSectionAsset asset, GameObject prefab)
    {
        loopId = id;
        loopName = name ?? "";
        displayName = loopName;
        sectionAsset = asset;
        if (prefab != null)
            meshPrefab = prefab;
        gameObject.name = ObjectName(loopName);
    }

    void OnValidate()
    {
        if (sectionAsset == null || string.IsNullOrEmpty(loopId))
            return;
        var loop = sectionAsset.GetLoop(loopId);
        if (loop == null || string.IsNullOrEmpty(loop.displayName))
            return;
        loopName = loop.displayName;
        displayName = loopName;
    }

    public static SkinnedMeshLoopSplitBounds FindForLoop(Transform meshRoot, string loopId)
    {
        if (meshRoot == null || string.IsNullOrEmpty(loopId))
            return null;
        var all = meshRoot.GetComponentsInChildren<SkinnedMeshLoopSplitBounds>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].loopId == loopId)
                return all[i];
        }
        return null;
    }

    public static SkinnedMeshLoopSplitBounds CreateUnderMesh(
        Transform meshRoot,
        Mesh mesh,
        string loopId,
        string displayName,
        SkinnedMeshLoopSectionAsset asset = null,
        GameObject meshPrefab = null)
    {
        if (meshRoot == null)
            return null;
        var existing = FindForLoop(meshRoot, loopId);
        if (existing != null)
        {
            existing.Associate(loopId, displayName, asset, meshPrefab);
            return existing;
        }

        var go = new GameObject(ObjectName(displayName));
        go.transform.SetParent(meshRoot, false);
        Bounds b = mesh != null ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        go.transform.localPosition = b.center;
        go.transform.localRotation = Quaternion.identity;
        float edge = Mathf.Max(0.05f, b.extents.magnitude * 0.7f);
        go.transform.localScale = new Vector3(edge, edge, edge);
        var box = go.AddComponent<SkinnedMeshLoopSplitBounds>();
        box.Associate(loopId, displayName, asset, meshPrefab);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = Vector3.zero;
        col.size = Vector3.one;
        return box;
    }

    public static string ObjectName(string displayName)
    {
        string n = string.IsNullOrEmpty(displayName) ? "Loop" : displayName;
        return "SplitBounds_" + n;
    }

    public bool ContainsWorldPoint(Vector3 world)
    {
        Vector3 p = transform.InverseTransformPoint(world);
        return Mathf.Abs(p.x) <= 0.5f && Mathf.Abs(p.y) <= 0.5f && Mathf.Abs(p.z) <= 0.5f;
    }

    public int CollectOverlapping(Vector3[] meshLocalVerts, Matrix4x4 meshLocalToWorld, List<int> dst)
    {
        if (dst == null)
            return 0;
        dst.Clear();
        if (meshLocalVerts == null)
            return 0;
        for (int i = 0; i < meshLocalVerts.Length; i++)
        {
            Vector3 world = meshLocalToWorld.MultiplyPoint3x4(meshLocalVerts[i]);
            if (ContainsWorldPoint(world))
                dst.Add(i);
        }
        return dst.Count;
    }

    /// <summary>
    /// Triangle indices (0-based faces) that have at least one vertex inside the current bounds pose.
    /// </summary>
    public int CollectOverlappingTriangles(
        Vector3[] meshLocalVerts,
        int[] triangles,
        Matrix4x4 meshLocalToWorld,
        List<int> dst)
    {
        if (dst == null)
            return 0;
        dst.Clear();
        if (meshLocalVerts == null || triangles == null)
            return 0;
        int triCount = triangles.Length / 3;
        for (int t = 0; t < triCount; t++)
        {
            int ia = triangles[t * 3];
            int ib = triangles[t * 3 + 1];
            int ic = triangles[t * 3 + 2];
            if (ia < 0 || ib < 0 || ic < 0 ||
                ia >= meshLocalVerts.Length || ib >= meshLocalVerts.Length || ic >= meshLocalVerts.Length)
                continue;
            Vector3 a = meshLocalToWorld.MultiplyPoint3x4(meshLocalVerts[ia]);
            Vector3 b = meshLocalToWorld.MultiplyPoint3x4(meshLocalVerts[ib]);
            Vector3 c = meshLocalToWorld.MultiplyPoint3x4(meshLocalVerts[ic]);
            if (ContainsWorldPoint(a) || ContainsWorldPoint(b) || ContainsWorldPoint(c))
                dst.Add(t);
        }
        return dst.Count;
    }

    public int ApplyOverlappingTriangles(
        SkinnedMeshLoopSectionAsset.LoopSection loop,
        Vector3[] meshLocalVerts,
        int[] triangles,
        Matrix4x4 meshLocalToWorld)
    {
        if (loop == null)
            return 0;
        if (loop.assignedTriangles == null)
            loop.assignedTriangles = new List<int>();
        CollectOverlappingTriangles(meshLocalVerts, triangles, meshLocalToWorld, loop.assignedTriangles);
        loop.seedTriangle = loop.assignedTriangles.Count > 0 ? loop.assignedTriangles[0] : -1;
        return loop.assignedTriangles.Count;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.55f, 0.35f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.55f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
