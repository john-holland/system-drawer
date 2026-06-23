using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class ContinuumScriptSpanOverlayTests
{
    [Test]
    public void Build_IncludesPromptClauseAndCommentRanges()
    {
        const string text = "Hello {P:name} world";
        var bindings = new[] { new LocalizationClauseBindingRecord { charStart = 6, charEnd = 14, propertyKey = "lemma" } };
        var comments = new[] { new ReviewerCommentRecord { textSelectionStart = 0, textSelectionEnd = 5, commentText = "tone" } };
        var spans = ContinuumScriptSpanOverlayModel.Build(text, bindings, comments);
        Assert.IsTrue(spans.Any(s => s.kind == ContinuumScriptSpanOverlayModel.SpanKind.Prompt));
        Assert.IsTrue(spans.Any(s => s.kind == ContinuumScriptSpanOverlayModel.SpanKind.Clause));
        Assert.IsTrue(spans.Any(s => s.kind == ContinuumScriptSpanOverlayModel.SpanKind.Comment));
    }

    [Test]
    public void Build_ResolvesClauseSpanFromFareyWhenCharCacheEmpty()
    {
        const string text = "abcdefghij";
        var bindings = new[]
        {
            new LocalizationClauseBindingRecord
            {
                charStart = 0,
                charEnd = 0,
                fareyLeftNum = 1,
                fareyLeftDen = 5,
                fareyRightNum = 1,
                fareyRightDen = 2,
                propertyKey = "lemma",
            }
        };
        var spans = ContinuumScriptSpanOverlayModel.Build(text, bindings, null);
        var clause = spans.Find(s => s.kind == ContinuumScriptSpanOverlayModel.SpanKind.Clause);
        Assert.AreEqual(2, clause.charStart);
        Assert.AreEqual(5, clause.charEnd);
    }

    [Test]
    public void SpanOverlayPainter_MultilineRects_SplitOnNewline()
    {
        const string text = "abc\ndef";
        var rects = SpanOverlayPainter.GetLineRects(text, 1, 6, SpanOverlayPainter.DefaultCharWidth, SpanOverlayPainter.DefaultLineHeight).ToList();
        Assert.AreEqual(2, rects.Count);
        Assert.Greater(rects[0].width, 0);
        Assert.Greater(rects[1].width, 0);
    }
}
