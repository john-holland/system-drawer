#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AirplaneDesignerTests
{
    [Test]
    public void WingSurface_TipCache_ValidatesAfterRecompute()
    {
        var root = new GameObject("wing_root");
        try
        {
            var wing = new AirplaneWingSurfaceParams
            {
                spanLength = 20f,
                centerlineLocalPos = Vector3.zero,
                tipTwistDeg = 2f
            };
            wing.RecomputeTipEndCache(root.transform);
            Assert.IsTrue(wing.ValidateTipCache(root.transform, 0.01f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Checklist_Generate_FillsDefaultSections()
    {
        var card = TSAChecklistCard.Generate(null);
        Assert.Greater(card.items.Count, 5);
        Assert.Contains("engines", card.dutyChecklist);
        Assert.Contains("landing_gear", card.dutyChecklist);
        Assert.Contains("atc", card.dutyChecklist);
    }

    [Test]
    public void TakeoffLanding_GearOverrideFields_PopulateFromPlane()
    {
        var root = new GameObject("plane");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            plane.landingGearOpenCloseTopologyId = "custom_gear";
            var takeoff = TSATakeoffCard.Generate(new DispatchRequest { kind = AirportDispatchKinds.TsaTakeoff }, plane);
            var landing = TSALandingCard.Generate(new DispatchRequest { kind = AirportDispatchKinds.TsaLanding }, plane);
            Assert.AreEqual("custom_gear", takeoff.gearRaiseTopologyId);
            Assert.AreEqual("custom_gear", landing.landingGearOverrideTopologyId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PixelLightGridMount_EnsureRig_CreatesRig()
    {
        var root = new GameObject("mount_host");
        try
        {
            var mount = root.AddComponent<PixelLightGridMountGameObject>();
            var rig = mount.EnsureRig();
            Assert.IsNotNull(rig);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PixelLightGridMount_PickClosest_PrefersNearerMount()
    {
        var a = new GameObject("near");
        var b = new GameObject("far");
        try
        {
            a.transform.position = new Vector3(0f, 0f, 2f);
            b.transform.position = new Vector3(0f, 0f, 8f);
            var ma = a.AddComponent<PixelLightGridMountGameObject>();
            var mb = b.AddComponent<PixelLightGridMountGameObject>();
            var ray = new Ray(Vector3.zero, Vector3.forward);
            var pick = PixelLightGridMountGameObject.PickClosest(new[] { mb, ma }, ray, 4, 50f);
            Assert.AreSame(ma, pick);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void SelectDestinationAtc_DefaultVsNearestForDisaster()
    {
        var home = new GameObject("atc_home");
        var near = new GameObject("atc_near");
        var far = new GameObject("atc_far");
        try
        {
            home.transform.position = Vector3.zero;
            near.transform.position = new Vector3(10f, 0f, 0f);
            far.transform.position = new Vector3(100f, 0f, 0f);
            var atcHome = home.AddComponent<AirTrafficControlBioRhythm>();
            var atcNear = near.AddComponent<AirTrafficControlBioRhythm>();
            var atcFar = far.AddComponent<AirTrafficControlBioRhythm>();
            atcHome.serviceId = "atc_home";
            atcNear.serviceId = "atc_near";
            atcFar.serviceId = "atc_far";
            atcHome.defaultDestinationAtcServiceId = "atc_far";

            var routine = AirTrafficControlBioRhythm.SelectDestinationAtc(
                atcHome, new DispatchRequest { kind = AirportDispatchKinds.AtcLanding, worldTarget = Vector3.zero }, false);
            Assert.AreEqual("atc_far", routine.serviceId);

            var disaster = AirTrafficControlBioRhythm.SelectDestinationAtc(
                atcHome,
                new DispatchRequest
                {
                    kind = AirportDispatchKinds.TsaDisaster,
                    worldTarget = Vector3.zero,
                    notes = "potty"
                },
                preferNearest: true);
            Assert.AreEqual("atc_home", disaster.serviceId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(home);
            UnityEngine.Object.DestroyImmediate(near);
            UnityEngine.Object.DestroyImmediate(far);
        }
    }

    [Test]
    public void DialogueCatalog_MapsClearanceKind()
    {
        var catalog = new AtcDispatcherDialogueCatalog();
        catalog.EnsureDefaults();
        Assert.AreEqual("atc-dispatcher-takeoff", catalog.DialogueSetFor(AirportDispatchKinds.AtcTakeOff));
        Assert.AreEqual("atc-dispatcher-divert-potty", catalog.DialogueSetFor(AirportDispatchKinds.TsaDisaster));
    }

    [Test]
    public void AircraftRouteMerger_InsertsQueueAndRefuel_Idempotent()
    {
        var root = new GameObject("route");
        var planeGo = new GameObject("plane");
        try
        {
            var legGo = new GameObject("leg");
            legGo.transform.SetParent(root.transform, false);
            var leg = legGo.AddComponent<TravelLegSequenceNode>();
            leg.children = new List<BehaviorTreeNode>();
            var plane = planeGo.AddComponent<AirplaneVehicleRagdoll>();
            plane.insertLandingQueue = true;
            plane.insertRefuelBeforePark = true;
            plane.fuel01 = 0.1f;
            var seg = new MultiModalSegment { mode = TravelLegMode.Land };

            AircraftTravelRouteMerger.MergeIntoLeg(leg, plane, seg);
            AircraftTravelRouteMerger.MergeIntoLeg(leg, plane, seg);

            int queues = 0, refuels = 0;
            for (int i = 0; i < leg.children.Count; i++)
            {
                if (leg.children[i] is AircraftLandingQueueNode) queues++;
                if (leg.children[i] is AircraftRefuelNode) refuels++;
            }
            Assert.AreEqual(1, queues);
            Assert.AreEqual(1, refuels);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(planeGo);
        }
    }

    [Test]
    public void PowerBus_SumsDraw_AndDrainsBattery()
    {
        var packs = new List<AirplaneBatteryPack>
        {
            new AirplaneBatteryPack { capacityKwh = 10f, chargeKwh = 10f, maxDrawKw = 50f }
        };
        var systems = new List<AirplanePowerSystemDraw>();
        AirplanePowerBus.FillDefaultPowerSystems(systems);
        var bus = new AirplanePowerBus();
        float before = packs[0].chargeKwh;
        bus.Tick(3600f, packs, systems, chargeKw: 0f);
        Assert.Greater(bus.totalDrawKw, 0f);
        Assert.Less(packs[0].chargeKwh, before);
        Assert.Less(bus.charge01, 1f);
    }

    [Test]
    public void EnsureDefaultPowerSystems_FillsEmptyList()
    {
        var root = new GameObject("plane_pwr");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            plane.powerSystems.Clear();
            plane.batteries.Clear();
            plane.EnsureDefaultPowerSystems();
            Assert.Greater(plane.powerSystems.Count, 5);
            Assert.AreEqual(1, plane.batteries.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BioRhythm_Shed_DisablesOutletsBeforeWebtops_ThenRestores()
    {
        var root = new GameObject("plane_bio");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            plane.EnsureSystems();
            plane.batteries[0].capacityKwh = 1f;
            plane.batteries[0].chargeKwh = 0.05f;
            plane.batteries[0].maxDrawKw = 5f;
            plane.chargeKwWhenEnginesOn = 0f;
            var bio = plane.airplaneBio;
            bio.enginesRunning = false;
            bio.shedChargeThreshold01 = 0.5f;
            bio.restoreChargeThreshold01 = 0.9f;

            bool outletShed = false;
            for (int step = 0; step < 20; step++)
            {
                bio.Tick(DateTime.UtcNow, 1f);
                var outlets = plane.powerSystems.Find(s => s.systemId == "seat_power_outlets");
                var webtops = plane.powerSystems.Find(s => s.systemId == "seatback_webtops");
                if (outlets != null && !outlets.enabled)
                {
                    outletShed = true;
                    Assert.IsTrue(webtops == null || webtops.enabled || outlets.shedPriority < webtops.shedPriority);
                    break;
                }
            }
            Assert.IsTrue(outletShed);

            plane.batteries[0].chargeKwh = 1f;
            plane.batteries[0].maxDrawKw = 200f;
            bio.enginesRunning = true;
            plane.chargeKwWhenEnginesOn = 100f;
            for (int step = 0; step < 30; step++)
                bio.Tick(DateTime.UtcNow, 1f);
            var outletsAfter = plane.powerSystems.Find(s => s.systemId == "seat_power_outlets");
            Assert.IsTrue(outletsAfter == null || outletsAfter.enabled);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SeatCounts_ScaleDrawRows()
    {
        var root = new GameObject("plane_seats");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            plane.EnsureDefaultPowerSystems();
            plane.seatPowerOutletCount = 100;
            plane.seatOutletDrawKwEach = 0.1f;
            plane.seatbackWebtopCount = 50;
            plane.seatbackWebtopDrawKwEach = 0.02f;
            plane.seatbackWebtopsEnabled = true;
            var bus = new AirplanePowerBus();
            bus.ScaleComfortDrawRows(plane.powerSystems, plane);
            Assert.AreEqual(10f, plane.powerSystems.Find(s => s.systemId == "seat_power_outlets").drawKwWhenOn, 0.001f);
            Assert.AreEqual(1f, plane.powerSystems.Find(s => s.systemId == "seatback_webtops").drawKwWhenOn, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SetMusicSource_ChorusToPa_UpdatesCabinSource()
    {
        var root = new GameObject("plane_music");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            plane.EnsureSystems();
            plane.cabinMusicSystem.SetMusicSource(AirplaneCabinMusicSource.Chorus);
            Assert.AreEqual(AirplaneCabinMusicSource.Chorus, plane.cabinMusicSystem.source);
            plane.cabinMusicSystem.SetMusicSource(AirplaneCabinMusicSource.PaProgram);
            Assert.AreEqual(AirplaneCabinMusicSource.PaProgram, plane.cabinMusicSystem.source);
            Assert.AreEqual(AirplaneCabinMusicSource.PaProgram, plane.defaultMusicSource);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LemmaKeys_IncludeAirplaneDesignerKeys()
    {
        Assert.IsTrue(AirportLemmaPropertyKeys.IsAirportLemma(AirportLemmaPropertyKeys.BatteryPower));
        Assert.IsTrue(AirportLemmaPropertyKeys.IsAirportLemma(AirportLemmaPropertyKeys.DisasterDivertNearestAtc));
        Assert.IsTrue(AirportLemmaPropertyKeys.IsAirportLemma(AirportLemmaPropertyKeys.LandingQueue));
        Assert.IsTrue(AirportLemmaPropertyKeys.IsAirportLemma(AirportLemmaPropertyKeys.Refuel));
    }

    [Test]
    public void ConfigAsset_ApplyTo_RoundTripsIdentity()
    {
        var root = new GameObject("plane_cfg");
        try
        {
            var plane = root.AddComponent<AirplaneVehicleRagdoll>();
            var asset = ScriptableObject.CreateInstance<AirplaneConfigurationAsset>();
            asset.EnsureDefaults();
            asset.planeName = "ConcordeX";
            asset.callsign = "CUU-TEST";
            asset.ApplyTo(plane);
            Assert.AreEqual("ConcordeX", plane.planeName);
            Assert.AreEqual("CUU-TEST", plane.callsign);
            Assert.IsNotNull(plane.airplaneBio);
            UnityEngine.Object.DestroyImmediate(asset);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
#endif
