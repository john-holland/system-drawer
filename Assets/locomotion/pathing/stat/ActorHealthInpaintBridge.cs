using System;
using UnityEngine;

public enum HealthInpaintKind
{
    Hives = 0,
    Rash = 1,
    Infection = 2,
    Wound = 3,
    AdhesiveResidue = 4,
    Custom = 5
}

[Serializable]
public sealed class HealthInpaintRequest
{
    public GameObject actor;
    public HealthInpaintKind kind;
    public string partId;
    public string allergenOrBondKey;
    public string promptHint;
    public float intensity01 = 0.5f;
    public bool partSpecific;
}

[AddComponentMenu("Locomotion/Stat/Actor Health Inpaint Settings")]
public sealed class ActorHealthInpaintSettings : MonoBehaviour
{
    public bool disablePartSpecificHealthInpainting;
    public string defaultPartId;
}

/// <summary>Requests part- or whole-body health visuals from cellular/allergy/glue outcomes. Does not apply life illness.</summary>
public static class ActorHealthInpaintBridge
{
    public static HealthInpaintRequest LastRequest { get; private set; }

    public static HealthInpaintRequest Request(HealthInpaintRequest req)
    {
        if (req == null) return null;
        bool disablePart = false;
        string defaultPart = null;
        if (req.actor != null)
        {
            var settings = req.actor.GetComponent<ActorHealthInpaintSettings>()
                           ?? req.actor.GetComponentInParent<ActorHealthInpaintSettings>();
            if (settings != null)
            {
                disablePart = settings.disablePartSpecificHealthInpainting;
                defaultPart = settings.defaultPartId;
            }
        }

        string part = req.partId;
        if (string.IsNullOrEmpty(part) && !string.IsNullOrEmpty(defaultPart))
            part = defaultPart;
        if (string.IsNullOrEmpty(part) && req.actor != null)
            part = ResolvePartId(req.actor, req.kind);

        bool partSpecific = !disablePart && !string.IsNullOrEmpty(part);
        if (!partSpecific)
            part = null;

        req.partId = part;
        req.partSpecific = partSpecific;
        LastRequest = req;

        if (req.actor == null) return req;

        if (req.kind == HealthInpaintKind.Wound || req.kind == HealthInpaintKind.Infection)
        {
            var wound = req.actor.GetComponent<WoundSiteRuntime>()
                        ?? req.actor.GetComponentInChildren<WoundSiteRuntime>()
                        ?? req.actor.AddComponent<WoundSiteRuntime>();
            var evt = new CombatDamageEvent
            {
                type = CombatDamageType.Pierce,
                limbId = partSpecific ? part : "Chest",
                amount01 = Mathf.Clamp01(req.intensity01),
                worldHit = req.actor.transform.position,
                direction = Vector3.forward
            };
            wound.OpenFromDamage(evt, Mathf.Clamp01(req.intensity01), autoSuture: false);
        }

        var paint = req.actor.GetComponent<SpatialDescriptionComponent>()
                    ?? req.actor.GetComponentInChildren<SpatialDescriptionComponent>();
        if (paint != null)
        {
            string hint = string.IsNullOrEmpty(req.promptHint)
                ? $"{req.kind} {req.allergenOrBondKey}".Trim()
                : req.promptHint;
            paint.PaintFromModifiers(new[] { hint });
        }

        return req;
    }

    public static HealthInpaintRequest RequestFromTrade(
        GameObject actor, TradeEdge edge, string partId = null, float intensity01 = 0.5f)
    {
        if (edge == null) return null;
        HealthInpaintKind kind = HealthInpaintKind.Custom;
        switch (edge.relationKind)
        {
            case TradeRelationKind.AllergySensitize:
            case TradeRelationKind.AllergyAvoid:
                kind = HealthInpaintKind.Hives;
                break;
            case TradeRelationKind.ChemicalBond:
                kind = HealthInpaintKind.AdhesiveResidue;
                break;
            case TradeRelationKind.ChemicalRelease:
                return null;
        }
        return Request(new HealthInpaintRequest
        {
            actor = actor,
            kind = kind,
            partId = partId,
            allergenOrBondKey = edge.allergenOrBondKey ?? edge.commodityKey,
            promptHint = $"{kind} from {edge.allergenOrBondKey ?? edge.commodityKey}",
            intensity01 = intensity01
        });
    }

    public static HealthInpaintRequest RequestFromRule(
        GameObject actor, string ruleId, string partId = null, string allergenKey = null, float intensity01 = 0.5f)
    {
        HealthInpaintKind kind = HealthInpaintKind.Custom;
        if (string.Equals(ruleId, "allergy_crosslink", StringComparison.OrdinalIgnoreCase))
            kind = HealthInpaintKind.Hives;
        else if (string.Equals(ruleId, "glue_cure", StringComparison.OrdinalIgnoreCase))
            kind = HealthInpaintKind.AdhesiveResidue;
        else if (string.Equals(ruleId, "tongue_freeze_bond", StringComparison.OrdinalIgnoreCase))
        {
            kind = HealthInpaintKind.AdhesiveResidue;
            if (string.IsNullOrEmpty(partId)) partId = "Tongue";
        }
        else if (ruleId != null && ruleId.IndexOf("infect", StringComparison.OrdinalIgnoreCase) >= 0)
            kind = HealthInpaintKind.Infection;

        return Request(new HealthInpaintRequest
        {
            actor = actor,
            kind = kind,
            partId = partId,
            allergenOrBondKey = allergenKey,
            promptHint = $"{kind} ({ruleId})",
            intensity01 = intensity01
        });
    }

    static string ResolvePartId(GameObject actor, HealthInpaintKind kind)
    {
        if (kind == HealthInpaintKind.AdhesiveResidue)
        {
            var tongue = actor.GetComponentInChildren<TongueRuntime>();
            if (tongue != null) return "Tongue";
        }
        var wound = actor.GetComponentInChildren<WoundSiteRuntime>();
        if (wound != null && wound.wounds != null && wound.wounds.Count > 0 &&
            wound.wounds[0]?.spec != null)
            return wound.wounds[0].spec.limbId;
        return null;
    }
}
