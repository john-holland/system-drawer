using NUnit.Framework;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryHorizonLodAltitudeTests
    {
        [Test]
        public void CloudLayer_AtHighAltitude()
        {
            var settings = ScriptableObject.CreateInstance<PlanetaryHorizonLodSettings>();
            var ctrl = new PlanetaryHorizonLodController(settings);
            var band = ctrl.SelectBand(5000f, 1000f, 3000f);
            Assert.AreEqual(PlanetaryAltitudeBand.CloudLayer, band);
        }

        [Test]
        public void Space_AboveUpperAtmosphere()
        {
            var settings = ScriptableObject.CreateInstance<PlanetaryHorizonLodSettings>();
            var ctrl = new PlanetaryHorizonLodController(settings);
            Assert.AreEqual(PlanetaryAltitudeBand.Space, ctrl.SelectBand(90000f, 0f, 0f));
        }
    }
}
