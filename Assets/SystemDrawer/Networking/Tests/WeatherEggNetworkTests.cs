#if UNITY_EDITOR
using NUnit.Framework;
using Weather.Executor;

namespace SystemDrawer.Networking.Tests
{
    public sealed class WeatherEggNetworkTests
    {
        [Test]
        public void WeatherEggApplyPayload_RoundTripsJson()
        {
            var payload = new WeatherEggApplyPayload
            {
                frameIndex = 3,
                authorityClientId = "server",
                definitionLevel = 0.75f
            };
            string json = WeatherEggPayloadSerializer.ToJson(payload);
            var decoded = WeatherEggPayloadSerializer.FromJson<WeatherEggApplyPayload>(json);
            Assert.NotNull(decoded);
            Assert.AreEqual(3, decoded.frameIndex);
            Assert.AreEqual(0.75f, decoded.definitionLevel, 0.001f);
        }

        [Test]
        public void WeatherEggBootstrapPayload_RoundTripsJson()
        {
            var payload = new WeatherEggBootstrapPayload
            {
                latDeg = 45f,
                lonDeg = -122f,
                weatherFrameJson = "{}"
            };
            string json = WeatherEggPayloadSerializer.ToJson(payload);
            var decoded = WeatherEggPayloadSerializer.FromJson<WeatherEggBootstrapPayload>(json);
            Assert.NotNull(decoded);
            Assert.AreEqual(45f, decoded.latDeg, 0.01f);
        }
    }
}
#endif
