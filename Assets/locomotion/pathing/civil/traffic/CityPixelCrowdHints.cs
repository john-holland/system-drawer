using UnityEngine;

/// <summary>Writes city/campus stamp crowd hints onto nearby TravelAgents and waypoint guidance.</summary>
public static class CityPixelCrowdHints
{
    public static void ApplyStamp(CityPixelBrushStamp stamp, Vector3 world, float radiusM)
    {
        if (stamp == null || stamp.crowdHint == CityPixelCrowdHint.None)
            return;
        float r2 = Mathf.Max(0.25f, radiusM) * Mathf.Max(0.25f, radiusM);
        var agents = TravelAgentRegistry.All;
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null) continue;
            Vector3 p = a.transform.position;
            if ((p - world).sqrMagnitude > r2) continue;
            ApplyToAgent(a, stamp);
            var guide = a.GetComponent<WaypointGuidanceService>();
            if (guide != null)
                ApplyToGuidance(guide, stamp, world);
        }
    }

    public static void ApplyGridFrame(CityPixelGrid grid, int frameIndex)
    {
        if (grid == null || grid.brushStamps == null) return;
        float radius = Mathf.Max(grid.cellWorldSize, 1f) * 1.25f;
        for (int i = 0; i < grid.brushStamps.Count; i++)
        {
            var s = grid.brushStamps[i];
            if (s == null || s.frameIndex != frameIndex) continue;
            ApplyStamp(s, grid.CellToWorld(s.cellX, s.cellY), radius);
        }
    }

    public static void ApplyToAgent(TravelAgent agent, CityPixelBrushStamp stamp)
    {
        if (agent == null || stamp == null) return;
        agent.crowdHint = stamp.crowdHint;
        if (!string.IsNullOrEmpty(stamp.flockGroupId))
            agent.flockGroupId = stamp.flockGroupId;
        if (!string.IsNullOrEmpty(stamp.ambulationCacheKey))
            agent.ambulationCacheKey = stamp.ambulationCacheKey;
        agent.ambulationCacheLikelihood01 = stamp.cacheLikelihood01;
        if (stamp.cacheToleranceM > 0f)
            agent.cacheToleranceM = stamp.cacheToleranceM;
        if (stamp.travelHintRow != null)
            agent.travelHintRow = stamp.travelHintRow;
    }

    public static void ApplyToGuidance(WaypointGuidanceService guide, CityPixelBrushStamp stamp, Vector3 world)
    {
        if (guide == null || stamp == null) return;
        if (stamp.travelHintRow != null)
        {
            stamp.travelHintRow.kind = TravelAuthoringRowKind.Hint;
            stamp.travelHintRow.worldPosition = world;
        }
        guide.OnRouteChanged();
    }
}
