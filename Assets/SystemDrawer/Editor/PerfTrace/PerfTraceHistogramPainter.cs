#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI horizontal histogram rendering and hit testing.</summary>
public static class PerfTraceHistogramPainter
{
    static readonly int LabelHashSeed = "PerfTrace".GetHashCode();

    public static void DrawHistogram(IReadOnlyList<PerfTraceNode> nodes, PerfTraceNode hover, PerfTraceNode selected)
    {
        if (nodes == null)
            return;
        for (int i = 0; i < nodes.Count; i++)
            DrawBar(nodes[i], hover, selected);
    }

    static void DrawBar(PerfTraceNode node, PerfTraceNode hover, PerfTraceNode selected)
    {
        var r = node.LayoutRect;
        if (r.width < 1f || r.height < 1f)
            return;

        bool isHover = hover == node;
        bool isSel = selected == node;
        Color baseColor = ColorFromLabel(node.Label);
        if (isSel)
            baseColor = Color.Lerp(baseColor, Color.white, 0.35f);
        else if (isHover)
            baseColor = Color.Lerp(baseColor, Color.white, 0.2f);

        EditorGUI.DrawRect(r, baseColor);
        Handles.color = new Color(0f, 0f, 0f, 0.35f);
        Handles.DrawLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin));
        Handles.DrawLine(new Vector3(r.xMin, r.yMax), new Vector3(r.xMax, r.yMax));

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };
        string text = node.Label + "  " + PerfTraceFormat.Ms(node.TotalTicks);
        GUI.Label(new Rect(r.x + 4f, r.y, r.width - 8f, r.height), text, style);
    }

    public static PerfTraceNode HitTest(IReadOnlyList<PerfTraceNode> nodes, Vector2 mouse)
    {
        if (nodes == null)
            return null;
        PerfTraceNode best = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].LayoutRect.Contains(mouse))
                best = nodes[i];
        }
        return best;
    }

    static Color ColorFromLabel(string label)
    {
        int hash = (label ?? "").GetHashCode() ^ LabelHashSeed;
        float h = (hash & 0x7FFFFFFF) % 360 / 360f;
        return Color.HSVToRGB(h, 0.55f, 0.75f);
    }
}
#endif
