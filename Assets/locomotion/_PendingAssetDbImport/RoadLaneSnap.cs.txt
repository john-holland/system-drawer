using System.Collections.Generic;
using UnityEngine;

/// <summary>Snaps drive waypoints onto (s, laneIndex) cells before TravelMultibodyPathAdjuster.</summary>
public static class RoadLaneSnap
{
    public delegate void SampleAt(float distanceAlong, out Vector3 position, out Vector3 binormal);

    public static Vector3 Reconstruct(Vector3 center, Vector3 binormal, float lateral) =>
        center + binormal * lateral;

    public static float SnapS(float distanceAlong, float cellLengthM)
    {
        float cell = Mathf.Max(0.25f, cellLengthM);
        return Mathf.Round(distanceAlong / cell) * cell;
    }

    public static float BlendLateral(float freeLat, float laneCenter, float stayInLanes01) =>
        Mathf.Lerp(freeLat, laneCenter, Mathf.Clamp01(stayInLanes01));

    public static Vector3 ApplyPolicy(
        Vector3 world,
        float distanceAlong,
        float lateralOffset,
        TravelLanePolicy policy,
        float stayInLanes01,
        RoadLaneLayout layout,
        float cellLengthM,
        SampleAt sample)
    {
        if (sample == null) return world;
        float s = distanceAlong;
        if (policy != TravelLanePolicy.IgnoreLaneGrid)
            s = SnapS(distanceAlong, cellLengthM);
        sample(s, out Vector3 pos, out Vector3 bin);
        if (policy == TravelLanePolicy.IgnoreLaneGrid)
            return pos;
        if (policy == TravelLanePolicy.AlignGridIgnoreLanes)
            return Reconstruct(pos, bin, lateralOffset);
        float laneCenter = 0f;
        if (layout != null)
            laneCenter = layout.LaneCenterOffset(layout.LaneFromLateral(lateralOffset));
        float lat = BlendLateral(lateralOffset, laneCenter, stayInLanes01);
        return Reconstruct(pos, bin, lat);
    }

    public static List<Vector3> SnapList(
        IList<Vector3> waypoints,
        IList<float> distanceAlong,
        IList<float> lateralOffset,
        TravelLanePolicy policy,
        float stayInLanes01,
        RoadLaneLayout layout,
        float cellLengthM,
        SampleAt sample)
    {
        var result = new List<Vector3>();
        if (waypoints == null) return result;
        for (int i = 0; i < waypoints.Count; i++)
        {
            float s = distanceAlong != null && i < distanceAlong.Count ? distanceAlong[i] : 0f;
            float lat = lateralOffset != null && i < lateralOffset.Count ? lateralOffset[i] : 0f;
            result.Add(ApplyPolicy(waypoints[i], s, lat, policy, stayInLanes01, layout, cellLengthM, sample));
        }
        return result;
    }
}
