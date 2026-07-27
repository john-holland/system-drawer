using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Resolves {P:have|...} possessives against InventoryManager loadouts (silent miss).</summary>
public static class InventoryLemmaResolver
{
    public static InventoryLemmaProperties ResolveFromSegments(IReadOnlyList<PromptSegment> segments)
    {
        var props = InventoryLemmaProperties.Defaults;
        if (segments == null) return props;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            if (!string.Equals(seg.placeholderName, InventoryLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(seg.placeholderName, "possess", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(seg.placeholderName, "give", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(seg.placeholderName, "take", StringComparison.OrdinalIgnoreCase))
                continue;
            ApplyParams(ref props, seg.placeholderParams, seg.placeholderName);
        }
        return props;
    }

    public static void ApplyParams(ref InventoryLemmaProperties props, Dictionary<string, string> p, string placeholder = null)
    {
        if (p == null) return;
        if (TryGet(p, InventoryLemmaPropertyKeys.Op, out var op))
            props.op = ParseOp(op);
        else if (!string.IsNullOrEmpty(placeholder))
            props.op = ParseOp(placeholder);
        if (TryGet(p, InventoryLemmaPropertyKeys.Item, out var item))
            props.item = item;
        if (TryGet(p, InventoryLemmaPropertyKeys.From, out var from))
            props.fromActorId = from;
        if (TryGet(p, InventoryLemmaPropertyKeys.To, out var to))
            props.toActorId = to;
    }

    public static string Execute(InventoryLemmaProperties props)
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return "no inventory manager";
        if (!string.IsNullOrEmpty(props.item))
            mgr.NoteScriptMention(props.item);

        if (string.IsNullOrEmpty(props.item))
            return "ok"; // silent — nothing to assert

        var found = mgr.FindByName(props.item);
        if (found == null)
            return "ok"; // silent fallthrough to tool-use

        switch (props.op)
        {
            case InventoryLemmaOp.Give:
            case InventoryLemmaOp.Transfer:
            case InventoryLemmaOp.Take:
                mgr.TryPossessiveOrTransfer(props.item, props.fromActorId, props.toActorId, requireMention: true);
                return $"transfer {props.item}";
            case InventoryLemmaOp.Assert:
            case InventoryLemmaOp.Have:
            default:
                if (!string.IsNullOrEmpty(props.toActorId) || !string.IsNullOrEmpty(props.fromActorId))
                    mgr.TryPossessiveOrTransfer(props.item, props.fromActorId, props.toActorId ?? props.fromActorId, requireMention: true);
                return $"have {props.item}";
        }
    }

    public static string ExecuteFromScript(string script, IReadOnlyList<PromptSegment> segments)
    {
        var props = ResolveFromSegments(segments);
        return Execute(props);
    }

    static InventoryLemmaOp ParseOp(string s)
    {
        if (string.IsNullOrEmpty(s)) return InventoryLemmaOp.Have;
        return s.ToLowerInvariant() switch
        {
            "give" => InventoryLemmaOp.Give,
            "take" => InventoryLemmaOp.Take,
            "transfer" => InventoryLemmaOp.Transfer,
            "assert" => InventoryLemmaOp.Assert,
            "have" or "possess" => InventoryLemmaOp.Have,
            _ => InventoryLemmaOp.Have
        };
    }

    static bool TryGet(Dictionary<string, string> p, string key, out string value)
    {
        value = null;
        if (p == null || string.IsNullOrEmpty(key)) return false;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }
}

/// <summary>Resolves {P:waypoint|x=…|y=…|z=…} and named waypoints into a WaypointRoute.</summary>
public static class WaypointLemmaResolver
{
    static readonly Regex Vec3 = new Regex(
        @"\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)",
        RegexOptions.Compiled);

    public static string Execute(WaypointRoute route, IReadOnlyList<PromptSegment> segments, FormationCatalog catalog = null)
    {
        if (route == null) route = new WaypointRoute();
        if (segments == null) return "no segments";
        string pendingFrom = null;
        string pendingFormation = route.defaultFormationId;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            bool isWp = string.Equals(seg.placeholderName, WaypointLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase);
            bool isForm = string.Equals(seg.placeholderName, FormationLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase);
            if (!isWp && !isForm) continue;

            var p = seg.placeholderParams ?? new Dictionary<string, string>();
            if (TryGet(p, FormationLemmaPropertyKeys.Id, out var fid) || TryGet(p, WaypointLemmaPropertyKeys.Formation, out fid))
            {
                pendingFormation = catalog != null ? catalog.NormalizeId(fid) : fid;
            }

            if (TryGet(p, WaypointLemmaPropertyKeys.From, out var fromName))
                pendingFrom = fromName;
            if (TryGet(p, WaypointLemmaPropertyKeys.To, out var toName))
            {
                EnsureNamed(route, pendingFrom, pendingFormation);
                EnsureNamed(route, toName, pendingFormation);
                // Set formation on the "to" marker (leg arrival)
                var m = FindNamed(route, toName);
                if (m != null) m.formationId = pendingFormation;
                pendingFrom = toName;
            }

            string name = null;
            TryGet(p, WaypointLemmaPropertyKeys.Name, out name);
            Vector3 pos = Vector3.zero;
            bool hasPos = false;
            if (TryGet(p, WaypointLemmaPropertyKeys.X, out var xs) &&
                TryGet(p, WaypointLemmaPropertyKeys.Y, out var ys) &&
                TryGet(p, WaypointLemmaPropertyKeys.Z, out var zs) &&
                float.TryParse(xs, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(ys, NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(zs, NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                pos = new Vector3(x, y, z);
                hasPos = true;
            }
            else if (TryGet(p, WaypointLemmaPropertyKeys.Vec, out var v) && TryParseVec3(v, out pos))
                hasPos = true;

            if (!string.IsNullOrEmpty(name))
            {
                var existing = FindNamed(route, name);
                if (existing == null)
                {
                    var m = route.Add(hasPos ? pos : Vector3.zero, name, pendingFormation);
                    if (!hasPos) m.worldPosition = Vector3.zero;
                }
                else if (hasPos)
                {
                    existing.worldPosition = pos;
                    existing.formationId = pendingFormation;
                }
            }
        }
        return $"waypoints={route.Count}";
    }

    static void EnsureNamed(WaypointRoute route, string name, string formation)
    {
        if (string.IsNullOrEmpty(name) || FindNamed(route, name) != null) return;
        route.Add(Vector3.zero, name, formation);
    }

    static WaypointMarker FindNamed(WaypointRoute route, string name)
    {
        if (route?.markers == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < route.markers.Count; i++)
            if (route.markers[i] != null && string.Equals(route.markers[i].name, name, StringComparison.OrdinalIgnoreCase))
                return route.markers[i];
        return null;
    }

    public static bool TryParseVec3(string s, out Vector3 v)
    {
        v = Vector3.zero;
        if (string.IsNullOrEmpty(s)) return false;
        var m = Vec3.Match(s);
        if (!m.Success) return false;
        return float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v.x) &&
               float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v.y) &&
               float.TryParse(m.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v.z);
    }

    static bool TryGet(Dictionary<string, string> p, string key, out string value)
    {
        value = null;
        if (p == null) return false;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }
}

/// <summary>Sets active formation id on route / guidance from {P:formation|id=…}.</summary>
public static class FormationLemmaResolver
{
    public static string Execute(WaypointRoute route, FormationCatalog catalog, IReadOnlyList<PromptSegment> segments)
    {
        if (segments == null) return "no segments";
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg == null || !seg.isPlaceholder) continue;
            if (!string.Equals(seg.placeholderName, FormationLemmaPropertyKeys.PlaceholderName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (seg.placeholderParams != null)
            {
                string id = null;
                if (seg.placeholderParams.TryGetValue(FormationLemmaPropertyKeys.Id, out id) ||
                    TryGetIgnoreCase(seg.placeholderParams, FormationLemmaPropertyKeys.Id, out id))
                {
                    string normalized = catalog != null ? catalog.NormalizeId(id) : id;
                    if (route != null)
                    {
                        route.defaultFormationId = normalized;
                        if (route.Active != null) route.Active.formationId = normalized;
                    }
                    return $"formation={normalized}";
                }
            }
        }
        return "ok";
    }

    static bool TryGetIgnoreCase(Dictionary<string, string> p, string key, out string value)
    {
        value = null;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        return false;
    }
}
