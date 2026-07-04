using Locomotion.Drink;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Drink.Tests
{
    public sealed class DrinkFlooshTrajectoryTests
    {
        [Test]
        public void IsFeasible_ReturnsTrueForNearbyMouth()
        {
            Vector3 nozzle = Vector3.zero;
            Vector3 mouth = new Vector3(0.1f, -0.05f, 0.2f);
            Assert.IsTrue(DrinkFlooshTrajectory.IsFeasible(nozzle, mouth, 0.7f));
        }

        [Test]
        public void VolumeForSip_SplitsAcrossSips()
        {
            var props = DrinkLemmaProperties.Defaults;
            props.totalVolumeLiters = 0.2f;
            props.sipCount = 4;
            float v = DrinkFlooshTrajectory.VolumeForSip(props, 0, 0.2f);
            Assert.AreEqual(0.05f, v, 0.0001f);
        }
    }
}
