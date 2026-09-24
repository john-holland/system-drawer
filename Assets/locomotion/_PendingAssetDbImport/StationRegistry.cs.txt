using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene singleton: enumerate station hierarchy + build level-stats upload DTO.</summary>
[AddComponentMenu("Locomotion/Stations/Station Registry")]
public sealed class StationRegistry : MonoBehaviour
{
    static StationRegistry _instance;
    public static StationRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<StationRegistry>();
            return _instance;
        }
    }

    readonly List<StationHierarchyNode> _nodes = new List<StationHierarchyNode>();
    public string defaultCityId = "demo-city";
    public string defaultLevelId = "default";

    void Awake()
    {
        _instance = this;
        RefreshFromScene();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void Register(StationHierarchyNode node)
    {
        if (node == null || _nodes.Contains(node)) return;
        _nodes.Add(node);
    }

    public void Unregister(StationHierarchyNode node)
    {
        _nodes.Remove(node);
    }

    public void RefreshFromScene()
    {
        _nodes.Clear();
        var found = FindObjectsByType<StationHierarchyNode>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            Register(found[i]);
    }

    public IReadOnlyList<StationHierarchyNode> All => _nodes;

    public List<StationHierarchyNode> OrderedHierarchy()
    {
        RefreshFromScene();
        var byId = new Dictionary<string, StationHierarchyNode>();
        for (int i = 0; i < _nodes.Count; i++)
        {
            var n = _nodes[i];
            if (n != null && !string.IsNullOrEmpty(n.stableId))
                byId[n.stableId] = n;
        }
        var ordered = new List<StationHierarchyNode>();
        var visiting = new HashSet<string>();
        void Visit(StationHierarchyNode n)
        {
            if (n == null || string.IsNullOrEmpty(n.stableId) || !visiting.Add(n.stableId))
                return;
            if (!string.IsNullOrEmpty(n.parentStableId) && byId.TryGetValue(n.parentStableId, out var parent))
                Visit(parent);
            if (!ordered.Contains(n))
                ordered.Add(n);
        }
        foreach (var kv in byId)
            Visit(kv.Value);
        return ordered;
    }

    public Dictionary<string, object> BuildLevelStatsPayload()
    {
        RefreshFromScene();
        var byKind = new Dictionary<string, int>();
        float commodityQty = 0f;
        int assignmentCount = 0;
        var roster = new List<object>();
        for (int i = 0; i < _nodes.Count; i++)
        {
            var n = _nodes[i];
            if (n == null) continue;
            string k = StationHierarchyNode.KindToApi(n.kind);
            byKind[k] = byKind.TryGetValue(k, out int c) ? c + 1 : 1;
            if (n.config?.commodities != null)
            {
                for (int j = 0; j < n.config.commodities.Count; j++)
                    if (n.config.commodities[j] != null)
                        commodityQty += n.config.commodities[j].quantity;
            }
            if (n.config?.assignments != null)
            {
                assignmentCount += n.config.assignments.Count;
                for (int j = 0; j < n.config.assignments.Count; j++)
                {
                    var a = n.config.assignments[j];
                    if (a == null) continue;
                    roster.Add(new Dictionary<string, object>
                    {
                        ["station"] = n.stableId,
                        ["assignType"] = a.assignType,
                        ["refId"] = a.refId,
                        ["role"] = a.role
                    });
                }
            }
        }
        return new Dictionary<string, object>
        {
            ["countsByKind"] = byKind,
            ["stationCount"] = _nodes.Count,
            ["commodityQuantityTotal"] = commodityQty,
            ["assignmentCount"] = assignmentCount,
            ["roster"] = roster
        };
    }

    public List<object> BuildPlacardList()
    {
        var list = new List<object>();
        var ordered = OrderedHierarchy();
        for (int i = 0; i < ordered.Count; i++)
            list.Add(ordered[i].ToPlacardDto());
        return list;
    }

    public Dictionary<string, object> BuildUploadBody(string cityId = null, string levelId = null)
    {
        return new Dictionary<string, object>
        {
            ["cityId"] = cityId ?? defaultCityId,
            ["levelId"] = levelId ?? defaultLevelId,
            ["stats"] = BuildLevelStatsPayload(),
            ["stations"] = BuildPlacardList()
        };
    }
}
