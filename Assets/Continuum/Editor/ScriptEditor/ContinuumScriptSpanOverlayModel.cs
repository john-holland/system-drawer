#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Span overlay model for script editor (prompt, clause, comment).</summary>
public static class ContinuumScriptSpanOverlayModel
{
    public enum SpanKind { Prompt, Clause, Comment }

    public struct OverlaySpan
    {
        public int charStart;
        public int charEnd;
        public SpanKind kind;
        public string label;
    }

    public static List<OverlaySpan> Build(string scriptText, IEnumerable<LocalizationClauseBindingRecord> bindings, IEnumerable<ReviewerCommentRecord> comments)
    {
        var spans = new List<OverlaySpan>();
        foreach (PromptSegment seg in PromptSpanParser.Parse(scriptText ?? ""))
        {
            if (seg.isPlaceholder)
                spans.Add(new OverlaySpan { charStart = seg.start, charEnd = seg.start + seg.length, kind = SpanKind.Prompt, label = seg.placeholderName });
        }
        if (bindings != null)
        {
            foreach (var b in bindings)
            {
                if (b == null) continue;
                spans.Add(new OverlaySpan { charStart = b.charStart, charEnd = b.charEnd, kind = SpanKind.Clause, label = b.propertyKey });
            }
        }
        if (comments != null)
        {
            foreach (var c in comments)
            {
                if (c == null) continue;
                spans.Add(new OverlaySpan { charStart = c.textSelectionStart, charEnd = c.textSelectionEnd, kind = SpanKind.Comment, label = c.commentText });
            }
        }
        return spans;
    }

    public static Color ColorFor(SpanKind kind) => kind switch
    {
        SpanKind.Prompt => new Color(0.1f, 0.45f, 0.95f, 0.9f),
        SpanKind.Clause => new Color(0.2f, 0.65f, 0.25f, 0.9f),
        SpanKind.Comment => new Color(0.9f, 0.55f, 0.1f, 0.9f),
        _ => Color.gray
    };
}

#endif
