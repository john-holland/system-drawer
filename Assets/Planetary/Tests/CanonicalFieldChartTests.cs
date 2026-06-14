#if UNITY_EDITOR
using NUnit.Framework;
using Planetary.Composition;
using Planetary.Field;
using UnityEngine;
using Weather;

namespace Planetary.Tests
{
    public class CanonicalFieldChartTests
    {
        [Test]
        public void TransitionWeights_SumToOne()
        {
            var settings = ScriptableObject.CreateInstance<HorizonLodSettings>();
            TransitionWeightSet w = TransitionWeightSet.Compute(500f, 10f, 0f, 2000f, settings);
            Assert.AreEqual(1f, w.Sum, 0.001f);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void TransitionWeights_SurfaceDominatesNearGround()
        {
            var settings = ScriptableObject.CreateInstance<HorizonLodSettings>();
            TransitionWeightSet w = TransitionWeightSet.Compute(100f, 5f, 0f, 2000f, settings);
            Assert.Greater(w.surfaceTangent + w.world, w.spaceTimeMetric);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Bounds4AxisAlignedVolume_MatchesBounds4Contains()
        {
            var b4 = new Locomotion.Narrative.Bounds4(Vector3.zero, Vector3.one * 4f, 0f, 10f);
            var vol = new Locomotion.Narrative.Bounds4AxisAlignedVolume(b4);
            Assert.IsTrue(vol.Contains(Vector3.zero, 5f));
            Assert.IsFalse(vol.Contains(Vector3.zero, 11f));
        }

        [Test]
        public void CanonicalField_TrySampleBlended_UsesManifold()
        {
            var go = new GameObject("field_test");
            var manifoldGo = new GameObject("manifold");
            var manifold = manifoldGo.AddComponent<WeatherPhysicsManifold>();
            manifold.worldBounds = new Bounds(Vector3.zero, Vector3.one * 20f);
            manifold.cellCount = new Vector3Int(4, 4, 4);
            var field = go.AddComponent<CanonicalSpatiotemporalField>();
            field.manifold = manifold;

            ManifoldCellData stamped = manifold.GetDataAtPosition(Vector3.zero);
            stamped.surfaceFriction = 0.42f;
            stamped.velocity = Vector3.forward * 3f;
            manifold.SetDataAtPosition(Vector3.zero, stamped);

            Assert.IsTrue(field.TrySampleBlended(Vector3.zero, 0f, out SpatiotemporalSample sample));
            Assert.AreEqual(0.42f, sample.surfaceFriction, 0.01f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(manifoldGo);
        }
    }
}
#endif
