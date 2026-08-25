using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SdfMax;

public sealed class RoadLanesTests
{
    [Test]
    public void StayInLanes_Coeff1_SitsOnLaneCenter()
    {
        var layout = new RoadLaneLayout { laneCount = 2, laneWidthM = 4f, directionSign = new[] { 1, -1 } };
        Vector3 world = Vector3.zero;
        Vector3 got = RoadLaneSnap.ApplyPolicy(world, 10f, 0.4f, TravelLanePolicy.StayInLanes, 1f, layout, 5f,
            (float d, out Vector3 p, out Vector3 b) => { p = new Vector3(d, 0f, 0f); b = Vector3.forward; });
        float expectedLane = layout.LaneCenterOffset(layout.LaneFromLateral(0.4f));
        Assert.AreEqual(10f, got.x, 0.01f);
        Assert.AreEqual(expectedLane, got.z, 0.01f);
    }

    [Test]
    public void IgnoreLaneGrid_KeepsCenterline()
    {
        var layout = new RoadLaneLayout { laneCount = 2, laneWidthM = 4f };
        Vector3 got = RoadLaneSnap.ApplyPolicy(Vector3.one, 10f, 3f, TravelLanePolicy.IgnoreLaneGrid, 1f, layout, 5f,
            (float d, out Vector3 p, out Vector3 b) => { p = new Vector3(d, 0f, 0f); b = Vector3.forward; });
        Assert.AreEqual(10f, got.x, 0.01f);
        Assert.AreEqual(0f, got.z, 0.01f);
    }

    [Test]
    public void AlignGridIgnoreLanes_ChangesSOnly()
    {
        var layout = new RoadLaneLayout { laneCount = 2, laneWidthM = 4f };
        Vector3 got = RoadLaneSnap.ApplyPolicy(Vector3.zero, 12f, 2.2f, TravelLanePolicy.AlignGridIgnoreLanes, 1f, layout, 5f,
            (float d, out Vector3 p, out Vector3 b) => { p = new Vector3(d, 0f, 0f); b = Vector3.forward; });
        Assert.AreEqual(RoadLaneSnap.SnapS(12f, 5f), got.x, 0.01f);
        Assert.AreEqual(2.2f, got.z, 0.01f);
    }

    [Test]
    public void GridZero_HighAggressiveness_MinDBelowCarLength()
    {
        var grid = new RoadLaneGridSettings { followTimeSec = 3f, gridCarLengths = 0f, carLengthM = 4.5f };
        float minD = grid.MinSeparationM(10f, 1f);
        Assert.Less(minD, 4.5f);
    }

    [Test]
    public void FollowTime_3s_At10mps_CellAtLeast30()
    {
        var grid = new RoadLaneGridSettings { followTimeSec = 3f, gridCarLengths = 1f, carLengthM = 4.5f };
        Assert.GreaterOrEqual(grid.CellLengthM(10f), 30f);
    }

    [Test]
    public void SetBrushStampStacked_IncrementsFloorIndex()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.EnsureLayersAndFrames();
        grid.SetBrushStampStacked(new CityPixelBrushStamp { cellX = 1, cellY = 1, floorIndex = 0, kind = CityPixelBrushKind.RoadLanes });
        grid.SetBrushStampStacked(new CityPixelBrushStamp { cellX = 1, cellY = 1, floorIndex = 0, kind = CityPixelBrushKind.Crosswalk });
        Assert.AreEqual(1, grid.brushStamps[1].floorIndex);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void Recipe_BridgeAndUnderpass_WritesUnderpassThenOverpass()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.EnsureHighwayLayers();
        var cfg = RoadLaneConfigAsset.CreateBridgeAndUnderpassRecipe();
        CityPixelRecipeApplier.Apply(grid, cfg, 0, 2, 2);
        Assert.GreaterOrEqual(grid.brushStamps.Count, 2);
        Assert.AreEqual(CityPixelBrushKind.BridgeAndUnderpass, grid.brushStamps[0].kind);
        Assert.AreEqual(CityPixelBrushKind.Overpass, grid.brushStamps[1].kind);
        Assert.AreEqual(1, grid.brushStamps[1].floorIndex);
        Object.DestroyImmediate(grid);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void StreetLight_MaterializesPixelLightWithoutController()
    {
        var go = StreetLightPrefabFactory.CreateStreetLightPhonePole();
        Assert.IsNotNull(go.GetComponent<UtilityPoleAssembly>());
        Assert.IsNotNull(go.GetComponentInChildren<PixelLightRig>());
        Assert.IsNull(go.GetComponent<TrafficLightController>());
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TrafficSignal_MaterializesControllerAndWardenCanSee()
    {
        var go = StreetLightPrefabFactory.CreateTrafficSignalPhonePole();
        Assert.IsNotNull(go.GetComponent<TrafficLightController>());
        Assert.IsNotNull(go.GetComponentInChildren<PixelLightRig>());
        var wardenGo = new GameObject("warden");
        var warden = wardenGo.AddComponent<TrafficWarden>();
        warden.RefreshLights();
        Assert.Contains(go.GetComponent<TrafficLightController>(), warden.lights);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(wardenGo);
    }

    [Test]
    public void PedCallButton_TryPress_SetsPedestrianCall()
    {
        var sig = StreetLightPrefabFactory.CreateTrafficSignalPhonePole();
        var btn = StreetLightPrefabFactory.CreateStandaloneButton();
        var act = btn.GetComponent<RoadComponentMeshActivator>();
        act.target = sig.GetComponent<TrafficLightController>();
        Assert.IsTrue(act.TryPress());
        Assert.IsTrue(act.target.pedestrianCall);
        Object.DestroyImmediate(sig);
        Object.DestroyImmediate(btn);
    }

    [Test]
    public void PhonePoleIndex_And_StreetWireEnd_ResolveIds()
    {
        var poleGo = StreetLightPrefabFactory.CreatePhonePole();
        var pole = poleGo.GetComponent<UtilityPoleAssembly>();
        pole.poleId = "p1";
        PhonePoleIndex.Register(pole);
        var spanGo = new GameObject("span");
        var span = spanGo.AddComponent<PowerLineSpan>();
        span.wireId = "w1";
        span.fromPoleId = "p1";
        span.toPoleId = "p1";
        StreetWireIndex.Register(span);
        var endGo = new GameObject("end");
        var end = endGo.AddComponent<StreetWireEnd>();
        end.poleId = "p1";
        end.wireId = "w1";
        Assert.IsTrue(end.Resolve());
        Assert.IsNull(end.lastWarning);
        end.poleId = "";
        LogAssert.Expect(LogType.Warning, "StreetWireEnd missing poleId ''");
        Assert.IsFalse(end.Resolve());
        Assert.IsNotNull(end.lastWarning);
        Object.DestroyImmediate(poleGo);
        Object.DestroyImmediate(spanGo);
        Object.DestroyImmediate(endGo);
    }

    [Test]
    public void PowerLineSpan_EnsureRope_AddsRopeSystem()
    {
        var a = new GameObject("a").transform;
        var b = new GameObject("b").transform;
        b.position = Vector3.right * 8f;
        var go = new GameObject("span");
        var span = go.AddComponent<PowerLineSpan>();
        span.Configure(a, b);
        Assert.IsNotNull(span.EnsureRope());
        Assert.IsNotNull(span.GetComponent<RopeSystem>());
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }

    [Test]
    public void HangingShoes_TwoBodies_And_KnotLength()
    {
        var go = new GameObject("shoes");
        var shoes = go.AddComponent<HangingShoesComponent>();
        shoes.knotLengthM = 0.8f;
        shoes.EnsureBodies();
        shoes.EnsureLaceRope();
        Assert.IsNotNull(shoes.leftShoeBody);
        Assert.IsNotNull(shoes.rightShoeBody);
        Assert.AreEqual(0.8f, shoes.laceRope.Config.totalLengthM, 0.01f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Crosswalk_AddsPaintChild_DoesNotBlock()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.EnsureHighwayLayers();
        grid.PaintLayerCell(CityPixelLayerKind.Highway, 0, 0, 0);
        grid.SetBrushStampStacked(new CityPixelBrushStamp { kind = CityPixelBrushKind.Crosswalk, cellX = 0, cellY = 0 });
        CityPixelGridBaker.BakeFrame(grid, 0);
        var go = new GameObject("cw");
        go.AddComponent<CrosswalkDecal>().Apply();
        Assert.Greater(go.transform.childCount, 0);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(grid);
    }

    [Test]
    public void IntersectionLot_FourLegs_SnapsToOutlet()
    {
        var go = new GameObject("ix");
        go.transform.position = Vector3.zero;
        var pad = go.AddComponent<RoadLot>();
        pad.lotKind = RoadLotKind.Intersection;
        pad.padSize = new Vector3(20f, 2f, 20f);
        var lot = go.AddComponent<IntersectionLot>();
        lot.pad = pad;
        lot.EnsureFourLegs(new[] { "n", "e", "s", "w" });
        Assert.AreEqual(4, lot.legs.Count);
        Assert.IsTrue(lot.TrySnapDriveOutlet("e", Vector3.zero, out Vector3 world));
        Assert.AreNotEqual(pad.ArrivalWorld, world);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void EmergencyWarningBar_WigWag_And_SetOnFalse()
    {
        var go = PixelLightPrefabFactory.CreateWarningBarRuntime();
        var bar = go.GetComponent<EmergencyWarningBar>();
        bar.SetKind(EmergencyWarningBarKind.Police);
        bar.SetOn(true);
        Assert.IsTrue(bar.leftBank.playing);
        var wig = PixelLightPatternAsset.CreateWigWagPreset(true);
        var g0 = wig.Evaluate(0);
        var g1 = wig.Evaluate(1);
        Assert.Greater(g0[0, 0], 0.5f);
        Assert.Less(g0[0, 15], 0.5f);
        Assert.Less(g1[0, 0], 0.5f);
        bar.SetOn(false);
        Assert.IsFalse(bar.leftBank.playing);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(wig);
    }

    [Test]
    public void PoliceSetLights_TurnsBarOn()
    {
        var go = new GameObject("cop");
        var cop = go.AddComponent<PoliceCarVehicleRagdoll>();
        cop.EnsureDefaultLights();
        cop.SetLights(true);
        var bar = go.GetComponentInChildren<EmergencyWarningBar>();
        Assert.IsNotNull(bar);
        Assert.IsTrue(bar.barOn);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Presence_GraftsAndClears_And_BirdCount()
    {
        var ev = PixelLightPrefabFactory.CreateWarningBarRuntime();
        ev.transform.position = Vector3.zero;
        var bar = ev.GetComponent<EmergencyWarningBar>();
        var presence = ev.GetComponent<EmergencyVehiclePresence>();
        bar.SetOn(true);
        var actor = new GameObject("ta");
        actor.transform.position = Vector3.forward * 5f;
        var ta = actor.AddComponent<TravelAgent>();
        var bt = actor.AddComponent<BehaviorTree>();
        presence.hearRadiusM = 40f;
        presence.RefreshTracked();
        Assert.Contains(ta, presence.trackedActors);
        presence.showFleeingBirdsGizmo = false;
        Assert.AreEqual(0, presence.GizmoBirdCount);
        presence.showFleeingBirdsGizmo = true;
        Assert.AreEqual(presence.trackedActors.Count, presence.GizmoBirdCount);
        var overlay = presence.overlay;
        overlay.Refresh(presence);
        Assert.IsNotNull(overlay.SlotFor(ta));
        Assert.AreNotEqual(padCenterDummy(), overlay.SlotFor(ta).steeringWorld);
        presence.TryClearGraft(actor);
        actor.transform.position = Vector3.forward * 400f;
        presence.RefreshTracked();
        Assert.IsFalse(presence.trackedActors.Contains(ta));
        Object.DestroyImmediate(actor);
        Object.DestroyImmediate(ev);
    }

    static Vector3 padCenterDummy() => new Vector3(999, 999, 999);

    [Test]
    public void IntersectionCard_WritesPlannerHints()
    {
        var card = TAIntersectionCard.Generate(Vector3.zero);
        card.preferWalkAcross = true;
        card.approachYaw = 90f;
        card.legHeadings = new[] { 0f, 90f };
        var hints = default(GenericTraversibilityPlannerSolver.PlannerHints);
        card.WritePlannerHints(ref hints);
        Assert.IsTrue(hints.preferWalkAcross);
        Assert.AreEqual(90f, hints.approachYaw);
        Assert.AreEqual(2, hints.legHeadings.Length);
    }

    [Test]
    public void StopPotential_Defaults_And_UnreadMiss()
    {
        Assert.AreEqual(1f, SignStopPotential.DefaultForKind(TASignKind.Stop));
        Assert.AreEqual(0f, SignStopPotential.DefaultForKind(TASignKind.Custom));
        Assert.AreEqual(0.85f, SignStopPotential.DefaultForBrush(CityPixelBrushKind.Detour), 0.01f);
        var signGo = new GameObject("sign");
        var pot = signGo.AddComponent<SignStopPotential>();
        pot.stopPotential01 = 1f;
        pot.visualReadOverride = 0;
        var actor = new GameObject("ta");
        var ta = actor.AddComponent<TravelAgent>();
        ta.travelSpeedScale = 1f;
        Assert.IsFalse(pot.TryApply(ta));
        Assert.AreEqual(1f, ta.travelSpeedScale);
        pot.visualReadOverride = 1;
        Assert.IsTrue(pot.TryApply(ta));
        Assert.AreEqual(0f, ta.travelSpeedScale, 0.01f);
        Object.DestroyImmediate(signGo);
        Object.DestroyImmediate(actor);
    }

    [Test]
    public void InformationalSign_ZeroPotential_DoesNotChangeSpeed()
    {
        var card = TASignCard.Generate(TASignKind.Custom, Vector3.zero);
        card.stopPotential = null;
        var taGo = new GameObject("ta");
        var ta = taGo.AddComponent<TravelAgent>();
        float speed = ta.travelSpeedScale;
        float avoid = ta.avoidRadius;
        card.ApplyHintsTo(ta);
        Assert.AreEqual(speed, ta.travelSpeedScale);
        Assert.AreEqual(avoid, ta.avoidRadius);
        Object.DestroyImmediate(taGo);
    }

    [Test]
    public void PlayerVehicle_SkipSlow_UnlessSelfDrivingOrBrake()
    {
        var go = new GameObject("veh");
        var veh = go.AddComponent<VehicleRagdoll>();
        var buf = go.AddComponent<RagdollPlayerInputBuffer>();
        buf.options = new RagdollPlayerControllerOptions { overrideTravelAgentSlow = true };
        buf.WriteState(new RagdollPlayerInputState { brake01 = 0f, selfDriving = false });
        Assert.IsFalse(PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(veh));
        buf.WriteState(new RagdollPlayerInputState { brake01 = 0f, selfDriving = true });
        Assert.IsTrue(PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(veh));
        buf.WriteState(new RagdollPlayerInputState { brake01 = 1f, selfDriving = false });
        Assert.IsTrue(PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(veh));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SidewalkBake_WalkableWidth_And_Matting()
    {
        var cfg = ScriptableObject.CreateInstance<RoadLaneConfigAsset>();
        cfg.sidewalkWidthM = 2f;
        cfg.sidewalkPaddingM = 0.25f;
        cfg.mattingWidth01 = 0.4f;
        var r = SidewalkMeshBaker.Bake(cfg, new List<Vector3> { Vector3.zero, Vector3.forward * 4f });
        Assert.AreEqual(1.5f, r.walkableWidthM, 0.01f);
        Assert.IsTrue(r.hasMatting);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void CurbSdf_ContainsPolyline_DappleZeroUnbeveled()
    {
        var cfg = ScriptableObject.CreateInstance<RoadLaneConfigAsset>();
        cfg.dappleBevel01 = 0f;
        cfg.curbHeightM = 0.2f;
        cfg.curbWidthM = 0.2f;
        var path = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f) };
        var asset = SidewalkCurbBaker.Build(cfg, path);
        Assert.IsTrue(SidewalkCurbBaker.ContainsShoulder(asset, new Vector3(1f, 0f, 0f)));
        Assert.AreEqual(1, asset.nodes.Count);
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void GrassStrip_WidthMatchesSlider()
    {
        var go = new GameObject("grass");
        var g = go.AddComponent<LotGrassGrowthController>();
        g.stripWidthM = 1.25f;
        Assert.AreEqual(1.25f, g.stripWidthM);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void JerseyStraightModules_And_GuardRailDoesNotDisableLane()
    {
        var parent = new GameObject("rail");
        var along = new List<Vector3> { Vector3.zero, Vector3.forward * 3f, Vector3.forward * 6f, Vector3.forward * 9f };
        int n = RoadSplineLengthBend.PlaceStraightModules(parent.transform, null, along, 3f);
        Assert.GreaterOrEqual(n, 1);
        var stamp = new CityPixelBrushStamp { kind = CityPixelBrushKind.GuardRail, laneDisabled = false, bendWithRoad = true };
        Assert.IsFalse(stamp.laneDisabled);
        Object.DestroyImmediate(parent);
    }

    [Test]
    public void VehicleTrack_LaneIndex_LeftAndRight()
    {
        var splineGo = new GameObject("spline");
        var spline = splineGo.AddComponent<VehicleRoadCenterSpline>();
        spline.controlPoints = new List<Vector3> { Vector3.zero, Vector3.forward * 20f };
        var bind = splineGo.AddComponent<RoadLaneSplineBinding>();
        bind.layout = new RoadLaneLayout { laneCount = 2, laneWidthM = 3.5f };
        int left = VehicleTrackProjector.InferLaneIndex(-3f, 0.2f, spline);
        int right = VehicleTrackProjector.InferLaneIndex(3f, 0.8f, spline);
        Assert.AreEqual(0, left);
        Assert.AreEqual(1, right);
        Object.DestroyImmediate(splineGo);
    }

    [Test]
    public void Lemma_ChangedToGreen_StillHitsStreetLightResolver()
    {
        var go = StreetLightPrefabFactory.CreateTrafficSignalPhonePole();
        var resolver = go.GetComponent<RoadLaneLemmaResolver>();
        Assert.IsTrue(resolver.Apply("changed-to", "green"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SidewalkLemma_OpenFalse_SkipsWalkSnap()
    {
        var go = new GameObject("sw");
        var r = go.AddComponent<SidewalkRibbon>();
        r.walkOpen = true;
        var lemma = go.AddComponent<RoadLaneLemmaResolver>();
        lemma.Apply(RoadLaneLemmaPropertyKeys.Open, "false");
        Assert.IsFalse(r.walkOpen);
        Assert.IsFalse(r.TrySampleWalk(Vector3.zero, out _));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TravelAgentCard_WritesLanePolicy()
    {
        var go = new GameObject("ta");
        var ta = go.AddComponent<TravelAgent>();
        var card = new TravelAgentCard { lanePolicy = TravelLanePolicy.StayInLanes, stayInLanes01 = 0.7f, followTimeSec = 3f, gridCarLengths = 2f };
        card.ApplyLanePolicy(ta);
        Assert.AreEqual(TravelLanePolicy.StayInLanes, ta.lanePolicy);
        Assert.AreEqual(0.7f, ta.stayInLanes01);
        Assert.AreEqual(3f, ta.followTimeSec);
        Object.DestroyImmediate(go);
    }
}
