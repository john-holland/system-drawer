#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weather.Lod;

namespace Weather.Tests
{
    public sealed class SphericalHyperplaneRegressionTests
    {
        [Test]
        public void FitFromSamples_CollapsesToLinearLayer_WhenUniform()
        {
            var regression = new SphericalHyperplaneRegression();
            var samples = new List<ManifoldSample>
            {
                new ManifoldSample
                {
                    position = Vector3.zero,
                    data = new ManifoldCellData { velocity = Vector3.forward, temperature = 10f, pressure = 1010f }
                },
                new ManifoldSample
                {
                    position = Vector3.right,
                    data = new ManifoldCellData { velocity = Vector3.forward, temperature = 10f, pressure = 1010f }
                }
            };

            regression.FitFromSamples(Vector3.zero, samples, 1f, 4);
            Assert.AreEqual(1, regression.LayerCount);
            Assert.IsTrue(float.IsPositiveInfinity(regression.effectiveRadius));
        }

        [Test]
        public void Evaluate_ReturnsFiniteData()
        {
            var regression = new SphericalHyperplaneRegression();
            regression.FitFromSamples(Vector3.zero, new List<ManifoldSample>(), 0.5f, 4);
            ManifoldCellData data = regression.Evaluate(Vector3.up);
            Assert.IsFalse(float.IsNaN(data.temperature));
            Assert.IsFalse(float.IsNaN(data.pressure));
        }

        [Test]
        public void CircuitBreaker_FoldsLargeDiff()
        {
            var breaker = new WeatherDiffCircuitBreaker { byteBudget = 100 };
            Assert.IsTrue(breaker.ShouldFoldToRegression(500, 0.5f));
        }
    }
}
#endif
