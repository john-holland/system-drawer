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
    public void SpanOverlayPainter_MultilineRects_SplitOnNewline()
    {
        const string text = "abc\ndef";
        var rects = SpanOverlayPainter.GetLineRects(text, 1, 6, SpanOverlayPainter.DefaultCharWidth, SpanOverlayPainter.DefaultLineHeight).ToList();
        Assert.AreEqual(2, rects.Count);
        Assert.Greater(rects[0].width, 0);
        Assert.Greater(rects[1].width, 0);
    }
}
