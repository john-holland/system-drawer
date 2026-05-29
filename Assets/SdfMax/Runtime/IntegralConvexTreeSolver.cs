using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    public sealed class IntegralConvexTreeSolver : IIntegralConvexTreeSolver
    {
        readonly List<IntegralConvexTreeNode> _leaves = new List<IntegralConvexTreeNode>();
        Bounds _rootBounds;

        public IReadOnlyList<IntegralConvexTreeNode> Leaves => _leaves;
        public Bounds RootBounds => _rootBounds;

        public void Build(SdfMaxEvaluator evaluator, Bounds localBounds, SdfMaxSolverProfile profile)
        {
            _leaves.Clear();
            _rootBounds = localBounds;
            if (evaluator == null)
                return;

            int maxDepth = profile != null ? profile.maxDepth : 8;
            float minExtent = profile != null ? profile.minLeafExtent : 0.1f;
            float eps = profile != null ? profile.sampleEpsilon : 0.001f;

            var pending = new List<Bounds> { localBounds };
            var depth = new List<int> { maxDepth };

            while (pending.Count > 0)
            {
                int last = pending.Count - 1;
                Bounds b = pending[last];
                int d = depth[last];
                pending.RemoveAt(last);
                depth.RemoveAt(last);

                float variation = EstimateVariation(evaluator, b);
                float ext = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 2f;

                if (d <= 0 || ext <= minExtent + 1e-5f || variation < eps)
                {
                    _leaves.Add(new IntegralConvexTreeNode
                    {
                        LeafBounds = b,
                        IntegratedMeasure = variation
                    });
                    continue;
                }

                Vector3 center = b.center;
                Vector3 grad = Gradient(evaluator, center, eps);
                if (grad.sqrMagnitude < 1e-8f)
                {
                    _leaves.Add(new IntegralConvexTreeNode { LeafBounds = b, IntegratedMeasure = variation });
                    continue;
                }

                int axis = DominantAxis(grad);
                float split = center[axis];
                Bounds a = b;
                Bounds c = b;
                Vector3 sizeA = b.size;
                Vector3 sizeC = b.size;
                sizeA[axis] *= 0.5f;
                sizeC[axis] *= 0.5f;
                Vector3 centerA = b.center;
                Vector3 centerC = b.center;
                centerA[axis] = split - sizeA[axis] * 0.5f;
                centerC[axis] = split + sizeC[axis] * 0.5f;
                a.center = centerA;
                a.size = sizeA;
                c.center = centerC;
                c.size = sizeC;

                pending.Add(a);
                depth.Add(d - 1);
                pending.Add(c);
                depth.Add(d - 1);
            }

            if (_leaves.Count == 0)
                _leaves.Add(new IntegralConvexTreeNode { LeafBounds = localBounds });
        }

        static float EstimateVariation(SdfMaxEvaluator eval, Bounds b)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;
            float min = float.MaxValue;
            float max = float.MinValue;
            SampleCorner(eval, c, e, ref min, ref max);
            SampleCorner(eval, c, -e, ref min, ref max);
            return max - min;
        }

        static void SampleCorner(SdfMaxEvaluator eval, Vector3 c, Vector3 e, ref float min, ref float max)
        {
            for (int ix = -1; ix <= 1; ix += 2)
            for (int iy = -1; iy <= 1; iy += 2)
            for (int iz = -1; iz <= 1; iz += 2)
            {
                Vector3 p = c + Vector3.Scale(e, new Vector3(ix, iy, iz));
                float v = eval.Sample(p, 0f);
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        static Vector3 Gradient(SdfMaxEvaluator eval, Vector3 p, float eps)
        {
            float dx = eval.Sample(p + Vector3.right * eps, 0f) - eval.Sample(p - Vector3.right * eps, 0f);
            float dy = eval.Sample(p + Vector3.up * eps, 0f) - eval.Sample(p - Vector3.up * eps, 0f);
            float dz = eval.Sample(p + Vector3.forward * eps, 0f) - eval.Sample(p - Vector3.forward * eps, 0f);
            return new Vector3(dx, dy, dz) / (2f * eps);
        }

        static int DominantAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x);
            float ay = Mathf.Abs(v.y);
            float az = Mathf.Abs(v.z);
            if (ax >= ay && ax >= az) return 0;
            if (ay >= az) return 1;
            return 2;
        }
    }
}
