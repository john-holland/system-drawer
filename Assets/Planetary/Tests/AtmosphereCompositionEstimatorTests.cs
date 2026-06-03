using NUnit.Framework;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Tests
{
    public class AtmosphereCompositionEstimatorTests
    {
        [Test]
        public void Estimate_ReturnsCloudExtents()
        {
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            var cloudGo = new GameObject("cloud");
            var cloud = cloudGo.AddComponent<Weather.Cloud>();
            cloud.altitude = new Vector2(800f, 2500f);
            var est = new AtmosphereCompositionEstimator();
            var profile = est.Estimate(body, null);
            Assert.Greater(profile.cloudTopM, profile.cloudBaseM);
            Object.DestroyImmediate(cloudGo);
            Object.DestroyImmediate(go);
        }
    }
}
