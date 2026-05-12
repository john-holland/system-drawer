using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight registry of all active TravelAgent instances for multibody solves (no full scene scan each rebuild).
/// </summary>
public static class TravelAgentRegistry
{
    static readonly List<TravelAgent> s_agents = new List<TravelAgent>(32);

    public static void Register(TravelAgent agent)
    {
        if (agent == null)
            return;
        if (!s_agents.Contains(agent))
            s_agents.Add(agent);
    }

    public static void Unregister(TravelAgent agent)
    {
        if (agent == null)
            return;
        s_agents.Remove(agent);
    }

    public static IReadOnlyList<TravelAgent> All => s_agents;

    public static void CopyPeersExcluding(TravelAgent exclude, List<TravelAgent> into)
    {
        into.Clear();
        for (int i = 0; i < s_agents.Count; i++)
        {
            TravelAgent a = s_agents[i];
            if (a != null && a != exclude)
                into.Add(a);
        }
    }
}
