using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CareerAdvancementTree", menuName = "Locomotion/Civil/Career Advancement Tree")]
public sealed class CareerAdvancementTree : ScriptableObject
{
    public List<CareerRoleSpec> roles = new List<CareerRoleSpec>();

    public CareerRoleSpec FindRole(string roleId)
    {
        if (string.IsNullOrEmpty(roleId) || roles == null) return null;
        for (int i = 0; i < roles.Count; i++)
        {
            var r = roles[i];
            if (r != null && string.Equals(r.roleId, roleId, StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return null;
    }

    public CareerRoleSpec NextPromotion(string currentRoleId)
    {
        var current = FindRole(currentRoleId);
        if (current == null || roles == null) return null;
        for (int i = 0; i < roles.Count; i++)
        {
            var r = roles[i];
            if (r == null || r == current || r.prerequisiteRoleIds == null) continue;
            for (int p = 0; p < r.prerequisiteRoleIds.Length; p++)
            {
                if (string.Equals(r.prerequisiteRoleIds[p], current.roleId, StringComparison.OrdinalIgnoreCase))
                    return r;
            }
        }
        return null;
    }

    public CareerRoleSpec PreviousDemotion(string currentRoleId)
    {
        var current = FindRole(currentRoleId);
        if (current == null || current.prerequisiteRoleIds == null || current.prerequisiteRoleIds.Length == 0)
            return null;
        return FindRole(current.prerequisiteRoleIds[0]);
    }
}
