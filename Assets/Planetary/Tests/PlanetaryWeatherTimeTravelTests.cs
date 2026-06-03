using NUnit.Framework;
using Planetary.TimeTravel;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryWeatherTimeTravelTests
    {
        [Test]
        public void PushAndUndo_RoundTrip()
        {
            var go = new GameObject("tt");
            var sys = go.AddComponent<PlanetaryWeatherTimeTravelSystem>();
            sys.PushFrameBeforeApply(new WeatherTimeTravelFrame { narrativeTime = 1f });
            Assert.IsTrue(sys.Undo());
            Object.DestroyImmediate(go);
        }
    }
}
