using UnityEditor;
using UnityEngine;

/// <summary>IMGUI power diamond with optional blue / green / red / dashed-white overlays.</summary>
public static class PowerDiamondDrawer
{
    public static readonly string[] ConstructionAxes = { "Commodities", "Resources", "Vehicle", "Blockage" };

    public static void DrawOverlay(Rect rect, string[] axes, float[] blue01, float[] redLimit01, float[] dashedWhite01, float threatHalo01)
    {
        DrawOverlay(rect, axes, blue01, redLimit01, dashedWhite01, threatHalo01, null, 0f);
    }

    public static void DrawOverlay(
        Rect rect,
        string[] axes,
        float[] blue01,
        float[] redLimit01,
        float[] dashedWhite01,
        float threatHalo01,
        float[] green01,
        float specularShine01)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f, 1f));
        Vector2 c = rect.center;
        float rad = Mathf.Min(rect.width, rect.height) * 0.38f;
        Vector2 top = c + new Vector2(0f, -rad);
        Vector2 right = c + new Vector2(rad, 0f);
        Vector2 bottom = c + new Vector2(0f, rad);
        Vector2 left = c + new Vector2(-rad, 0f);

        Handles.BeginGUI();
        if (threatHalo01 > 0.01f)
        {
            Handles.color = new Color(1f, 0.15f, 0.1f, Mathf.Clamp01(threatHalo01) * 0.45f);
            Handles.DrawSolidDisc(c, Vector3.forward, rad * 0.95f);
        }

        Handles.color = new Color(0.35f, 0.35f, 0.4f, 1f);
        Handles.DrawAAPolyLine(2f, top, right, bottom, left, top);

        DrawPoly(c, top, right, bottom, left, redLimit01, new Color(1f, 0.2f, 0.15f, 0.25f), new Color(1f, 0.25f, 0.2f, 1f), 2.5f, false);
        DrawPoly(c, top, right, bottom, left, blue01, new Color(0.25f, 0.45f, 1f, 0.35f), new Color(0.35f, 0.55f, 1f, 1f), 2.5f, false);
        DrawPoly(c, top, right, bottom, left, green01, new Color(0.2f, 0.75f, 0.35f, 0.32f), new Color(0.25f, 0.9f, 0.4f, 1f), 2.5f, false);
        float shine = Mathf.Clamp01(specularShine01);
        Color whiteLine = new Color(1f, 1f, 1f, 0.55f + 0.45f * shine);
        DrawPoly(c, top, right, bottom, left, dashedWhite01, Color.clear, whiteLine, 2f, true);
        if (dashedWhite01 != null && dashedWhite01.Length >= 4 && shine > 0.01f)
        {
            Color spec = new Color(1f, 1f, 1f, 0.25f + 0.55f * shine);
            DrawPolyOffset(c, top, right, bottom, left, dashedWhite01, spec, 3f);
        }
        Handles.EndGUI();

        var label = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        string[] names = axes != null && axes.Length >= 4 ? axes : ConstructionAxes;
        GUI.Label(new Rect(top.x - 36f, rect.yMin + 2f, 72f, 14f), names[0], label);
        GUI.Label(new Rect(right.x - 8f, right.y - 7f, 56f, 14f), names[1], label);
        GUI.Label(new Rect(bottom.x - 28f, rect.yMax - 18f, 56f, 14f), names[2], label);
        GUI.Label(new Rect(rect.xMin + 2f, left.y - 7f, 56f, 14f), names[3], label);
    }

    static void DrawPoly(Vector2 c, Vector2 top, Vector2 right, Vector2 bottom, Vector2 left, float[] v, Color fill, Color line, float width, bool dashed)
    {
        if (v == null || v.Length < 4) return;
        Vector2 p0 = Vector2.Lerp(c, top, Mathf.Clamp01(v[0]));
        Vector2 p1 = Vector2.Lerp(c, right, Mathf.Clamp01(v[1]));
        Vector2 p2 = Vector2.Lerp(c, bottom, Mathf.Clamp01(v[2]));
        Vector2 p3 = Vector2.Lerp(c, left, Mathf.Clamp01(v[3]));
        if (fill.a > 0.01f)
        {
            Handles.color = fill;
            Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
        }
        Handles.color = line;
        if (dashed)
        {
            Handles.DrawDottedLine(p0, p1, 4f);
            Handles.DrawDottedLine(p1, p2, 4f);
            Handles.DrawDottedLine(p2, p3, 4f);
            Handles.DrawDottedLine(p3, p0, 4f);
        }
        else
            Handles.DrawAAPolyLine(width, p0, p1, p2, p3, p0);
    }

    static void DrawPolyOffset(Vector2 c, Vector2 top, Vector2 right, Vector2 bottom, Vector2 left, float[] v, Color line, float offsetPx)
    {
        Vector2 p0 = Vector2.Lerp(c, top, Mathf.Clamp01(v[0])) + new Vector2(0f, -offsetPx);
        Vector2 p1 = Vector2.Lerp(c, right, Mathf.Clamp01(v[1])) + new Vector2(offsetPx, 0f);
        Vector2 p2 = Vector2.Lerp(c, bottom, Mathf.Clamp01(v[2])) + new Vector2(0f, offsetPx);
        Vector2 p3 = Vector2.Lerp(c, left, Mathf.Clamp01(v[3])) + new Vector2(-offsetPx, 0f);
        Handles.color = line;
        Handles.DrawDottedLine(p0, p1, 3f);
        Handles.DrawDottedLine(p1, p2, 3f);
        Handles.DrawDottedLine(p2, p3, 3f);
        Handles.DrawDottedLine(p3, p0, 3f);
    }
}
