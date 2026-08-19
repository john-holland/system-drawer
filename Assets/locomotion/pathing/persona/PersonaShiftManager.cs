using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PersonaShiftSlot
{
    public string role;
    public string personaKey;
    [CronExpr] public string openCron = "* 8-18 * * 1-5";
    [CronExpr] public string closeCron = "";
    public int peckingOrder = 20;
    public BuildingRagdoll building;
    [NonSerialized] public bool isOnShift;
}

/// <summary>
/// Requests specific staff personas for a BuildingRagdoll according to open/close cron;
/// auto-works with PersonaDayManager venue wake/sleep.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Persona/Persona Shift Manager")]
public sealed class PersonaShiftManager : MonoBehaviour
{
    public List<PersonaShiftSlot> shifts = new List<PersonaShiftSlot>();
    public BuildingRagdoll defaultBuilding;
    public CompanyRegistration company;

    void Awake()
    {
        if (defaultBuilding == null)
            defaultBuilding = GetComponent<BuildingRagdoll>();
        if (company == null)
            company = GetComponent<CompanyRegistration>();
        if (shifts.Count == 0)
            SeedDefaultAirportShifts();
    }

    public void SeedDefaultAirportShifts()
    {
        shifts.Add(new PersonaShiftSlot { role = "tsa_agent", personaKey = "tsa_agent", openCron = "* 5-23 * * *", closeCron = "", peckingOrder = 15 });
        shifts.Add(new PersonaShiftSlot { role = "gate_agent", personaKey = "gate_agent", openCron = "* 5-23 * * *", closeCron = "", peckingOrder = 18 });
        shifts.Add(new PersonaShiftSlot { role = "ground_crew", personaKey = "ground_crew", openCron = "* 5-23 * * *", closeCron = "", peckingOrder = 25 });
        shifts.Add(new PersonaShiftSlot { role = "pilot", personaKey = "pilot", openCron = "* 6-22 * * *", closeCron = "", peckingOrder = 12 });
    }

    public void Tick(DateTime utcNow, CivilVenueNode venue)
    {
        BuildingRagdoll building = defaultBuilding;
        if (venue?.contextOwner != null)
        {
            var b = venue.contextOwner.GetComponent<BuildingRagdoll>();
            if (b != null) building = b;
        }

        for (int i = 0; i < shifts.Count; i++)
        {
            PersonaShiftSlot slot = shifts[i];
            if (slot == null) continue;
            BuildingRagdoll host = slot.building != null ? slot.building : building;
            bool open = CronDue.IsActiveSchedule(slot.openCron, utcNow);
            bool closed = !string.IsNullOrEmpty(slot.closeCron) && CronDue.IsActiveSchedule(slot.closeCron, utcNow);
            bool shouldWork = open && !closed;

            if (shouldWork && !slot.isOnShift)
            {
                slot.isOnShift = true;
                EnsureStaffEntry(slot);
                WakePersona(venue, slot);
                SendMessage("OnPersonaShiftOpen", slot, SendMessageOptions.DontRequireReceiver);
            }
            else if (!shouldWork && slot.isOnShift)
            {
                slot.isOnShift = false;
                SleepPersona(venue, slot);
                SendMessage("OnPersonaShiftClose", slot, SendMessageOptions.DontRequireReceiver);
            }

            _ = host;
        }
    }

    void EnsureStaffEntry(PersonaShiftSlot slot)
    {
        if (company == null) return;
        for (int i = 0; i < company.staff.Count; i++)
        {
            var s = company.staff[i];
            if (s != null && s.personaKey == slot.personaKey) return;
        }
        company.staff.Add(new RetinuePeckingEntry
        {
            role = slot.role,
            personaKey = slot.personaKey,
            peckingOrder = slot.peckingOrder
        });
    }

    static void WakePersona(CivilVenueNode venue, PersonaShiftSlot slot)
    {
        if (venue?.retinue == null) return;
        for (int i = 0; i < venue.retinue.Count; i++)
        {
            var m = venue.retinue[i];
            if (m == null || m.actor == null) continue;
            if (!string.IsNullOrEmpty(slot.personaKey) &&
                m.actor.name.IndexOf(slot.personaKey, StringComparison.OrdinalIgnoreCase) < 0 &&
                (m.personaKey == null || m.personaKey != slot.personaKey))
                continue;
            if (!m.actor.activeSelf)
                m.actor.SetActive(true);
        }
    }

    static void SleepPersona(CivilVenueNode venue, PersonaShiftSlot slot)
    {
        if (venue?.retinue == null) return;
        for (int i = 0; i < venue.retinue.Count; i++)
        {
            var m = venue.retinue[i];
            if (m?.actor == null) continue;
            if (!string.IsNullOrEmpty(slot.personaKey) &&
                (m.personaKey == null || m.personaKey != slot.personaKey) &&
                m.actor.name.IndexOf(slot.personaKey, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (m.actor.activeSelf)
                m.actor.SetActive(false);
        }
    }

    public static PersonaShiftManager FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        return host.GetComponent<PersonaShiftManager>() ?? host.AddComponent<PersonaShiftManager>();
    }
}
