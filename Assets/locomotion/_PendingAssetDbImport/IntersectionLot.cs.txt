using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IntersectionLotLeg
{
    public string roadSegmentId;
    public string approachId = "main";
    public float headingYaw;
    public RoadLaneLayout laneLayout;
    public RoadLotOutlet outlet;
}

/// <summary>Discrete intersection pad with 2–4 legs. Composes a RoadLot; does not subclass it.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RoadLot))]
[AddComponentMenu("Locomotion/Civil/Roads/Intersection Lot")]
public sealed class IntersectionLot : MonoBehaviour
{
    public string lotId;
    public RoadLot pad;
    public TAIntersectionCard intersectionCard;
    public TrafficLightController lights;
    public List<IntersectionLotLeg> legs = new List<IntersectionLotLeg>();
    public List<StreetWireEnd> wireEnds = new List<StreetWireEnd>();

    static readonly List<IntersectionLot> Registry = new List<IntersectionLot>();
    public static IReadOnlyList<IntersectionLot> All => Registry;

    void Awake()
    {
        if (pad == null)
            pad = GetComponent<RoadLot>() ?? gameObject.AddComponent<RoadLot>();
        pad.lotKind = RoadLotKind.Intersection;
        if (string.IsNullOrEmpty(lotId))
            lotId = string.IsNullOrEmpty(pad.lotId) ? gameObject.name : pad.lotId;
        pad.lotId = lotId;
        if (!Registry.Contains(this))
            Registry.Add(this);
        if (intersectionCard == null)
            intersectionCard = TAIntersectionCard.Generate(transform.position);
        intersectionCard.BindLot(this);
    }

    void OnDestroy() => Registry.Remove(this);

    public bool ContainsWaypoint(Vector3 world) => pad != null && pad.ContainsXZ(world);

    public bool TrySnapDriveOutlet(string roadSegmentId, Vector3 from, out Vector3 world)
    {
        world = pad != null ? pad.ArrivalWorld : transform.position;
        IntersectionLotLeg match = null;
        for (int i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            if (!string.IsNullOrEmpty(roadSegmentId) && leg.roadSegmentId == roadSegmentId)
            {
                match = leg;
                break;
            }
        }
        if (match == null && legs.Count > 0)
            match = NearestLeg(from);
        if (match?.outlet == null) return pad != null;
        world = OutletWorld(match);
        if (pad != null)
            world.y = pad.SampleHeight(world);
        return true;
    }

    public Vector3 OutletWorld(IntersectionLotLeg leg)
    {
        if (leg?.outlet == null)
            return pad != null ? pad.ArrivalWorld : transform.position;
        Vector3 padPos = pad != null ? pad.ArrivalWorld : transform.position;
        Vector3 dir = Quaternion.Euler(0f, leg.headingYaw, 0f) * Vector3.forward;
        float along = leg.outlet.distanceAlongMeters > 0f ? leg.outlet.distanceAlongMeters : 8f;
        return padPos + dir * along + Vector3.right * (leg.outlet.lateralSide * leg.outlet.curbWidth);
    }

    IntersectionLotLeg NearestLeg(Vector3 from)
    {
        IntersectionLotLeg best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            if (leg == null) continue;
            float sq = (OutletWorld(leg) - from).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = leg;
            }
        }
        return best;
    }

    public static IntersectionLot FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].lotId == id)
                return Registry[i];
        return null;
    }

    public static IntersectionLot FindNearest(Vector3 world, float maxDist = 40f)
    {
        IntersectionLot best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null) continue;
            Vector3 p = lot.pad != null ? lot.pad.ArrivalWorld : lot.transform.position;
            float sq = (p - world).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = lot;
            }
        }
        return best;
    }

    public void EnsureFourLegs(string[] segmentIds)
    {
        legs.Clear();
        float[] yaws = { 0f, 90f, 180f, 270f };
        int n = segmentIds != null ? Mathf.Min(4, segmentIds.Length) : 4;
        for (int i = 0; i < n; i++)
        {
            string id = segmentIds != null && i < segmentIds.Length ? segmentIds[i] : "leg_" + i;
            legs.Add(new IntersectionLotLeg
            {
                roadSegmentId = id,
                approachId = i % 2 == 0 ? "main" : "side",
                headingYaw = yaws[i],
                outlet = new RoadLotOutlet { roadSegmentId = id, curbWidth = 3f, lateralSide = 1f, distanceAlongMeters = 8f }
            });
        }
    }
}
