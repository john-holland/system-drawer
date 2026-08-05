using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TrafficCorridorNode
{
    public long id;
    public Vector3 world;
}

[Serializable]
public struct TrafficCorridorEdge
{
    public long a;
    public long b;
    public float length;
    public float demand;
}

/// <summary>Undirected weighted corridor graph built from cached TravelAgent Drive plans.</summary>
public sealed class TrafficCorridorGraph
{
    public float cellSize = 4f;
    public readonly Dictionary<long, TrafficCorridorNode> nodes = new Dictionary<long, TrafficCorridorNode>();
    public readonly List<TrafficCorridorEdge> edges = new List<TrafficCorridorEdge>();
    readonly Dictionary<long, int> _edgeIndex = new Dictionary<long, int>();

    public void Clear()
    {
        nodes.Clear();
        edges.Clear();
        _edgeIndex.Clear();
    }

    public long SnapId(Vector3 world)
    {
        float c = Mathf.Max(0.25f, cellSize);
        int ix = Mathf.RoundToInt(world.x / c);
        int iz = Mathf.RoundToInt(world.z / c);
        // Pack into 64-bit key (y ignored for drive corridors).
        return ((long)ix << 32) ^ (uint)iz;
    }

    public Vector3 SnapWorld(Vector3 world)
    {
        float c = Mathf.Max(0.25f, cellSize);
        return new Vector3(
            Mathf.Round(world.x / c) * c,
            world.y,
            Mathf.Round(world.z / c) * c);
    }

    public void EnsureNode(Vector3 world)
    {
        long id = SnapId(world);
        if (nodes.ContainsKey(id)) return;
        nodes[id] = new TrafficCorridorNode { id = id, world = SnapWorld(world) };
    }

    static long EdgeKey(long a, long b)
    {
        if (a > b)
        {
            long t = a;
            a = b;
            b = t;
        }
        return a * 397L ^ b;
    }

    public void AddPathDemand(IReadOnlyList<Vector3> waypoints, float demandAdd = 1f)
    {
        if (waypoints == null || waypoints.Count < 2) return;
        for (int i = 1; i < waypoints.Count; i++)
        {
            Vector3 wa = waypoints[i - 1];
            Vector3 wb = waypoints[i];
            EnsureNode(wa);
            EnsureNode(wb);
            long a = SnapId(wa);
            long b = SnapId(wb);
            if (a == b) continue;
            float len = Vector3.Distance(nodes[a].world, nodes[b].world);
            long key = EdgeKey(a, b);
            if (_edgeIndex.TryGetValue(key, out int idx))
            {
                var e = edges[idx];
                e.demand += demandAdd;
                e.length = len;
                edges[idx] = e;
            }
            else
            {
                _edgeIndex[key] = edges.Count;
                edges.Add(new TrafficCorridorEdge { a = a, b = b, length = len, demand = demandAdd });
            }
        }
    }

    public void IngestTravelAgentPlans(IEnumerable<TravelAgent> agents, bool driveLegsPreferred = true)
    {
        if (agents == null) return;
        foreach (var ta in agents)
        {
            if (ta == null || ta.CachedPlan == null || ta.CachedPlan.IsEmpty) continue;
            var pts = new List<Vector3>();
            var segs = ta.CachedPlan.segments;
            for (int s = 0; s < segs.Count; s++)
            {
                var seg = segs[s];
                if (seg?.waypoints == null || seg.waypoints.Count == 0) continue;
                if (driveLegsPreferred && seg.mode != TravelLegMode.Drive && seg.mode != TravelLegMode.Walk)
                    continue;
                for (int w = 0; w < seg.waypoints.Count; w++)
                    pts.Add(seg.waypoints[w]);
            }
            if (pts.Count < 2)
                pts = ta.CachedPlan.FlattenWaypointsForGizmos();
            AddPathDemand(pts);
        }
    }
}
