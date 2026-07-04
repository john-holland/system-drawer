#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative.Music;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    public static class MusicCompositionGraphDrawer
    {
        public static readonly Color OverlayGreen = new Color(0.29f, 0.87f, 0.5f, 1f);
        public static readonly Color BaselineDark = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        public static void DrawEdge(Vector2 from, Vector2 to, MusicOverlayEdgeKind kind, bool overlay)
        {
            Color c = overlay ? OverlayGreen : BaselineDark;
            if (kind == MusicOverlayEdgeKind.Return)
            {
                Handles.color = c;
                Handles.DrawDottedLine(from, to, 4f);
            }
            else if (kind == MusicOverlayEdgeKind.Release)
            {
                Handles.color = c;
                Handles.DrawDottedLine(from, to, 2f);
            }
            else
            {
                Handles.color = c;
                Handles.DrawLine(from, to);
            }
        }

        public static Dictionary<string, Vector2> LayoutNodes(IReadOnlyList<MusicBehaviorNode> nodes, Rect area)
        {
            var positions = new Dictionary<string, Vector2>();
            if (nodes == null || nodes.Count == 0) return positions;

            float stepX = area.width / Mathf.Max(1, nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                MusicBehaviorNode n = nodes[i];
                float x = area.x + stepX * i + stepX * 0.5f;
                float y = area.y + area.height * 0.5f;
                positions[n.nodeId] = new Vector2(x, y);
            }
            return positions;
        }

        public static void DrawNode(Vector2 pos, MusicBehaviorNode node, bool selected, bool suspended)
        {
            float size = 40f;
            var rect = new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size);
            Color fill = suspended ? new Color(0.4f, 0.4f, 0.35f, 0.6f) : new Color(0.25f, 0.35f, 0.45f, 0.9f);
            if (selected) fill = Color.Lerp(fill, OverlayGreen, 0.35f);
            EditorGUI.DrawRect(rect, fill);
            Handles.color = OverlayGreen;
            Handles.DrawWireCube(pos, Vector3.one * size);

            if (node != null && node.exitCut != MusicPointCutMode.None)
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(new Vector3(pos.x + size * 0.5f, pos.y - size * 0.5f, 0),
                    new Vector3(pos.x + size * 0.5f, pos.y - size * 0.5f - 12f, 0));
            }
        }
    }
}
#endif
