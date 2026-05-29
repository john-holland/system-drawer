using System.Collections.Generic;
using UnityEngine;

/// <summary>Maps cohort index to formation slot + optional wrap rows when N &gt; M.</summary>
public static class TravelFormationAssignment
{
    /// <summary>Collects all agents with the same non-empty <paramref name="groupId"/>, sorted by <see cref="Object.GetInstanceID"/>.</summary>
    public static void BuildSortedCohort(string groupId, List<TravelAgent> into)
    {
        into.Clear();
        if (string.IsNullOrEmpty(groupId))
            return;

        foreach (TravelAgent a in TravelAgentRegistry.All)
        {
            if (a == null)
                continue;
            if (a.multibodyFormationGroupId == groupId)
                into.Add(a);
        }

        into.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
    }

    /// <summary>Index within sorted cohort, or -1 if not found / invalid group.</summary>
    public static int ResolveCohortIndex(TravelAgent self, IReadOnlyList<TravelAgent> cohortSorted)
    {
        if (self == null || cohortSorted == null)
            return -1;
        for (int i = 0; i < cohortSorted.Count; i++)
        {
            if (cohortSorted[i] == self)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// World-space offset (typically applied on XZ) for this agent's slot, including wrap rows.
    /// </summary>
    public static Vector3 ComputeWorldOffsetFromFormation(
        TravelAgent self,
        TravelFormationAsset formation,
        TravelAgentMultibodySettings settings,
        Vector3 travelForwardXZ)
    {
        if (self == null || formation == null || !formation.HasSlots || settings == null)
            return Vector3.zero;

        if (string.IsNullOrEmpty(self.multibodyFormationGroupId))
            return Vector3.zero;

        var cohort = new List<TravelAgent>(8);
        BuildSortedCohort(self.multibodyFormationGroupId, cohort);

        int cohortIndex;
        if (self.formationSlotIndex >= 0)
            cohortIndex = self.formationSlotIndex;
        else
        {
            cohortIndex = ResolveCohortIndex(self, cohort);
            if (cohortIndex < 0)
                return Vector3.zero;
        }

        int m = formation.SlotCount;
        if (m <= 0)
            return Vector3.zero;

        int slotIdx = cohortIndex % m;
        int row = cohortIndex / m;

        Vector3 slotLocal = formation.slots[slotIdx].localOffset;
        Vector3 fwd = travelForwardXZ;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-8f)
            fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        if (right.sqrMagnitude < 1e-8f)
            right = Vector3.right;
        right.Normalize();

        Vector3 world = right * slotLocal.x + Vector3.up * slotLocal.y + fwd * slotLocal.z;

        if (row <= 0)
            return world;

        float rowSpacing = settings.ResolveFormationWrapRowSpacing(formation);
        switch (settings.formationWrapDirection)
        {
            case TravelFormationWrapDirection.Back:
                world += -fwd * (rowSpacing * row);
                break;
            case TravelFormationWrapDirection.Right:
                world += right * (rowSpacing * row);
                break;
            case TravelFormationWrapDirection.Left:
                world += -right * (rowSpacing * row);
                break;
        }

        return world;
    }
}
