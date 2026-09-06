using NUnit.Framework;
using UnityEngine;

public sealed class PixelLightRadialBrushTests
{
    [Test]
    public void Stamp_NxN_AroundCentroid_PlacesRing()
    {
        var spec = new RadialBuildSpec { count = 4, wrapAngleDeg = 360f, radius = 1f };
        var cells = PixelLightRadialStamp.Enumerate(
            8, 8, 0.25f, Vector3.up, Vector3.zero,
            4, 4, 2, 2, RadialSide.Center, spec, default, false, false, 1, 1);
        Assert.AreEqual(4, cells.Count);
        Vector3 centroid = PixelLightGridMountGameObject.CellLocalPosition(
            8, 8, 0.25f, Vector3.up, Vector3.zero, 4, 4);
        float r0 = Vector3.Distance(cells[0].localPosition, centroid);
        for (int i = 0; i < cells.Count; i++)
        {
            Assert.IsFalse(cells[i].nested);
            Assert.AreEqual(r0, Vector3.Distance(cells[i].localPosition, centroid), 0.05f);
        }
        Assert.Greater(Vector3.Distance(cells[0].localPosition, cells[2].localPosition), 1f);
    }

    [Test]
    public void Stamp_SideOrigin_UpperLeft_NotCenter()
    {
        var spec = new RadialBuildSpec { count = 3, radius = 0.5f };
        var center = PixelLightRadialStamp.Enumerate(
            8, 8, 0.25f, Vector3.up, Vector3.zero,
            3, 3, 1, 3, RadialSide.Center, spec, default, false, false, 1, 1);
        var ul = PixelLightRadialStamp.Enumerate(
            8, 8, 0.25f, Vector3.up, Vector3.zero,
            3, 3, 1, 3, RadialSide.UpperLeft, spec, default, false, false, 1, 1);
        Assert.AreEqual(3, center.Count);
        Assert.AreEqual(3, ul.Count);
        Assert.Greater(Vector3.Distance(center[0].localPosition, ul[0].localPosition), 0.02f);
    }

    [Test]
    public void Stamp_RecursiveBlock_AddsNestedCells()
    {
        var spec = new RadialBuildSpec { count = 2, radius = 0.8f };
        var cells = PixelLightRadialStamp.Enumerate(
            8, 8, 0.25f, Vector3.up, Vector3.zero,
            4, 4, 2, 1, RadialSide.Center, spec, default, false, true, 2, 2);
        int outer = 0, nested = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].nested) nested++;
            else outer++;
        }
        Assert.AreEqual(2, outer);
        Assert.AreEqual(8, nested);
    }

    [Test]
    public void Mount_CopyApply_RoundTripsRadialFields()
    {
        var go = new GameObject("mount");
        try
        {
            var mount = go.AddComponent<PixelLightGridMountGameObject>();
            mount.minigridW = 3;
            mount.minigridH = 3;
            mount.centroidCellX = 2;
            mount.centroidCellY = 5;
            mount.radialSide = RadialSide.Right;
            mount.recursiveBlock = true;
            mount.nestedMinigridW = 2;
            mount.radialBuild = new RadialBuildSpec { count = 5, joinKind = RadialJoinKind.Offset, joinOffset = 0.2f };
            var vs = new PixelLightViewScopeSettings();
            vs.CopyFromMount(mount, null);
            Assert.AreEqual(3, vs.minigridW);
            Assert.AreEqual(RadialSide.Right, vs.radialSide);
            Assert.AreEqual(5, vs.radialBuild.count);
            var go2 = new GameObject("mount2");
            var mount2 = go2.AddComponent<PixelLightGridMountGameObject>();
            vs.ApplyToMount(mount2);
            Assert.AreEqual(3, mount2.minigridW);
            Assert.AreEqual(2, mount2.centroidCellX);
            Assert.AreEqual(RadialSide.Right, mount2.radialSide);
            Assert.AreEqual(RadialJoinKind.Offset, mount2.radialBuild.joinKind);
            Object.DestroyImmediate(go2);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Mount_Enumerate_UsesMinigrid()
    {
        var go = new GameObject("mount");
        try
        {
            var mount = go.AddComponent<PixelLightGridMountGameObject>();
            mount.gridWidth = 8;
            mount.gridHeight = 8;
            mount.cellSize = 0.25f;
            mount.minigridW = 2;
            mount.minigridH = 2;
            mount.centroidCellX = 4;
            mount.centroidCellY = 4;
            mount.radialBuild = new RadialBuildSpec { count = 4, radius = 0.6f };
            var cells = mount.EnumerateRadialStamp();
            Assert.AreEqual(4, cells.Count);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
