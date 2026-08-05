using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class TrafficWardenTests
{
    [Test]
    public void TrafficMst_BuildsOnSyntheticPolylines()
    {
        var graph = new TrafficCorridorGraph { cellSize = 4f };
        graph.AddPathDemand(new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(8, 0, 0),
            new Vector3(16, 0, 0)
        });
        graph.AddPathDemand(new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 8),
            new Vector3(0, 0, 16)
        });
        graph.AddPathDemand(new List<Vector3>
        {
            new Vector3(16, 0, 0),
            new Vector3(16, 0, 16),
            new Vector3(0, 0, 16)
        });

        var mst = TrafficMstBuilder.Build(graph);
        Assert.Greater(mst.Count, 0);
        Assert.Less(mst.Count, graph.edges.Count + 1);
    }

    [Test]
    public void TrafficCarEnqueue_AssignsGoalsAlongBackbone()
    {
        var graph = new TrafficCorridorGraph { cellSize = 4f };
        graph.AddPathDemand(new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(12, 0, 0)
        });
        var backbone = TrafficMstBuilder.Build(graph);
        Assert.Greater(backbone.Count, 0);

        var go = new GameObject("car");
        var ta = go.AddComponent<TravelAgent>();
        ta.previewStartWorld = Vector3.zero;
        var enqueue = new TrafficCarEnqueue { maxReleasesPerTick = 4 };
        enqueue.Enqueue(ta);
        int n = enqueue.ReleaseAlongBackbone(backbone, graph, null);
        Assert.AreEqual(1, n);
        Assert.AreNotEqual(Vector3.zero, ta.previewGoalWorld);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PoliceDispatch_FacilitateCards_EmitsTrafficDetailLadder()
    {
        var go = new GameObject("police");
        var bio = go.AddComponent<PoliceDispatchBioRhythm>();
        bio.trafficDetailLadder = TrafficDetailLadderAsset.CreateDefaultRuntime();
        var cards = bio.FacilitateCards(new DispatchRequest
        {
            kind = "traffic_detail",
            worldTarget = new Vector3(5, 0, 5),
            notes = "traffic_detail"
        });
        Assert.Greater(cards.Count, 0);
        bool foundDetail = false;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] is DispatchPoliceDetailCard)
            {
                foundDetail = true;
                break;
            }
        }
        Assert.IsTrue(foundDetail);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SoftAvoid_RaisesCostNearCop_UnlessIgnored()
    {
        var solverGo = new GameObject("solver");
        var solver = solverGo.AddComponent<HierarchicalPathingSolver>();
        solver.SetSoftAvoid(new[] { Vector3.zero }, 10f, 5f, enabled: true);
        Assert.AreEqual(5f, solver.softAvoidCostMultiplier, 0.01f);

        var taGo = new GameObject("racer");
        var ta = taGo.AddComponent<TravelAgent>();
        ta.pathingSolverForPreview = solver;
        ta.avoidRadius = 10f;
        ta.avoidCostMultiplier = 5f;
        var cop = new GameObject("cop");
        cop.transform.position = Vector3.zero;
        ta.avoidActors.Add(cop.transform);

        ta.ignoreTrafficAvoidance = false;
        ta.ApplySoftAvoidToPathingSolver(solver);
        Assert.AreEqual(5f, solver.EvaluateSoftAvoidMultiplier(Vector3.zero), 0.01f);
        Assert.AreEqual(1f, solver.EvaluateSoftAvoidMultiplier(new Vector3(100, 0, 100)), 0.01f);

        ta.ignoreTrafficAvoidance = true;
        ta.ApplySoftAvoidToPathingSolver(solver);
        Assert.IsFalse(solver.softAvoidEnabled);
        Assert.AreEqual(1f, solver.EvaluateSoftAvoidMultiplier(Vector3.zero), 0.01f);

        Object.DestroyImmediate(solverGo);
        Object.DestroyImmediate(taGo);
        Object.DestroyImmediate(cop);
    }

    [Test]
    public void TrafficFlowVolume_GraftsTravelAgentGoal()
    {
        var wardenGo = new GameObject("warden");
        var warden = wardenGo.AddComponent<TrafficWarden>();
        warden.RebuildCorridorMst();

        var volGo = new GameObject("vol");
        var col = volGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        var vol = volGo.AddComponent<TrafficFlowVolumeTrigger>();
        vol.warden = warden;
        vol.spatiotemporalVolume = new Bounds4(Vector3.zero, Vector3.one * 20f, 0f, 999999f);

        var agentGo = new GameObject("agent");
        agentGo.transform.position = Vector3.zero;
        var ta = agentGo.AddComponent<TravelAgent>();
        var bt = agentGo.AddComponent<BehaviorTree>();

        Assert.IsTrue(vol.TryGraft(agentGo));
        Assert.IsNotNull(bt.currentGoal);
        Assert.AreEqual("traffic_flow", bt.currentGoal.goalName);
        Assert.AreEqual(GoalType.TravelAgent, bt.currentGoal.type);

        Object.DestroyImmediate(wardenGo);
        Object.DestroyImmediate(volGo);
        Object.DestroyImmediate(agentGo);
    }

    [Test]
    public void TrafficWarden_RequestsPoliceDetailViaHub()
    {
        var hubGo = new GameObject("hub");
        var hub = hubGo.AddComponent<CentralDispatchHub>();
        var policeGo = new GameObject("police");
        var police = policeGo.AddComponent<PoliceDispatchBioRhythm>();
        police.serviceId = "police";
        hub.Subscribe("police", police);

        var wardenGo = new GameObject("warden");
        var warden = wardenGo.AddComponent<TrafficWarden>();
        warden.hub = hub;
        Assert.IsTrue(warden.RequestPoliceTrafficDetail(new Vector3(1, 0, 2)));
        Assert.AreEqual(1, police.Pending.Count);
        Assert.AreEqual("traffic_detail", police.Pending[0].kind);

        Object.DestroyImmediate(hubGo);
        Object.DestroyImmediate(policeGo);
        Object.DestroyImmediate(wardenGo);
    }

    [Test]
    public void PoliceCar_DispatchToDetail_RegistersAvoid()
    {
        var wardenGo = new GameObject("warden");
        var warden = wardenGo.AddComponent<TrafficWarden>();
        var carGo = new GameObject("cruiser");
        var car = carGo.AddComponent<PoliceCarVehicleRagdoll>();
        var target = new GameObject("target");
        target.transform.position = new Vector3(10, 0, 0);

        car.DispatchToDetail(target, "traffic_detail");
        Assert.IsFalse(car.available);
        Assert.IsTrue(car.lightsOn);
        CollectionAssert.Contains(warden.avoidSources, car.transform);

        car.ClearDetailDispatch();
        Assert.IsTrue(car.available);
        Assert.IsFalse(warden.avoidSources.Contains(car.transform));

        Object.DestroyImmediate(wardenGo);
        Object.DestroyImmediate(carGo);
        Object.DestroyImmediate(target);
    }
}
