using System.Reflection;
using Locomotion.Drink;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class CabinTurbulenceDriverTests
    {
        static void InvokeUpdate(CabinTurbulenceDriver driver)
        {
            var m = typeof(CabinTurbulenceDriver).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            m?.Invoke(driver, null);
        }

        [Test]
        public void InactiveDriver_HasZeroIntensity()
        {
            var root = new GameObject("cabin");
            var driver = root.AddComponent<CabinTurbulenceDriver>();
            driver.cabinRoot = root.transform;
            driver.SetActiveForBeat(false);
            InvokeUpdate(driver);
            Assert.AreEqual(0f, driver.TurbulenceIntensity01, 0.0001f);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ActiveDriver_ProducesNonZeroIntensity()
        {
            var root = new GameObject("cabin");
            var driver = root.AddComponent<CabinTurbulenceDriver>();
            driver.cabinRoot = root.transform;
            driver.shakeAmplitude = 0.1f;
            driver.SetActiveForBeat(true);
            for (int i = 0; i < 5; i++)
                InvokeUpdate(driver);
            Assert.Greater(driver.TurbulenceIntensity01, 0f);
            Object.DestroyImmediate(root);
        }
    }
}
