using NUnit.Framework;
using SpatialVolumes;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SyncSDFTreeShapeTests
    {
        GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SyncSDFTreeShapeTests");
            SpatialVolumeCacheRegistry.InvalidateAll();
        }

        [TearDown]
        public void TearDown()
        {
            SpatialVolumeCacheRegistry.InvalidateAll();
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void Provider_WithComposition_BuildsAndSamplesInside()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f
            });
            comp.rootNodeIndex = 0;

            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.backend = VolumeBackend.SdfMaxComposition;
            provider.composition = comp;
            provider.SyncSDFTreeShape = true;
            provider.RebuildIfDirty(force: true);

            Assert.IsTrue(provider.TrySample(_go.transform.position, 0f, out _, out bool inside));
            Assert.IsTrue(inside);
            Object.DestroyImmediate(comp);
        }

        [Test]
        public void TransformMove_WithSyncInvalidatesAndRebuilds()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            SdfMaxMeshAutoSetup.ApplyToComposition(comp, _go.transform, null);
            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.composition = comp;
            provider.SyncSDFTreeShape = true;
            provider.notifyOnTransformChange = true;
            provider.RebuildIfDirty(force: true);

            _go.transform.position = new Vector3(5f, 0f, 0f);
            SpatialVolumeCacheRegistry.Invalidate(provider);
            provider.RebuildIfDirty(force: true);

            Assert.IsTrue(provider.TrySample(new Vector3(5f, 0f, 0f), 0f, out _, out bool inside));
            Assert.IsTrue(inside);
            Object.DestroyImmediate(comp);
        }
    }
}
