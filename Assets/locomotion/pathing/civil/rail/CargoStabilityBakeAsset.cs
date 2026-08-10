using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Prebaked deck polygon, mass props, and strap routes for cargo stability tests.</summary>
[CreateAssetMenu(fileName = "CargoStabilityBake", menuName = "Locomotion/Civil/Rail/Cargo Stability Bake")]
public sealed class CargoStabilityBakeAsset : ScriptableObject
{
    public string bakeId;
    public Vector3 localCom;
    public float massKg = 1000f;
    public List<Vector2> deckPolygonXZ = new List<Vector2>
    {
        new Vector2(-1f, -2f),
        new Vector2(1f, -2f),
        new Vector2(1f, 2f),
        new Vector2(-1f, 2f)
    };
    public List<string> strapRouteIds = new List<string>();
    public float prebakedTipRisk01;

    public bool ComInsidePolygon(Vector3 worldCom, Transform deckRoot)
    {
        if (deckRoot == null || deckPolygonXZ == null || deckPolygonXZ.Count < 3) return true;
        Vector3 local = deckRoot.InverseTransformPoint(worldCom);
        return PointInPolygon(new Vector2(local.x, local.z), deckPolygonXZ);
    }

    public static bool PointInPolygon(Vector2 p, IList<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            var pi = poly[i];
            var pj = poly[j];
            if (((pi.y > p.y) != (pj.y > p.y))
                && (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + 1e-8f) + pi.x))
                inside = !inside;
        }
        return inside;
    }
}
