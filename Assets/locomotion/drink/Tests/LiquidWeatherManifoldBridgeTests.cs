using Locomotion.Drink.Flow;
using Locomotion.Liquid;
using NUnit.Framework;
using UnityEngine;
using Weather;

namespace Locomotion.Drink.Tests
{
    public sealed class LiquidWeatherManifoldBridgeTests
    {
        [Test]
        public void PaintWaterSphere_SetsWaterModeAtPosition()
        {
            var weatherGo = new GameObject("weather");
            var manifold = weatherGo.AddComponent<WeatherPhysicsManifold>();
            manifold.worldBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            manifold.cellResolution = 0.5f;
            manifold.cellCount = new Vector3Int(8, 8, 8);

            var bridgeGo = new GameObject("bridge");
            var bridge = bridgeGo.AddComponent<LiquidWeatherManifoldBridge>();
            bridge.manifold = manifold;

            Vector3 center = Vector3.up * 0.5f;
            bridge.PaintWaterSphere(center, 0.2f, Vector3.down, 101325f);
            var sample = bridge.SampleAt(center);
            Assert.AreEqual(WeatherMode.Water, sample.mode);

            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(weatherGo);
        }
    }

    public sealed class DrinkFlowBakeManifoldTests
    {
        [Test]
        public void Bake_RecordsNonZeroFlowCurve()
        {
            var vesselGo = new GameObject("vessel");
            var vessel = vesselGo.AddComponent<DrinkVesselComponent>();
            vessel.capacityLiters = 0.5f;
            vessel.currentVolumeLiters = 0.5f;
            var flow = vesselGo.AddComponent<DrinkFlowModel>();
            flow.vessel = vessel;
            flow.openRimAreaM2 = 0.0004f;

            var solver = new DrinkFlowBakeSolver();
            var asset = solver.Bake(flow, 0.5f, 0.1f, null);
            Assert.IsNotNull(asset.flowLitersPerSecond);
            Assert.Greater(asset.flowLitersPerSecond.length, 0);
            Object.DestroyImmediate(vesselGo);
        }

        [Test]
        public void Bake_WithBridge_PaintsManifold()
        {
            var weatherGo = new GameObject("weather");
            var manifold = weatherGo.AddComponent<WeatherPhysicsManifold>();
            manifold.worldBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            manifold.cellResolution = 0.5f;
            manifold.cellCount = new Vector3Int(8, 8, 8);

            var bridgeGo = new GameObject("bridge");
            var bridge = bridgeGo.AddComponent<LiquidWeatherManifoldBridge>();
            bridge.manifold = manifold;

            var vesselGo = new GameObject("vessel");
            var vessel = vesselGo.AddComponent<DrinkVesselComponent>();
            vessel.currentVolumeLiters = 0.5f;
            vessel.capacityLiters = 0.5f;
            var flow = vesselGo.AddComponent<DrinkFlowModel>();
            flow.vessel = vessel;
            flow.weatherBridge = bridge;
            flow.openRimAreaM2 = 0.0004f;

            var solver = new DrinkFlowBakeSolver();
            solver.Bake(flow, 0.2f, 0.1f, bridge);
            var sample = manifold.GetDataAtPosition(flow.StreamTipPosition());
            Assert.AreEqual(WeatherMode.Water, sample.mode);

            Object.DestroyImmediate(vesselGo);
            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(weatherGo);
        }
    }
}
