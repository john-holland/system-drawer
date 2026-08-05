using System.Collections.Generic;
using UnityEngine;
using Weather;

/// <summary>
/// City-scoped traffic coordinator: MST over cached TravelAgent paths, car enqueue,
/// light policy, police traffic detail, avoid-cop registration.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Traffic Warden")]
public sealed class TrafficWarden : MonoBehaviour
{
    public static TrafficWarden Instance { get; private set; }

    [Tooltip("Optional CityPixelGrid — when bakedCaches exist for the active frame, prefer bake for enqueue backbone.")]
    public CityPixelGrid cityGrid;

    [Tooltip("When set with cityGrid, prefer baked MST over live sampling when available.")]
    public bool preferCityGridBake = true;

    public CityPixelGridRuntime cityGridRuntime;

    public CentralDispatchHub hub;
    public TrafficDispatchBioRhythm trafficBio;
    public HierarchicalPathingSolver pathingSolver;
    public float rebuildIntervalSec = 2f;
    public float corridorCellSize = 4f;
    public float congestionDemandThreshold = 8f;
    public bool narrativeLeaseActive;

    public readonly TrafficCorridorGraph corridorGraph = new TrafficCorridorGraph();
    public readonly TrafficCarEnqueue carEnqueue = new TrafficCarEnqueue();
    public readonly TrafficWardenStateMachine stateMachine = new TrafficWardenStateMachine();
    public readonly List<TrafficLightController> lights = new List<TrafficLightController>();
    public readonly List<Transform> avoidSources = new List<Transform>();
    public List<TrafficCorridorEdge> backboneEdges = new List<TrafficCorridorEdge>();

    float _rebuildT;
    public float MaxEdgeDemand { get; private set; }

    void Awake()
    {
        Instance = this;
        stateMachine.Bind(this);
        stateMachine.congestedDemandThreshold = congestionDemandThreshold;
        if (hub == null)
            hub = CentralDispatchHub.Instance ?? FindFirstObjectByType<CentralDispatchHub>();
        if (trafficBio == null)
            trafficBio = GetComponent<TrafficDispatchBioRhythm>()
                         ?? gameObject.AddComponent<TrafficDispatchBioRhythm>();
        trafficBio.warden = this;
        if (pathingSolver == null)
            SceneServiceLookup.TryResolve("pathing.hierarchical", out pathingSolver);
        RefreshLights();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        _rebuildT += dt;
        if (_rebuildT >= rebuildIntervalSec)
        {
            _rebuildT = 0f;
            RebuildCorridorMst();
        }

        stateMachine.Tick(dt, MaxEdgeDemand, narrativeLeaseActive);
        ApplyLightPolicy(dt);
        carEnqueue.ReleaseAlongBackbone(backboneEdges, corridorGraph, lights);
    }

    public void RefreshLights()
    {
        lights.Clear();
        lights.AddRange(FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None));
    }

    public void RebuildCorridorMst()
    {
        if (preferCityGridBake && TryApplyCityGridBake())
        {
            MaxEdgeDemand = 0f;
            for (int i = 0; i < backboneEdges.Count; i++)
                MaxEdgeDemand = Mathf.Max(MaxEdgeDemand, backboneEdges[i].demand);
            // Merge live demand for dirty tracking.
            corridorGraph.IngestTravelAgentPlans(TravelAgentRegistry.All, driveLegsPreferred: true);
            return;
        }

        corridorGraph.Clear();
        corridorGraph.cellSize = corridorCellSize > 0.01f
            ? corridorCellSize
            : (pathingSolver != null ? pathingSolver.cellSize : 4f);
        corridorGraph.IngestTravelAgentPlans(TravelAgentRegistry.All, driveLegsPreferred: true);
        backboneEdges = TrafficMstBuilder.Build(corridorGraph);
        MaxEdgeDemand = 0f;
        for (int i = 0; i < corridorGraph.edges.Count; i++)
            MaxEdgeDemand = Mathf.Max(MaxEdgeDemand, corridorGraph.edges[i].demand);
    }

    bool TryApplyCityGridBake()
    {
        if (cityGrid == null) return false;
        int frame = 0;
        if (cityGridRuntime == null)
            cityGridRuntime = FindFirstObjectByType<CityPixelGridRuntime>();
        if (cityGridRuntime != null && cityGridRuntime.grid == cityGrid)
            frame = cityGridRuntime.ActiveFrameIndex;
        var bake = cityGrid.FindBake(frame);
        if (bake == null || bake.mstEdges == null || bake.mstEdges.Count == 0)
            return false;
        CityPixelGridBaker.ApplyBakeToWarden(cityGrid, frame, this);
        return backboneEdges != null && backboneEdges.Count > 0;
    }

    public void EnqueueCar(TravelAgent agent) => carEnqueue.Enqueue(agent);

    public void SeedFromParkingLots(float radiusM = 40f)
    {
        var lots = FindObjectsByType<ParkingLot>(FindObjectsSortMode.None);
        for (int i = 0; i < lots.Length; i++)
        {
            var lot = lots[i];
            if (lot == null) continue;
            lot.SeedTravelAgents(radiusM);
            var agents = FindObjectsByType<TravelAgent>(FindObjectsSortMode.None);
            Vector3 goal = lot.ArrivalWorld;
            for (int a = 0; a < agents.Length; a++)
            {
                var ta = agents[a];
                if (ta == null) continue;
                if ((ta.transform.position - goal).sqrMagnitude > radiusM * radiusM) continue;
                EnqueueCar(ta);
            }
        }
    }

    public Vector3 SuggestFlowGoal(Vector3 from)
    {
        if (backboneEdges == null || backboneEdges.Count == 0 || corridorGraph.nodes.Count == 0)
            return from + Vector3.forward * 8f;
        float best = float.PositiveInfinity;
        Vector3 goal = from;
        for (int i = 0; i < backboneEdges.Count; i++)
        {
            var e = backboneEdges[i];
            if (!corridorGraph.nodes.TryGetValue(e.b, out var nb)) continue;
            float d = (nb.world - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                goal = nb.world;
            }
        }
        return goal;
    }

    public void RegisterAvoidSource(Transform t)
    {
        if (t == null || avoidSources.Contains(t)) return;
        avoidSources.Add(t);
    }

    public void UnregisterAvoidSource(Transform t)
    {
        if (t == null) return;
        avoidSources.Remove(t);
    }

    public void RegisterAvoidSource(PoliceCarVehicleRagdoll cruiser)
    {
        if (cruiser == null) return;
        RegisterAvoidSource(cruiser.transform);
    }

    public void ClearAvoidSources() => avoidSources.Clear();

    public void CopyAvoidPoints(List<Vector3> into)
    {
        into.Clear();
        for (int i = 0; i < avoidSources.Count; i++)
        {
            if (avoidSources[i] != null)
                into.Add(avoidSources[i].position);
        }
    }

    public void OnStateEntered(TrafficWardenMode mode)
    {
        if (mode == TrafficWardenMode.PoliceDetailActive || mode == TrafficWardenMode.EmergencyPreempt)
            RequestPoliceTrafficDetail(stateMachine.detailTargetWorld);
    }

    public void BeginPoliceDetail(Vector3 worldTarget)
    {
        stateMachine.BeginPoliceDetail(worldTarget);
    }

    public bool RequestPoliceTrafficDetail(Vector3 worldTarget)
    {
        var request = new DispatchRequest
        {
            kind = "traffic_detail",
            worldTarget = worldTarget,
            notes = "traffic_detail",
            priority01 = 0.75f
        };
        if (hub == null)
            hub = CentralDispatchHub.Instance;
        return hub != null && hub.RequestCrossDispatch("traffic_warden", "police", request);
    }

    void ApplyLightPolicy(float dt)
    {
        if (lights.Count == 0) return;
        switch (stateMachine.Mode)
        {
            case TrafficWardenMode.CongestedHold:
                for (int i = 0; i < lights.Count; i++)
                {
                    var l = lights[i];
                    if (l == null) continue;
                    if (l.Phase != TrafficSignalPhase.AllRed)
                        l.Enter(TrafficSignalPhase.AllRed);
                }
                break;
            case TrafficWardenMode.EmergencyPreempt:
            case TrafficWardenMode.PoliceDetailActive:
                PreemptToward(stateMachine.detailTargetWorld);
                break;
            case TrafficWardenMode.NarrativeLease:
                // Soft hold — same as congested for MVP.
                goto case TrafficWardenMode.CongestedHold;
        }
    }

    void PreemptToward(Vector3 target)
    {
        TrafficLightController closest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < lights.Count; i++)
        {
            var l = lights[i];
            if (l == null) continue;
            float d = (l.transform.position - target).sqrMagnitude;
            if (d < best)
            {
                best = d;
                closest = l;
            }
        }

        if (closest != null && !closest.MainProceed)
            closest.Enter(TrafficSignalPhase.MainGreen);
    }

    public void OnTrafficLightPhase(TrafficLightController ctrl)
    {
        // Hook for sensors / tests; lights already registered.
    }
}
