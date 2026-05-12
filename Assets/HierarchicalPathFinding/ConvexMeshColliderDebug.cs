using UnityEngine;

/// <summary>
/// Scene debugging for <see cref="ConvexTreeMeshColliderService"/> caches on convex <see cref="MeshCollider"/>.
/// </summary>
[AddComponentMenu("Hierarchical Path Finding/Convex Mesh Collider Debug")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshCollider))]
public class ConvexMeshColliderDebug : MonoBehaviour
{
    [Tooltip("Draw octree leaf bounds as wire cubes (Scene view, when object is selected).")]
    public bool drawConvexTreeLinesInSceneView;

    public Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.9f);

    [Tooltip("Skip drawing leaves beyond this count for performance.")]
    public int maxLeavesToDraw = 2048;

    [Tooltip("Overrides builder max depth when > 0.")]
    [Range(0, 16)]
    public int debugMaxDepth;

    [Tooltip("Overrides min leaf extent when > 0.")]
    public float debugMinLeafExtent;

    [Tooltip("Overrides max triangles per leaf when > 0.")]
    public int debugMaxTrianglesPerLeaf;

    MeshCollider _meshCollider;

    void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawConvexTreeLinesInSceneView)
            return;
        var mc = _meshCollider != null ? _meshCollider : GetComponent<MeshCollider>();
        if (mc == null || !mc.convex)
            return;

        if (!ConvexTreeMeshColliderService.TryGetCache(mc, out var cache))
        {
            TryRebuild(mc);
            if (!ConvexTreeMeshColliderService.TryGetCache(mc, out cache))
                return;
        }

        Gizmos.color = gizmoColor;
        var leaves = cache.Leaves;
        int n = Mathf.Min(leaves.Count, Mathf.Max(1, maxLeavesToDraw));
        int stride = leaves.Count <= n ? 1 : Mathf.CeilToInt((float)leaves.Count / n);
        for (int i = 0; i < leaves.Count; i += stride)
        {
            Bounds b = leaves[i].Bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }

    public void TryRebuild(MeshCollider mc)
    {
        int d = debugMaxDepth > 0 ? debugMaxDepth : ConvexMeshTreeCacheBuilder.DefaultMaxDepth;
        float e = debugMinLeafExtent > 0f ? debugMinLeafExtent : ConvexMeshTreeCacheBuilder.DefaultMinExtent;
        int mt = debugMaxTrianglesPerLeaf > 0 ? debugMaxTrianglesPerLeaf : ConvexMeshTreeCacheBuilder.DefaultMaxTrianglesPerLeaf;
        ConvexTreeMeshColliderService.EnsureBuilt(mc, d, e, mt);
    }
}
