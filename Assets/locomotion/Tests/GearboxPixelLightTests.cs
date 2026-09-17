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
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.ShellInclusion,
                Bounds4SdfInclusionLemmas.ToInclusionLemma(m.inclusionKind));
            m.inclusionKind = Bounds4SdfInclusionLemmas.FromLemma("frame");
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

    [Test]
    public void BakeSubtract_HollowSlot_IsOutsideSolid()
    {
        var go = new GameObject("mount");
        var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
        try
        {
            var m = go.AddComponent<Bounds4SdfInclusionPixelLightMount>();
            m.gridWidth = 8;
            m.gridHeight = 8;
            m.cellSize = 0.1f;
            m.inclusionKind = Bounds4SdfInclusionKind.Shell;
            catalog.EnsureHollow("bore", "Bore", "frame", Bounds4SdfInclusionKind.Shell, 2, 4, 0.04f);
            var baked = m.BakeHardwareSubtract(catalog, null, true);
            Assert.Greater(baked.nodes.Count, 1);
            var eval = new SdfMax.SdfMaxEvaluator(new SdfMax.SdfMaxExpressionGraph(baked, null, Matrix4x4.identity));
            Assert.Less(eval.Sample(Vector3.zero, 0f), 0f);
            Assert.Greater(eval.Sample(m.CellLocalPosition(2, 4), 0f), 0f);
            Object.DestroyImmediate(baked);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BakeSubtract_ZIndexOrder_SecondHollowWinsCount()
    {
        var go = new GameObject("mount");
        var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
        try
        {
            var m = go.AddComponent<Bounds4SdfInclusionPixelLightMount>();
            m.inclusionKind = Bounds4SdfInclusionKind.Shell;
            catalog.EnsureHollow("h0", "H0", "shell", Bounds4SdfInclusionKind.Shell, 1, 1, 0.02f);
            catalog.EnsureHollow("h1", "H1", "shell", Bounds4SdfInclusionKind.Shell, 2, 2, 0.02f);
            catalog.FindSlot("h1").zIndex = 8;
            catalog.FindSlot("h0").zIndex = 1;
            catalog.SortSlotsByZ();
            var baked = m.BakeHardwareSubtract(catalog);
            Assert.GreaterOrEqual(baked.nodes.Count, 5);
            Object.DestroyImmediate(baked);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(go);
        }
    }
}
