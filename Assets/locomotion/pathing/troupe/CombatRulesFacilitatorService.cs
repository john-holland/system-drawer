using System;
using System.Collections.Generic;
using UnityEngine;

public enum CombatTeamId
{
    Neutral,
    Friendly,
    Hostile
}

public enum CombatCommuniqueChannel
{
    Voice,
    Handheld,
    Phone,
    WebtopTerminal
}

[Serializable]
public sealed class TroupeMember
{
    public string actorId;
    public GameObject actor;
    public CombatTeamId team = CombatTeamId.Friendly;
    public TravelGuidanceMode guidanceMode = TravelGuidanceMode.NpcFull;
    public TravelFeatureCoefficients coeffs = new TravelFeatureCoefficients();
    public bool canIssueOrders;
    public bool allowVehicleAccess = true;
    public bool allowRideableActors = true;
}

[Serializable]
public sealed class TroupeParameters
{
    public string troupeId = "default";
    public string displayName = "Troupe";
    public List<TroupeMember> members = new List<TroupeMember>();
    public TravelFeatureCoefficients defaultCoeffs = new TravelFeatureCoefficients();
    public FormationCatalog formationCatalog;
    public string defaultFormationId = "triangle";
    public float callToArmsRangeMeters = 25f;
    public float dialogCommsRangeMeters = 25f;
    public CombatCommuniqueChannel defaultChannel = CombatCommuniqueChannel.Voice;
    public bool allowOrdersFromAnyFriendly;
}

/// <summary>Teams, call-to-arms, order gating, and telecom for troupes.</summary>
[AddComponentMenu("Locomotion/Combat/Rules Facilitator")]
public sealed class CombatRulesFacilitatorService : MonoBehaviour
{
    public List<TroupeParameters> troupes = new List<TroupeParameters>();
    public WaypointGuidanceService guidance;
    public WaypointRoute sharedRoute;
    public bool ignoreCallToArmsRange;

    public bool TryGetTroupe(string troupeId, out TroupeParameters troupe)
    {
        troupe = null;
        if (troupes == null) return false;
        for (int i = 0; i < troupes.Count; i++)
        {
            if (troupes[i] != null &&
                string.Equals(troupes[i].troupeId, troupeId, StringComparison.OrdinalIgnoreCase))
            {
                troupe = troupes[i];
                return true;
            }
        }
        return false;
    }

    public bool CanIssueOrders(GameObject issuer, string troupeId)
    {
        if (!TryGetTroupe(troupeId, out var t) || issuer == null) return false;
        if (t.allowOrdersFromAnyFriendly)
        {
            for (int i = 0; i < t.members.Count; i++)
            {
                var m = t.members[i];
                if (m?.actor == issuer && m.team == CombatTeamId.Friendly)
                    return true;
            }
        }
        for (int i = 0; i < t.members.Count; i++)
        {
            var m = t.members[i];
            if (m?.actor == issuer && m.canIssueOrders)
                return true;
        }
        return false;
    }

    public float EffectiveCommsRange(TroupeParameters t, bool dialogOverrideIgnoreRange)
    {
        if (ignoreCallToArmsRange || dialogOverrideIgnoreRange) return float.MaxValue;
        if (t == null) return 25f;
        return Mathf.Max(0.1f, t.callToArmsRangeMeters);
    }

    /// <summary>Join NPCs in range to the shared route / guidance troupe.</summary>
    public int CallToArms(string troupeId, Vector3 origin, bool dialogOverrideIgnoreRange = false)
    {
        if (!TryGetTroupe(troupeId, out var t) || t == null) return 0;
        float range = EffectiveCommsRange(t, dialogOverrideIgnoreRange);
        float r2 = range * range;
        int joined = 0;
        for (int i = 0; i < t.members.Count; i++)
        {
            var m = t.members[i];
            if (m?.actor == null || m.guidanceMode == TravelGuidanceMode.PlayerGuide) continue;
            if ((m.actor.transform.position - origin).sqrMagnitude > r2) continue;
            joined++;
            if (guidance != null)
            {
                guidance.troupeId = troupeId;
                if (sharedRoute != null) guidance.BindRoute(sharedRoute);
                guidance.DriveAgentsTowardActive();
            }
        }
        return joined;
    }

    public bool InCommsRange(GameObject a, GameObject b, string troupeId, bool dialogOverrideIgnoreRange = false)
    {
        if (a == null || b == null) return false;
        if (!TryGetTroupe(troupeId, out var t)) return true;
        float range = EffectiveCommsRange(t, dialogOverrideIgnoreRange);
        return (a.transform.position - b.transform.position).sqrMagnitude <= range * range;
    }
}
