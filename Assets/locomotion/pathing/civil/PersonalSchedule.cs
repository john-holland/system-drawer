using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PersonalScheduleSlot
{
    public string slotId;
    public CivilianDutyKind duty = CivilianDutyKind.Leisure;
    public string venueStableId;
    public GameObject venueTarget;
    [CronExpr] public string hoursCron = "* 9-17 * * 1-5";
    public int priority = 50;
}

/// <summary>Home ↔ work ↔ venue schedule driven by building biorhythm open windows + duty cron.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Personal Schedule")]
public sealed class PersonalSchedule : MonoBehaviour
{
    public string personaKey = "civilian";
    public List<PersonalScheduleSlot> slots = new List<PersonalScheduleSlot>();
    public BuildingBioRhythmService homeBio;
    public BuildingBioRhythmService workBio;
    public CivilianDutyKind CurrentDuty { get; private set; } = CivilianDutyKind.Leisure;
    public GameObject CurrentVenueTarget { get; private set; }
    public string CurrentVenueStableId { get; private set; }

    public void Tick(DateTime utcNow)
    {
        PersonalScheduleSlot best = null;
        int bestPri = int.MaxValue;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null) continue;
            if (!CronDue.IsActiveSchedule(s.hoursCron, utcNow)) continue;
            if (s.priority < bestPri)
            {
                bestPri = s.priority;
                best = s;
            }
        }

        if (best == null)
        {
            // Pull toward open work/home bio
            if (workBio != null && workBio.isOpen && workBio.health != null && workBio.health.occupancyLoad01 < 0.9f)
            {
                CurrentDuty = CivilianDutyKind.WorkShift;
                CurrentVenueTarget = workBio.gameObject;
                CurrentVenueStableId = workBio.gameObject.name;
                return;
            }
            if (homeBio != null)
            {
                CurrentDuty = CivilianDutyKind.RestAtHome;
                CurrentVenueTarget = homeBio.gameObject;
                CurrentVenueStableId = homeBio.gameObject.name;
                return;
            }
            CurrentDuty = CivilianDutyKind.Leisure;
            CurrentVenueTarget = null;
            CurrentVenueStableId = null;
            return;
        }

        CurrentDuty = best.duty;
        CurrentVenueTarget = best.venueTarget;
        CurrentVenueStableId = best.venueStableId;
        // Closed / damaged venues push leisure
        var ragdoll = CurrentVenueTarget != null ? CurrentVenueTarget.GetComponent<BuildingRagdoll>() : null;
        if (ragdoll?.Health != null && ragdoll.Health.integrity01 < 0.25f)
            CurrentDuty = CivilianDutyKind.Leisure;
    }

    public void ForceFlee()
    {
        CurrentDuty = CivilianDutyKind.FleeThreat;
    }
}
