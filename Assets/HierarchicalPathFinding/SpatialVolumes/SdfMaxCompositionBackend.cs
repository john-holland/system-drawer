using System.Collections.Generic;
using SdfMax;
using UnityEngine;

namespace SpatialVolumes
{
    public sealed class SdfMaxCompositionBackend : ISpatialVolumeBackend
    {
        SdfMaxEvaluator _evaluator;
        SdfMaxExpressionGraph _graph;
        IntegralConvexTreeSolver _integralTree = new IntegralConvexTreeSolver();
        SdfMaxGridCache _gridCache = new SdfMaxGridCache();
        SdfMaxBoneFieldContext _boneContext;
        int _buildVersion;
        Bounds _worldBounds;
        Matrix4x4 _localToWorld;

        public int BuildVersion => _buildVersion;
        public Bounds WorldBounds => _worldBounds;

        public bool EnsureBuilt(SpatialVolumeBuildContext ctx)
        {
            if (ctx.Composition == null || ctx.Composition.nodes == null || ctx.Composition.nodes.Count == 0)
            {
                _evaluator = null;
                _worldBounds = ctx.ProviderTransform != null
                    ? new Bounds(ctx.ProviderTransform.position, Vector3.one)
                    : new Bounds(Vector3.zero, Vector3.one);
                return false;
            }

            _localToWorld = ctx.ProviderTransform != null ? ctx.ProviderTransform.localToWorldMatrix : Matrix4x4.identity;
            _boneContext = ctx.BoneFieldContext;
            _graph = new SdfMaxExpressionGraph(ctx.Composition, ctx.Profile, _localToWorld);
            _evaluator = new SdfMaxEvaluator(_graph);
            _worldBounds = _evaluator.WorldBounds;
            _integralTree.Build(_evaluator, _worldBounds, ctx.Profile);

            var profile = ctx.Profile;
            if (profile != null && profile.useGridCache)
            {
                _gridCache.Build(
                    _evaluator,
                    _worldBounds,
                    profile.gridResX,
                    profile.gridResY,
                    profile.gridResZ);
            }

            _buildVersion++;
            return true;
        }

        public void Invalidate()
        {
            _evaluator = null;
            _graph = null;
            _buildVersion++;
        }

        public void CollectLeaves(Bounds query, float t, List<SpatialVolumeLeaf> outLeaves)
        {
            if (outLeaves == null || _integralTree == null)
                return;

            var leaves = _integralTree.Leaves;
            for (int i = 0; i < leaves.Count; i++)
            {
                Bounds wb = leaves[i].LeafBounds;
                if (wb.Intersects(query))
                    outLeaves.Add(new SpatialVolumeLeaf { Bounds = wb, SourceLeafIndex = i });
            }
        }

        public float Sample(Vector3 worldPos, float t)
        {
            if (_evaluator == null)
                return 1000f;
            if (_boneContext != null && _boneContext.IsValid)
            {
                Vector3 bind = _boneContext.WorldToBind(worldPos);
                Vector3 sampleWorld = _boneContext.BindRootLocalToWorld.MultiplyPoint3x4(bind);
                return _evaluator.Sample(sampleWorld, t);
            }
            return _evaluator.Sample(worldPos, t);
        }

        public bool IsInside(Vector3 worldPos, float t)
        {
            return Sample(worldPos, t) < 0f;
        }

        public void ExportVolumeBounds(List<SpatialVolumeBounds4> outVolumes, float tMin, float tMax, Matrix4x4 localToWorld)
        {
            if (outVolumes == null || _integralTree == null)
                return;
            var leaves = _integralTree.Leaves;
            for (int i = 0; i < leaves.Count; i++)
            {
                Bounds wb = leaves[i].LeafBounds;
                outVolumes.Add(SpatialVolumeBounds4.FromBoundsAndTime(wb, tMin, tMax));
            }
        }
    }
}
