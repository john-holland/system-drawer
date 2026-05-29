using System.Collections.Generic;
using NUnit.Framework;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class IntegralConvexTreeSolverTests
    {
        [Test]
        public void Build_ProducesLeavesCoveringSamplePoint()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f
            });
            comp.rootNodeIndex = 0;

            var eval = new SdfMaxEvaluator(new SdfMaxExpressionGraph(comp, null, Matrix4x4.identity));
            var solver = new IntegralConvexTreeSolver();
            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.maxDepth = 4;
            profile.minLeafExtent = 0.05f;

            Bounds wb = eval.WorldBounds;
            solver.Build(eval, wb, profile);
            Assert.Greater(solver.Leaves.Count, 0);

            bool covered = false;
            for (int i = 0; i < solver.Leaves.Count; i++)
            {
                if (solver.Leaves[i].LeafBounds.Contains(Vector3.zero))
                {
                    covered = true;
                    break;
                }
            }
            Assert.IsTrue(covered);

            Object.DestroyImmediate(comp);
            Object.DestroyImmediate(profile);
        }
    }
}
