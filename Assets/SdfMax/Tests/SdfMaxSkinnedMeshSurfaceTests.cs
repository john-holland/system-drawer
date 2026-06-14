using NUnit.Framework;
using SpatialVolumes;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SdfMaxSkinnedMeshSurfaceTests
    {
        GameObject _go;
        Transform _boneA;
        Transform _boneB;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SdfMaxSkinnedMeshSurfaceTests");
            _boneA = new GameObject("BoneA").transform;
            _boneB = new GameObject("BoneB").transform;
            _boneA.SetParent(_go.transform, false);
            _boneB.SetParent(_go.transform, false);
            _boneA.localPosition = new Vector3(-0.5f, 0f, 0f);
            _boneB.localPosition = new Vector3(0.5f, 0f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            SpatialVolumeCacheRegistry.InvalidateAll();
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void RebuildSurfaceMesh_BoneWeightsSumToOne()
        {
            var comp = CreateSphereComposition();
            var profile = ScriptableObject.CreateInstance<SdfMaxSolverProfile>();
            profile.surfaceGridRes = 14;

            var provider = _go.AddComponent<SpatialVolumeProvider>();
            provider.composition = comp;
            provider.profile = profile;
            provider.renderMode = SdfMaxRenderMode.SkinnedMesh;
            provider.SyncSDFTreeShape = true;
            provider.SyncRenderModeComponents();

            var skinned = _go.GetComponent<SdfMaxSkinnedMeshSurface>();
            skinned.rootBone = _go.transform;
            skinned.bones = new[] { _boneA, _boneB };
            skinned.RebuildSurfaceMesh();

            var smr = _go.GetComponent<SkinnedMeshRenderer>();
            Assert.IsNotNull(smr.sharedMesh);
            Assert.AreEqual(2, smr.bones.Length);

            var weights = smr.sharedMesh.boneWeights;
            Assert.Greater(weights.Length, 0);
            for (int i = 0; i < weights.Length; i++)
            {
                float sum = weights[i].weight0 + weights[i].weight1 + weights[i].weight2 + weights[i].weight3;
                Assert.AreEqual(1f, sum, 0.05f);
            }

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
