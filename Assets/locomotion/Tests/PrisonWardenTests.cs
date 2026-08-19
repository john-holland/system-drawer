using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class PrisonWardenTests
{
    [Test]
    public void ScoreStep_OverLimit_RecommendsRestraint()
    {
        var go = new GameObject("warden");
        var w = go.AddComponent<PrisonWarden>();
        w.limits = ScriptableObject.CreateInstance<PrisonWardenLimits>();
        w.limits.dialog01 = 0.3f;
        var action = w.ScoreStep("dialog", 0.9f, false);
        Assert.AreEqual(PrisonWardenAction.Restraint, action);
        Assert.IsTrue(w.OverUpperLimit("dialog", 0.9f));
        Object.DestroyImmediate(w.limits);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void JusticeAgent_DefaultPipeline_And_Calendar()
    {
        var go = new GameObject("ta");
        var agent = go.AddComponent<JusticeRehabilitationTravelAgent>();
        Assert.GreaterOrEqual(agent.steps.Count, 10);
        var calGo = new GameObject("cal");
        var cal = calGo.AddComponent<NarrativeCalendarAsset>();
        int n = agent.PrebakeCalendar(cal);
        Assert.Greater(n, 0);
        Assert.AreEqual(n, cal.events.Count);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(calGo);
    }

    [Test]
    public void JusticeSeatCard_Roles()
    {
        var busGo = new GameObject("bus");
        var bus = busGo.AddComponent<BusVehicleRagdoll>();
        var s0 = new GameObject("seat0");
        s0.transform.SetParent(busGo.transform);
        var s1 = new GameObject("seat1");
        s1.transform.SetParent(busGo.transform);
        bus.seatAnchors = new List<Transform> { s0.transform, s1.transform };
        var guard = JusticeSeatCard.Generate(null, bus, JusticeSeatRole.Guard);
        var prisoner = JusticeSeatCard.Generate(null, bus, JusticeSeatRole.Prisoner);
        Assert.AreEqual(s0.transform, guard.seatAnchor);
        Assert.AreEqual(s1.transform, prisoner.seatAnchor);
        var transport = TAVehicleJusticeTransportCard.Generate(new DispatchRequest { kind = "justice_bus" }, bus);
        Assert.AreEqual("ta_vehicle_justice_transport", transport.sectionName);
        Object.DestroyImmediate(busGo);
    }

    [Test]
    public void PrisonerSchedule_CustodyHasYard()
    {
        var slots = PrisonerScheduleFactory.SlotsFor(PrisonerStatus.Custody, "p1", null);
        Assert.IsTrue(slots.Exists(s => s.duty == CivilianDutyKind.PrisonYard));
        Assert.IsTrue(slots.Exists(s => s.duty == CivilianDutyKind.PrisonCafeteria));
    }

    [Test]
    public void RetinueClient_ApplyBundle_SetsKind()
    {
        var go = new GameObject("prison");
        var client = go.AddComponent<PrisonRetinueClient>();
        client.venue = new CivilVenueNode { stableId = "p1" };
        var bundle = PersonaRequestBundle.CreateDefault("guard", CivilSystemKind.Prison);
        bundle.govAgencyId = "corrections";
        client.ApplyBundle(bundle);
        Assert.AreEqual(CivilSystemKind.Prison, client.venue.kind);
        Assert.AreEqual("corrections", client.venue.lastBundle.govAgencyId);
        Object.DestroyImmediate(go);
    }
}
