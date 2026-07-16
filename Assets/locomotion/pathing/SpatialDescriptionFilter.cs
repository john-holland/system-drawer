using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Matches spatial generator / ORM description keys for walk-reminder waypoints.
/// Defaults: outside, sink, bathroom — overridable by developers.
/// </summary>
[Serializable]
public sealed class SpatialDescriptionFilter
{
    public List<string> allowedDescriptions = new List<string> { "outside", "sink", "bathroom" };

    public SpatialDescriptionFilter() { }

    public SpatialDescriptionFilter(IEnumerable<string> descriptions)
    {
        allowedDescriptions = new List<string>();
        if (descriptions == null) return;
        foreach (var d in descriptions)
        {
            if (!string.IsNullOrWhiteSpace(d))
                allowedDescriptions.Add(d.Trim());
        }
    }

    public bool Matches(string descriptionOrKey)
    {
        if (string.IsNullOrEmpty(descriptionOrKey) || allowedDescriptions == null || allowedDescriptions.Count == 0)
            return false;
        for (int i = 0; i < allowedDescriptions.Count; i++)
        {
            string a = allowedDescriptions[i];
            if (string.IsNullOrEmpty(a)) continue;
            if (string.Equals(a, descriptionOrKey, StringComparison.OrdinalIgnoreCase))
                return true;
            if (descriptionOrKey.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    public bool TryPickWaypoint(IList<SpatialTaggedPoint> candidates, out Vector3 worldPos, out string matchedKey)
    {
        worldPos = Vector3.zero;
        matchedKey = null;
        if (candidates == null) return false;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c == null) continue;
            if (Matches(c.descriptionKey))
            {
                worldPos = c.worldPosition;
                matchedKey = c.descriptionKey;
                return true;
            }
        }
        return false;
    }
}

/// <summary>Tagged spatial point for description-filtered octree / SG search results.</summary>
[Serializable]
public sealed class SpatialTaggedPoint
{
    public string descriptionKey;
    public Vector3 worldPosition;
}
