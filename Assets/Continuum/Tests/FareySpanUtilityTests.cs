using NUnit.Framework;

public class FareySpanUtilityTests
{
    [Test]
    public void Contains_RootContainsInner()
    {
        var root = FareySpanRecord.Root;
        var inner = new FareySpanRecord { ln = 1, ld = 2, rn = 3, rd = 4 };
        Assert.IsTrue(FareySpanUtility.Contains(root, inner));
    }

    [Test]
    public void Contains_InnerDoesNotContainDisjointOuter()
    {
        var a = new FareySpanRecord { ln = 1, ld = 4, rn = 1, rd = 2 };
        var b = new FareySpanRecord { ln = 3, ld = 4, rn = 1, rd = 1 };
        Assert.IsFalse(FareySpanUtility.Contains(a, b));
    }

    [Test]
    public void TryParse_ValidKey()
    {
        Assert.IsTrue(FareySpanUtility.TryParse("0/1-1/1", out FareySpanRecord span));
        Assert.AreEqual(0, span.ln);
        Assert.AreEqual(1, span.rn);
    }

    [Test]
    public void CharRangeToFareySpan_ProportionalMapping()
    {
        var span = FareySpanUtility.CharRangeToFareySpan("hello", 1, 3);
        Assert.AreEqual(1, span.ln);
        Assert.AreEqual(5, span.ld);
        Assert.AreEqual(3, span.rn);
        Assert.AreEqual(5, span.rd);
    }

    [Test]
    public void FareySpanToCharRange_RoundTripsProportionalSpan()
    {
        const string text = "abcdefghij";
        var span = FareySpanUtility.CharRangeToFareySpan(text, 2, 5);
        FareySpanUtility.FareySpanToCharRange(text, span, out int charStart, out int charEnd);
        Assert.AreEqual(2, charStart);
        Assert.AreEqual(5, charEnd);
    }
}
