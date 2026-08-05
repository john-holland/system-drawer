using System.Collections.Generic;

/// <summary>Kruskal MST over a <see cref="TrafficCorridorGraph"/>.</summary>
public static class TrafficMstBuilder
{
    public static List<TrafficCorridorEdge> Build(TrafficCorridorGraph graph)
    {
        var result = new List<TrafficCorridorEdge>();
        if (graph == null || graph.edges.Count == 0) return result;

        var sorted = new List<TrafficCorridorEdge>(graph.edges);
        sorted.Sort((x, y) =>
        {
            int c = x.length.CompareTo(y.length);
            if (c != 0) return c;
            return y.demand.CompareTo(x.demand);
        });

        var parent = new Dictionary<long, long>();
        long Find(long x)
        {
            if (!parent.ContainsKey(x)) parent[x] = x;
            if (parent[x] != x) parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(long a, long b)
        {
            long ra = Find(a);
            long rb = Find(b);
            if (ra != rb) parent[rb] = ra;
        }

        foreach (var e in sorted)
        {
            if (Find(e.a) == Find(e.b)) continue;
            Union(e.a, e.b);
            result.Add(e);
        }

        return result;
    }
}
