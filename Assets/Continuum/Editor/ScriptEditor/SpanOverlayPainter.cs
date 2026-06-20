#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>Multiline-aware char range to rect mapping for dotted overlays.</summary>
public static class SpanOverlayPainter
{
    public const float DefaultCharWidth = 7.2f;
    public const float DefaultLineHeight = 16f;

    public static void DrawDottedSpan(Rect textArea, string text, int charStart, int charEnd, Color color, float charWidth = DefaultCharWidth, float lineHeight = DefaultLineHeight)
    {
        if (string.IsNullOrEmpty(text) || charEnd <= charStart)
            return;
        charStart = Mathf.Clamp(charStart, 0, text.Length);
        charEnd = Mathf.Clamp(charEnd, charStart, text.Length);

        foreach (Rect lineRect in GetLineRects(text, charStart, charEnd, charWidth, lineHeight))
        {
            var r = new Rect(textArea.x + lineRect.x, textArea.y + lineRect.y, lineRect.width, lineRect.height);
            DrawDottedRect(r, color);
        }
    }

    public static IEnumerable<Rect> GetLineRects(string text, int charStart, int charEnd, float charWidth, float lineHeight)
    {
        int lineStart = 0;
        int row = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            bool eol = i == text.Length || text[i] == '\n';
            if (!eol)
                continue;

            int lineEnd = i;
            int segStart = Mathf.Max(charStart, lineStart);
            int segEnd = Mathf.Min(charEnd, lineEnd);
            if (segEnd > segStart)
            {
                float x = (segStart - lineStart) * charWidth;
                float w = (segEnd - segStart) * charWidth;
                yield return new Rect(x, row * lineHeight, w, lineHeight);
            }

            lineStart = i + 1;
            row++;
        }
    }

    static void DrawDottedRect(Rect r, Color color)
    {
        UnityEditor.Handles.color = color;
        const float dash = 4f;
        DrawDottedLine(new Vector3(r.xMin, r.yMax - 1), new Vector3(r.xMax, r.yMax - 1), dash);
    }

    static void DrawDottedLine(Vector3 a, Vector3 b, float dash)
    {
        float len = Vector3.Distance(a, b);
        if (len < 0.01f) return;
        Vector3 dir = (b - a).normalized;
        float t = 0f;
        while (t < len)
        {
            float t2 = Mathf.Min(t + dash, len);
            UnityEditor.Handles.DrawLine(a + dir * t, a + dir * t2);
            t += dash * 2f;
        }
    }
}

#endif
