#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;

public class AirportATCTests
{
    [Test]
    public void ATC_FacilitateCards_TakeOff_YieldsPilotAndAtcCards()
    {
        var root = new GameObject("atc_root");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var atc = root.AddComponent<AirTrafficControlBioRhythm>();
            var cards = atc.FacilitateCards(new DispatchRequest
            {
                kind = AirportDispatchKinds.AtcTakeOff
            });
            Assert.IsTrue(cards.Exists(c => c is ATCTakeOffCard));
            Assert.IsTrue(cards.Exists(c => c is PilotTakeOffCard));
            Assert.IsTrue(cards.Exists(c => c is DispatchConfirmCard));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AirportBootstrap_CreatesHubPeersAndAuth()
    {
        var root = new GameObject("airport");
        try
        {
            var stub = root.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.Airport;
            root.AddComponent<AirportBootstrap>().Ensure();

            Assert.IsNotNull(root.GetComponent<AirportRuntime>());
            Assert.IsNotNull(root.GetComponent<AirPortBioRhythm>());
            Assert.IsNotNull(root.GetComponent<AirportBuildingRagdoll>());
            Assert.IsNotNull(root.GetComponent<AirTrafficControlBioRhythm>());
            Assert.IsNotNull(root.GetComponent<TransportationAuthorityBioRhythm>());
            Assert.IsNotNull(root.GetComponent<AuthWarden>());
            Assert.IsNotNull(root.GetComponent<PersonaShiftManager>());
            Assert.IsNotNull(CentralDispatchHub.Instance ?? root.GetComponent<CentralDispatchHub>());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void AuthWarden_GrantDeny_FiresEvents()
    {
        var root = new GameObject("auth");
        try
        {
            var warden = root.AddComponent<AuthWarden>();
            warden.zones.Add(new AuthZone
            {
                locationId = "gate_a",
                requiredTier = AuthAccessTier.Secure,
                publicAccess = false
            });

            bool granted = false;
            bool denied = false;
            warden.OnAuthGranted += (_, __) => granted = true;
            warden.OnAuthDenied += (_, __) => denied = true;

            Assert.IsFalse(warden.TryAuthorize("gate_a", "passenger", AuthAccessTier.Public));
            Assert.IsTrue(denied);
            Assert.IsFalse(granted);

            Assert.IsTrue(warden.TryAuthorize("gate_a", "tsa_agent", AuthAccessTier.Restricted));
            Assert.IsTrue(granted);
            Assert.IsTrue(warden.HasGrant("gate_a", "tsa_agent"));

            bool revoked = false;
            warden.OnAuthRevoked += (_, __) => revoked = true;
            warden.Revoke("gate_a", "tsa_agent");
            Assert.IsTrue(revoked);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TSAPatrol_Thresholds_ScaleWithTerrorLevel()
    {
        var low = TSAPatrolCard.Generate(new DispatchRequest { kind = AirportDispatchKinds.TsaPatrol }, 0.1f);
        var high = TSAPatrolCard.Generate(new DispatchRequest { kind = AirportDispatchKinds.TsaPatrol }, 0.9f);
        Assert.Less(low.violenceThreshold01, high.violenceThreshold01);
        Assert.GreaterOrEqual(high.violenceThreshold01, 0.7f);
    }

    [Test]
    public void AirportRunwaySgPack_LemmaParse_LargeAndSmall()
    {
        var large = AirportRunwaySgPackSettings.FromLemmaFragment("scale=large,strips=14,diagonal=45,diag_strips=8");
        Assert.AreEqual(AirportRunwaySgPackSettings.AirportScale.LargeHub, large.scale);
        Assert.AreEqual(14, large.parallelStripCount);
        Assert.AreEqual(45f, large.diagonalAngleDeg, 0.01f);

        var small = AirportRunwaySgPackSettings.FromLemmaFragment("scale=small_single");
        Assert.AreEqual(AirportRunwaySgPackSettings.AirportScale.SmallSingle, small.scale);
        Assert.AreEqual(1, small.parallelStripCount);
        Assert.IsTrue(AirportLemmaPropertyKeys.IsAirportLemma(AirportLemmaPropertyKeys.BoardingParty(3)));
    }

    [Test]
    public void PersonaShiftManager_CronOpen_MarksOnShift()
    {
        var root = new GameObject("shift_host");
        try
        {
            var mgr = root.AddComponent<PersonaShiftManager>();
            mgr.shifts.Clear();
            mgr.shifts.Add(new PersonaShiftSlot
            {
                role = "tsa_agent",
                personaKey = "tsa_agent",
                openCron = "* 5-23 * * *",
                closeCron = ""
            });
            var venue = new CivilVenueNode { contextOwner = root, kind = CivilSystemKind.Airport };
            var openAt = new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc);
            mgr.Tick(openAt, venue);
            Assert.IsTrue(mgr.shifts[0].isOnShift);

            var closedAt = new DateTime(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc);
            mgr.Tick(closedAt, venue);
            Assert.IsFalse(mgr.shifts[0].isOnShift);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RoadDeformationRepairWindow_DateWindow()
    {
        var root = new GameObject("road");
        try
        {
            var win = root.AddComponent<RoadDeformationRepairWindow>();
            win.startDateIso = "1996-11-11";
            win.endDateIso = "1997-08-08";
            win.crewCron = "";
            win.keepRepairDecalMemory = true;

            Assert.IsTrue(win.IsInDamageWindow(new DateTime(1997, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            win.Tick(new DateTime(1997, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            Assert.IsTrue(win.damageActive);

            win.Tick(new DateTime(1998, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            Assert.IsFalse(win.damageActive);
            Assert.IsTrue(win.repaired);
            Assert.IsNotNull(root.GetComponent<RoadRepairDecal>());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BuildingRequirementSpec_Airport_HasRunwaySlots()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("airport_terminal");
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "runway"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "security"));
        Assert.IsTrue(slots.Exists(s => s != null && s.slotId == "gate"));
    }
}
#endif
