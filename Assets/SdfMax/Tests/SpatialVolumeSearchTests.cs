using System.Collections.Generic;
using NUnit.Framework;
using SpatialVolumes;
using SdfMax;
using UnityEngine;

namespace SdfMax.Tests
{
    public class SpatialVolumeSearchTests
    {
        [Test]
        public void SearchLeaves_ReturnsOverlappingLeaf()
        {
            var go = new GameObject("SpatialVolumeSearchTests");
            try
            {
                var comp = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
                comp.nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.Box,
                    halfExtents = Vector3.one
                });
                comp.rootNodeIndex = 0;

                var provider = go.AddComponent<SpatialVolumeProvider>();
                provider.composition = comp;
                provider.RebuildIfDirty(force: true);

                var results = new List<SpatialVolumeLeaf>();
                Bounds q = new Bounds(Vector3.zero, Vector3.one * 4f);
                Assert.IsTrue(provider.SearchLeaves(q, 0f, results));
                Assert.Greater(results.Count, 0);
                Object.DestroyImmediate(comp);
            }
            finally
            {
                Object.DestroyImmediate(go);
                SpatialVolumeCacheRegistry.InvalidateAll();
            }
        }
    }
}
