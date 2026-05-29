using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies constant formation world offset to walk/drive/fly segment waypoints (MVP).</summary>
public static class TravelFormationPathOffset
{
    public static bool ShouldApply(TravelAgent self)
    {
        if (self == null || self.multibody == null)
            return false;
        return self.multibody.formation != null && self.multibody.formation.HasSlots
               && !string.IsNullOrEmpty(self.multibodyFormationGroupId);
    }

    /// <summary>Mutates <paramref name="plan"/> waypoints in-place (plan expected to be a clone).</summary>
    public static void ApplyToPlan(TravelAgent self, GenericMultiModalPathPlan plan, Vector3 actorWorld)
    {
        if (!ShouldApply(self) || plan == null || plan.segments == null)
            return;

        Vector3 fwd = TravelMultibodyPathAdjuster.ComputeTravelForwardXZ(plan, actorWorld);
        Vector3 delta = TravelFormationAssignment.ComputeWorldOffsetFromFormation(
            self,
            self.multibody.formation,
            self.multibody,
            fwd);

        if (delta.sqrMagnitude < 1e-10f)
            return;

        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg == null || seg.waypoints == null)
                continue;
            if (!IsAdjustable(seg))
                continue;
            for (int i = 0; i < seg.waypoints.Count; i++)
                seg.waypoints[i] += delta;
        }
    }

    static bool IsAdjustable(MultiModalSegment seg)
    {
        if (seg == null)
            return false;
        TravelLegMode m = seg.mode;
        return m == TravelLegMode.Walk || m == TravelLegMode.Drive || m == TravelLegMode.Fly;
    }
}
