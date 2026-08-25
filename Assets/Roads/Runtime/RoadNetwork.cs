using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    /// <summary>Registry of road spline segments for travel and pathing.</summary>
    public class RoadNetwork : MonoBehaviour
    {
        public List<RoadSpline3D> segments = new List<RoadSpline3D>();

        static RoadNetwork _instance;
        public static RoadNetwork Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<RoadNetwork>();
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance == null)
                _instance = this;
            RefreshSegments();
        }

        public void RefreshSegments()
        {
            segments.Clear();
            segments.AddRange(FindObjectsByType<RoadSpline3D>(FindObjectsSortMode.None));
        }

        public bool TryFindNearestSegment(Vector3 worldPoint, out RoadSpline3D segment, out float distanceAlong, out float lateralOffset)
        {
            segment = null;
            distanceAlong = 0f;
            lateralOffset = 0f;
            float best = float.MaxValue;
            foreach (var s in segments)
            {
                if (s == null)
                    continue;
                s.ProjectPointOntoSpline(worldPoint, out float d, out float lat);
                var sample = s.GetSampleAtDistance(d);
                float dist = Vector3.Distance(sample.position, worldPoint);
                if (dist < best)
                {
                    best = dist;
                    segment = s;
                    distanceAlong = d;
                    lateralOffset = lat;
                }
            }
            return segment != null;
        }

        public List<Vector3> SnapWaypointsToRoad(IList<Vector3> waypoints, float maxSnapDistance = 8f)
        {
            var result = new List<Vector3>();
            if (waypoints == null)
                return result;
            foreach (var wp in waypoints)
            {
                if (TryFindNearestSegment(wp, out var seg, out float d, out float lat) && Mathf.Abs(lat) < maxSnapDistance)
                    result.Add(seg.GetSampleAtDistance(d).position);
                else
                    result.Add(wp);
            }
            return result;
        }

        /// <summary>Keep distanceAlong and reconstruct with original lateral offset (AlignGridIgnoreLanes).</summary>
        public List<Vector3> SnapWaypointsToRoadKeepingLateral(IList<Vector3> waypoints, float maxSnapDistance = 8f)
        {
            var result = new List<Vector3>();
            if (waypoints == null)
                return result;
            foreach (var wp in waypoints)
            {
                if (TryFindNearestSegment(wp, out var seg, out float d, out float lat) && Mathf.Abs(lat) < maxSnapDistance)
                {
                    var sample = seg.GetSampleAtDistance(d);
                    result.Add(sample.position + sample.binormal * lat);
                }
                else
                    result.Add(wp);
            }
            return result;
        }

        /// <summary>Keep distanceAlong, offset by nearest lane center blended with stayInLanes01. Primitive params — no Locomotion types.</summary>
        public List<Vector3> SnapWaypointsToRoadLaneCenter(
            IList<Vector3> waypoints,
            float maxSnapDistance,
            int laneCount,
            float laneWidthM,
            float stayInLanes01)
        {
            var result = new List<Vector3>();
            if (waypoints == null)
                return result;
            int n = Mathf.Max(1, laneCount);
            float width = Mathf.Max(0.1f, laneWidthM);
            float half = (n - 1) * 0.5f;
            float blend = Mathf.Clamp01(stayInLanes01);
            foreach (var wp in waypoints)
            {
                if (TryFindNearestSegment(wp, out var seg, out float d, out float lat) && Mathf.Abs(lat) < maxSnapDistance)
                {
                    var sample = seg.GetSampleAtDistance(d);
                    int i = Mathf.Clamp(Mathf.RoundToInt(lat / width + half), 0, n - 1);
                    float laneCenter = (i - half) * width;
                    float blended = Mathf.Lerp(lat, laneCenter, blend);
                    result.Add(sample.position + sample.binormal * blended);
                }
                else
                    result.Add(wp);
            }
            return result;
        }
    }
}
