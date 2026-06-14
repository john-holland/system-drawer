#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class PhysicalMediumVolumeIndexTests
{
    [Test]
    public void MediumAllowsMode_WaterBlocksDrive()
    {
        Assert.IsFalse(PhysicalMediumVolumeRules.MediumAllowsMode(PhysicalPathingMedium.Water, TravelLegMode.Drive));
        Assert.IsTrue(PhysicalMediumVolumeRules.MediumAllowsMode(PhysicalPathingMedium.Water, TravelLegMode.Fly));
    }

    [Test]
    public void MediumAllowsMode_SpaceAllowsFlyOnly()
    {
        Assert.IsTrue(PhysicalMediumVolumeRules.MediumAllowsMode(PhysicalPathingMedium.Space, TravelLegMode.Fly));
        Assert.IsFalse(PhysicalMediumVolumeRules.MediumAllowsMode(PhysicalPathingMedium.Space, TravelLegMode.Walk));
    }
}
#endif
