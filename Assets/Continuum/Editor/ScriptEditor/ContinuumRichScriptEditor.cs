#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI rich script surface with dotted span overlays (UIToolkit-style underlay pattern).</summary>
public sealed class ContinuumRichScriptEditor
{
    string _text = "";
    Vector2 _scroll;
    List<ContinuumScriptSpanOverlayModel.OverlaySpan> _spans = new List<ContinuumScriptSpanOverlayModel.OverlaySpan>();
    bool _readOnly;

    public string Text => _text;
    public bool ReadOnly => _readOnly;

    public void SetContent(string text, IEnumerable<ContinuumScriptSpanOverlayModel.OverlaySpan> spans, bool readOnly)
    {
        _text = text ?? "";
        _spans = spans != null ? new List<ContinuumScriptSpanOverlayModel.OverlaySpan>(spans) : new List<ContinuumScriptSpanOverlayModel.OverlaySpan>();
        _readOnly = readOnly;
    }

    public void SetSpans(IEnumerable<ContinuumScriptSpanOverlayModel.OverlaySpan> spans)
    {
        _spans = spans != null ? new List<ContinuumScriptSpanOverlayModel.OverlaySpan>(spans) : new List<ContinuumScriptSpanOverlayModel.OverlaySpan>();
    }

    public (int charStart, int charEnd, string selectedText) GetSelection()
    {
        return (0, 0, "");
    }

    public string Draw(Rect area)
    {
        const float lineHeight = SpanOverlayPainter.DefaultLineHeight;
        const float charWidth = SpanOverlayPainter.DefaultCharWidth;
        int lineCount = string.IsNullOrEmpty(_text) ? 1 : _text.Split('\n').Length;
        float contentHeight = Mathf.Max(area.height - 4, lineCount * lineHeight + 8);

        var inner = new Rect(area.x + 4, area.y + 4, area.width - 8, contentHeight);
        _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, inner.width - 16, contentHeight));

        var overlayRect = new Rect(4, 4, inner.width, contentHeight);
        foreach (var span in _spans)
        {
            var color = ContinuumScriptSpanOverlayModel.ColorFor(span.kind);
            SpanOverlayPainter.DrawDottedSpan(overlayRect, _text, span.charStart, span.charEnd, color, charWidth, lineHeight);
        }

        GUI.color = new Color(1, 1, 1, 0.01f);
        EditorGUI.BeginDisabledGroup(_readOnly);
        var style = new GUIStyle(EditorStyles.textArea) { font = EditorStyles.standardFont, wordWrap = true };
        _text = EditorGUI.TextArea(overlayRect, _text, style);
        EditorGUI.EndDisabledGroup();
        GUI.color = Color.white;

        GUI.EndScrollView();
        return _text;
    }
}

#endif
