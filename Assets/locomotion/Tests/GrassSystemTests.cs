using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class GrassSystemTests
{
    [Test]
    public void HorizonOrder_IsPedological()
    {
        SoilHorizonId[] order = SoilHorizonOrder.Pedological;
        Assert.AreEqual(6, order.Length);
        Assert.AreEqual(SoilHorizonId.O, order[0]);
        Assert.AreEqual(SoilHorizonId.A, order[1]);
        Assert.AreEqual(SoilHorizonId.E, order[2]);
        Assert.AreEqual(SoilHorizonId.B, order[3]);
        Assert.AreEqual(SoilHorizonId.C, order[4]);
        Assert.AreEqual(SoilHorizonId.R, order[5]);
    }

    [Test]
    public void GmoResilience_IgnoredUnlessEnabled()
    {
        var slot = new StressAdapter { curvature = 1f, resilience01 = 1f };
        Assert.Greater(slot.Destructibility(0.5f, false), 0.01f);
        Assert.AreEqual(0f, slot.Destructibility(0.5f, true), 1e-4f);
    }

    [Test]
    public void RhizomeNode_PrecedesEmergence()
    {
        Assert.IsTrue(GrassDevelopment.RhizomeNodePrecedesEmergence(GrassDevelopment.DefaultPipeline()));
        var reversed = new List<GrassDevStep>
        {
            new GrassDevStep { kind = GrassDevStepKind.RhizomeEmergence },
            new GrassDevStep { kind = GrassDevStepKind.RhizomeNode }
        };
        Assert.IsFalse(GrassDevelopment.RhizomeNodePrecedesEmergence(reversed));
    }

    [Test]
    public void CutSnapshot_RestoresBlade()
    {
        var set = ScriptableObject.CreateInstance<GroundCompositionSet>();
        try
        {
            set.RecordCut("blade-a", 35f, 1.5f, 0.2f, false, 0.1f, 10f);
            set.CaptureSnapshot(10f);
            set.blades.Clear();
            Assert.IsNull(set.FindBlade("blade-a"));
            Assert.IsTrue(set.RestoreSnapshot(10f));
            GrassBladeRecord blade = set.FindBlade("blade-a");
            Assert.IsNotNull(blade);
            Assert.IsTrue(blade.cut);
            Assert.AreEqual(35f, blade.cutAngleDeg, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(set);
        }
    }

    [Test]
    public void FastForward_AppliesPendingGrassCutsInOrder()
    {
        var set = ScriptableObject.CreateInstance<GroundCompositionSet>();
        var go = new GameObject("grass-fast-forward");
        try
        {
            var provider = go.AddComponent<UnityNarrativeTimeProvider>();
            provider.narrativeSecondsPerUnitySecond = 1d;
            provider.startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);
            var clock = go.AddComponent<NarrativeClock>();
            clock.timeProvider = provider;
            var executor = go.AddComponent<NarrativeExecutor>();
            executor.clock = clock;
            var scheduler = go.AddComponent<NarrativeScheduler>();
            scheduler.applyPastEventsOnEnable = false;
            scheduler.autoFindReferences = false;
            scheduler.clock = clock;
            scheduler.executor = executor;
            var calendar = go.AddComponent<NarrativeCalendarAsset>();
            scheduler.calendar = calendar;

            var earlyAt = new NarrativeDateTime(2025, 1, 1, 1, 0, 0);
            var lateAt = new NarrativeDateTime(2025, 1, 1, 3, 0, 0);
            var early = new NarrativeCalendarEvent { title = "cut early", startDateTime = earlyAt };
            early.actions.Add(new GrassCutAction
            {
                composition = set,
                bladeId = "early",
                angleDeg = 20f,
                radius = 1.2f,
                shedSize = 0.1f,
                at = earlyAt
            });
            var late = new NarrativeCalendarEvent { title = "cut late", startDateTime = lateAt };
            late.actions.Add(new GrassCutAction
            {
                composition = set,
                bladeId = "late",
                angleDeg = 40f,
                radius = 1.2f,
                shedSize = 0.1f,
                at = lateAt
            });
            calendar.events.Add(late);
            calendar.events.Add(early);

            int applied = scheduler.FastForwardTo(new NarrativeDateTime(2025, 1, 1, 2, 0, 0));
            Assert.AreEqual(1, applied);
            Assert.IsNotNull(set.FindBlade("early"));
            Assert.IsTrue(set.FindBlade("early").cut);
            Assert.IsNull(set.FindBlade("late"));
            Assert.IsTrue(executor.GetRuntimeState().triggeredEventIds.Contains(early.id));
            Assert.IsFalse(executor.GetRuntimeState().triggeredEventIds.Contains(late.id));
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(set);
        }
    }

    [Test]
    public void WindChord_PacksGustAndFlutter()
    {
        Color chord = PlantRadialWindCache.EncodeChord(0.8f, 0.2f, new Vector2(1f, 0f));
        Assert.AreEqual(0.8f, chord.r, 1e-4f);
        Assert.AreEqual(0.2f, chord.g, 1e-4f);
        Vector2 warp = GrassWindField.HeightWarp(new Vector2(1f, 0f), 1f, 0f);
        Assert.AreEqual(0f, warp.x, 1e-5f);
    }

    [Test]
    public void ShorelineGate_SeversWetAndDry()
    {
        float gate = ScreenSpaceConvolutionKernel.DepthGate(1f, 1f, 8f, true, false);
        Assert.AreEqual(0f, gate, 1e-6f);
        float same = ScreenSpaceConvolutionKernel.DepthGate(1f, 1f, 8f, true, true);
        Assert.Greater(same, 0.9f);
    }

    [Test]
    public void GrassDiamond_HasFourAxes()
    {
        var go = new GameObject("grass-agent");
        try
        {
            var agent = go.AddComponent<GrassTravelAgent>();
            Assert.AreEqual(4, agent.BlueOptimal01().Length);
            Assert.AreEqual(4, agent.RedLimit01().Length);
            Assert.AreEqual(4, agent.DashedGreenCurrent01().Length);
            Assert.AreEqual("Water", GrassTravelAgent.DiamondAxes[0]);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
