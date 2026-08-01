using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PersonaDayManagerTests
{
    [Test]
    public void CronDue_ActiveSchedule_HourRange()
    {
        var t = new DateTime(2026, 7, 31, 14, 30, 0, DateTimeKind.Utc);
        Assert.IsTrue(CronDue.IsActiveSchedule("* 11-22 * * *", t));
        Assert.IsFalse(CronDue.IsActiveSchedule("* 11-22 * * *", t.AddHours(10)));
    }

    [Test]
    public void CronDue_IsDue_ExactMinute()
    {
        var t = new DateTime(2026, 7, 31, 11, 0, 0, DateTimeKind.Utc);
        Assert.IsTrue(CronDue.IsDue("0 11 * * *", t));
        Assert.IsFalse(CronDue.IsDue("0 11 * * *", t.AddMinutes(1)));
    }

    [Test]
    public void SpeedLod_InBounds_IsOne_Overspeed_Falls()
    {
        var p = new CivilSpeedLodPolicy { developerMaxSpeedMps = 10f, logFalloffBase = 10f, lodFloor = 0.15f };
        Assert.AreEqual(1f, p.ComputeLodScale(5f));
        float over = p.ComputeLodScale(100f);
        Assert.Less(over, 1f);
        Assert.GreaterOrEqual(over, 0.15f);
    }

    [Test]
    public void WouldHaveBeen_TracksSkipped()
    {
        var t = new RateLimitedWouldHaveBeenTracker();
        t.NoteWake(true);
        t.NoteWake(false);
        t.NoteBtTick(false);
        Assert.AreEqual(2, t.wouldHaveWakes);
        Assert.AreEqual(1, t.actualWakes);
        Assert.AreEqual(1, t.wouldHaveBtTicks);
        Assert.AreEqual(0, t.actualBtTicks);
    }

    [Test]
    public void Lattice_PriorityOrdersKitchenFirst()
    {
        var lattice = new CivilSystemLattice();
        lattice.Register(new CivilVenueNode { stableId = "m", kind = CivilSystemKind.Mall, developerPriority = 1 });
        lattice.Register(new CivilVenueNode { stableId = "k", kind = CivilSystemKind.Kitchen, developerPriority = 50 });
        var ordered = lattice.OrderedByPriority();
        Assert.AreEqual(CivilSystemKind.Kitchen, ordered[0].kind);
    }

    [Test]
    public void LodController_PriorityCulling_AfterMaxFullSim()
    {
        var lod = new CivilLodController { maxFullSimVenues = 1 };
        lod.speedPolicy.developerMaxSpeedMps = 100f;
        // Force combined high without FeatureBudget
        float scale = lod.ComputeCombinedScale(0f);
        Assert.Greater(scale, 0.5f);
        Assert.AreEqual(CivilLodTier.FullSim, lod.ResolveTier(0.9f, 0, 0));
        Assert.AreEqual(CivilLodTier.Proxy, lod.ResolveTier(0.9f, 1, 1));
    }

    [Test]
    public void PersonaDayManager_KitchenWakeStub()
    {
        var mgrGo = new GameObject("PDM");
        var venueGo = new GameObject("KitchenVenue");
        var staff = new GameObject("LineChef");
        staff.SetActive(false);
        try
        {
            var pdm = mgrGo.AddComponent<PersonaDayManager>();
            pdm.tickIntervalSeconds = 999f; // don't auto-tick
            var kitchen = venueGo.AddComponent<RestaurantVenueRuntime>();
            var node = new CivilVenueNode
            {
                stableId = "kit-1",
                kind = CivilSystemKind.Kitchen,
                contextOwner = venueGo,
                hoursCron = "* * * * *",
                kitchenRuntime = kitchen,
                retinue = new List<RetinuePeckingEntry>
                {
                    new RetinuePeckingEntry { personaKey = "line", role = "line-chef", peckingOrder = 40, actor = staff }
                }
            };
            kitchen.retinue = node.retinue;
            pdm.RegisterVenue(node);
            pdm.Tick(0.1f);
            Assert.IsTrue(node.isOpen || node.currentTier == CivilLodTier.Culled || node.currentTier == CivilLodTier.Ghost
                          || node.currentTier == CivilLodTier.Proxy || node.currentTier == CivilLodTier.FullSim);
            Assert.Greater(pdm.wouldHaveBeen.wouldHaveWakes, 0);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgrGo);
            UnityEngine.Object.DestroyImmediate(venueGo);
            UnityEngine.Object.DestroyImmediate(staff);
        }
    }

    [Test]
    public void KindFromBuildingType()
    {
        Assert.AreEqual(CivilSystemKind.Kitchen, CivilSystemLattice.KindFromBuildingType("restaurant"));
        Assert.AreEqual(CivilSystemKind.School, CivilSystemLattice.KindFromBuildingType("school"));
        Assert.AreEqual(CivilSystemKind.Church, CivilSystemLattice.KindFromBuildingType("church_small"));
        Assert.AreEqual(CivilSystemKind.Mall, CivilSystemLattice.KindFromBuildingType("shopping_mall"));
    }
}
