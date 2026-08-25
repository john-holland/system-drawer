using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Thin SpatialGenerator veneer — no BedogaGenerator asmdef reference.</summary>
public static class RoadDebrisSizeSolver
{
    public struct Placement
    {
        public GameObject prefab;
        public Vector3 center;
        public Vector3 size;
        public bool found;
    }

    public static List<Placement> Solve(IList<RoadDebrisDef> defs, Bounds parentBounds, MonoBehaviour spatialGenerator = null)
    {
        var result = new List<Placement>();
        if (defs == null) return result;
        for (int i = 0; i < defs.Count; i++)
        {
            var def = defs[i];
            if (def == null) continue;
            var p = new Placement
            {
                prefab = def.prefab,
                size = def.optimalSpace,
                center = parentBounds.center,
                found = true
            };
            if (spatialGenerator != null && TryFindSpace(spatialGenerator, def, parentBounds, out Bounds slot))
            {
                p.center = slot.center;
                p.size = slot.size;
                p.found = true;
            }
            else
            {
                p.center = parentBounds.center + Vector3.right * (i * def.optimalSpace.x);
            }
            result.Add(p);
        }
        return result;
    }

    static bool TryFindSpace(MonoBehaviour gen, RoadDebrisDef def, Bounds parent, out Bounds slot)
    {
        slot = parent;
        var type = gen.GetType();
        var method = type.GetMethod("FindAvailableSpaceForNode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null) return false;
        try
        {
            object node = Activator.CreateInstance(method.GetParameters()[0].ParameterType);
            var result = method.Invoke(gen, new[] { node, null, null });
            if (result is Bounds b)
            {
                slot = b;
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    public static List<RoadDebrisDef> FromScanHits(IList<Collider> hits)
    {
        var list = new List<RoadDebrisDef>();
        if (hits == null) return list;
        for (int i = 0; i < hits.Count; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            var b = c.bounds;
            list.Add(new RoadDebrisDef
            {
                prefab = c.gameObject,
                minSpace = b.size * 0.5f,
                optimalSpace = b.size,
                maxSpace = b.size * 2f
            });
        }
        return list;
    }

    public static Collider[] ScanRibbon(Vector3 origin, Vector3 size, Quaternion rot, LayerMask mask)
    {
        return Physics.OverlapBox(origin, size * 0.5f, rot, mask, QueryTriggerInteraction.Ignore);
    }
}
