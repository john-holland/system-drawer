using NUnit.Framework;
using UnityEngine;

public class RopeOverlapIndexTests
{
    [Test]
    public void OverlapIndex_IgnoresAdjacentSegments()
    {
        var config = new RopeConfig { totalLengthM = 4f, segmentLengthM = 1f, arcBinSizeM = 0.5f };
        var arc = new RopeArcLengthState(config);
        var index = new RopeOverlapIndex();

        var root = new GameObject("a").AddComponent<RopeSegmentBody>();
        var root2 = new GameObject("b").AddComponent<RopeSegmentBody>();
        root.logicalSegmentIndex = 2;
        root2.logicalSegmentIndex = 3;

        index.RegisterCollision(root, root2, Vector3.zero, Vector3.up, -0.01f, arc);
        Assert.AreEqual(0, index.Entries.Count);
        Object.DestroyImmediate(root.gameObject);
        Object.DestroyImmediate(root2.gameObject);
    }

    [Test]
    public void OverlapIndex_RecordsNonAdjacent()
    {
        var config = new RopeConfig { totalLengthM = 6f, segmentLengthM = 1f, arcBinSizeM = 0.5f };
        var arc = new RopeArcLengthState(config);
        var index = new RopeOverlapIndex();

        var a = new GameObject("a").AddComponent<RopeSegmentBody>();
        var b = new GameObject("b").AddComponent<RopeSegmentBody>();
        a.logicalSegmentIndex = 1;
        b.logicalSegmentIndex = 4;

        index.RegisterCollision(a, b, Vector3.one, Vector3.forward, -0.02f, arc);
        Assert.AreEqual(1, index.Entries.Count);
        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    [Test]
    public void OverlapIndex_InvalidateOnWind_RemovesEntries()
    {
        var config = new RopeConfig { totalLengthM = 6f, segmentLengthM = 1f, arcBinSizeM = 0.5f };
        var arc = new RopeArcLengthState(config);
        var index = new RopeOverlapIndex();
        var a = new GameObject("a").AddComponent<RopeSegmentBody>();
        var b = new GameObject("b").AddComponent<RopeSegmentBody>();
        a.logicalSegmentIndex = 1;
        b.logicalSegmentIndex = 5;
        index.RegisterCollision(a, b, Vector3.zero, Vector3.up, -0.01f, arc);
        index.InvalidateLogicalRange(1, 5);
        Assert.AreEqual(0, index.Entries.Count);
        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }
}
