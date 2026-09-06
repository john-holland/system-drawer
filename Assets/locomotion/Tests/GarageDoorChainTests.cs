using NUnit.Framework;
using UnityEngine;

public sealed class GarageDoorChainTests
{
    [Test]
    public void LinkKinds_MasterVsBroken_BreakTension()
    {
        var steel = GarageSteelLimits.DefaultSteel();
        var master = new GarageChainLinkDef { kind = GarageChainLinkKind.Master };
        var broken = new GarageChainLinkDef { kind = GarageChainLinkKind.Broken };
        Assert.Greater(master.BreakTensionN(steel), broken.BreakTensionN(steel));
        Assert.AreEqual(1f, broken.BreakTensionN(steel), 0.01f);
        Assert.AreEqual(18000f, master.BreakTensionN(steel), 0.01f);
        Assert.AreEqual(12000f, new GarageChainLinkDef { kind = GarageChainLinkKind.Chain }.BreakTensionN(steel), 0.01f);
    }

    [Test]
    public void Steel_ApplyToRope_WeakestLink()
    {
        var cfg = new RopeConfig();
        GarageSteelLimits.DefaultSteel().ApplyTo(cfg, GarageChainLinkKind.Broken);
        Assert.AreEqual(RopeTotalStrengthPolicy.WeakestLink, cfg.totalStrengthPolicy);
        Assert.AreEqual(1f, cfg.breakTensionN, 0.01f);
    }

    [Test]
    public void ToothPoses_MatchRadialSlotMath()
    {
        Vector3 c = Vector3.zero;
        Vector3 axis = Vector3.right;
        const int n = 8;
        const float r = 0.2f;
        var poses = GarageChainWheelTeeth.AllToothPoses(c, axis, r, n);
        Assert.AreEqual(n, poses.Count);
        for (int i = 0; i < n; i++)
        {
            Vector3 expect = RadialSlotMath.PolarSlot(c, axis, r, i, n, 0f, 360f);
            Assert.AreEqual(0f, Vector3.Distance(expect, poses[i]), 0.001f);
        }
    }

    [Test]
    public void SphBake_CoversLength_NoReSimOnBendSample()
    {
        var spec = ScriptableObject.CreateInstance<GarageChainSpec>();
        spec.totalLengthM = 4f;
        spec.linkPitchM = 0.08f;
        spec.pitchRadiusM = 0.12f;
        spec.toothCount = 10;
        var field = new GarageChainSphPullField();
        field.Bake(spec);
        Assert.Greater(field.BinCount, 4);
        Assert.AreEqual(1, field.bakeCount);
        float t0 = field.SampleTension(0.5f);
        Vector3 b0 = field.SampleBend(0.25f);
        field.SampleTension(0.9f);
        field.SampleBend(0.8f);
        Assert.AreEqual(1, field.bakeCount);
        Assert.Greater(t0, 0f);
        Assert.Greater(b0.sqrMagnitude, 0f);
        field.Bake(spec);
        Assert.AreEqual(1, field.bakeCount);
        Object.DestroyImmediate(spec);
    }

    [Test]
    public void DoorLemmaFragment_SetsPackAndSides()
    {
        var s = GarageDoorSgPackSettings.FromLemmaFragment("pack=3d,placement=uniform,pad=0.04,sides=4");
        Assert.AreEqual(GarageDoorSgPackSettings.PackDimension.ThreeDimensional, s.dimension);
        Assert.AreEqual(GarageDoorSgPackSettings.PackPlacement.UniformQueue, s.placement);
        Assert.AreEqual(0.04f, s.paddingMeters, 0.001f);
        Assert.AreEqual(4, s.mouldingSides);
        var door = ScriptableObject.CreateInstance<DoorAssemblySpec>();
        s.ApplyToDoor(door);
        Assert.AreEqual(4, door.mouldingSides);
        Assert.AreEqual(4, door.MouldingSideCount);
        Object.DestroyImmediate(door);
    }

    [Test]
    public void DriveLink_LiftsWhenIntact_ZeroWhenBroken()
    {
        var root = new GameObject("drive");
        var door = new GameObject("leaf");
        door.transform.SetParent(root.transform, false);
        try
        {
            var spec = ScriptableObject.CreateInstance<GarageChainSpec>();
            spec.selectedKind = GarageChainLinkKind.Chain;
            spec.pitchRadiusM = 0.1f;
            var link = root.AddComponent<GarageDoorDriveLink>();
            link.spec = spec;
            link.doorLeaf = door.transform;
            link.axleTorqueNm = 80f;
            link.axleAngularRateRad = 1f;
            link.slideMeters = 2f;
            float force = link.ComputeTangentForceN();
            Assert.Greater(force, 0f);
            link.Apply(0.1f);
            Assert.Greater(link.open01, 0f);
            Assert.Greater(door.transform.localPosition.y, 0f);

            spec.selectedKind = GarageChainLinkKind.Broken;
            Assert.AreEqual(0f, link.ComputeTangentForceN(), 0.001f);
            Assert.AreEqual(0f, link.ComputeWindRateMps(), 0.001f);
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void VerticalTrack_WallPlane_NotGroundRibbon()
    {
        var go = new GameObject("track");
        try
        {
            var track = go.AddComponent<GarageDoorVerticalTrack>();
            track.EnsureWallPlaneSpline();
            Assert.IsNotNull(track.spline);
            Assert.GreaterOrEqual(track.spline.controlPoints.Count, 3);
            float ySpan = 0f;
            for (int i = 0; i < track.spline.controlPoints.Count; i++)
                ySpan = Mathf.Max(ySpan, track.spline.controlPoints[i].y);
            Assert.Greater(ySpan, 1f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ChainAssembly_RebuildsLinkMountsAroundAxle()
    {
        var go = new GameObject("chain");
        try
        {
            var spec = ScriptableObject.CreateInstance<GarageChainSpec>();
            spec.totalLengthM = 0.8f;
            spec.linkPitchM = 0.2f;
            spec.pitchRadiusM = 0.15f;
            var asm = go.AddComponent<GarageChainAssembly>();
            asm.spec = spec;
            asm.RebuildLinkMounts();
            Assert.AreEqual(spec.LinkCount, asm.linkMounts.Count);
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SdfBuiltins_MouldingRadial_DefaultFourSides()
    {
        var radial = GarageDoorSdfBuiltins.MouldingRadial(4);
        Assert.AreEqual(4, radial.count);
        var spec = ScriptableObject.CreateInstance<DoorAssemblySpec>();
        var shell = GarageDoorSdfBuiltins.BuildDoorShell(spec);
        Assert.IsNotNull(shell);
        Assert.Greater(shell.nodes.Count, 0);
        Object.DestroyImmediate(spec);
        Object.DestroyImmediate(shell);
    }

    [Test]
    public void AngularLemmas_StilePerpRail_MullionParallel()
    {
        Assert.AreEqual(90f, DoorCarpentryLemmaPropertyKeys.DefaultStilePerpRailDeg, 0.01f);
        Assert.AreEqual(0f, DoorCarpentryLemmaPropertyKeys.DefaultMullionParallelStileDeg, 0.01f);
        Assert.Contains(DoorCarpentryLemmaPropertyKeys.LemmaWrapMoulding, DoorCarpentryLemmaPropertyKeys.AllKeys);
    }
}
