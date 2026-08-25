#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class CityPixelGridCellSelectionTests
{
    [Test]
    public void Click_ReplacesSelection()
    {
        var sel = new CityPixelGridCellSelection();
        sel.Begin(1, 2, false, false);
        sel.EndDrag();
        sel.Begin(4, 5, false, false);
        sel.EndDrag();
        Assert.AreEqual(1, sel.Count);
        Assert.IsTrue(sel.Contains(4, 5));
        Assert.IsFalse(sel.Contains(1, 2));
    }

    [Test]
    public void Drag_SelectsRectangle()
    {
        var sel = new CityPixelGridCellSelection();
        sel.Begin(1, 1, false, false);
        sel.DragTo(3, 2);
        sel.EndDrag();
        Assert.AreEqual(6, sel.Count);
        Assert.IsTrue(sel.Contains(1, 1));
        Assert.IsTrue(sel.Contains(3, 2));
        Assert.IsTrue(sel.Contains(2, 1));
        Assert.IsFalse(sel.Contains(0, 0));
    }

    [Test]
    public void ShiftDrag_AddsRectangle()
    {
        var sel = new CityPixelGridCellSelection();
        sel.Begin(0, 0, false, false);
        sel.EndDrag();
        sel.Begin(2, 0, true, false);
        sel.DragTo(3, 0);
        sel.EndDrag();
        Assert.AreEqual(3, sel.Count);
        Assert.IsTrue(sel.Contains(0, 0));
        Assert.IsTrue(sel.Contains(2, 0));
        Assert.IsTrue(sel.Contains(3, 0));
    }

    [Test]
    public void CtrlClick_TogglesCell()
    {
        var sel = new CityPixelGridCellSelection();
        sel.Begin(1, 1, false, false);
        sel.EndDrag();
        sel.Begin(1, 1, false, true);
        sel.EndDrag();
        Assert.AreEqual(0, sel.Count);
        sel.Begin(1, 1, false, true);
        sel.EndDrag();
        Assert.IsTrue(sel.Contains(1, 1));
    }
}
#endif
