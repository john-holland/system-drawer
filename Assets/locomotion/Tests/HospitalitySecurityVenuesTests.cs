using System;
using NUnit.Framework;
using UnityEngine;

public sealed class HospitalitySecurityVenuesTests
{
    [Test]
    public void KindFromBuildingType_HospitalitySecurity()
    {
        Assert.AreEqual(CivilSystemKind.NightClub, CivilSystemLattice.KindFromBuildingType("nightclub"));
        Assert.AreEqual(CivilSystemKind.Bar, CivilSystemLattice.KindFromBuildingType("tavern"));
        Assert.AreEqual(CivilSystemKind.Hotel, CivilSystemLattice.KindFromBuildingType("hotel"));
        Assert.AreEqual(CivilSystemKind.Inn, CivilSystemLattice.KindFromBuildingType("inn"));
        Assert.AreEqual(CivilSystemKind.MilitaryCheckpoint, CivilSystemLattice.KindFromBuildingType("military_checkpoint"));
        Assert.AreEqual(CivilSystemKind.SpyAgency, CivilSystemLattice.KindFromBuildingType("spy_agency"));
        Assert.AreEqual(CivilSystemKind.Embassy, CivilSystemLattice.KindFromBuildingType("embassy"));
        Assert.AreEqual(CivilSystemKind.GovLegislative, CivilSystemLattice.KindFromBuildingType("legislative"));
        Assert.AreEqual(CivilSystemKind.Monarchic, CivilSystemLattice.KindFromBuildingType("palace"));
        Assert.AreEqual(CivilSystemKind.Spa, CivilSystemLattice.KindFromBuildingType("spa"));
        Assert.AreEqual(CivilSystemKind.PrivateIndustry, CivilSystemLattice.KindFromBuildingType("office_building"));
        Assert.AreEqual(CivilSystemKind.BarberShop, CivilSystemLattice.KindFromBuildingType("barbershop"));
    }

    [Test]
    public void ParkingLot_SeedsTravelAgentPreviewGoal()
    {
        var lotGo = new GameObject("lot");
        var lot = lotGo.AddComponent<ParkingLot>();
        lot.arrivalAnchor = lotGo.transform;
        lotGo.transform.position = new Vector3(10f, 0f, 5f);

        var agentGo = new GameObject("agent");
        agentGo.transform.position = new Vector3(12f, 0f, 5f);
        var ta = agentGo.AddComponent<TravelAgent>();

        lot.SeedTravelAgents(40f);
        Assert.AreEqual(lot.ArrivalWorld, ta.previewGoalWorld);

        UnityEngine.Object.DestroyImmediate(agentGo);
        UnityEngine.Object.DestroyImmediate(lotGo);
    }

    [Test]
    public void KeycardRegistry_BindAndUnlock()
    {
        var regGo = new GameObject("reg");
        var reg = regGo.AddComponent<KeycardAccessRegistry>();
        var doorGo = new GameObject("door");
        var lockComp = doorGo.AddComponent<KeycardLock>();
        lockComp.nodeStableId = "room-1";
        lockComp.defaultLocked = true;
        lockComp.locked = true;

        Assert.IsFalse(lockComp.TryUnlock("kc-1", reg));
        reg.Bind("kc-1", "room-1", new[] { "guest-a" });
        Assert.IsTrue(lockComp.TryUnlock("kc-1", reg));
        Assert.IsFalse(lockComp.locked);

        UnityEngine.Object.DestroyImmediate(doorGo);
        UnityEngine.Object.DestroyImmediate(regGo);
    }

    [Test]
    public void MusicSchedule_AndBeatBinder()
    {
        var go = new GameObject("club");
        var schedule = go.AddComponent<MusicAmbianceSchedule>();
        schedule.slots.Add(new MusicAmbianceSlot
        {
            slotId = "prime",
            hoursCron = "* * * * *",
            ambiance = MusicAmbianceTag.Club,
            ambianceScoreBias01 = 0.8f
        });
        var binder = go.AddComponent<BeatQuantizedActionBinder>();
        schedule.Tick(DateTime.UtcNow, 0.6f);
        Assert.IsNotNull(schedule.Current);
        Assert.AreEqual(MusicAmbianceTag.Club, schedule.AmbianceNow);
        binder.ApplyBpmFromSchedule(schedule);
        Assert.Greater(binder.bpm, 100f);
        Assert.GreaterOrEqual(binder.QuantizeDelaySec(), 0f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void NightClubAndBar_Cards()
    {
        var go = new GameObject("night");
        var club = go.AddComponent<NightClubVenueRuntime>();
        club.SetOpen(true);
        Assert.IsTrue(club.isOpen);
        Assert.IsNotNull(club.FloorDuty());
        Assert.IsNotNull(club.DoorDuty());

        var barGo = new GameObject("bar");
        var bar = barGo.AddComponent<BarVenueRuntime>();
        Assert.AreEqual("serve", bar.ServeDuty().duty);

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(barGo);
    }

    [Test]
    public void InnHotel_KeycardLinensMaintenance()
    {
        var go = new GameObject("inn");
        var inn = go.AddComponent<InnHotelVenueRuntime>();
        inn.isHotel = false;
        Assert.Greater(inn.SleepInPaintComfort01(), 0.5f);
        inn.IssueKeycard("kc-room", "node-room", "guest-1");
        Assert.IsTrue(inn.keycards.Allows("kc-room", "node-room"));
        Assert.IsNotNull(inn.CheckInStub("room-2"));
        Assert.IsNotNull(inn.MaidStub("room-2", true));
        Assert.IsTrue(inn.maintenance.hasSuper);

        var hotelGo = new GameObject("hotel");
        var hotel = hotelGo.AddComponent<InnHotelVenueRuntime>();
        hotel.isHotel = true;
        hotel.SyncCorporateMode();
        Assert.IsTrue(hotel.bio.corporateMode);

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(hotelGo);
    }

    [Test]
    public void SecurityGov_JusticeDefaults()
    {
        var spyGo = new GameObject("spy");
        var spy = spyGo.AddComponent<SpyAgencyVenueRuntime>();
        var staff = spy.StaffJustice();
        Assert.Less(staff.violenceThreshold01, 0.7f);

        var embGo = new GameObject("emb");
        var emb = embGo.AddComponent<EmbassyVenueRuntime>();
        // Steeper army Justice => lower violence threshold than civilian staff.
        Assert.Less(emb.ArmyJustice().violenceThreshold01, emb.CivilianStaffJustice().violenceThreshold01);

        var cpGo = new GameObject("cp");
        var cp = cpGo.AddComponent<CheckpointVenueRuntime>();
        Assert.IsTrue(cp.OpsDuty().opsCenterDuty);

        UnityEngine.Object.DestroyImmediate(spyGo);
        UnityEngine.Object.DestroyImmediate(embGo);
        UnityEngine.Object.DestroyImmediate(cpGo);
    }

    [Test]
    public void SpaBarber_Compose()
    {
        var spaGo = new GameObject("spa");
        var spa = spaGo.AddComponent<SpaVenueRuntime>();
        var treatment = spa.Treatment("massage");
        Assert.IsNotNull(treatment.wrestlingContact);

        var barberGo = new GameObject("barber");
        var barber = barberGo.AddComponent<BarberShopVenueRuntime>();
        var cut = barber.CutDuty("fade");
        Assert.IsNotNull(cut.spaCompose);
        Assert.IsNotNull(cut.wrestlingCompose);
        Assert.Greater(cut.HairdoBlendProgress01(), 0f);

        UnityEngine.Object.DestroyImmediate(spaGo);
        UnityEngine.Object.DestroyImmediate(barberGo);
    }

    [Test]
    public void BuildingRequirementSpec_HospitalitySlots()
    {
        var night = BuildingRequirementSpec.DefaultSlotsFor("nightclub");
        Assert.IsTrue(night.Exists(s => s.slotId == "dance_floor"));
        var hotel = BuildingRequirementSpec.DefaultSlotsFor("hotel");
        Assert.IsTrue(hotel.Exists(s => s.slotId == "front_desk"));
        var barber = BuildingRequirementSpec.DefaultSlotsFor("barbershop");
        Assert.IsTrue(barber.Exists(s => s.slotId == "chair"));
    }

    [Test]
    public void CompanyRegistration_Dto()
    {
        var go = new GameObject("co");
        var co = go.AddComponent<CompanyRegistration>();
        co.companyId = "acme";
        co.displayName = "Acme";
        var dto = co.ToDto();
        Assert.AreEqual("acme", dto["companyId"]);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
