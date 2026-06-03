using NUnit.Framework;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SdfMaxPlanarNoisePrimitiveTests
    {
        [Test]
        public void FractalNoisePrimitive_ReturnsFiniteValue()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.FractalNoise,
                noiseSeed = 42,
                noiseFrequency = 0.1f
            });
            comp.rootNodeIndex = 0;
            var graph = new SdfMaxExpressionGraph(comp, ScriptableObject.CreateInstance<SdfMaxSolverProfile>(), Matrix4x4.identity);
            float v = graph.SampleWorld(Vector3.zero, 0f);
            Assert.IsFalse(float.IsInfinity(v));
        }
    }
}
