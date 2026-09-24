using System.Collections.Generic;
using UnityEngine;

/// <summary>Kitchen / cooking physics card: duty checklist + activity solvers.</summary>
[System.Serializable]
public class ChefCard : GoodSection
{
    [Header("Chef")]
    public ChefDutyMode dutyMode = ChefDutyMode.Line;
    public ChefActivity activity = ChefActivity.Place;
    public GameObject stationOrTarget;
    public GameObject ingredientOrTool;
    public List<string> dutyChecklist = new List<string>();
    public List<ChefMaterialEvolutionCard> evolutionCards = new List<ChefMaterialEvolutionCard>();
    public string orderTicketId;
    public float pourRateLitersPerSec = 0.5f;
    public float accuracy01 = 0.85f;
    public bool requireCleanHands;

    public ChefCard()
    {
        isChefGoal = true;
        physicalPathingTag = "chef";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "kitchen";
    }

    public bool MeetsChefRequirements(GameObject actor, GameObject target = null, RagdollSystem actorRagdoll = null)
    {
        GameObject t = target != null ? target : stationOrTarget;
        if (activity == ChefActivity.Idle)
            return actor != null;
        if (t == null && activity != ChefActivity.WashHands && activity != ChefActivity.Plating)
            return false;
        if (requireCleanHands && actor != null)
        {
            var life = actor.GetComponent<LifeSystemsSheet>();
            if (life != null && life.Get01(LifeSystemsChannelCatalog.Ablution) < 0.35f)
                return false;
        }
        return true;
    }

    public static ChefCard Generate(
        ChefDutyMode mode,
        ChefActivity activity,
        GameObject stationOrTarget,
        RagdollState state = null)
    {
        var card = new ChefCard
        {
            dutyMode = mode,
            activity = activity,
            stationOrTarget = stationOrTarget,
            sectionName = $"chef_{mode}_{activity}",
            description = $"{mode} {activity}",
            isChefGoal = true,
            physicalPathingTag = $"chef_{activity.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 120f, maxTorque = 40f, maxVelocityChange = 2f },
            dutyChecklist = DefaultChecklist(activity)
        };
        return card;
    }

    public static List<string> DefaultChecklist(ChefActivity activity)
    {
        switch (activity)
        {
            case ChefActivity.Sear:
                return new List<string> { "heat_on", "oil_present", "flip_once", "rest" };
            case ChefActivity.Filet:
                return new List<string> { "stabilize", "cut_along_bone", "portion" };
            case ChefActivity.Pour:
            case ChefActivity.Sprinkle:
                return new List<string> { "aim", "meter_flow", "stop" };
            case ChefActivity.WashHands:
                return new List<string> { "wet", "soap", "lather", "rinse", "dry" };
            case ChefActivity.SeasonPan:
                return new List<string> { "scrape", "clean", "season", "park" };
            case ChefActivity.WashDish:
                return new List<string> { "pick", "scrub_or_rinse", "place", "tool_return" };
            default:
                return new List<string> { activity.ToString().ToLowerInvariant() };
        }
    }

    public string DutySummary()
    {
        if (dutyChecklist == null || dutyChecklist.Count == 0)
            return activity.ToString();
        return $"{activity}: {string.Join(",", dutyChecklist)}";
    }
}
