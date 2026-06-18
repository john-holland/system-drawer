#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather.Executor;

namespace Weather.Tests
{
    public sealed class WeatherSystemPerfTests
    {
        [Test]
        public void Wind_GenerateWindField_DoesNotStampManifold()
        {
            var go = new GameObject("wind");
            var wind = go.AddComponent<Wind>();
            var manifoldGo = new GameObject("manifold");
            var manifold = manifoldGo.AddComponent<WeatherPhysicsManifold>();
            manifold.wind = wind;
            manifold.cellCount = new Vector3Int(4, 4, 4);
            manifold.worldBounds = new Bounds(Vector3.zero, Vector3.one * 4f);

            wind.GenerateWindField();
            ManifoldCellData before = manifold.GetDataAtPosition(Vector3.zero);

            wind.GenerateWindField();
            ManifoldCellData after = manifold.GetDataAtPosition(Vector3.zero);
            Assert.AreEqual(before.velocity, after.velocity);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(manifoldGo);
        }

        [Test]
        public void WeatherEventRegistry_TracksEnableDisable()
        {
            var go = new GameObject("evt");
            var evt = go.AddComponent<WeatherEvent>();
            WeatherEventRegistry.Register(evt);
            Assert.AreEqual(1, WeatherEventRegistry.Count);
            WeatherEventRegistry.Unregister(evt);
            Assert.AreEqual(0, WeatherEventRegistry.Count);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EggPayload_RoundTrip()
        {
            var payload = new WeatherEggClientPayload
            {
                clientId = "c1",
                frameIndex = 7,
                eggCenter = Vector3.one,
                eggRadii = new Vector3(10f, 20f, 10f),
                confidence = 0.8f
            };
            string json = WeatherEggPayloadSerializer.ToJson(payload);
            var decoded = WeatherEggPayloadSerializer.FromJson<WeatherEggClientPayload>(json);
            Assert.NotNull(decoded);
            Assert.AreEqual("c1", decoded.clientId);
            Assert.AreEqual(7, decoded.frameIndex);
        }
    }
}
#endif
