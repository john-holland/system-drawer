using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>IMGUI treemap rendering and hit testing.</summary>
public static class MemorySwizzleTreemapPainter
{
    static readonly int LabelHashSeed = "MemorySwizzle".GetHashCode();

    public static void DrawTreemap(IReadOnlyList<MemorySwizzleNode> nodes, MemorySwizzleNode hover, MemorySwizzleNode selected)
    {
        if (nodes == null)
            return;
        for (int i = 0; i < nodes.Count; i++)
            DrawTile(nodes[i], hover, selected);
    }

    static void DrawTile(MemorySwizzleNode node, MemorySwizzleNode hover, MemorySwizzleNode selected)
    {
        var r = node.LayoutRect;
        if (r.width < SquarifiedTreemapLayout.MinTileEdge || r.height < SquarifiedTreemapLayout.MinTileEdge)
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
        Handles.DrawLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMin, r.yMax));

        if (r.width > 40f && r.height > 14f)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            string text = node.Label;
            if (r.height > 28f)
                text += "\n" + MemorySwizzleFormat.Bytes(node.SizeBytes);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), text, style);
        }
    }

    public static MemorySwizzleNode HitTest(IReadOnlyList<MemorySwizzleNode> nodes, Vector2 mouse)
    {
        if (nodes == null)
            return null;
        MemorySwizzleNode best = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].LayoutRect.Contains(mouse))
                best = nodes[i];
        }
        return best;
    }

    static Color ColorFromLabel(string label)
    {
        int h = (label ?? "").GetHashCode() ^ LabelHashSeed;
        float hue = (h & 0xFF) / 255f;
        return Color.HSVToRGB(hue, 0.55f, 0.75f);
    }
}
