using System.Collections.Generic;
using UnityEngine;

/// <summary>Horizontal bar histogram layout (width proportional to duration).</summary>
public static class PerfTraceHistogramLayout
{
    public const float MinBarWidth = 4f;
    public const float RowHeight = 22f;
    public const float RowGap = 2f;

    public static void Apply(IReadOnlyList<PerfTraceNode> nodes, Rect area)
    {
        if (nodes == null || nodes.Count == 0 || area.width <= 1f || area.height <= 1f)
            return;

        long total = 0;
        for (int i = 0; i < nodes.Count; i++)
            total += nodes[i].TotalTicks;
        if (total <= 0)
            total = 1;

        float y = area.y;
        float maxWidth = area.width;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            float ratio = (float)node.TotalTicks / total;
            float w = Mathf.Max(MinBarWidth, ratio * maxWidth);
            if (i == nodes.Count - 1)
                w = Mathf.Max(MinBarWidth, area.xMax - area.x - 0f);
            node.LayoutRect = new Rect(area.x, y, Mathf.Min(w, area.xMax - area.x), RowHeight);
            y += RowHeight + RowGap;
            if (y > area.yMax)
                break;
        }
    }
}
