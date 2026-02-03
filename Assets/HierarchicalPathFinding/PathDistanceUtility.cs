using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Position at distance along a path (arc-length). Used for "stand at distance from goal" (e.g. throw stance).
/// </summary>
public static class PathDistanceUtility
{
    /// <summary>
    /// Get total arc-length of path (sum of segment lengths).
    /// </summary>
    public static float GetPathLength(IList<Vector3> path)
    {
        if (path == null || path.Count < 2)
            return 0f;
        float len = 0f;
        for (int i = 1; i < path.Count; i++)
            len += Vector3.Distance(path[i - 1], path[i]);
        return len;
    }

    /// <summary>
    /// Get world position at arc-length distance along path. Clamp: distance ≤ 0 → first point; distance ≥ path length → last point.
    /// </summary>
    /// <param name="path">Path waypoints.</param>
    /// <param name="distance">Arc-length distance.</param>
    /// <param name="fromStart">If true, distance is from path start; if false, from path end.</param>
    /// <param name="segmentIndex">Optional: segment index (0-based) containing the point.</param>
    /// <param name="t">Optional: parameter 0..1 along the segment.</param>
    /// <returns>World position at that distance.</returns>
    public static Vector3 GetPositionAtDistanceAlongPath(
        IList<Vector3> path,
        float distance,
        bool fromStart,
        out int segmentIndex,
        out float t)
    {
        segmentIndex = 0;
        t = 0f;

        if (path == null || path.Count == 0)
            return Vector3.zero;
        if (path.Count == 1)
            return path[0];

        float totalLength = GetPathLength(path);
        if (totalLength <= 0f)
            return path[0];

        if (!fromStart)
            distance = totalLength - distance;

        if (distance <= 0f)
            return path[0];
        if (distance >= totalLength)
        {
            segmentIndex = path.Count - 2;
            t = 1f;
            return path[path.Count - 1];
        }

        float acc = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            float segLen = Vector3.Distance(path[i - 1], path[i]);
            if (acc + segLen >= distance)
            {
                segmentIndex = i - 1;
                t = segLen > 0.001f ? (distance - acc) / segLen : 1f;
                return Vector3.Lerp(path[i - 1], path[i], t);
            }
            acc += segLen;
        }

        segmentIndex = path.Count - 2;
        t = 1f;
        return path[path.Count - 1];
    }

    /// <summary>
    /// Overload without segment index and t.
    /// </summary>
    public static Vector3 GetPositionAtDistanceAlongPath(IList<Vector3> path, float distance, bool fromStart = true)
    {
        return GetPositionAtDistanceAlongPath(path, distance, fromStart, out _, out _);
    }
}
