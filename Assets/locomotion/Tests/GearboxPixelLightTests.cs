using NUnit.Framework;
using UnityEngine;

public sealed class GearboxPixelLightTests
{
    [Test]
    public void GearRatio_DrivenOverDriving()
    {
        var spec = ScriptableObject.CreateInstance<GearboxSpec>();
        var pair = spec.AddGearPair(12, 36);
        Assert.AreEqual(3f, pair.Ratio, 0.001f);
        pair.kind = GearboxDriveKind.Belt;
        pair.drivingPulleyM = 0.1f;
        pair.drivenPulleyM = 0.2f;
        Assert.AreEqual(2f, pair.Ratio, 0.001f);
        Assert.Greater(pair.BeltPathLengthM(), pair.ChainBeltPathLengthM() * 0.2f);
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void Inclusion_ShellInside_FrameOnMember()
    {
        var go = new GameObject("mount");
        try
        {
            var m = go.AddComponent<Bounds4SdfInclusionPixelLightMount>();
            m.gridWidth = 8;
            m.gridHeight = 8;
            m.cellSize = 0.1f;
            m.inclusionKind = Bounds4SdfInclusionKind.Shell;
            Assert.IsTrue(m.ShellContains(Vector3.zero));
            Assert.IsFalse(m.FrameMemberContains(Vector3.zero));
            m.inclusionKind = Bounds4SdfInclusionKind.Frame;
            float r = m.ShellRadius();
            Assert.IsTrue(m.FrameMemberContains(Vector3.right * r));
            Assert.IsFalse(m.ShellContains(Vector3.right * r * 3f));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Catalog_IncrementingMountLabels()
    {
        var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
        try
        {
            Assert.AreEqual("Mount 1", catalog.NextIncrementingLabel("Mount"));
            catalog.AddSlot();
            catalog.AddSlot();
            Assert.AreEqual("Mount 3", catalog.NextIncrementingLabel("Mount"));
            Assert.AreEqual("Mount 1", catalog.gridSlots[0].label);
            Assert.AreEqual("Mount 2", catalog.gridSlots[1].label);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void SixIsometricViews_DistinctRotations()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (PixelLightDesignerView v in System.Enum.GetValues(typeof(PixelLightDesignerView)))
            seen.Add(PixelLightIsometricViews.RotationFor(v).eulerAngles.ToString("0.0"));
        Assert.AreEqual(6, seen.Count);
        Assert.AreEqual(PixelLightDesignerScope.Frame, (PixelLightDesignerScope)2);
        Assert.AreEqual(PixelLightDesignerScope.Shell, (PixelLightDesignerScope)3);
    }

    [Test]
    public void BakeSubtract_HasRoot()
    {
        var go = new GameObject("mount");
        try
        {
            var m = go.AddComponent<Bounds4SdfInclusionPixelLightMount>();
            var baked = m.BakeHardwareSubtract();
            Assert.GreaterOrEqual(baked.nodes.Count, 1);
            Assert.GreaterOrEqual(baked.ResolveRootIndex(), 0);
            Object.DestroyImmediate(baked);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
