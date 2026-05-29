using UnityEngine;

namespace SdfMax
{
    public static class SdfMaxMeshAutoSetup
    {
        public static bool TryComputeHierarchyBounds(Transform root, out Bounds localBounds)
        {
            localBounds = new Bounds(Vector3.zero, Vector3.zero);
            if (root == null)
                return false;

            bool any = false;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Bounds wb = renderers[i].bounds;
                Vector3 lc = root.InverseTransformPoint(wb.center);
                Vector3 ls = root.InverseTransformVector(wb.extents);
                var lb = new Bounds(lc, ls * 2f);
                if (!any) { localBounds = lb; any = true; }
                else localBounds.Encapsulate(lb);
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                Bounds wb = colliders[i].bounds;
                Vector3 lc = root.InverseTransformPoint(wb.center);
                Vector3 ls = root.InverseTransformVector(wb.extents);
                var lb = new Bounds(lc, ls * 2f);
                if (!any) { localBounds = lb; any = true; }
                else localBounds.Encapsulate(lb);
            }

            if (!any)
            {
                localBounds = new Bounds(Vector3.zero, Vector3.one);
                return false;
            }

            return true;
        }

        public static void ApplyToComposition(SdfMaxCompositionAsset composition, Transform root, SdfMaxSolverProfile profile)
        {
            if (composition == null || root == null)
                return;

            if (!TryComputeHierarchyBounds(root, out Bounds localBounds))
                localBounds = new Bounds(Vector3.zero, Vector3.one);

            composition.nodes.Clear();

            var box = new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.MeshBounds,
                localPosition = localBounds.center,
                halfExtents = localBounds.extents,
                tMin = profile != null ? profile.defaultTMin : 0f,
                tMax = profile != null ? profile.defaultTMax : 1f
            };
            composition.nodes.Add(box);

            Vector3 size = localBounds.size;
            float maxAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            float minAxis = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (maxAxis > minAxis * 1.75f && composition.nodes.Count == 1)
            {
                int longAxis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
                Vector3 he = localBounds.extents * 0.45f;
                Vector3 offset = Vector3.zero;
                offset[longAxis] = localBounds.extents[longAxis] * 0.55f;

                var extra = new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.Box,
                    localPosition = localBounds.center + offset,
                    halfExtents = he,
                    tMin = box.tMin,
                    tMax = box.tMax
                };
                composition.nodes.Add(extra);

                var smooth = new SdfMaxNode
                {
                    op = SdfMaxOp.SmoothMax,
                    childIndexA = 0,
                    childIndexB = 1,
                    smoothRadius = 0.25f,
                    tMin = box.tMin,
                    tMax = box.tMax
                };
                composition.nodes.Add(smooth);
                composition.rootNodeIndex = 2;
            }
            else
            {
                composition.rootNodeIndex = 0;
            }
        }
    }
}
