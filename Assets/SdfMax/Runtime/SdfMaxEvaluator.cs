using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxEvaluator
    {
        readonly SdfMaxExpressionGraph _graph;

        public SdfMaxEvaluator(SdfMaxExpressionGraph graph)
        {
            _graph = graph;
        }

        public float Sample(Vector3 worldPos, float narrativeTime)
        {
            return _graph != null ? _graph.SampleWorld(worldPos, narrativeTime) : 1000f;
        }

        public bool IsInside(Vector3 worldPos, float narrativeTime)
        {
            return Sample(worldPos, narrativeTime) < 0f;
        }

        public Bounds WorldBounds => _graph != null ? _graph.ComputeWorldBounds() : new Bounds(Vector3.zero, Vector3.one);
    }
}
