using System;
using NUnit.Framework;
using UnityEngine;

public sealed class WoodMillTests
{
    [Test]
    public void Conveyer_ClampsSizeAndWeight()
    {
        var go = new GameObject("conv");
        try
        {
            var agent = go.AddComponent<StationConveyerTravelAgent>();
            var step = agent.SelectedStep;
            step.size01 = 0.99f;
            step.weight01 = 0.99f;
            step.limitSize01 = 0.5f;
            step.limitWeight01 = 0.4f;
            Assert.IsTrue(agent.OverLimit());
            Assert.IsTrue(agent.ClampSizeWeight());
            Assert.AreEqual(0.5f, step.size01, 1e-4f);
            Assert.AreEqual(0.4f, step.weight01, 1e-4f);
            Assert.IsFalse(agent.OverLimit());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LoadZone_ToTruckUnload_AndDeliveryMechanisms()
    {
        var req = new DispatchRequest { kind = "load", worldTarget = Vector3.one };
        var load = FactoryLoadZoneCard.GenerateLoad(req);
        var unload = load.ToTruckUnload(req);
        Assert.IsInstanceOf<FactoryTruckUnloadCard>(unload);
        Assert.AreEqual(Vector3.one, unload.goalWorld);
        var river = TAVehicleDeliveryCard.Generate(req, "river");
        Assert.AreEqual("river", river.mechanism);
        var canal = TAVehicleDeliveryCard.Generate(req, "canal");
        Assert.AreEqual("canal", canal.mechanism);
        var combo = TAVehicleDeliveryCard.Generate(req, "river_canal_dam");
        Assert.AreEqual("river_canal_dam", combo.mechanism);
    }

    [Test]
    public void MillBio_LoadAndDeliveryCards()
    {
        var go = new GameObject("mill");
        try
        {
            var bio = go.AddComponent<WoodMillBioRhythm>();
            var loadCards = bio.FacilitateCards(new DispatchRequest { kind = "load" });
            Assert.IsTrue(loadCards.Exists(c => c is FactoryLoadZoneCard));
            Assert.IsTrue(loadCards.Exists(c => c is FactoryTruckUnloadCard));
            var ship = bio.FacilitateCards(new DispatchRequest { kind = "canal" });
            Assert.IsTrue(ship.Exists(c => c is TAVehicleDeliveryCard));
            var plank = bio.FacilitateCards(new DispatchRequest { kind = "plank_cut" });
            Assert.IsTrue(plank.Exists(c => c is WoodMillPlankCutCard));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void KerfCuts_OddCenter_LatheLead()
    {
        Assert.AreEqual(5, MillKerfCuts.OddCutCount(4));
        Assert.AreEqual(5, MillKerfCuts.OddCutCount(5));
        Assert.AreEqual(2, MillKerfCuts.CenterLineIndex(5));
        Assert.AreEqual(6, MillKerfCuts.PlankCount(5));
        MillKerfCuts.DualCrescentOffsets(1f, out var l, out var r);
        Assert.Less(l.x, 0f);
        Assert.Greater(r.x, 0f);
        var lathe = ScriptableObject.CreateInstance<LatheSpec>();
        lathe.bedwaysLengthM = 2f;
        lathe.carriageM = 1f;
        Assert.AreEqual(0.5f, lathe.CarriageClamp01(), 1e-4f);
        lathe.leadScrewPitchMm = 3f;
        Assert.AreEqual(0.006f, lathe.LeadAdvanceM(2f), 1e-5f);
        UnityEngine.Object.DestroyImmediate(lathe);
    }

    [Test]
    public void WritingCard_OcrFallsBackToDialog()
    {
        var card = WritingCard.GenerateGrade("B");
        string grade = card.TryOcrThenDialog(null);
        Assert.AreEqual("B", grade);
        Assert.IsTrue(card.usedDialogFallback);
        Assert.IsNotNull(card.scribe);
    }

    [Test]
    public void WoodPile_QuadtreeDepth_SawdustSubtract()
    {
        var pile = ScriptableObject.CreateInstance<WoodPileSpec>();
        pile.AddQuadtreeBucket(2, Vector3.up);
        pile.AddQuadtreeBucket(0, Vector3.zero);
        Assert.AreEqual(0, pile.slots[0].depthIndex);
        Assert.AreEqual(1, pile.SlotCountAtDepth(2));
        UnityEngine.Object.DestroyImmediate(pile);

        var go = new GameObject("dust");
        try
        {
            var vol = go.AddComponent<DiggableVolume>();
            vol.sdf = ScriptableObject.CreateInstance<SdfMax.SdfMaxCompositionAsset>();
            vol.sdf.nodes.Add(new SdfMax.SdfMaxNode
            {
                op = SdfMax.SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfMax.SdfPrimitiveType.Sphere,
                radius = 1f
            });
            vol.sdf.rootNodeIndex = 0;
            var hose = ScriptableObject.CreateInstance<SawdustHoseSpec>();
            hose.hoseEnd = go.transform.position;
            hose.vacuumThroughput01 = 0.8f;
            int n = hose.Prebake(vol, new DigScoopSph());
            Assert.AreEqual(1, n);
            UnityEngine.Object.DestroyImmediate(hose);
            UnityEngine.Object.DestroyImmediate(vol.sdf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SuperDeformo_HighMu()
    {
        var go = new GameObject("deformo");
        try
        {
            var capGo = new GameObject("log");
            capGo.transform.SetParent(go.transform);
            var cap = capGo.AddComponent<CapsuleCollider>();
            var d = go.AddComponent<PhysicsSuperDeformo>();
            d.logCapsules.Add(cap);
            d.frictionMu = 1.6f;
            d.ApplyHighMu();
            Assert.IsNotNull(cap.sharedMaterial);
            Assert.Greater(cap.sharedMaterial.dynamicFriction, 1f);
            UnityEngine.Object.DestroyImmediate(capGo);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ShiftSchedule_CronActive_BananaStand()
    {
        var go = new GameObject("shifts");
        try
        {
            var s = go.AddComponent<StationShiftSchedule>();
            s.shiftCount = 3;
            s.hoursPerShift = 8f;
            Assert.AreEqual("* * * * *", s.ResolvedHoursCron("* 8-20 * * *"));
            Assert.IsTrue(s.IsShiftActive(new DateTime(2026, 9, 9, 10, 0, 0, DateTimeKind.Utc), 1));
            s.bananaStand18h = true;
            Assert.AreEqual("* 6-23 * * *", s.ResolvedHoursCron(null));
            Assert.IsTrue(s.IsShiftActive(new DateTime(2026, 9, 9, 7, 0, 0, DateTimeKind.Utc), 0));
            Assert.IsFalse(s.IsShiftActive(new DateTime(2026, 9, 9, 3, 0, 0, DateTimeKind.Utc), 0));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void WoodTravelAgent_DefaultDiamond()
    {
        var go = new GameObject("wood_ta");
        try
        {
            var agent = go.AddComponent<WoodTravelAgent>();
            Assert.AreEqual(11, WoodTravelAgent.DefaultPipeline().Count);
            Assert.AreEqual(4, agent.BlueOptimal01().Length);
            Assert.AreEqual("Size", WoodTravelAgent.DiamondAxes[0]);
            Assert.IsFalse(agent.OverLimit());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void MillBio_ShiftCronActive()
    {
        var go = new GameObject("mill_shift");
        try
        {
            var bio = go.AddComponent<WoodMillBioRhythm>();
            var shifts = go.AddComponent<StationShiftSchedule>();
            shifts.bananaStand18h = true;
            bio.shifts = shifts;
            var openAt = new DateTime(2026, 9, 9, 7, 0, 0, DateTimeKind.Utc);
            bio.Tick(openAt, 0.016f);
            Assert.AreEqual("* 6-23 * * *", bio.hoursCron);
            Assert.Greater(bio.unitsAvailable01, 0.1f);
            var closedAt = new DateTime(2026, 9, 9, 3, 0, 0, DateTimeKind.Utc);
            bio.Tick(closedAt, 0.016f);
            Assert.Less(bio.unitsAvailable01, 0.1f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Pipeline_InsertsPlankCutAndSection_FromLatheKerfs()
    {
        var go = new GameObject("wood_ta");
        var lathe = ScriptableObject.CreateInstance<LatheSpec>();
        try
        {
            var agent = go.AddComponent<WoodTravelAgent>();
            agent.steps = new System.Collections.Generic.List<WoodMillStep>
            {
                new WoodMillStep { kind = WoodMillStepKind.Receive },
                new WoodMillStep { kind = WoodMillStepKind.Lathe },
                new WoodMillStep { kind = WoodMillStepKind.Convey },
                new WoodMillStep { kind = WoodMillStepKind.Ship }
            };
            agent.EnsurePipeline();
            Assert.AreEqual(11, agent.steps.Count);
            Assert.AreEqual(WoodMillStepKind.Receive, agent.steps[0].kind);
            Assert.AreEqual(WoodMillStepKind.Debark, agent.steps[1].kind);
            Assert.AreEqual(WoodMillStepKind.Lathe, agent.steps[2].kind);
            Assert.AreEqual(WoodMillStepKind.PlankCut, agent.steps[3].kind);
            Assert.AreEqual(WoodMillStepKind.Section, agent.steps[4].kind);
            Assert.AreEqual(WoodMillStepKind.Convey, agent.steps[5].kind);
            Assert.AreEqual(WoodMillStepKind.Grade, agent.steps[6].kind);
            Assert.AreEqual(WoodMillStepKind.Convey, agent.steps[7].kind);
            Assert.AreEqual(WoodMillStepKind.Pile, agent.steps[8].kind);
            Assert.AreEqual(WoodMillStepKind.Bind, agent.steps[9].kind);
            Assert.AreEqual(WoodMillStepKind.Ship, agent.steps[10].kind);
            lathe.millCutCount = 4;
            agent.lathe = lathe;
            agent.ApplyLatheKerfsToPlankSteps();
            Assert.AreEqual(5, agent.steps[3].kerfCount);
            Assert.AreEqual(6, agent.steps[3].plankCount);
            Assert.AreEqual(6, agent.steps[4].sectionCount);
            var full = WoodTravelAgent.DefaultPipeline();
            Assert.AreEqual(11, full.Count);
            Assert.AreEqual("convey_grade", full[5].sgInstanceId);
            Assert.AreEqual("convey_pile", full[7].sgInstanceId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lathe);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
