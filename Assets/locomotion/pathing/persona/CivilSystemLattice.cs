using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Registry of civil venues ordered by developer priority (lower = more important).</summary>
[Serializable]
public sealed class CivilSystemLattice
{
    public List<CivilVenueNode> venues = new List<CivilVenueNode>();

    /// <summary>Kind priority order from settings (first = highest). Used to break ties / filter.</summary>
    public List<CivilSystemKind> kindPriorityOrder = new List<CivilSystemKind>
    {
        CivilSystemKind.Kitchen,
        CivilSystemKind.School,
        CivilSystemKind.Church,
        CivilSystemKind.Library,
        CivilSystemKind.Mall,
        CivilSystemKind.Generic
    };

    public void Register(CivilVenueNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.stableId)) return;
        for (int i = 0; i < venues.Count; i++)
        {
            if (venues[i] != null && venues[i].stableId == node.stableId)
            {
                venues[i] = node;
                return;
            }
        }
        venues.Add(node);
    }

    public CivilVenueNode Get(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return null;
        for (int i = 0; i < venues.Count; i++)
            if (venues[i] != null && venues[i].stableId == stableId)
                return venues[i];
        return null;
    }

    public List<CivilVenueNode> OrderedByPriority()
    {
        var list = new List<CivilVenueNode>();
        for (int i = 0; i < venues.Count; i++)
            if (venues[i] != null)
                list.Add(venues[i]);
        list.Sort(ComparePriority);
        return list;
    }

    int ComparePriority(CivilVenueNode a, CivilVenueNode b)
    {
        int ka = KindRank(a.kind);
        int kb = KindRank(b.kind);
        if (ka != kb) return ka.CompareTo(kb);
        int pa = a.developerPriority;
        int pb = b.developerPriority;
        if (pa != pb) return pa.CompareTo(pb);
        return string.CompareOrdinal(a.stableId, b.stableId);
    }

    int KindRank(CivilSystemKind kind)
    {
        if (kindPriorityOrder == null) return 100;
        int idx = kindPriorityOrder.IndexOf(kind);
        return idx >= 0 ? idx : 100;
    }

    public static CivilSystemKind KindFromBuildingType(string buildingTypeId)
    {
        if (string.IsNullOrEmpty(buildingTypeId)) return CivilSystemKind.Generic;
        var id = buildingTypeId.ToLowerInvariant();
        if (id.Contains("restaurant") || id.Contains("kitchen")) return CivilSystemKind.Kitchen;
        if (id.Contains("school")) return CivilSystemKind.School;
        // Church before mall — "church_small" contains substring "mall".
        if (id.Contains("church")) return CivilSystemKind.Church;
        if (id.Contains("library")) return CivilSystemKind.Library;
        if (id.Contains("mall")) return CivilSystemKind.Mall;
        return CivilSystemKind.Generic;
    }
}
