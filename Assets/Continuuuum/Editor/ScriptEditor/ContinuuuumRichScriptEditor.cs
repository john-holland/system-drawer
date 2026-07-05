#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI rich script surface with dotted span overlays (UIToolkit-style underlay pattern).</summary>
public sealed class ContinuuuumRichScriptEditor
{
    string _text = "";
    Vector2 _scroll;
    List<ContinuuuumScriptSpanOverlayModel.OverlaySpan> _spans = new List<ContinuuuumScriptSpanOverlayModel.OverlaySpan>();
    bool _readOnly;
    GUIStyle _textStyle;

    public string Text => _text;
    public bool ReadOnly => _readOnly;

    public void SetContent(string text, IEnumerable<ContinuuuumScriptSpanOverlayModel.OverlaySpan> spans, bool readOnly)
    {
        _text = text ?? "";
        _spans = spans != null ? new List<ContinuuuumScriptSpanOverlayModel.OverlaySpan>(spans) : new List<ContinuuuumScriptSpanOverlayModel.OverlaySpan>();
        _readOnly = readOnly;
    }

    public void SetSpans(IEnumerable<ContinuuuumScriptSpanOverlayModel.OverlaySpan> spans)
    {
        _spans = spans != null ? new List<ContinuuuumScriptSpanOverlayModel.OverlaySpan>(spans) : new List<ContinuuuumScriptSpanOverlayModel.OverlaySpan>();
    }

    public (int charStart, int charEnd, string selectedText) GetSelection()
    {
        var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        if (te != null && !string.IsNullOrEmpty(_text))
        {
            int start = Mathf.Clamp(Mathf.Min(te.selectIndex, te.cursorIndex), 0, _text.Length);
            int end = Mathf.Clamp(Mathf.Max(te.selectIndex, te.cursorIndex), 0, _text.Length);
            if (end > start)
                return (start, end, _text.Substring(start, end - start));
        }
        return (0, 0, "");
    }

    GUIStyle TextStyle()
    {
        if (_textStyle != null)
            return _textStyle;

        const float ScriptTextGray = 34f / 255f;
        var scriptTextColor = new Color(ScriptTextGray, ScriptTextGray, ScriptTextGray);
        _textStyle = new GUIStyle(EditorStyles.textArea)
        {
            font = EditorStyles.standardFont,
            wordWrap = true,
            richText = false,
        };
        _textStyle.normal.textColor = scriptTextColor;
        _textStyle.normal.background = Texture2D.whiteTexture;
        _textStyle.focused.textColor = scriptTextColor;
        _textStyle.hover.textColor = scriptTextColor;
        _textStyle.active.textColor = scriptTextColor;
        return _textStyle;
    }

    public string Draw(Rect area)
    {
        const float lineHeight = SpanOverlayPainter.DefaultLineHeight;
        const float charWidth = SpanOverlayPainter.DefaultCharWidth;
        int lineCount = string.IsNullOrEmpty(_text) ? 1 : _text.Split('\n').Length;
        float contentHeight = Mathf.Max(area.height - 4, lineCount * lineHeight + 8);
        float contentWidth = Mathf.Max(64f, area.width - 24f);

        _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, contentWidth, contentHeight));

        var textRect = new Rect(4, 4, contentWidth - 8, contentHeight - 8);
        var style = TextStyle();
        string before = _text;

        if (Event.current.type == EventType.Repaint)
        {
            var bgColor = new Color(238f / 255f, 238f / 255f, 238f / 255f);
            EditorGUI.DrawRect(textRect, bgColor);

            Handles.BeginGUI();
            foreach (var span in _spans)
            {
                var color = ContinuuuumScriptSpanOverlayModel.ColorFor(span.kind);
                SpanOverlayPainter.DrawDottedSpan(textRect, before, span.charStart, span.charEnd, color, charWidth, lineHeight);
            }
            Handles.EndGUI();
        }

        _text = EditorGUI.TextArea(textRect, _text, style);
        if (_readOnly)
            _text = before;

        GUI.EndScrollView();
        return _readOnly ? before : _text;
    }
}

#endif
