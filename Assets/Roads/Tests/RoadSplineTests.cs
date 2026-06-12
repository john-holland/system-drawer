using NUnit.Framework;
using UnityEngine;

namespace Roads.Tests
{
    public class RoadSplineTests
    {
        [Test]
        public void CatmullRom_ArcLength_IsMonotonic()
        {
            var go = new GameObject("test_road");
            var spline = go.AddComponent<RoadSplineBase>();
            spline.controlPoints.Add(new Vector3(0, 0, 0));
            spline.controlPoints.Add(new Vector3(10, 0, 0));
            spline.controlPoints.Add(new Vector3(20, 5, 0));
            spline.controlPoints.Add(new Vector3(30, 5, 0));
            spline.RebuildLengthTable();

            var samples = spline.BuildSamples(2f);
            float prev = -1f;
            foreach (var s in samples)
            {
                Assert.GreaterOrEqual(s.distance, prev);
                prev = s.distance;
            }
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RibbonMesh_HasValidUVRange()
        {
            var go = new GameObject("test_ribbon");
            var spline = go.AddComponent<RoadSpline3D>();
            spline.controlPoints.Add(new Vector3(0, 0, 0));
            spline.controlPoints.Add(new Vector3(10, 0, 0));
            spline.controlPoints.Add(new Vector3(20, 0, 0));
            spline.conformToTerrain = false;
            var sampler = go.AddComponent<SplinePathMeshSampler>();
            sampler.spline = spline;
            sampler.sampleSpacingMeters = 2f;

            var mesh = sampler.BuildRibbonMesh();
            Assert.Greater(mesh.vertexCount, 0);
            var uvs = mesh.uv;
            foreach (var uv in uvs)
            {
                Assert.GreaterOrEqual(uv.y, 0f);
                Assert.LessOrEqual(uv.y, 1f);
            }
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bounds4Gateway_ExportSnapshot_HasSegmentId()
        {
            var go = new GameObject("test_4d");
            var rs4d = go.AddComponent<RoadSpline4D>();
            rs4d.controlPoints.Add(Vector3.zero);
            rs4d.controlPoints.Add(new Vector3(5, 0, 0));
            rs4d.controlPoints.Add(new Vector3(10, 0, 0));
            var snap = rs4d.ExportSnapshot();
            Assert.IsNotNull(snap.roadSegmentId);
            Assert.AreEqual(3, snap.controlPoints.Count);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ErosionSplit_ProducesPieces()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                Vector3.zero, Vector3.right, Vector3.forward,
                Vector3.one, Vector3.up, Vector3.left
            };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            var peak = new RoadFlowCell
            {
                arcLength = 5f,
                flowDir = Vector3.forward,
                intensity = 2f
            };
            var pieces = RoadMeshTriangleSplitter.SplitByFlow(mesh, peak, 1f, 1f);
            Assert.Greater(pieces.Count, 0);
        }

        [Test]
        public void FlowBreak_OrientedParallelToFlow()
        {
            var flow = new RoadFlowCell { flowDir = Vector3.right, intensity = 1.5f, arcLength = 3f };
            Assert.AreEqual(1f, flow.flowDir.x, 0.01f);
            Assert.Greater(flow.intensity, 0f);
        }

        [Test]
        public void CacheMode_EnumValues_Exist()
        {
            Assert.AreEqual(0, (int)RoadErosionCacheMode.PREBAKE);
            Assert.AreEqual(1, (int)RoadErosionCacheMode.CACHE);
            Assert.AreEqual(2, (int)RoadErosionCacheMode.NOCACHE);
        }
    }
}
