#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SeatedIkTowingTests
{
    [Test]
    public void SitSurface_ProjectCog_InsidePolygon_LowTipRisk()
    {
        var go = new GameObject("Seat");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(go.transform, go.transform.position, Vector3.up, 0.3f, 0.3f);
            Vector3 cog = go.transform.position + Vector3.up * 0.5f;
            bool inside = contact.TryProjectCog(cog, out Vector3 projected, out float tip);
            Assert.IsTrue(inside);
            Assert.Less(tip, 0.6f);
            Assert.AreEqual(0f, Mathf.Abs(projected.y - go.transform.position.y), 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void TowChain_Sit_BuildsPelvisLink()
    {
        var seat = new GameObject("ChairSeat");
        var pelvis = new GameObject("Pelvis");
        try
        {
            pelvis.transform.position = seat.transform.position + Vector3.up * 0.4f;
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, seat.transform.position, Vector3.up);
            var chain = IkTowChain.BuildSit(contact, pelvis.transform);
            Assert.IsTrue(chain.active);
            Assert.AreEqual(SurfaceOccupancyMode.Sit, chain.mode);
            Assert.GreaterOrEqual(chain.links.Count, 1);
            Assert.AreEqual("seat_to_pelvis", chain.links[0].name);
            chain.Tick(0.02f);
            Assert.Less(chain.MaxLinkError(), 1f);
        }
        finally
        {
            Object.DestroyImmediate(seat);
            Object.DestroyImmediate(pelvis);
        }
    }

    [Test]
    public void TowChain_StandOn_BuildsFootLinks()
    {
        var seat = new GameObject("ChairSeat");
        var lf = new GameObject("LeftFoot");
        var rf = new GameObject("RightFoot");
        var pelvis = new GameObject("Pelvis");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, seat.transform.position + Vector3.up, Vector3.up);
            var chain = IkTowChain.BuildStandOn(contact, lf.transform, rf.transform, pelvis.transform);
            Assert.AreEqual(SurfaceOccupancyMode.StandOn, chain.mode);
            Assert.GreaterOrEqual(chain.links.Count, 2);
            Assert.AreEqual("seat_to_left_foot", chain.links[0].name);
        }
        finally
        {
            Object.DestroyImmediate(seat);
            Object.DestroyImmediate(lf);
            Object.DestroyImmediate(rf);
            Object.DestroyImmediate(pelvis);
        }
    }

    [Test]
    public void CogStabilizer_OutsidePolygon_BuildsRestoreImpulses()
    {
        var seat = new GameObject("Seat");
        var actor = new GameObject("Actor");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, Vector3.zero, Vector3.up, 0.2f, 0.2f);
            actor.transform.position = new Vector3(2f, 0.5f, 0f);
            var cog = new SeatedCogStabilizer { surface = contact, mode = SurfaceOccupancyMode.Sit, tipRiskThreshold = 0.2f };
            cog.Evaluate(actor);
            Assert.Greater(cog.LastTipRisk01, 0.5f);
            var impulses = cog.BuildRestoreImpulses(feetReachGround: false);
            Assert.Greater(impulses.Count, 0);
            bool hasAbs = impulses.Exists(i => i.muscleGroup == "abdomen");
            bool hasShoulder = impulses.Exists(i => i.muscleGroup != null && i.muscleGroup.Contains("shoulder"));
            Assert.IsTrue(hasAbs);
            Assert.IsTrue(hasShoulder);
        }
        finally
        {
            Object.DestroyImmediate(seat);
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void SitBalance_StackHasArmsLegsAbs()
    {
        var stack = SitBalanceCard.BuildBalanceStack(0.7f);
        Assert.IsTrue(stack.Exists(i => i.muscleGroup == "abdomen"));
        Assert.IsTrue(stack.Exists(i => i.muscleGroup == "left_thigh"));
        Assert.IsTrue(stack.Exists(i => i.muscleGroup == "left_shoulder"));
    }

    [Test]
    public void GoalTypes_SitAndStandOn_Exist()
    {
        Assert.AreEqual(GoalType.Sit, GoalType.Sit);
        Assert.AreEqual(GoalType.StandOnSurface, GoalType.StandOnSurface);
    }
}

public sealed class StandOnSurfaceTests
{
    [Test]
    public void StandOnCard_FlagsAndPlantStack()
    {
        var seat = new GameObject("Seat");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, Vector3.up, Vector3.up);
            var card = StandOnSurfaceCard.Generate(contact, null);
            Assert.IsTrue(card.isStandOnSurfaceGoal);
            Assert.IsFalse(card.isSitGoal);
            Assert.AreEqual(SurfaceOccupancyMode.StandOn, card.occupancyMode);
            Assert.IsTrue(card.impulseStack.Exists(i => i.muscleGroup == "left_ankle"));
            Assert.IsTrue(card.impulseStack.Exists(i => i.muscleGroup == "right_ankle"));
        }
        finally
        {
            Object.DestroyImmediate(seat);
        }
    }

    [Test]
    public void StandOn_TipRecovery_PrefersAnklesFirst()
    {
        var seat = new GameObject("Seat");
        var actor = new GameObject("Actor");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, Vector3.zero, Vector3.up, 0.15f, 0.15f);
            actor.transform.position = new Vector3(1.5f, 1f, 0f);
            var cog = new SeatedCogStabilizer { surface = contact, mode = SurfaceOccupancyMode.StandOn, tipRiskThreshold = 0.2f };
            cog.Evaluate(actor);
            var impulses = cog.BuildRestoreImpulses(true);
            Assert.IsTrue(impulses.Count >= 2);
            Assert.AreEqual("left_ankle", impulses[0].muscleGroup);
            Assert.AreEqual("right_ankle", impulses[1].muscleGroup);
        }
        finally
        {
            Object.DestroyImmediate(seat);
            Object.DestroyImmediate(actor);
        }
    }
}

public sealed class ChairRotateSchoochTests
{
    [Test]
    public void RotateSequence_HasThighAbsFootCasterDodge()
    {
        var seq = ChairRotateCard.BuildRotateSequence(0.1f, SurfaceOccupancyMode.Sit);
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "left_thigh"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "abdomen"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "lumbar"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "left_ankle" || i.muscleGroup == "left_foot"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "right_thigh"));
    }

    [Test]
    public void SchoochSequence_IncludesLiftAndToolHold()
    {
        var seq = ChairSchoochCard.BuildSchoochSequence(0.1f, 0.85f, SurfaceOccupancyMode.Sit);
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "left_shoulder"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "abdomen"));
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "left_hip"));
    }

    [Test]
    public void SchoochStandOn_UsesAnkleLift()
    {
        var seq = ChairSchoochCard.BuildSchoochSequence(0.1f, 0.85f, SurfaceOccupancyMode.StandOn);
        Assert.IsTrue(seq.Exists(i => i.muscleGroup == "left_ankle" && i.forceDirection == Vector3.up));
    }

    [Test]
    public void Rotate_FootClearsCasters()
    {
        var seat = new GameObject("Seat");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, Vector3.zero, Vector3.up);
            var card = ChairRotateCard.Generate(contact, 30f, null);
            Assert.IsFalse(card.FootClearsCasters(seat.transform.TransformPoint(Vector3.zero)));
            Assert.IsTrue(card.FootClearsCasters(seat.transform.TransformPoint(new Vector3(2f, 0f, 2f))));
        }
        finally
        {
            Object.DestroyImmediate(seat);
        }
    }

    [Test]
    public void StressManifold_PrefersCenteredCog()
    {
        var seat = new GameObject("Seat");
        var good = new GameObject("GoodActor");
        var bad = new GameObject("BadActor");
        try
        {
            var contact = SitSurfaceContact.FromWorldPlane(seat.transform, Vector3.zero, Vector3.up, 0.3f, 0.3f);
            good.transform.position = new Vector3(0f, 0.5f, 0f);
            bad.transform.position = new Vector3(3f, 0.5f, 0f);
            var est = new SeatedStressManifoldEstimator { surface = contact };
            float fGood = est.TrainingFitness(good);
            float fBad = est.TrainingFitness(bad);
            Assert.Greater(fGood, fBad);
        }
        finally
        {
            Object.DestroyImmediate(seat);
            Object.DestroyImmediate(good);
            Object.DestroyImmediate(bad);
        }
    }
}

public sealed class SeatedWalkReminderTests
{
    [Test]
    public void Timer_MinEqualsMax_FiresPredictably_WhenIdle()
    {
        var policy = new SeatedWalkReminderPolicy
        {
            enabled = true,
            timerMinSeconds = 1f,
            timerMaxSeconds = 1f,
            idleDebounceSeconds = 0.2f,
            requireHypertensiveLoad = false
        };
        Assert.IsFalse(policy.Tick(0.1f, playerInputActive: false, isSeated: true, sheet: null));
        Assert.IsFalse(policy.Tick(0.15f, false, true, null)); // still in debounce
        // After debounce, need 1s timer
        float t = 0f;
        bool fired = false;
        while (t < 2f && !fired)
        {
            fired = policy.Tick(0.1f, false, true, null);
            t += 0.1f;
        }
        Assert.IsTrue(fired);
        Assert.AreEqual(1f, policy.ChosenDuration, 0.001f);
    }

    [Test]
    public void Input_ResetsDebounceAndTimer()
    {
        var policy = new SeatedWalkReminderPolicy
        {
            timerMinSeconds = 5f,
            timerMaxSeconds = 5f,
            idleDebounceSeconds = 0.5f
        };
        policy.Tick(0.6f, false, true, null);
        policy.Tick(1f, false, true, null);
        Assert.Greater(policy.TimerElapsed, 0.5f);
        policy.NotifyPlayerInput();
        Assert.AreEqual(0f, policy.TimerElapsed, 0.001f);
        Assert.AreEqual(0f, policy.IdleAccum, 0.001f);
    }

    [Test]
    public void DescriptionFilter_DefaultsAndOverride()
    {
        var filter = new SpatialDescriptionFilter();
        Assert.IsTrue(filter.Matches("bathroom"));
        Assert.IsTrue(filter.Matches("outside"));
        Assert.IsFalse(filter.Matches("kitchen"));

        filter = new SpatialDescriptionFilter(new[] { "kitchen", "hallway" });
        Assert.IsTrue(filter.Matches("kitchen"));
        Assert.IsFalse(filter.Matches("bathroom"));

        var pts = new List<SpatialTaggedPoint>
        {
            new SpatialTaggedPoint { descriptionKey = "desk", worldPosition = Vector3.one },
            new SpatialTaggedPoint { descriptionKey = "hallway", worldPosition = Vector3.right }
        };
        Assert.IsTrue(filter.TryPickWaypoint(pts, out Vector3 pos, out string key));
        Assert.AreEqual("hallway", key);
        Assert.AreEqual(Vector3.right, pos);
    }

    [Test]
    public void Periphery_OccupyOpensGate_VacateCloses()
    {
        var stationGo = new GameObject("Station");
        var chair = new GameObject("Chair");
        var actor = new GameObject("Actor");
        try
        {
            chair.transform.SetParent(stationGo.transform);
            var station = stationGo.AddComponent<ComputerPeripheryStation>();
            station.chairHost = chair.transform;
            station.EnsureSeatContact();
            Assert.IsFalse(station.toolUseGate.AllowsToolUse());
            station.Occupy(actor, SurfaceOccupancyMode.Sit);
            Assert.IsTrue(station.toolUseGate.AllowsToolUse());
            var runtime = actor.GetComponent<SeatedOccupancyRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsTrue(runtime.occupied);
            station.Vacate(actor);
            Assert.IsFalse(station.toolUseGate.AllowsToolUse());
            Assert.IsFalse(runtime.occupied);
        }
        finally
        {
            Object.DestroyImmediate(stationGo);
            Object.DestroyImmediate(actor);
        }
    }
}
#endif
