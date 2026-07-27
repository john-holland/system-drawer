using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Breaks food mesh into chewable sections (convex-tree style leaf bounds) and fits against front-teeth ellipsoid.
/// Uses HierarchicalPathFinding convex cache when available; otherwise octree-splits renderer bounds.
/// </summary>
public static class ChewConvexTreeBakeService
{
    public sealed class ChewSection
    {
        public Bounds bounds;
        public int leafIndex;
        public float fitRadius;
        public bool fitsFrontEllipsoid;
        public bool inedible;
    }

    public sealed class BakeResult
    {
        public List<ChewSection> sections = new List<ChewSection>();
        public Bounds frontEllipsoid;
    }

    public static BakeResult Bake(FoodItem food, MouthInteriorRuntime mouth)
    {
        var result = new BakeResult();
        if (food == null || mouth == null)
            return result;

        result.frontEllipsoid = mouth.FrontTeethExposureEllipsoid();
        Mesh mesh = food.meshFilter != null ? food.meshFilter.sharedMesh : null;

        // Prefer convex mesh collider tree cache when present.
        var mc = food.GetComponentInChildren<MeshCollider>();
        if (mc != null && mc.convex && mc.sharedMesh != null &&
            ConvexTreeMeshColliderService.EnsureBuilt(mc) &&
            ConvexTreeMeshColliderService.TryGetCache(mc, out var cache) &&
            cache != null)
        {
            var leaves = ExtractLeafBounds(cache, food.transform);
            for (int i = 0; i < leaves.Count; i++)
            {
                bool inedible = SampleInedible(food.maskInedible, leaves[i].center, food.transform);
                result.sections.Add(MakeSection(leaves[i], i, result.frontEllipsoid, inedible));
            }
            if (result.sections.Count > 0)
                return result;
        }

        Bounds whole;
        if (mesh != null)
        {
            whole = mesh.bounds;
            whole.center = food.transform.TransformPoint(whole.center);
            whole.size = Vector3.Scale(whole.size, food.transform.lossyScale);
        }
        else
        {
            var rend = food.GetComponentInChildren<Renderer>();
            whole = rend != null
                ? rend.bounds
                : new Bounds(food.transform.position, Vector3.one * food.biteFitRadius * 2f);
        }

        // Simple recursive split into chewable chunks (destructible-tree style without asm cycle).
        SplitBounds(whole, 0, 3, result.sections, result.frontEllipsoid, food);
        if (result.sections.Count == 0)
            result.sections.Add(MakeSection(whole, 0, result.frontEllipsoid, false));
        return result;
    }

    static List<Bounds> ExtractLeafBounds(ConvexMeshTreeCache cache, Transform xf)
    {
        var list = new List<Bounds>();
        if (cache == null) return list;
        var leaves = cache.Leaves;
        if (leaves == null) return list;
        for (int i = 0; i < leaves.Count; i++)
        {
            var leaf = leaves[i];
            if (leaf == null) continue;
            list.Add(leaf.Bounds);
        }
        return list;
    }

    static void SplitBounds(
        Bounds b, int depth, int maxDepth,
        List<ChewSection> into, Bounds ellipsoid, FoodItem food)
    {
        float r = b.extents.magnitude;
        float maxR = Mathf.Max(ellipsoid.extents.x, Mathf.Max(ellipsoid.extents.y, ellipsoid.extents.z));
        bool smallEnough = r <= maxR * 1.15f || depth >= maxDepth;
        if (smallEnough)
        {
            bool inedible = SampleInedible(food.maskInedible, b.center, food.transform);
            into.Add(MakeSection(b, into.Count, ellipsoid, inedible));
            return;
        }

        // Split longest axis.
        Vector3 e = b.extents;
        int axis = e.x >= e.y && e.x >= e.z ? 0 : (e.y >= e.z ? 1 : 2);
        Vector3 c = b.center;
        Vector3 s = b.size;
        s[axis] *= 0.5f;
        Vector3 o = Vector3.zero;
        o[axis] = s[axis] * 0.5f;
        SplitBounds(new Bounds(c - o, s), depth + 1, maxDepth, into, ellipsoid, food);
        SplitBounds(new Bounds(c + o, s), depth + 1, maxDepth, into, ellipsoid, food);
    }

    static ChewSection MakeSection(Bounds b, int leaf, Bounds ellipsoid, bool inedible)
    {
        float r = b.extents.magnitude;
        float maxR = Mathf.Max(ellipsoid.extents.x, Mathf.Max(ellipsoid.extents.y, ellipsoid.extents.z));
        return new ChewSection
        {
            bounds = b,
            leafIndex = leaf,
            fitRadius = r,
            fitsFrontEllipsoid = r <= maxR * 1.15f,
            inedible = inedible
        };
    }

    static bool SampleInedible(Texture2D mask, Vector3 world, Transform foodXf)
    {
        if (mask == null || !mask.isReadable || foodXf == null) return false;
        Vector3 local = foodXf.InverseTransformPoint(world);
        float u = Mathf.Clamp01(local.x * 0.5f + 0.5f);
        float v = Mathf.Clamp01(local.z * 0.5f + 0.5f);
        return mask.GetPixelBilinear(u, v).r > 0.5f;
    }
}
