using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WaterNode
{
    public string nodeId;
    public Vector3 worldPosition;
    public GameObject building;
    public bool isStreetMain;
    public bool isHouseTap;
}

[Serializable]
public sealed class WaterEdge
{
    public string fromId;
    public string toId;
    public float capacity01 = 1f;
    [Range(0f, 1f)] public float load01;
}

/// <summary>City water mains, separate from <see cref="SewerGraph"/> so shutoff does not collapse sewer.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Water Graph")]
public sealed class WaterGraph : MonoBehaviour
{
    public List<WaterNode> nodes = new List<WaterNode>();
    public List<WaterEdge> edges = new List<WaterEdge>();
    public MunicipalWaterService municipalWater;

    void Awake()
    {
        if (municipalWater == null)
            municipalWater = MunicipalWaterService.Instance;
    }

    public WaterNode AddOrGet(string id, Vector3 world, bool streetMain = false, bool houseTap = false, GameObject building = null)
    {
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null && nodes[i].nodeId == id)
                return nodes[i];
        var n = new WaterNode
        {
            nodeId = id,
            worldPosition = world,
            isStreetMain = streetMain,
            isHouseTap = houseTap,
            building = building
        };
        nodes.Add(n);
        return n;
    }

    public WaterNode AddOrGetBuildingTap(GameObject building, Vector3 tapWorld)
    {
        if (building == null) return null;
        return AddOrGet("w_" + building.GetInstanceID(), tapWorld, houseTap: true, building: building);
    }

    public void Connect(string fromId, string toId)
    {
        if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId) || fromId == toId)
            return;
        if (HasEdge(fromId, toId) || HasEdge(toId, fromId))
            return;
        edges.Add(new WaterEdge { fromId = fromId, toId = toId, capacity01 = 1f });
    }

    public bool HasEdge(string from, string to)
    {
        for (int i = 0; i < edges.Count; i++)
            if (edges[i] != null && edges[i].fromId == from && edges[i].toId == to)
                return true;
        return false;
    }

    public void TickFlow(float dt)
    {
        float scale = municipalWater != null ? municipalWater.EffectivePressure01() : 1f;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null) continue;
            e.load01 = Mathf.MoveTowards(e.load01, e.capacity01 * scale, dt * 0.1f);
        }
    }
}
