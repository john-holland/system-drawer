using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FamilyMemberEntry
{
    public string personaKey;
    public string role = "resident";
    public int peckingOrder = 50;
    public GameObject actor;
    [Range(-1f, 1f)] public float affinity01;
    [Range(0f, 1f)] public float authority01 = 0.5f;
    public string relationshipLabel;

    public RetinuePeckingEntry ToRetinue() =>
        new RetinuePeckingEntry
        {
            personaKey = personaKey,
            role = role,
            peckingOrder = peckingOrder,
            actor = actor
        };
}

/// <summary>Resident / family pecking with relationship settings (industry retinue pattern).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Family Pecking Order")]
public sealed class FamilyPeckingOrder : MonoBehaviour
{
    public List<FamilyMemberEntry> members = new List<FamilyMemberEntry>();

    public List<RetinuePeckingEntry> AsRetinue()
    {
        var list = new List<RetinuePeckingEntry>();
        for (int i = 0; i < members.Count; i++)
            if (members[i] != null)
                list.Add(members[i].ToRetinue());
        list.Sort((a, b) => a.peckingOrder.CompareTo(b.peckingOrder));
        return list;
    }

    public FamilyMemberEntry HighestAuthority()
    {
        FamilyMemberEntry best = null;
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m == null) continue;
            if (best == null || m.authority01 > best.authority01)
                best = m;
        }
        return best;
    }
}
