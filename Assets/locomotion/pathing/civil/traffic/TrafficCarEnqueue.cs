using System.Collections.Generic;
using UnityEngine;

/// <summary>Releases / goals cars along MST backbone edges under capacity and light proceed.</summary>
public sealed class TrafficCarEnqueue
{
    public int maxReleasesPerTick = 2;
    public float lightSearchRadius = 24f;

    readonly Queue<TravelAgent> _pending = new Queue<TravelAgent>();

    public int PendingCount => _pending.Count;

    public void Enqueue(TravelAgent agent)
    {
        if (agent == null) return;
        _pending.Enqueue(agent);
    }

    public void EnqueueRange(IEnumerable<TravelAgent> agents)
    {
        if (agents == null) return;
        foreach (var a in agents)
            Enqueue(a);
    }

    public int ReleaseAlongBackbone(
        IReadOnlyList<TrafficCorridorEdge> backbone,
        TrafficCorridorGraph graph,
        IReadOnlyList<TrafficLightController> lights)
    {
        if (backbone == null || backbone.Count == 0 || graph == null) return 0;
        int released = 0;
        while (released < maxReleasesPerTick && _pending.Count > 0)
        {
            var ta = _pending.Dequeue();
            if (ta == null) continue;

            int edgeIdx = released % backbone.Count;
            var edge = backbone[edgeIdx];
            if (!graph.nodes.TryGetValue(edge.b, out var nodeB))
            {
                if (!graph.nodes.TryGetValue(edge.a, out nodeB))
                    continue;
            }

            Vector3 goal = nodeB.world;
            if (!LightAllowsRelease(goal, lights))
            {
                _pending.Enqueue(ta);
                break;
            }

            ta.previewGoalWorld = goal;
            ta.ApplyAvoidHintsFromWarden();
            ta.RebuildCachedPlan();
            released++;
        }

        return released;
    }

    bool LightAllowsRelease(Vector3 near, IReadOnlyList<TrafficLightController> lights)
    {
        if (lights == null || lights.Count == 0) return true;
        float r2 = lightSearchRadius * lightSearchRadius;
        TrafficLightController closest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < lights.Count; i++)
        {
            var l = lights[i];
            if (l == null) continue;
            float d = (l.transform.position - near).sqrMagnitude;
            if (d < best && d <= r2)
            {
                best = d;
                closest = l;
            }
        }

        if (closest == null) return true;
        return closest.MainProceed || closest.SideProceed;
    }
}
