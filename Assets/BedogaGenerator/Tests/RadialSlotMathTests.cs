using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RadialSlotMathTests
{
    [Test]
    public void SideOrigin_NineWay_MatchesCellCornersAndEdges()
    {
        var cell = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        Assert.AreEqual(Vector3.zero, RadialSlotMath.SideOrigin(cell, RadialSide.Center));
        Assert.AreEqual(new Vector3(-1f, 1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.UpperLeft));
        Assert.AreEqual(new Vector3(0f, 1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.Up));
        Assert.AreEqual(new Vector3(1f, 1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.UpperRight));
        Assert.AreEqual(new Vector3(1f, 0f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.Right));
        Assert.AreEqual(new Vector3(1f, -1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.LowerRight));
        Assert.AreEqual(new Vector3(0f, -1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.Bottom));
        Assert.AreEqual(new Vector3(-1f, -1f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.LowerLeft));
        Assert.AreEqual(new Vector3(-1f, 0f, 0f), RadialSlotMath.SideOrigin(cell, RadialSide.Left));
    }

    [Test]
    public void SlotAngle_Closed360_EqualSteps()
    {
        Assert.AreEqual(0f, RadialSlotMath.SlotAngleDeg(0, 4, 0f, 360f), 0.001f);
        Assert.AreEqual(90f, RadialSlotMath.SlotAngleDeg(1, 4, 0f, 360f), 0.001f);
        Assert.AreEqual(180f, RadialSlotMath.SlotAngleDeg(2, 4, 0f, 360f), 0.001f);
        Assert.AreEqual(270f, RadialSlotMath.SlotAngleDeg(3, 4, 0f, 360f), 0.001f);
    }

    [Test]
    public void PolarSlot_FourAroundOrigin_EqualRadius()
    {
        Vector3 c = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = RadialSlotMath.PolarSlot(c, Vector3.up, 2f, i, 4, 0f, 360f);
            Assert.AreEqual(2f, p.magnitude, 0.01f);
        }
        Vector3 a = RadialSlotMath.PolarSlot(c, Vector3.up, 2f, 0, 4, 0f, 360f);
        Vector3 b = RadialSlotMath.PolarSlot(c, Vector3.up, 2f, 2, 4, 0f, 360f);
        Assert.Greater((a - b).magnitude, 3.5f);
    }

    [Test]
    public void NeighborJoin_OffsetLargerThanNatural()
    {
        Vector3 size = new Vector3(1f, 1f, 1f);
        RadialSlotMath.NeighborJoinPoints(Vector3.zero, Vector3.up, 2f, 0, 4, 0f, 360f, size, 0f,
            out Vector3 n0, out Vector3 n1);
        RadialSlotMath.NeighborJoinPoints(Vector3.zero, Vector3.up, 2f, 0, 4, 0f, 360f, size, 0.5f,
            out Vector3 o0, out Vector3 o1);
        Assert.Greater(Vector3.Distance(o0, o1), Vector3.Distance(n0, n1) - 0.001f);
    }

    [Test]
    public void CustomAngle_180_TwoPieces()
    {
        var pose = new CustomRadialSidePose { flyAway = new Bounds(Vector3.zero, Vector3.one * 0.02f), customAngle = 180f };
        var cfgs = RadialSlotMath.SolveWorkingJoints(
            pose, Vector3.one, RadialJoinKind.Natural, 0f,
            Vector3.zero, Vector3.up, false, Vector3.zero, Vector3.zero);
        Assert.Greater(cfgs.Count, 0);
        bool found = false;
        for (int i = 0; i < cfgs.Count; i++)
        {
            if (cfgs[i].count == 2 && Mathf.Abs(cfgs[i].wrapAngleDeg - 180f) < 0.1f)
                found = true;
        }
        Assert.IsTrue(found);
    }

    [Test]
    public void CustomAngleObject_90_YieldsWrap90()
    {
        var pose = new CustomRadialSidePose
        {
            flyAway = new Bounds(Vector3.zero, Vector3.one * 0.02f),
            hasCustomAngleObject = true,
            customAngleObjectWorld = new Vector3(1f, 0f, 0f)
        };
        Vector3 start = new Vector3(0f, 0f, 1f);
        float wrap = RadialSlotMath.ResolveWrapDeg(pose, Vector3.zero, Vector3.up, start, true);
        Assert.AreEqual(90f, Mathf.Abs(wrap), 0.5f);
    }

    [Test]
    public void SolveWorkingJoints_RegularNgon_ReturnsConfigs()
    {
        var pose = new CustomRadialSidePose { flyAway = new Bounds(Vector3.zero, Vector3.one * 0.02f) };
        var cfgs = RadialSlotMath.SolveWorkingJoints(
            pose, Vector3.one, RadialJoinKind.Natural, 0f,
            Vector3.zero, Vector3.up, false, Vector3.zero, Vector3.zero);
        Assert.Greater(cfgs.Count, 0);
        bool has4 = false;
        for (int i = 0; i < cfgs.Count; i++)
            if (cfgs[i].count == 4) has4 = true;
        Assert.IsTrue(has4);
    }

    [Test]
    public void SolveWorkingJoints_FlyAwayCollide_ReturnsNone()
    {
        var pose = new CustomRadialSidePose { flyAway = new Bounds(Vector3.zero, Vector3.one * 20f) };
        var cfgs = RadialSlotMath.SolveWorkingJoints(
            pose, Vector3.one, RadialJoinKind.Natural, 0f,
            Vector3.zero, Vector3.up, false, Vector3.zero, Vector3.zero);
        Assert.AreEqual(0, cfgs.Count);
    }

    [Test]
    public void StartPostAnchor_FiltersToMatchingOnly()
    {
        var pose = new CustomRadialSidePose { flyAway = new Bounds(Vector3.zero, Vector3.one * 0.02f) };
        Vector3 center = Vector3.zero;
        Vector3 axis = Vector3.up;
        float radius = RadialSlotMath.NaturalRadius(Vector3.one, 4, 360f, 0f);
        Vector3 slot0 = RadialSlotMath.PolarSlot(center, axis, radius, 0, 4, 0f, 360f);
        var match = RadialSlotMath.SolveWorkingJoints(
            pose, Vector3.one, RadialJoinKind.Natural, 0f,
            center, axis, true, slot0, (center - slot0).normalized);
        Assert.Greater(match.Count, 0);
        for (int i = 0; i < match.Count; i++)
            Assert.IsTrue(match[i].matchesStartPostAnchor);

        var miss = RadialSlotMath.SolveWorkingJoints(
            pose, Vector3.one, RadialJoinKind.Natural, 0f,
            center, axis, true, center, Vector3.forward);
        Assert.AreEqual(0, miss.Count);
    }

    [Test]
    public void EffectiveJoinOffset_NaturalIsZero()
    {
        var spec = new RadialBuildSpec { joinKind = RadialJoinKind.Natural, joinOffset = 0.4f };
        Assert.AreEqual(0f, spec.EffectiveJoinOffset());
        spec.joinKind = RadialJoinKind.Offset;
        Assert.AreEqual(0.4f, spec.EffectiveJoinOffset(), 0.001f);
    }

    [Test]
    public void ApplySolved_WritesCountRadiusWrap()
    {
        var spec = new RadialBuildSpec();
        spec.ApplySolved(new RadialSolvedConfig
        {
            count = 6,
            radius = 3.2f,
            startAngleDeg = 15f,
            wrapAngleDeg = 180f,
            joinKind = RadialJoinKind.Offset
        });
        Assert.AreEqual(6, spec.count);
        Assert.AreEqual(3.2f, spec.radius, 0.001f);
        Assert.AreEqual(15f, spec.startAngleDeg, 0.001f);
        Assert.AreEqual(180f, spec.wrapAngleDeg, 0.001f);
        Assert.AreEqual(RadialJoinKind.Offset, spec.joinKind);
    }
}
