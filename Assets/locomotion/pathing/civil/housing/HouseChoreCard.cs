using System.Collections.Generic;
using UnityEngine;

public enum HouseChoreKind
{
    TakeOutTrash = 0,
    Dishes = 1,
    Laundry = 2,
    Clean = 3,
    Yard = 4,
    Rest = 5,
    UtilityMaintain = 6
}

[System.Serializable]
public class HouseChoreCard : CivilCard
{
    public HouseChoreKind chore = HouseChoreKind.Clean;
    public HousingBuildingRagdoll house;

    public HouseChoreCard()
    {
        isCivilGoal = true;
        civicDuty = CivilianDutyKind.WorkShift;
        physicalPathingTag = "house_chore";
    }

    public void Apply()
    {
        house?.houseBio?.ApplyChore(chore);
    }

    public static HouseChoreCard Generate(HouseChoreKind chore, HousingBuildingRagdoll house = null)
    {
        return new HouseChoreCard
        {
            chore = chore,
            house = house,
            civicDuty = CivilianDutyKind.WorkShift,
            sectionName = $"house_{chore}",
            description = chore.ToString(),
            isCivilGoal = true,
            dutyChecklist = new List<string> { chore.ToString().ToLowerInvariant() }
        };
    }
}

public static class HouseChoreCatalog
{
    public static List<HouseChoreCard> DefaultChores(HousingBuildingRagdoll house)
    {
        return new List<HouseChoreCard>
        {
            HouseChoreCard.Generate(HouseChoreKind.TakeOutTrash, house),
            HouseChoreCard.Generate(HouseChoreKind.Dishes, house),
            HouseChoreCard.Generate(HouseChoreKind.Laundry, house),
            HouseChoreCard.Generate(HouseChoreKind.Clean, house),
            HouseChoreCard.Generate(HouseChoreKind.Yard, house),
            HouseChoreCard.Generate(HouseChoreKind.UtilityMaintain, house)
        };
    }
}
