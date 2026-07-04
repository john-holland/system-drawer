#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather.Activation;
using Weather.Coarse;
using Weather.Emergence;
using Weather.Scheduling;

namespace Weather.Tests
{
    public sealed class WeatherEmergenceTests
    {
        [Test]
        public void EmergenceVectorField_Influence_IsZeroOutsideRadius()
        {
            var v = EmergenceVector.Segment(Vector3.zero, Vector3.forward * 50f, 5f, 1f, "test");
            float w = EmergenceVectorField.InfluenceAt(v, new Vector3(100f, 0f, 0f));
            Assert.AreEqual(0f, w, 0.001f);
        }

        [Test]
        public void EmergenceVectorField_Influence_IsNonZeroOnSegment()
        {
            var v = EmergenceVector.Segment(Vector3.zero, Vector3.forward * 50f, 10f, 1f, "test");
            float w = EmergenceVectorField.InfluenceAt(v, new Vector3(0f, 0f, 25f));
            Assert.Greater(w, 0.5f);
        }

        [Test]
        public void EmergenceEggShaper_ElongatesAlongPath()
        {
            var field = new EmergenceVectorField();
            var list = new System.Collections.Generic.List<EmergenceVector>
            {
                EmergenceVector.Segment(Vector3.zero, Vector3.right * 100f, 20f, 1f, "path"),
            };
            field.SetVectors(list);

            EmergenceEggShaper.ShapeEgg(
                Vector3.zero,
                new Vector3(10f, 20f, 10f),
                field,
                out Vector3 center,
                out Vector3 radii);

            Assert.Greater(radii.x, 10f);
        }

        [Test]
        public void ActivationGate_Hysteresis_EntersAndExits()
        {
            var gate = new WeatherActivationGate { enterThreshold = 0.35f, exitThreshold = 0.25f };
            gate.ApplyHysteresis(0.4f);
            Assert.IsTrue(gate.IsActive(WeatherFeatureMask.CoarseAdvection, 0.3f, false));
            Assert.IsFalse(gate.IsActive(WeatherFeatureMask.WindField, 0.3f, false));
            gate.ApplyHysteresis(0.2f);
            Assert.IsFalse(gate.IsActive(WeatherFeatureMask.WeatherEvents, 0.2f, false));
            Assert.IsFalse(gate.IsActive(WeatherFeatureMask.WindField, 0.2f, false));
        }

        [Test]
        public void ActivationGate_EmergenceOnlyMode_BlocksWindOutside()
        {
            var gate = new WeatherActivationGate { emergenceOnlyMode = true };
            Assert.IsFalse(gate.IsActive(WeatherFeatureMask.WindField, 0.1f, false));
            Assert.IsTrue(gate.IsActive(WeatherFeatureMask.LodEggs, 0f, false));
        }

        [Test]
        public void SimScheduler_L1_OutsideInterval_IsSlower()
        {
            var cfg = ScriptableObject.CreateInstance<WeatherSimLayerConfig>();
            var sched = new WeatherSimScheduler { config = cfg };
            Assert.IsTrue(sched.ShouldTick(WeatherSimLayerId.L1_CoarseAdvection, 0.5f, true, 0f));
            Assert.IsFalse(sched.ShouldTick(WeatherSimLayerId.L1_CoarseAdvection, 0.5f, true, 0.1f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void EmergenceChecksum_IsStableForSameVectors()
        {
            var list = new System.Collections.Generic.List<EmergenceVector>
            {
                EmergenceVector.Point(Vector3.zero, 10f, 1f, "a"),
            };
            int a = EmergenceVectorField.ComputeChecksum(list);
            int b = EmergenceVectorField.ComputeChecksum(list);
            Assert.AreEqual(a, b);
        }
    }
}
#endif
