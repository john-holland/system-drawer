using System.Collections.Generic;
using UnityEngine;

/// <summary>Hanoi dish move card under ChefDutyMode.Dish.</summary>
[System.Serializable]
public class DishwashingCard : GoodSection
{
    public string dishItemId;
    public DishZoneKind fromZone = DishZoneKind.Dirty;
    public DishZoneKind toZone = DishZoneKind.Sink;
    public float scrubSeconds = 2.5f;
    public float rinseLiters = 0.4f;
    public DishToolKind tool = DishToolKind.Sponge;
    public DishFinishPreference finishPreference = DishFinishPreference.Either;
    public DishScrubMode scrubMode = DishScrubMode.TimingAndFlood;
    public List<string> dutyChecklist = new List<string>();

    public DishwashingCard()
    {
        isChefGoal = true;
        physicalPathingTag = "dishwashing";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "kitchen";
    }

    public static DishwashingCard Generate(
        string dishId,
        DishZoneKind from,
        DishZoneKind to,
        DishFinishPreference finish = DishFinishPreference.Either)
    {
        return new DishwashingCard
        {
            dishItemId = dishId,
            fromZone = from,
            toZone = to,
            finishPreference = finish,
            sectionName = $"wash_{from}_to_{to}",
            description = $"{dishId}: {from} → {to}",
            dutyChecklist = new List<string> { "pick", "scrub_or_rinse", "place", "tool_return" }
        };
    }
}
