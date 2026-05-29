using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TravelFormationRuntimeTests
{
    [Test]
    public void JsonLoader_ValidSlots_Parses()
    {
        const string json = "{\"version\":1,\"slots\":[{\"x\":0,\"y\":1,\"z\":2},{\"x\":-0.5,\"y\":0,\"z\":0}]}";
        Assert.IsTrue(TravelFormationJsonLoader.TryParseSlots(json, out var list, out string err), err);
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(new Vector3(0f, 1f, 2f), list[0]);
        Assert.AreEqual(new Vector3(-0.5f, 0f, 0f), list[1]);
    }

    [Test]
    public void JsonLoader_Invalid_ReturnsFalse()
    {
        Assert.IsFalse(TravelFormationJsonLoader.TryParseSlots("{}", out _, out _));
        Assert.IsFalse(TravelFormationJsonLoader.TryParseSlots("", out _, out _));
    }

    [Test]
    public void ResolveFormationWrapRowSpacing_UsesClearanceWhenFlagged()
    {
        var s = new TravelAgentMultibodySettings
        {
            clearanceRadius = 0.4f,
            formationRowSpacingUsesClearance = true
        };
        Assert.AreEqual(0.8f, s.ResolveFormationWrapRowSpacing(null), 1e-5f);
    }

    [Test]
    public void ComputeWorldOffset_WrapBack_AddsNegativeForwardRow()
    {
        var go = new GameObject("soloFormationTest");
        var agent = go.AddComponent<TravelAgent>();
        agent.multibodyFormationGroupId = "testSquad";
        agent.formationSlotIndex = 3;

        var formation = ScriptableObject.CreateInstance<TravelFormationAsset>();
        formation.slots.Add(new TravelFormationSlot { localOffset = Vector3.zero });
        formation.slots.Add(new TravelFormationSlot { localOffset = new Vector3(1f, 0f, 0f) });

        var settings = new TravelAgentMultibodySettings
        {
            formationWrapDirection = TravelFormationWrapDirection.Back,
            formationWrapRowSpacing = 2f
        };

        Vector3 travelFwd = new Vector3(0f, 0f, 1f);
        Vector3 off = TravelFormationAssignment.ComputeWorldOffsetFromFormation(agent, formation, settings, travelFwd);

        Vector3 right = Vector3.Cross(Vector3.up, travelFwd).normalized;
        Vector3 expectedSlot = right * 1f;
        Vector3 expectedWrap = -travelFwd * (2f * 1f);
        Vector3 expected = expectedSlot + expectedWrap;

        Assert.AreEqual(expected.x, off.x, 0.02f);
        Assert.AreEqual(expected.y, off.y, 0.02f);
        Assert.AreEqual(expected.z, off.z, 0.02f);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PathOffset_ShiftsWalkWaypoints()
    {
        var go = new GameObject("pathOffsetTest");
        var agent = go.AddComponent<TravelAgent>();
        agent.multibodyFormationGroupId = "g";
        agent.multibody.formation = ScriptableObject.CreateInstance<TravelFormationAsset>();
        agent.multibody.formation.slots.Add(new TravelFormationSlot { localOffset = new Vector3(2f, 0f, 0f) });
        agent.formationSlotIndex = 0;

        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3>
        {
            new Vector3(10f, 0f, 0f),
            new Vector3(20f, 0f, 0f)
        }));

        TravelFormationPathOffset.ApplyToPlan(agent, plan, new Vector3(10f, 0f, 0f));

        Assert.AreEqual(10f, plan.segments[0].waypoints[0].x, 0.05f);
        Assert.AreEqual(-2f, plan.segments[0].waypoints[0].z, 0.05f);
        Assert.AreEqual(20f, plan.segments[0].waypoints[1].x, 0.05f);
        Assert.AreEqual(-2f, plan.segments[0].waypoints[1].z, 0.05f);

        Object.DestroyImmediate(go);
    }
}
