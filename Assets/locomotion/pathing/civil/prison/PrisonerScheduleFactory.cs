using System.Collections.Generic;
using UnityEngine;

/// <summary>PersonalSchedule slots derived from PrisonerStatus.</summary>
public static class PrisonerScheduleFactory
{
    public static List<PersonalScheduleSlot> SlotsFor(PrisonerStatus status, string venueStableId, GameObject venue)
    {
        var slots = new List<PersonalScheduleSlot>();
        void Add(string id, CivilianDutyKind duty, string cron, int pri)
        {
            slots.Add(new PersonalScheduleSlot
            {
                slotId = id,
                duty = duty,
                venueStableId = venueStableId,
                venueTarget = venue,
                hoursCron = cron,
                priority = pri
            });
        }

        Add("lockdown", CivilianDutyKind.PrisonCustody, "* 21-6 * * *", 10);
        switch (status)
        {
            case PrisonerStatus.Holding:
            case PrisonerStatus.Arrested:
            case PrisonerStatus.Trial:
                Add("holding", CivilianDutyKind.PrisonCustody, "* 7-20 * * *", 20);
                break;
            case PrisonerStatus.Bail:
                Add("leisure", CivilianDutyKind.Leisure, "* 8-20 * * *", 40);
                break;
            case PrisonerStatus.Outing:
            case PrisonerStatus.Rehab:
                Add("outing", CivilianDutyKind.PrisonRehabOuting, "0 9-15 * * 1-5", 15);
                Add("farm", CivilianDutyKind.PrisonFarm, "* 8-16 * * 1-5", 25);
                break;
            case PrisonerStatus.Parole:
                Add("parole", CivilianDutyKind.PrisonParole, "0 10 * * 3", 12);
                Add("custody", CivilianDutyKind.PrisonCustody, "* 8-20 * * *", 40);
                break;
            default:
                Add("yard", CivilianDutyKind.PrisonYard, "0 10-11 * * 1-5", 20);
                Add("cafeteria", CivilianDutyKind.PrisonCafeteria, "0 7,12,18 * * *", 18);
                Add("clinic", CivilianDutyKind.PrisonClinic, "0 14 * * 2", 22);
                Add("library", CivilianDutyKind.PrisonLibrary, "0 15-16 * * 1-5", 30);
                Add("weights", CivilianDutyKind.PrisonWeights, "0 16-17 * * 1-5", 32);
                Add("farm", CivilianDutyKind.PrisonFarm, "0 8-10 * * 1-5", 28);
                Add("nursery", CivilianDutyKind.PrisonNursery, "0 9-11 * * 6", 35);
                break;
        }
        return slots;
    }

    public static void Apply(PersonalSchedule schedule, PrisonerStatus status, string venueStableId, GameObject venue)
    {
        if (schedule == null) return;
        schedule.slots = SlotsFor(status, venueStableId, venue);
    }
}
