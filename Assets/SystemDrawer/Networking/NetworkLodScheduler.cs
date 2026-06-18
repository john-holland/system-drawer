using System.Collections.Generic;
using UnityEngine;

/// <summary>Server-side LOD pre-warm scheduler for tree streaming.</summary>
public sealed class NetworkLodScheduler
{
    readonly NetworkSettings _settings;
    readonly NetworkTreeRegistry _registry;
    readonly HashSet<string> _warmed = new HashSet<string>();

    public NetworkLodScheduler(NetworkSettings settings, NetworkTreeRegistry registry)
    {
        _settings = settings ?? NetworkSettings.Default;
        _registry = registry;
    }

    public float ClientRadius => _settings.clientLodRadius;
    public float ServerRadius => _settings.serverLodRadius;

    public IEnumerable<string> TreesToPreWarm(Vector3 observer, Vector3 treeCenter)
    {
        float dist = Vector3.Distance(observer, treeCenter);
        if (dist > ServerRadius)
            yield break;
        foreach (var pair in _registry.Trees)
        {
            if (pair.Value.StreamForOwnership || pair.Value.TransmitPolicy != TreeTransmitPolicy.LocalOnly)
                yield return pair.Key;
        }
    }

    public bool MarkWarmed(string treeId)
    {
        if (string.IsNullOrEmpty(treeId))
            return false;
        return _warmed.Add(treeId);
    }

    public bool IsWithinClientLod(Vector3 observer, Vector3 treeCenter) =>
        Vector3.Distance(observer, treeCenter) <= ClientRadius;

    public void Clear() => _warmed.Clear();
}
