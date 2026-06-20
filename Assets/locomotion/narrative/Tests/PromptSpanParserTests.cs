using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;

public class PromptSpanParserTests
{
    [Test]
    public void Parse_Join_RoundTrip_Mixed()
    {
        string s = "hello {P:door|key=alpha|role=entry} world";
        List<PromptSegment> seg = PromptSpanParser.Parse(s);
        Assert.AreEqual(3, seg.Count);
        Assert.IsFalse(seg[0].isPlaceholder);
        Assert.AreEqual("hello ", seg[0].textRun);
        Assert.IsTrue(seg[1].isPlaceholder);
        Assert.AreEqual("door", seg[1].placeholderName);
        Assert.AreEqual("alpha", seg[1].placeholderParams["key"]);
        Assert.AreEqual("entry", seg[1].placeholderParams["role"]);
        Assert.IsFalse(seg[2].isPlaceholder);
        Assert.AreEqual(" world", seg[2].textRun);

        string joined = PromptSpanParser.JoinSegments(seg);
        Assert.AreEqual(s, joined);
    }

    [Test]
    public void Parse_NameOnly_ContainsEqualsInText_IsNotSplitWrong()
    {
        string s = "{P:ladder}";
        var seg = PromptSpanParser.Parse(s);
        Assert.AreEqual(1, seg.Count);
        Assert.IsTrue(seg[0].isPlaceholder);
        Assert.AreEqual("ladder", seg[0].placeholderName);
        Assert.AreEqual("{P:ladder}", PromptSpanParser.JoinSegments(seg));
    }

    [Test]
    public void Parse_OnlyKeyValuePairs_NoName()
    {
        string s = "x {P:key=a|tag=b} y";
        var seg = PromptSpanParser.Parse(s);
        Assert.AreEqual(3, seg.Count);
        Assert.IsTrue(seg[1].isPlaceholder);
        Assert.AreEqual("", seg[1].placeholderName);
        Assert.AreEqual("a", seg[1].placeholderParams["key"]);
        Assert.AreEqual("b", seg[1].placeholderParams["tag"]);
        Assert.AreEqual(s, PromptSpanParser.JoinSegments(seg));
    }

    [Test]
    public void ReplaceRange_Works()
    {
        string t = "abcdef";
        // Replace 3 chars at index 2: "cde" -> "XYZ"
        Assert.AreEqual("abXYZf", PromptSpanParser.ReplaceRange(t, 2, 3, "XYZ"));
        // Replace 2 chars at index 2: "cd" -> "XYZ", leaves "ef"
        Assert.AreEqual("abXYZef", PromptSpanParser.ReplaceRange(t, 2, 2, "XYZ"));
    }

    [Test]
    public void Parse_DoubleBrace_QuotedName()
    {
        string s = "walk {{P:\"player walks\"|non-ik-animation=true}} here";
        var seg = PromptSpanParser.Parse(s);
        Assert.IsTrue(seg.Exists(x => x.isPlaceholder && x.placeholderName == "player walks"));
        PromptSegment ph = seg.Find(x => x.isPlaceholder);
        Assert.IsNotNull(ph);
        Assert.IsTrue(PromptSpanParser.TryGetBoolParam(ph, "non-ik-animation", out bool v) && v);
    }

    [Test]
    public void StripForLSTM_RemovesPlaceholders()
    {
        string s = "a {P:x|k=v} b";
        Assert.AreEqual("a  b", PromptSpanParser.StripForLSTM(s));
    }
}
