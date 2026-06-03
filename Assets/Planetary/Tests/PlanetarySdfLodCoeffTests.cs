using NUnit.Framework;
using Planetary.Composition;
using Planetary.Rendering;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetarySdfLodCoeffTests
    {
        [Test]
        public void HorizonSdfWeight_IncreasesWithAltitude()
        {
            var profile = ScriptableObject.CreateInstance<PlanetarySdfLodProfile>();
            profile.sdfHorizonMinAltM = 2000f;
            profile.sdfHorizonFullAltM = 12000f;
            var ctrl = new PlanetarySdfLodController(profile, null);
            var low = ctrl.Compute(Vector3.up * 6000f, Vector3.zero, 1000f, 0f, 1000f, 3000f, 0f);
            var high = ctrl.Compute(Vector3.up * 15000f, Vector3.zero, 1000f, 0f, 1000f, 3000f, 0f);
            Assert.Greater(high.horizonSdfWeight, low.horizonSdfWeight);
        }
    }
}
