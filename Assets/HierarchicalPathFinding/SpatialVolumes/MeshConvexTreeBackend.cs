using System.Collections.Generic;
using UnityEngine;

namespace SpatialVolumes
{
    public sealed class MeshConvexTreeBackend : ISpatialVolumeBackend
    {
        MeshCollider _meshCollider;
        ConvexMeshTreeCache _cache;
        int _buildVersion;
        Bounds _worldBounds;

        public int BuildVersion => _buildVersion;
        public Bounds WorldBounds => _worldBounds;

        public bool EnsureBuilt(SpatialVolumeBuildContext ctx)
        {
            _meshCollider = ctx.MeshCollider;
            if (_meshCollider == null || !_meshCollider.convex || _meshCollider.sharedMesh == null)
            {
                _cache = null;
                _worldBounds = ctx.ProviderTransform != null
                    ? new Bounds(ctx.ProviderTransform.position, Vector3.one)
                    : new Bounds(Vector3.zero, Vector3.one);
                return false;
            }

            if (!ConvexTreeMeshColliderService.EnsureBuilt(
                    _meshCollider, ctx.MaxDepth, ctx.MinLeafExtent, ctx.MaxTrianglesPerLeaf))
            {
                _cache = null;
                return false;
            }

            if (!ConvexTreeMeshColliderService.TryGetCache(_meshCollider, out _cache) || _cache == null)
                return false;

            _buildVersion = _cache.BuildVersion;
            _worldBounds = _cache.RootBounds;
            return _cache._leaves.Count > 0;
        }

        public void Invalidate()
        {
            if (_meshCollider != null)
                ConvexTreeMeshColliderService.Invalidate(_meshCollider);
            _cache = null;
        }

        public void CollectLeaves(Bounds query, float t, List<SpatialVolumeLeaf> outLeaves)
        {
            if (_cache == null || outLeaves == null)
                return;

            var leaves = _cache._leaves;
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].Bounds.Intersects(query))
                {
                    var leaf = new SpatialVolumeLeaf { Bounds = leaves[i].Bounds, SourceLeafIndex = i };
                    leaf.TriangleIndices.AddRange(leaves[i]._triangleIndices);
                    outLeaves.Add(leaf);
                }
            }
        }

        public float Sample(Vector3 worldPos, float t)
        {
            if (_cache == null)
                return 1000f;
            float best = 1000f;
            var leaves = _cache._leaves;
            for (int i = 0; i < leaves.Count; i++)
            {
                if (!leaves[i].Bounds.Contains(worldPos))
                    continue;
                float d = DistanceToBoundsSurface(leaves[i].Bounds, worldPos);
                if (d < best)
                    best = d;
            }
            return best;
        }

        public bool IsInside(Vector3 worldPos, float t)
        {
            return _meshCollider != null && _meshCollider.bounds.Contains(worldPos);
        }

        public void ExportVolumeBounds(List<SpatialVolumeBounds4> outVolumes, float tMin, float tMax, Matrix4x4 localToWorld)
        {
            if (_cache == null || outVolumes == null)
                return;
            var leaves = _cache._leaves;
            for (int i = 0; i < leaves.Count; i++)
                outVolumes.Add(SpatialVolumeBounds4.FromBoundsAndTime(leaves[i].Bounds, tMin, tMax));
        }

        static float DistanceToBoundsSurface(Bounds b, Vector3 p)
        {
            Vector3 c = b.ClosestPoint(p);
            return (p - c).magnitude;
        }
    }
}
