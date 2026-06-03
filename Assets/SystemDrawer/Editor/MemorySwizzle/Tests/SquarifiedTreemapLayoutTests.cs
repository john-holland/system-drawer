#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SquarifiedTreemapLayoutTests
{
    [Test]
    public void Apply_ChildRectsSumToContainerArea()
    {
        var root = MemorySwizzleNode.Create("r", "Root", 0, MemorySwizzleKind.Root, MemorySwizzleViewMode.UnitySystems);
        root.Children.Add(MemorySwizzleNode.Create("a", "A", 300, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems));
        root.Children.Add(MemorySwizzleNode.Create("b", "B", 200, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems));
        root.Children.Add(MemorySwizzleNode.Create("c", "C", 500, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems));
        root.ComputeTotalBytes();

        var area = new Rect(0, 0, 400, 300);
        SquarifiedTreemapLayout.ApplyFlat(root.Children, area);

        float childArea = 0f;
        for (int i = 0; i < root.Children.Count; i++)
            childArea += root.Children[i].LayoutRect.width * root.Children[i].LayoutRect.height;

        Assert.AreEqual(area.width * area.height, childArea, 1f);
    }

    [Test]
    public void ComputeTotalBytes_RollsUpChildren()
    {
        var root = MemorySwizzleNode.Create("r", "Root", 0, MemorySwizzleKind.Root, MemorySwizzleViewMode.UnitySystems);
        root.Children.Add(MemorySwizzleNode.Create("a", "A", 100, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems));
        root.Children.Add(MemorySwizzleNode.Create("b", "B", 250, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems));
        Assert.AreEqual(350, root.ComputeTotalBytes());
    }
}
#endif
