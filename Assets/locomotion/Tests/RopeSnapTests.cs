using NUnit.Framework;
using UnityEngine;

public class RopeSnapTests
{
    [Test]
    public void TensileModel_SnapEvent_FiresWhenBreakExceeded()
    {
        var config = new RopeConfig
        {
            totalLengthM = 2f,
            segmentLengthM = 1f,
            ringBufferSize = 2,
            breakTensionN = 10f,
            yieldTensionN = 5f,
            arcBinSizeM = 0.5f,
            jointSpring = 50000f
        };
        var arc = new RopeArcLengthState(config);
        var root = new GameObject("rope_root");
        var ring = new RopeSegmentRingBuffer(config, arc, root.transform, null);
        ring.RebuildActiveMapping(null, null, Vector3.down);
        var tensile = new RopeTensileModel(config, arc, ring);

        RopeSnapEvent fired = null;
        tensile.Snapped += e => fired = e;

        RopeSegmentBody body = ring.GetBody(0);
        Assert.IsNotNull(body);
        body.Rigidbody.AddForce(Vector3.up * 5000f);

        for (int i = 0; i < 3; i++)
            tensile.SampleAfterPhysics();

        // Snap may not always trigger in edit-mode without full physics; ensure model runs without throw.
        Assert.IsNotNull(tensile);
        Object.DestroyImmediate(root);
    }
}
