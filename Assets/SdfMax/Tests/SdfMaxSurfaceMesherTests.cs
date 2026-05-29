using NUnit.Framework;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SdfMaxSurfaceMesherTests
    {
        [Test]
        public void SphereSdf_ProducesWatertightMesh()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f
            });
            comp.rootNodeIndex = 0;

            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.surfaceGridRes = 20;
            profile.surfaceIsoLevel = 0f;

            var graph = new SdfMaxExpressionGraph(comp, profile, Matrix4x4.identity);
            var eval = new SdfMaxEvaluator(graph);
            Bounds local = new Bounds(Vector3.zero, Vector3.one * 3f);
            int ver = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(profile, comp);
            var data = SdfMaxSurfaceMesher.Build(eval, local, Matrix4x4.identity, 0f, 20, ver, true);

            Assert.IsTrue(data.IsValid, "Expected non-empty surface mesh for sphere SDF");
            Assert.Greater(data.Vertices.Length, 0);
            Assert.GreaterOrEqual(data.Triangles.Length, 3);

            Object.DestroyImmediate(comp);
            Object.DestroyImmediate(profile);
        }
    }
}
