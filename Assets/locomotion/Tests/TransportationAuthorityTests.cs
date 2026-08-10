#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TransportationAuthorityTests
{
    [Test]
    public void TransportationAuthority_FacilitateCards_Reroute_YieldsRouteCard()
    {
        var root = new GameObject("ta_root");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var ta = root.AddComponent<TransportationAuthorityBioRhythm>();
            ta.vehicleRoutes.Add(new TAVehicleRoute
            {
                routeId = "r1",
                vehicleId = "bus-1",
                serviceCron = "* * * * *"
            });

            var cards = ta.FacilitateCards(new DispatchRequest
            {
                kind = TADispatchKinds.Reroute,
                notes = "route:r1"
            });

            Assert.IsTrue(cards.Exists(c => c is TAVehicleRouteCard));
            Assert.IsTrue(cards.Exists(c => c is DispatchConfirmCard));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MissionControl_RequestRecall_EnqueuesOnTa()
    {
        var root = new GameObject("mc_root");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var ta = root.AddComponent<TransportationAuthorityBioRhythm>();
            var mc = root.AddComponent<MissionControlBioRhythm>();

            Assert.IsTrue(mc.RequestRecall("bus-9", 0.9f));
            Assert.IsTrue(ta.TryDequeue(out DispatchRequest req));
            Assert.AreEqual(TADispatchKinds.Recall, req.kind);
            Assert.IsTrue(req.notes.Contains("bus-9"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CommuterFindSeat_HasScanAndFindLemmas()
    {
        var root = new GameObject("bus_root");
        try
        {
            var bus = root.AddComponent<BusVehicleRagdoll>();
            var seat = new GameObject("seat_0");
            seat.transform.SetParent(root.transform);
            bus.seatAnchors.Add(seat.transform);

            var card = CommuterFindSeatCard.Generate(root, bus, "stop-a");
            Assert.IsTrue(card.lemmaTags.Contains(CommuterLemmaPropertyKeys.Scans));
            Assert.IsTrue(card.lemmaTags.Contains(CommuterLemmaPropertyKeys.Find));
            Assert.IsNotNull(card.seatAnchor);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BusStationSgPackSettings_FromLemma_AndApply()
    {
        var settings = BusStationSgPackSettings.FromLemmaFragment("pack=2d,placement=immediate,pad=0.5");
        Assert.AreEqual(BusStationSgPackSettings.PackDimension.TwoDimensional, settings.dimension);
        Assert.AreEqual(BusStationSgPackSettings.PackPlacement.Immediate, settings.placement);
        Assert.AreEqual(0.5f, settings.paddingMeters, 0.001f);

        var host = new GameObject("sg_host");
        try
        {
            var marker = host.AddComponent<BoxCollider>();
            settings.ApplyTo(marker); // no-op without SpatialGenerator fields
            Assert.IsNotNull(settings.slotPriority);
            Assert.IsTrue(settings.slotPriority.Contains("platform"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void TAVehicleRoute_CronDue_AndBayRepairMap()
    {
        var route = new TAVehicleRoute
        {
            routeId = "r",
            vehicleId = "v",
            serviceCron = "* * * * *",
            enabled = true
        };
        Assert.IsTrue(route.IsServiceDue(DateTime.UtcNow));

        var repair = TAVehicleBayRepairCard.Generate(new DispatchRequest { kind = TADispatchKinds.BayRepair }, null);
        var map = repair.ToSg4DPlacementMap();
        Assert.IsTrue(map.ContainsKey("carburetor"));
        Assert.IsTrue(map.ContainsKey("oil_tank"));
    }

    [Test]
    public void VehicleRepairCenter_SeedsParentTransitAuth()
    {
        var root = new GameObject("repair_root");
        try
        {
            var repair = root.AddComponent<VehicleRepairCenterRuntime>();
            repair.parentTransitAuthCompanyId = "public_transit_auth";
            repair.SeedOwnership();
            Assert.AreEqual("public_transit_auth", repair.company.parentCompanyId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BusStationBootstrap_CreatesAuthorityAndHub()
    {
        var root = new GameObject("depot");
        try
        {
            var stub = root.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.BusDepot;
            root.AddComponent<BusStationBootstrap>().Ensure();

            Assert.IsNotNull(root.GetComponent<BusStationRuntime>());
            Assert.IsNotNull(root.GetComponent<TransportationAuthorityBioRhythm>());
            Assert.IsNotNull(root.GetComponent<MissionControlBioRhythm>());
            Assert.IsNotNull(CentralDispatchHub.Instance ?? root.GetComponent<CentralDispatchHub>());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BuildingRequirementSpec_BusDepot_HasPlatformSlots()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("bus_depot");
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "platform"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "waiting"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "cafeteria"));
    }

    [Test]
    public void KindFromBuildingType_TransitHubAndBusStation()
    {
        Assert.AreEqual(CivilSystemKind.TransitHub,
            CivilSystemLattice.KindFromBuildingType("transit_hub"));
        Assert.AreEqual(CivilSystemKind.BusDepot,
            CivilSystemLattice.KindFromBuildingType("bus_station"));
        Assert.AreEqual(CivilSystemKind.BusDepot,
            CivilSystemLattice.KindFromBuildingType("bus_depot"));
        var hubSlots = BuildingRequirementSpec.DefaultSlotsFor("transit_hub");
        Assert.IsTrue(hubSlots.Exists(s => s != null && s.slotId == "platform"));
    }
}
#endif
