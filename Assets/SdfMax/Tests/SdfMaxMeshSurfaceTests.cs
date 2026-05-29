using NUnit.Framework;
using SpatialVolumes;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SdfMaxMeshSurfaceTests
    {
        GameObject _go;

        [SetUp]
        public void SetUp() => _go = new GameObject("SdfMaxMeshSurfaceTests");

        [TearDown]
        public void TearDown()
        {
            SpatialVolumeCacheRegistry.InvalidateAll();
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void RebuildSurfaceMesh_AssignsSharedMesh()
        {
            var comp = CreateSphereComposition();
            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.generateSurfaceMesh = true;
            profile.surfaceGridRes = 16;

            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.backend = VolumeBackend.SdfMaxComposition;
            provider.composition = comp;
            provider.profile = profile;
            provider.renderMode = SdfMaxRenderMode.StaticMesh;
            provider.SyncSDFTreeShape = true;

            var surface = _go.GetComponent<SdfMaxMeshSurface>();
            Assert.IsNotNull(surface);
            surface.RebuildSurfaceMesh();

            var mf = _go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf.sharedMesh);
            Assert.Greater(surface.LastVertexCount, 0);

            Object.DestroyImmediate(comp);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void TransformMove_DoesNotChangeVertexCount()
        {
            var comp = CreateSphereComposition();
            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.surfaceGridRes = 14;

            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.composition = comp;
            provider.profile = profile;
            provider.renderMode = SdfMaxRenderMode.StaticMesh;
            provider.SyncSDFTreeShape = true;

            var surface = _go.GetComponent<SdfMaxMeshSurface>();
            surface.RebuildSurfaceMesh();
            int before = surface.LastVertexCount;

            _go.transform.position = new Vector3(10f, 2f, -3f);
            _go.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            Assert.AreEqual(before, surface.LastVertexCount);

            Object.DestroyImmediate(comp);
            Object.DestroyImmediate(profile);
        }

        static SdfMaxCompositionAsset CreateSphereComposition()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f
            });
            comp.rootNodeIndex = 0;
            return comp;
        }
    }
}
