using NUnit.Framework;
using SpatialVolumes;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SyncSDFTreeShapeSkinnedTests
    {
        GameObject _go;
        Transform _bone;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SyncSDFTreeShapeSkinnedTests");
            _bone = new GameObject("Bone").transform;
            _bone.SetParent(_go.transform, false);
            _bone.localPosition = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            SpatialVolumeCacheRegistry.InvalidateAll();
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void BoneMove_ChangesSampleWithoutMeshRebuild()
        {
            var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            comp.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Sphere,
                radius = 1f,
                localPosition = new Vector3(0.5f, 0f, 0f)
            });
            comp.rootNodeIndex = 0;

            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.surfaceGridRes = 12;

            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.composition = comp;
            provider.profile = profile;
            provider.renderMode = SdfMaxRenderMode.SkinnedMesh;
            provider.SyncSDFTreeShape = true;
            provider.notifyOnTransformChange = true;

            var skinned = _go.GetComponent<SdfMaxSkinnedMeshSurface>();
            skinned.rootBone = _go.transform;
            skinned.bones = new[] { _bone };
            skinned.RebuildSurfaceMesh();
            int vertsBefore = skinned.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertexCount;

            Vector3 probe = _go.transform.position + new Vector3(0.5f, 0f, 0f);
            provider.RebuildIfDirty(force: true);
            Assert.IsTrue(provider.TrySample(probe, 0f, out float before, out bool insideBefore));
            Assert.IsTrue(insideBefore);

            _bone.localPosition = new Vector3(1.5f, 0f, 0f);
            provider.NotifyChanged();
            provider.RebuildIfDirty(force: true);

            Assert.IsTrue(provider.TrySample(probe, 0f, out float after, out bool insideAfter));
            Assert.That(Mathf.Abs(after - before), Is.GreaterThan(0.001f));

            int vertsAfter = skinned.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertexCount;
            Assert.AreEqual(vertsBefore, vertsAfter);

            Object.DestroyImmediate(comp);
            Object.DestroyImmediate(profile);
        }
    }
}
