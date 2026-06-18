#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather.Executor;
using Weather.Lod;

namespace Weather.Tests
{
    public sealed class WeatherEggMergeTests
    {
        [Test]
        public void ShellWeight_IsOneInsideEgg()
        {
            Vector3 center = Vector3.zero;
            Vector3 radii = new Vector3(10f, 20f, 10f);
            Assert.AreEqual(1f, WeatherEggBounds.ShellWeight(center, radii, center), 0.001f);
        }

        [Test]
        public void KalmanMerge_BlendCells_Interpolates()
        {
            var a = new ManifoldCellData { velocity = Vector3.zero, temperature = 0f, pressure = 1000f };
            var b = new ManifoldCellData { velocity = Vector3.one, temperature = 20f, pressure = 1020f };
            ManifoldCellData merged = WeatherKalmanMerge.BlendCells(a, b, 0.5f);
            Assert.AreEqual(0.5f, merged.velocity.x, 0.01f);
            Assert.AreEqual(10f, merged.temperature, 0.01f);
        }

        [Test]
        public void ServerClientWeight_HalvesOnTimeout()
        {
            float w0 = WeatherKalmanMerge.ServerClientWeight(1f, 0);
            float w1 = WeatherKalmanMerge.ServerClientWeight(1f, 1);
            Assert.Greater(w0, w1);
            Assert.AreEqual(0.5f, w1, 0.01f);
        }
    }
}
#endif
