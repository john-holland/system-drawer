using NUnit.Framework;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SdfMaxEvaluatorTests
    {
        [Test]
        public void MaxUnion_InsideSphere_ReturnsNegative()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f
            });
            comp.rootNodeIndex = 0;

            var graph = new SdfMaxExpressionGraph(comp, null, Matrix4x4.identity);
            var eval = new SdfMaxEvaluator(graph);
            Assert.Less(eval.Sample(Vector3.zero, 0f), 0f);
            Assert.Greater(eval.Sample(new Vector3(2f, 0f, 0f), 0f), 0f);
            Object.DestroyImmediate(comp);
        }

        [Test]
        public void Subtract_CarvesCenter()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Box,
                halfExtents = Vector3.one
            });
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 0.4f
            });
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Subtract,
                childIndexA = 0,
                childIndexB = 1
            });
            comp.rootNodeIndex = 2;

            var eval = new SdfMaxEvaluator(new SdfMaxExpressionGraph(comp, null, Matrix4x4.identity));
            Assert.Greater(eval.Sample(Vector3.zero, 0f), 0f);
            Object.DestroyImmediate(comp);
        }
    }
}
