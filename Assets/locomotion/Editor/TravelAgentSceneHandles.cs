using UnityEditor;
using UnityEngine;

/// Thick polyline / markers for TravelAgent cached plan in Scene view.
public static class TravelAgentSceneHandles
{
    public static void DrawCachedPlan(TravelAgent agent)
    {
        if (agent == null || agent.CachedPlan == null || agent.CachedPlan.IsEmpty)
            return;

        GenericMultiModalPathPlan plan = agent.CachedPlan;
        Handles.color = new Color(0.2f, 0.85f, 1f, 0.95f);

        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg?.waypoints == null || seg.waypoints.Count < 2)
                continue;
            var pts = seg.waypoints;
            for (int i = 1; i < pts.Count; i++)
                Handles.DrawAAPolyLine(6f, pts[i - 1], pts[i]);
        }

        Handles.color = Color.yellow;
        for (int i = 1; i < plan.segments.Count; i++)
        {
            MultiModalSegment prev = plan.segments[i - 1];
            MultiModalSegment cur = plan.segments[i];
            if (prev == null || cur == null || cur.waypoints == null || cur.waypoints.Count == 0)
                continue;
            if (prev.mode != cur.mode)
                Handles.SphereHandleCap(0, cur.waypoints[0], Quaternion.identity, 0.35f, EventType.Repaint);
        }
    }
}
