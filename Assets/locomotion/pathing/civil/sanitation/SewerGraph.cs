using System;
using System.Collections.Generic;
using UnityEngine;

public enum SewerFlowKind
{
    WaterIn = 0,
    GasIn = 1,
    PooOut = 2,
    SoapyWaterOut = 3
}

[Serializable]
public sealed class SewerNode
{
    public string nodeId;
    public Vector3 worldPosition;
    public GameObject building;
    public bool isPlantSink;
    public bool isStreetDrain;
    public bool isDryWell;
}

[Serializable]
public sealed class SewerEdge
{
    public string fromId;
    public string toId;
    public SewerFlowKind flow = SewerFlowKind.PooOut;
    public float capacity01 = 1f;
    [Range(0f, 1f)] public float load01;
}

/// <summary>Building-connected sewer graph — shared outflow for poo + soapy water into poop-quifers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sewer Graph")]
public sealed class SewerGraph : MonoBehaviour
{
    public List<SewerNode> nodes = new List<SewerNode>();
    public List<SewerEdge> edges = new List<SewerEdge>();
    public SanitationPoopQuifer plantSink;
    public MunicipalWaterService municipalWater;

    void Awake()
    {
        if (municipalWater == null)
            municipalWater = MunicipalWaterService.Instance;
        if (plantSink == null)
            plantSink = FindFirstObjectByType<SanitationPoopQuifer>();
    }

    public SewerNode AddOrGetBuildingNode(GameObject building)
    {
        if (building == null) return null;
        string id = "b_" + building.GetInstanceID();
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null && nodes[i].nodeId == id)
                return nodes[i];
        var n = new SewerNode
        {
            nodeId = id,
            worldPosition = building.transform.position,
            building = building
        };
        nodes.Add(n);
        return n;
    }

    public void EnsureFullyConnectedToPlant()
    {
        if (plantSink == null)
            plantSink = FindFirstObjectByType<SanitationPoopQuifer>();
        if (plantSink == null) return;
        string sinkId = "plant_" + plantSink.GetInstanceID();
        SewerNode sink = null;
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null && nodes[i].nodeId == sinkId)
                sink = nodes[i];
        if (sink == null)
        {
            sink = new SewerNode
            {
                nodeId = sinkId,
                worldPosition = plantSink.transform.position,
                isPlantSink = true,
                building = plantSink.gameObject
            };
            nodes.Add(sink);
        }
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null || n.nodeId == sinkId) continue;
            if (HasEdge(n.nodeId, sinkId)) continue;
            edges.Add(new SewerEdge
            {
                fromId = n.nodeId,
                toId = sinkId,
                flow = SewerFlowKind.PooOut,
                capacity01 = 1f
            });
        }
    }

    bool HasEdge(string from, string to)
    {
        for (int i = 0; i < edges.Count; i++)
            if (edges[i] != null && edges[i].fromId == from && edges[i].toId == to)
                return true;
        return false;
    }

    public void TickFlow(float dt)
    {
        float sewerScale = municipalWater != null ? municipalWater.sewerCapacity01 : 1f;
        float inflow = 0f;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null) continue;
            if (e.flow != SewerFlowKind.PooOut && e.flow != SewerFlowKind.SoapyWaterOut) continue;
            e.load01 = Mathf.MoveTowards(e.load01, e.capacity01 * sewerScale, dt * 0.1f);
            inflow += e.load01 * dt * 0.05f;
        }
        plantSink?.AcceptInflow(inflow);
    }

    public void TransmitFromFixture(string buildingNodeId, float poo01, float soapy01)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null || e.fromId != buildingNodeId) continue;
            e.load01 = Mathf.Clamp01(e.load01 + poo01 + soapy01);
        }
        plantSink?.AcceptInflow((poo01 + soapy01) * 0.5f);
    }
}
