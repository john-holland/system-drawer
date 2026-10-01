using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recursive rigidbody inspection: each body owns meshes/colliders until the next descendant rigidbody.
/// Shared by doorway convex refit and destructible prebake.
/// </summary>
public static class RigidbodyPhysicsWalk
{
    public const string DoorwayPortalTag = "doorway_portal";

    public sealed class BodyMeshGroup
    {
        public Rigidbody body;
        public readonly List<MeshFilter> meshFilters = new List<MeshFilter>();
        public readonly List<MeshCollider> meshColliders = new List<MeshCollider>();
        public readonly List<Renderer> renderers = new List<Renderer>();
    }

    public static List<BodyMeshGroup> Collect(Transform root, bool includeInactive = true)
    {
        var groups = new List<BodyMeshGroup>();
        if (root == null)
            return groups;

        var bodies = root.GetComponentsInChildren<Rigidbody>(includeInactive);
        if (bodies == null || bodies.Length == 0)
        {
            CollectMeshFiltersWithoutBody(root, includeInactive, groups);
            return groups;
        }

        var claimed = new HashSet<int>();
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (rb == null)
                continue;
            var group = new BodyMeshGroup { body = rb };
            CollectOwned(rb.transform, rb, includeInactive, group, claimed);
            if (group.meshFilters.Count > 0 || group.meshColliders.Count > 0)
                groups.Add(group);
        }
        return groups;
    }

    static void CollectMeshFiltersWithoutBody(Transform root, bool includeInactive, List<BodyMeshGroup> groups)
    {
        var group = new BodyMeshGroup();
        var filters = root.GetComponentsInChildren<MeshFilter>(includeInactive);
        for (int i = 0; i < filters.Length; i++)
        {
            if (SkipPortal(filters[i] != null ? filters[i].gameObject : null))
                continue;
            if (filters[i] != null && filters[i].sharedMesh != null)
                group.meshFilters.Add(filters[i]);
        }
        if (group.meshFilters.Count > 0)
            groups.Add(group);
    }

    static void CollectOwned(Transform t, Rigidbody owner, bool includeInactive, BodyMeshGroup group, HashSet<int> claimed)
    {
        if (t == null || (!includeInactive && !t.gameObject.activeInHierarchy))
            return;
        if (SkipPortal(t.gameObject))
            return;

        var other = t.GetComponent<Rigidbody>();
        if (other != null && other != owner)
            return;

        int id = t.GetInstanceID();
        if (!claimed.Add(id))
            return;

        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            group.meshFilters.Add(mf);
        var mc = t.GetComponent<MeshCollider>();
        if (mc != null)
            group.meshColliders.Add(mc);
        var rend = t.GetComponent<Renderer>();
        if (rend != null)
            group.renderers.Add(rend);

        for (int i = 0; i < t.childCount; i++)
            CollectOwned(t.GetChild(i), owner, includeInactive, group, claimed);
    }

    public static bool SkipPortal(GameObject go)
    {
        if (go == null)
            return false;
        if (string.Equals(go.tag, DoorwayPortalTag, System.StringComparison.Ordinal)
            || go.name.IndexOf(DoorwayPortalTag, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var portal = go.GetComponent<DoorwayEdgePortal>();
        if (portal != null && portal.overlayRenderer != null && portal.overlayRenderer.gameObject == go)
            return true;
        return portal != null && portal.IsOverlay(go);
    }
}
