using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RopeRingBufferTests
{
    [Test]
    public void ArcLength_Wind_IncreasesWoundLength()
    {
        var config = new RopeConfig { totalLengthM = 10f, segmentLengthM = 0.5f, ringBufferSize = 8, arcBinSizeM = 0.25f };
        var arc = new RopeArcLengthState(config);
        float before = arc.WoundLengthM;
        arc.Wind(2f, config.ringBufferSize);
        Assert.Greater(arc.WoundLengthM, before);
    }

    [Test]
    public void ArcLength_Unwind_DecreasesWoundLength()
    {
        var config = new RopeConfig { totalLengthM = 10f, segmentLengthM = 0.5f, ringBufferSize = 8, arcBinSizeM = 0.25f };
        var arc = new RopeArcLengthState(config);
        arc.Wind(4f, config.ringBufferSize);
        float wound = arc.WoundLengthM;
        arc.Unwind(1.5f, config.ringBufferSize);
        Assert.Less(arc.WoundLengthM, wound);
    }

    [Test]
    public void ArcLength_IsSegmentActive_RespectsWindow()
    {
        var config = new RopeConfig { totalLengthM = 8f, segmentLengthM = 1f, ringBufferSize = 4, arcBinSizeM = 0.5f };
        var arc = new RopeArcLengthState(config);
        arc.Wind(3f, config.ringBufferSize);
        Assert.IsFalse(arc.IsSegmentActive(0));
        Assert.IsTrue(arc.IsSegmentActive(arc.ActiveHeadSegment));
    }

    [Test]
    public void ArcLength_StoreWoundPose_RoundTripsBin()
    {
        var config = new RopeConfig { totalLengthM = 5f, arcBinSizeM = 0.5f, segmentLengthM = 0.5f, ringBufferSize = 4 };
        var arc = new RopeArcLengthState(config);
        arc.StoreWoundPose(1.2f, new Vector3(1, 2, 3), Quaternion.identity);
        Assert.IsTrue(arc.TryGetWoundPose(1.2f, out RopeWoundPose pose));
        Assert.AreEqual(new Vector3(1, 2, 3), pose.position);
    }
}
