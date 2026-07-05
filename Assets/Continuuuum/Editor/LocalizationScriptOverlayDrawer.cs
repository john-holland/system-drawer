#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI helper: dotted rects over char ranges in script text (Ace parity preview).</summary>
public static class LocalizationScriptOverlayDrawer
{
    public static void DrawDottedRangeOverlay(Rect textArea, string scriptText, int charStart, int charEnd, Color color)
    {
        if (string.IsNullOrEmpty(scriptText) || charEnd <= charStart)
            return;

        charStart = Mathf.Clamp(charStart, 0, scriptText.Length);
        charEnd = Mathf.Clamp(charEnd, charStart, scriptText.Length);

        GUIStyle style = EditorStyles.textArea;
        string before = scriptText.Substring(0, charStart);
        string span = scriptText.Substring(charStart, charEnd - charStart);
        Vector2 posBefore = style.CalcSize(new GUIContent(before));
        Vector2 sizeSpan = style.CalcSize(new GUIContent(span));

        var rect = new Rect(
            textArea.x + posBefore.x,
            textArea.y,
            sizeSpan.x,
            style.lineHeight);

        Handles.color = color;
        DrawDottedRect(rect);
    }

    static void DrawDottedRect(Rect r)
    {
        const float dash = 4f;
        DrawDottedLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin), dash);
        DrawDottedLine(new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax), dash);
        DrawDottedLine(new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax), dash);
        DrawDottedLine(new Vector3(r.xMin, r.yMax), new Vector3(r.xMin, r.yMin), dash);
    }

    static void DrawDottedLine(Vector3 a, Vector3 b, float dash)
    {
        float len = Vector3.Distance(a, b);
        if (len < 0.01f)
            return;
        Vector3 dir = (b - a).normalized;
        float t = 0f;
        while (t < len)
        {
            float t2 = Mathf.Min(t + dash, len);
            Handles.DrawLine(a + dir * t, a + dir * t2);
            t += dash * 2f;
        }
    }
}
#endif
