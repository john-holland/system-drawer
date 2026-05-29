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

    /// <summary>
    /// Fills <paramref name="into"/> with peers for multibody relaxation. When
    /// <paramref name="settings"/>.<c>limitMultibodyPeersToSameFormationGroup</c> is true and <paramref name="self"/> has a non-empty
    /// <see cref="TravelAgent.multibodyFormationGroupId"/>, only agents in that group are included; otherwise same as <see cref="CopyPeersExcluding"/>.
    /// </summary>
    public static void CopyPeersForMultibody(TravelAgent exclude, List<TravelAgent> into, TravelAgentMultibodySettings settings, TravelAgent self)
    {
        into.Clear();
        bool limit = settings != null && settings.limitMultibodyPeersToSameFormationGroup
                     && self != null && !string.IsNullOrEmpty(self.multibodyFormationGroupId);
        string gid = self != null ? self.multibodyFormationGroupId : null;

        for (int i = 0; i < s_agents.Count; i++)
        {
            TravelAgent a = s_agents[i];
            if (a == null || a == exclude)
                continue;
            if (limit)
            {
                if (string.IsNullOrEmpty(a.multibodyFormationGroupId) || a.multibodyFormationGroupId != gid)
                    continue;
            }
            into.Add(a);
        }
    }
}
