using UnityEditor;
using UnityEngine;

/// <summary>IMGUI power diamond: Front / Side / Back / Length proportions.</summary>
public static class HairdoPowerDiamondDrawer
{
    public static void Draw(Rect rect, float front, float side, float back, float length, bool hasBlend)
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
        Handles.color = new Color(0.35f, 0.35f, 0.4f, 1f);
        Handles.DrawAAPolyLine(2f, top, right, bottom, left, top);
        Handles.DrawAAPolyLine(1f, top, bottom);
        Handles.DrawAAPolyLine(1f, left, right);

        if (hasBlend)
        {
            front = Mathf.Clamp01(front);
            side = Mathf.Clamp01(side);
            back = Mathf.Clamp01(back);
            length = Mathf.Clamp01(length);

            Vector2 pFront = Vector2.Lerp(c, top, front);
            Vector2 pSide = Vector2.Lerp(c, right, side);
            Vector2 pBack = Vector2.Lerp(c, bottom, back);
            Vector2 pLen = Vector2.Lerp(c, left, length);

            Handles.color = new Color(0.25f, 0.75f, 0.95f, 0.35f);
            Handles.DrawAAConvexPolygon(pFront, pSide, pBack, pLen);
            Handles.color = new Color(0.4f, 0.9f, 1f, 1f);
            Handles.DrawAAPolyLine(2.5f, pFront, pSide, pBack, pLen, pFront);
        }

        Handles.EndGUI();

        var label = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        float pad = 2f;
        GUI.Label(new Rect(top.x - 24f, rect.yMin + pad, 48f, 14f), "Front", label);
        GUI.Label(new Rect(right.x - 8f, right.y - 7f, 40f, 14f), "Side", label);
        GUI.Label(new Rect(bottom.x - 20f, rect.yMax - 16f - pad, 40f, 14f), "Back", label);
        GUI.Label(new Rect(rect.xMin + pad, left.y - 7f, 48f, 14f), "Length", label);

        if (!hasBlend)
        {
            var help = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            GUI.Label(rect, "enable at least one cut", help);
        }
    }
}
