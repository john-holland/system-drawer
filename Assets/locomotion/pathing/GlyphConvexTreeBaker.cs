using UnityEngine;

/// <summary>
/// Bakes an extruded glyph mesh into a convex MeshCollider + convex mesh tree cache.
/// </summary>
public static class GlyphConvexTreeBaker
{
    public static bool TryBake(Mesh glyphMesh, Transform host, out MeshCollider collider, out ConvexMeshTreeCache cache)
    {
        collider = null;
        cache = null;
        if (glyphMesh == null || host == null)
            return false;

        var filter = host.GetComponent<MeshFilter>();
        if (filter == null)
            filter = host.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = glyphMesh;

        collider = host.GetComponent<MeshCollider>();
        if (collider == null)
            collider = host.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = glyphMesh;
        collider.convex = true;

        if (!ConvexTreeMeshColliderService.EnsureBuilt(collider))
            return false;
        return ConvexTreeMeshColliderService.TryGetCache(collider, out cache);
    }
}
