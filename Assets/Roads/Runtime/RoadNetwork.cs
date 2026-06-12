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
    }
}
