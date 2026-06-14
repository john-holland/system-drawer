using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Squarified treemap layout (WinDirStat-style area proportional tiles).</summary>
public static class SquarifiedTreemapLayout
{
    public const float MinTileEdge = 2f;

    /// <summary>Layout only immediate children into area (WinDirStat drill-down level).</summary>
    public static void ApplyFlat(IReadOnlyList<MemorySwizzleNode> children, Rect area)
    {
        if (children == null || children.Count == 0 || area.width <= 0f || area.height <= 0f)
            return;

        var items = new List<LayoutItem>(children.Count);
        long total = 0;
        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            long w = Math.Max(1, c.SizeBytes);
            items.Add(new LayoutItem(c, w));
            total += w;
        }

        if (total <= 0)
            return;

        LayoutSquarified(items, area, total);
        for (int i = 0; i < items.Count; i++)
            items[i].Node.LayoutRect = items[i].Rect;
    }

    public static void Apply(MemorySwizzleNode parent, Rect area)
    {
        if (parent == null || area.width <= 0f || area.height <= 0f)
            return;
        parent.LayoutRect = area;
        ApplyFlat(parent.Children, area);
    }

    static void LayoutSquarified(List<LayoutItem> items, Rect area, long totalWeight)
    {
        items.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        int i = 0;
        long remainingWeight = totalWeight;
        while (i < items.Count)
        {
            bool horizontal = area.width >= area.height;
            int rowCount = 1;
            float rowWeight = items[i].Weight;
            float aspect = WorstAspect(rowWeight, rowCount, area, horizontal, remainingWeight);

            for (int j = i + 1; j < items.Count; j++)
            {
                float trialWeight = rowWeight + items[j].Weight;
                float trialAspect = WorstAspect(trialWeight, j - i + 1, area, horizontal, remainingWeight);
                if (trialAspect <= aspect)
                {
                    rowWeight = trialWeight;
                    rowCount = j - i + 1;
                    aspect = trialAspect;
                }
                else
                    break;
            }

            PlaceRow(items, i, rowCount, area, horizontal, remainingWeight, out Rect remainder);
            long placedWeight = 0;
            for (int k = 0; k < rowCount; k++)
                placedWeight += items[i + k].Weight;
            remainingWeight -= placedWeight;
            area = remainder;
            i += rowCount;
        }
    }

    static float WorstAspect(float rowWeight, int count, Rect area, bool horizontal, long totalWeight)
    {
        if (count <= 0 || totalWeight <= 0 || area.width <= 0f || area.height <= 0f)
            return float.MaxValue;

        float frac = rowWeight / totalWeight;
        float rowLen = horizontal ? area.height * frac : area.width * frac;
        if (rowLen <= 0f)
            return float.MaxValue;

        float cross = horizontal ? area.width : area.height;
        float maxSide = Mathf.Max(rowLen, cross / count);
        float minSide = Mathf.Min(rowLen, cross / count);
        if (minSide <= 0f)
            return float.MaxValue;
        return maxSide / minSide;
    }

    static void PlaceRow(List<LayoutItem> items, int start, int count, Rect area, bool horizontal, long totalWeight,
        out Rect remainder)
    {
        long rowWeight = 0;
        for (int k = 0; k < count; k++)
            rowWeight += items[start + k].Weight;

        float frac = totalWeight > 0 ? (float)rowWeight / totalWeight : 0f;
        if (horizontal)
        {
            float rowH = area.height * frac;
            float x = area.x;
            float wSum = 0f;
            for (int k = 0; k < count; k++)
            {
                int idx = start + k;
                float w = area.width * (items[idx].Weight / (float)rowWeight);
                var item = items[idx];
                item.Rect = new Rect(x + wSum, area.y, w, rowH);
                items[idx] = item;
                wSum += w;
            }
            remainder = new Rect(area.x, area.y + rowH, area.width, area.height - rowH);
        }
        else
        {
            float rowW = area.width * frac;
            float y = area.y;
            float hSum = 0f;
            for (int k = 0; k < count; k++)
            {
                int idx = start + k;
                float h = area.height * (items[idx].Weight / (float)rowWeight);
                var item = items[idx];
                item.Rect = new Rect(area.x, y + hSum, rowW, h);
                items[idx] = item;
                hSum += h;
            }
            remainder = new Rect(area.x + rowW, area.y, area.width - rowW, area.height);
        }
    }

    struct LayoutItem
    {
        public MemorySwizzleNode Node;
        public long Weight;
        public Rect Rect;

        public LayoutItem(MemorySwizzleNode node, long weight)
        {
            Node = node;
            Weight = weight;
            Rect = default;
        }
    }
}
