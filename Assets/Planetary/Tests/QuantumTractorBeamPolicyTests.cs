using NUnit.Framework;
using Planetary.Celestial;
using UnityEngine;

namespace Planetary.Tests
{
    public class QuantumTractorBeamPolicyTests
    {
        [Test]
        public void BlacklistedStar_Rejected()
        {
            var policy = ScriptableObject.CreateInstance<QuantumTractorBeamPolicy>();
            policy.enforceLimits = true;
            policy.blacklistedBodyIds.Add("sol");
            var star = new GameObject("star").AddComponent<StarBody>();
            star.galacticBodyId = "sol";
            star.immovable = true;
            Assert.IsFalse(policy.CanTarget(star, out string reason));
            Assert.IsNotNull(reason);
            Object.DestroyImmediate(star.gameObject);
            Object.DestroyImmediate(policy);
        }

        [Test]
        public void RadiusLimit_EnforcedWhenSet()
        {
            var policy = ScriptableObject.CreateInstance<QuantumTractorBeamPolicy>();
            policy.enforceLimits = true;
            policy.maxTargetRadiusM = 100f;
            var bridge = new GameObject("planetoid").AddComponent<PlanetCelestialBridge>();
            var planet = bridge.gameObject.AddComponent<PlanetBody>();
            planet.planetRadius = 500f;
            Assert.IsFalse(policy.CanTarget(bridge, out _));
            Object.DestroyImmediate(bridge.gameObject);
            Object.DestroyImmediate(policy);
        }

        [Test]
        public void UnlimitedMode_AllowsLargePlanetoid()
        {
            var policy = ScriptableObject.CreateInstance<QuantumTractorBeamPolicy>();
            policy.enforceLimits = false;
            var bridge = new GameObject("planetoid").AddComponent<PlanetCelestialBridge>();
            var planet = bridge.gameObject.AddComponent<PlanetBody>();
            planet.planetRadius = 500f;
            Assert.IsTrue(policy.CanTarget(bridge, out _));
            Object.DestroyImmediate(bridge.gameObject);
            Object.DestroyImmediate(policy);
        }
    }
}
