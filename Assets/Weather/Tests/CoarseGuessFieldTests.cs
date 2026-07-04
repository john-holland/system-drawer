#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather.Coarse;
using Weather.Emergence;

namespace Weather.Tests
{
    public sealed class CoarseGuessFieldTests
    {
        [Test]
        public void GuessAt_ReturnsInitializedScalars()
        {
            var field = new CoarseMeteorologyGuessField();
            field.SetAnchor(Vector3.zero);
            ManifoldCellData data = field.GuessAt(new Vector3(8f, 4f, 8f));
            Assert.AreEqual(1013f, data.pressure, 1f);
            Assert.AreEqual(15f, data.temperature, 1f);
        }

        [Test]
        public void Step_1000Iterations_RemainsBounded()
        {
            var field = new CoarseMeteorologyGuessField { updateHz = 1000f };
            field.SetAnchor(Vector3.zero);
            var emergence = new EmergenceVectorField();
            var go = new GameObject("Wind");
            var wind = go.AddComponent<Wind>();
            wind.speed = 5f;
            wind.direction = 90f;

            for (int i = 0; i < 1000; i++)
            {
                field.Step(0.016f, wind, emergence);
                ManifoldCellData sample = field.GuessAt(Vector3.zero);
                Assert.Less(Mathf.Abs(sample.temperature), 200f);
                Assert.Less(sample.pressure, 2000f);
                Assert.Greater(sample.pressure, 500f);
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EmergenceInjection_IncreasesVelocityBias()
        {
            var field = new CoarseMeteorologyGuessField();
            field.SetAnchor(Vector3.zero);
            var emergence = new EmergenceVectorField();
            emergence.SetVectors(new System.Collections.Generic.List<EmergenceVector>
            {
                EmergenceVector.Segment(Vector3.zero, Vector3.forward * 40f, 15f, 1f, "corridor"),
            });

            field.Step(0.25f, null, emergence);
            ManifoldCellData data = field.GuessAt(new Vector3(8f, 4f, 8f));
            Assert.Greater(data.velocity.magnitude, 0.01f);
        }
    }
}
#endif
