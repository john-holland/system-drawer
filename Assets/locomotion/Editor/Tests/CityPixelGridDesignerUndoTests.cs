#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CityPixelGridDesignerUndoTests
{
    [Test]
    public void PaintStroke_UndoRedo_RestoresBrushStamps()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.EnsureLayersAndFrames();
        Assert.AreEqual(0, grid.brushStamps.Count);

        var stroke = new CityPixelGridPaintStroke();
        stroke.Begin(grid, "Paint test");
        grid.SetBrushStamp(new CityPixelBrushStamp
        {
            cellX = 1,
            cellY = 2,
            kind = CityPixelBrushKind.RoadLanes
        });
        stroke.End();
        Assert.AreEqual(1, grid.brushStamps.Count);

        Undo.PerformUndo();
        Assert.AreEqual(0, grid.brushStamps.Count);

        Undo.PerformRedo();
        Assert.AreEqual(1, grid.brushStamps.Count);
        Assert.AreEqual(1, grid.brushStamps[0].cellX);
        Assert.AreEqual(2, grid.brushStamps[0].cellY);

        Object.DestroyImmediate(grid);
    }

    [Test]
    public void PaintStroke_BeginTwice_StaysOneUndoGroup()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.EnsureLayersAndFrames();
        var stroke = new CityPixelGridPaintStroke();
        stroke.Begin(grid, "Paint test");
        grid.SetBrushStamp(new CityPixelBrushStamp { cellX = 0, cellY = 0, kind = CityPixelBrushKind.Crosswalk });
        stroke.Begin(grid, "Paint test");
        grid.SetBrushStamp(new CityPixelBrushStamp { cellX = 1, cellY = 1, kind = CityPixelBrushKind.Sidewalk });
        stroke.End();
        Assert.AreEqual(2, grid.brushStamps.Count);

        Undo.PerformUndo();
        Assert.AreEqual(0, grid.brushStamps.Count);

        Object.DestroyImmediate(grid);
    }
}
#endif
