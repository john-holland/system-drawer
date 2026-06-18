using Locomotion.Narrative;
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

        [Test]
        public void CaptureCurrent_UsesNarrativeClockWhenPresent()
        {
            var clockGo = new GameObject("clock");
            var clock = clockGo.AddComponent<NarrativeClock>();
            clock.fallbackStartDateTime = new NarrativeDateTime(2025, 6, 15, 10, 30, 0);
            var ttGo = new GameObject("tt");
            var sys = ttGo.AddComponent<PlanetaryWeatherTimeTravelSystem>();
            var frame = sys.CaptureCurrentPublic();
            Assert.AreEqual(NarrativeCalendarMath.DateTimeToSeconds(clock.Now), frame.narrativeTime, 0.001f);
            Object.DestroyImmediate(clockGo);
            Object.DestroyImmediate(ttGo);
        }

        [Test]
        public void LoadNearestFrame_PicksClosestAtOrBeforeTarget()
        {
            var go = new GameObject("tt");
            var sys = go.AddComponent<PlanetaryWeatherTimeTravelSystem>();
            sys.PushFrameBeforeApply(new WeatherTimeTravelFrame { narrativeTime = 5f });
            sys.PushFrameBeforeApply(new WeatherTimeTravelFrame { narrativeTime = 20f });
            var loaded = sys.LoadNearestFramePublic(12f);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(5f, loaded.narrativeTime, 0.001f);
            Object.DestroyImmediate(go);
        }
    }
}
