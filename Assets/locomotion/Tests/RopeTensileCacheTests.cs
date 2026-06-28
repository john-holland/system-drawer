using NUnit.Framework;
using UnityEngine;

public class RopeTensileCacheTests
{
    [Test]
    public void TensileModel_TotalBreak_WeakestLink_EqualsSegmentBreak()
    {
        var config = new RopeConfig
        {
            totalLengthM = 4f,
            segmentLengthM = 1f,
            ringBufferSize = 4,
            breakTensionN = 900f,
            totalStrengthPolicy = RopeTotalStrengthPolicy.WeakestLink,
            arcBinSizeM = 0.5f
        };
        var arc = new RopeArcLengthState(config);
        var root = new GameObject("rope_root");
        var ring = new RopeSegmentRingBuffer(config, arc, root.transform, null);
        var tensile = new RopeTensileModel(config, arc, ring);
        Assert.AreEqual(900f, tensile.TotalBreakTensionN);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void RadialCache_WriteFromSimulation_DoesNotThrow()
    {
        var config = new RopeConfig { totalLengthM = 4f, segmentLengthM = 1f, ringBufferSize = 4, arcBinSizeM = 0.5f, radialSlices = 4 };
        var arc = new RopeArcLengthState(config);
        var root = new GameObject("rope_root");
        var ring = new RopeSegmentRingBuffer(config, arc, root.transform, null);
        var tensile = new RopeTensileModel(config, arc, ring);
        var cache = new RopeRadialStrainCache(config, arc, tensile);
        cache.WriteFromSimulation();
        Assert.IsNotNull(cache.Texture);
        Object.DestroyImmediate(root);
    }
}
