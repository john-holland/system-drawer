using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies formation world offsets to walk/drive/fly segment waypoints (tangent-aligned).</summary>
public static class TravelFormationPathOffset
{
    public static bool ShouldApply(TravelAgent self)
    {
        if (self == null || self.multibody == null)
            return false;
        if (self.waypointFeatureCoeffs != null && !self.waypointFeatureCoeffs.AllowMultibody)
            return false;
        return self.multibody.formation != null && self.multibody.formation.HasSlots
               && !string.IsNullOrEmpty(self.multibodyFormationGroupId);
    }

    /// <summary>Mutates <paramref name="plan"/> waypoints in-place (plan expected to be a clone).</summary>
    public static void ApplyToPlan(TravelAgent self, GenericMultiModalPathPlan plan, Vector3 actorWorld)
    {
        if (!ShouldApply(self) || plan == null || plan.segments == null)
            return;

        TravelFormationAsset formation = self.multibody.formation;

        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg == null || seg.waypoints == null || !IsAdjustable(seg))
                continue;
            // Snapshot originals so later tangents are not skewed by earlier mutations.
            var originals = new List<Vector3>(seg.waypoints.Count);
            for (int i = 0; i < seg.waypoints.Count; i++)
                originals.Add(seg.waypoints[i]);

            for (int i = 0; i < seg.waypoints.Count; i++)
            {
                Vector3 tangent = EstimateTangent(originals, i);
                Vector3 delta = TravelFormationAssignment.ComputeWorldOffsetFromFormation(
                    self, formation, self.multibody, tangent);
                if (delta.sqrMagnitude < 1e-10f)
                {
                    int slot = ResolveFormationSlotIndex(self);
                    seg.waypoints[i] = TravelFormationTangentOffset.ComputeAtWaypoint(
                        formation, slot, originals[i], tangent);
                }
                else
                    seg.waypoints[i] = originals[i] + delta;
            }
        }
    }

    static int ResolveFormationSlotIndex(TravelAgent self)
    {
        if (self == null) return 0;
        if (self.formationSlotIndex >= 0) return self.formationSlotIndex;
        var cohort = new List<TravelAgent>(8);
        TravelFormationAssignment.BuildSortedCohort(self.multibodyFormationGroupId, cohort);
        int idx = TravelFormationAssignment.ResolveCohortIndex(self, cohort);
        if (idx < 0) return 0;
        int m = self.multibody?.formation != null ? self.multibody.formation.SlotCount : 1;
        return m > 0 ? idx % m : 0;
    }

    static Vector3 EstimateTangent(List<Vector3> wps, int i)
    {
        if (wps == null || wps.Count == 0) return Vector3.forward;
        if (wps.Count == 1) return Vector3.forward;
        if (i <= 0) return (wps[1] - wps[0]).normalized;
        if (i >= wps.Count - 1) return (wps[wps.Count - 1] - wps[wps.Count - 2]).normalized;
        return (wps[i + 1] - wps[i - 1]).normalized;
    }

    static bool IsAdjustable(MultiModalSegment seg)
    {
        if (seg == null)
            return false;
        TravelLegMode m = seg.mode;
        return m == TravelLegMode.Walk || m == TravelLegMode.Drive || m == TravelLegMode.Fly;
    }
}
