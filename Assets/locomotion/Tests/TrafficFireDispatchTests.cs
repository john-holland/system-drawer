using System;
using NUnit.Framework;
using UnityEngine;

public sealed class TrafficFireDispatchTests
{
    [Test]
    public void KindFromBuildingType_FireStation()
    {
        Assert.AreEqual(CivilSystemKind.FireStation, CivilSystemLattice.KindFromBuildingType("fire_station"));
        Assert.AreEqual(CivilSystemKind.FireStation, CivilSystemLattice.KindFromBuildingType("firehouse"));
    }

    [Test]
    public void CentralDispatchHub_CrossRequest()
    {
        var hubGo = new GameObject("hub");
        var hub = hubGo.AddComponent<CentralDispatchHub>();
        var fireGo = new GameObject("fire");
        var fire = fireGo.AddComponent<FirehouseBioRhythm>();
        fire.serviceId = "fire_department";
        hub.Subscribe("fire_department", fire);
        var emsGo = new GameObject("ems");
        var ems = emsGo.AddComponent<DispatchBioRhythm>();
        ems.serviceId = "hospital_ems";
        hub.Subscribe("hospital_ems", ems);

        Assert.IsTrue(hub.RequestCrossDispatch("fire_department", "hospital_ems", new DispatchRequest
        {
            kind = "passenger_pickup",
            priority01 = 0.8f
        }));
        Assert.AreEqual(1, ems.Pending.Count);

        UnityEngine.Object.DestroyImmediate(hubGo);
        UnityEngine.Object.DestroyImmediate(fireGo);
        UnityEngine.Object.DestroyImmediate(emsGo);
    }

    [Test]
    public void PixelLightRig_PushFrame()
    {
        var go = PixelLightPrefabFactory.CreateDefaultRuntime();
        var rig = go.GetComponent<PixelLightRig>();
        Assert.IsNotNull(rig);
        rig.PushFrame();
        Assert.GreaterOrEqual(rig.AverageLuminance01, 0f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void TrafficLight_PhasesDriveHeads()
    {
        var go = new GameObject("tl");
        var ctrl = go.AddComponent<TrafficLightController>();
        var decorator = go.AddComponent<TrafficLightPoleDecorator>();
        decorator.controller = ctrl;
        decorator.createHeadsIfMissing = true;
        decorator.EnsureHeads();
        ctrl.Enter(TrafficSignalPhase.MainGreen);
        Assert.IsTrue(ctrl.MainProceed);
        ctrl.SetPhaseFromLemma("red");
        Assert.AreEqual(TrafficSignalPhase.AllRed, ctrl.Phase);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void FireWarden_ReleasesTruck()
    {
        var stationGo = new GameObject("station");
        var station = stationGo.AddComponent<FireStationBuildingRagdoll>();
        var truckGo = new GameObject("truck");
        truckGo.transform.SetParent(stationGo.transform);
        var truck = truckGo.AddComponent<FireTruckVehicleRagdoll>();
        truck.waterTankLiters = 1000f;
        station.trucks.Add(truck);
        station.fireWarden.station = station;
        station.fireWarden.bio = station.firehouseBio;
        station.firehouseBio.waterReserveLiters = 3000f;
        station.firehouseBio.truckReadiness01 = 1f;

        station.fireWarden.OnThreatFire(truckGo, 0.8f, 0.6f);
        Assert.Greater(station.fireWarden.totalWaterDemandLiters, 0f);
        Assert.GreaterOrEqual(station.fireWarden.trucksReleased, 1);
        Assert.IsFalse(truck.available);

        UnityEngine.Object.DestroyImmediate(stationGo);
    }

    [Test]
    public void FireHydrant_Connects()
    {
        var hydGo = new GameObject("hyd");
        var hyd = hydGo.AddComponent<FireHydrant>();
        var truckGo = new GameObject("truck");
        var truck = truckGo.AddComponent<FireTruckVehicleRagdoll>();
        Assert.IsTrue(hyd.TryConnect(truck));
        Assert.IsTrue(hyd.connected);
        UnityEngine.Object.DestroyImmediate(hydGo);
        UnityEngine.Object.DestroyImmediate(truckGo);
    }

    [Test]
    public void VehicleRagdoll_Dto()
    {
        var go = new GameObject("v");
        var v = go.AddComponent<VehicleRagdoll>();
        v.vehicleId = "engine-1";
        var dto = v.ToDto();
        Assert.AreEqual("engine-1", dto["vehicleId"]);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DispatchCards_Facilitate()
    {
        var go = new GameObject("d");
        var bio = go.AddComponent<DispatchBioRhythm>();
        var cards = bio.FacilitateCards(new DispatchRequest { kind = "load" });
        Assert.Greater(cards.Count, 0);
        Assert.IsTrue(cards.Exists(c => c is DispatchRequestLoadCard));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildingRequirement_FireStationSlots()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("fire_station");
        Assert.IsTrue(slots.Exists(s => s.slotId == "engine_bay"));
        Assert.IsTrue(slots.Exists(s => s.slotId == "sleeping"));
    }

    [Test]
    public void FiremanCallin_AndHomeBinding()
    {
        var go = new GameObject("fh");
        var bio = go.AddComponent<FirehouseBioRhythm>();
        var bind = go.AddComponent<FiremanHomeBinding>();
        bind.firehouse = bio;
        bio.company = go.AddComponent<CompanyRegistration>();
        bio.company.staff.Add(new RetinuePeckingEntry { personaKey = "fireman-a", role = "firefighter" });
        bind.SyncHomePersonaKeys();
        Assert.Contains("fireman-a", bio.homePersonaKeys);
        var callin = bind.CallInOffShift("fireman-a");
        Assert.AreEqual("fireman-a", callin.personaKey);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
