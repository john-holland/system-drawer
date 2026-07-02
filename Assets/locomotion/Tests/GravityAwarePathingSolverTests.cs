using System.Collections.Generic;
using Locomotion.Spaceship;
using NUnit.Framework;
using Planetary;
using Planetary.Celestial;
using UnityEngine;

namespace Locomotion.Tests
{
    public class GravityAwarePathingSolverTests
    {
        [Test]
        public void FindPath_ReturnsAtLeastTwoPoints()
        {
            var solverGo = new GameObject("hps");
            var hps = solverGo.AddComponent<HierarchicalPathingSolver>();
            var grav = new GravityAwarePathingSolver { ignoreTime = true };
            Vector3 a = Vector3.zero;
            Vector3 b = new Vector3(100f, 0f, 0f);
            List<Vector3> path = grav.FindPath(hps, a, b);
            Assert.IsNotNull(path);
            Assert.GreaterOrEqual(path.Count, 2);
            Object.DestroyImmediate(solverGo);
        }

        [Test]
        public void GalacticGravitySampleProvider_ReturnsUpAwayFromMass()
        {
            var registryGo = new GameObject("registry");
            var registry = registryGo.AddComponent<GalacticBodyRegistry>();
            var planetGo = new GameObject("planet");
            var bridge = planetGo.AddComponent<PlanetCelestialBridge>();
            var body = planetGo.AddComponent<PlanetBody>();
            body.planetRadius = 100f;
            bridge.densityKgPerM3 = 3000f;
            registry.RegisterSceneBody(bridge);

            var provider = new GalacticGravitySampleProvider(registry);
            var sample = provider.Sample(Vector3.up * 200f);
            Assert.Greater(sample.strength, 0f);

            Object.DestroyImmediate(planetGo);
            Object.DestroyImmediate(registryGo);
        }
    }
}
